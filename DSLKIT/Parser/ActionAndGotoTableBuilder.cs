using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class ActionAndGotoTableBuilder
    {
        private readonly IReadOnlyList<ExProduction> _exProductions;
        private readonly IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> _follows;
        private readonly INonTerminal _root;
        private readonly IEnumerable<RuleSet> _ruleSets;
        private readonly TranslationTable _translationTable;
        private readonly IReadOnlyDictionary<string, PrecedenceRule> _precedenceByTerminalName;
        private readonly IReadOnlyDictionary<string, Resolve> _shiftReduceResolutions;
        private ActionAndGotoTable? _actionAndGotoTable;
        private List<MergedRow>? _mergedRows;

        public GrammarBuilder.ReductionStep0? OnReductionStep0 { get; }
        public GrammarBuilder.ReductionStep1? OnReductionStep1 { get; }

        internal ActionAndGotoTableBuilder(INonTerminal root,
            IReadOnlyList<ExProduction> exProductions,
            IReadOnlyDictionary<IExNonTerminal, IReadOnlyCollection<ITerm>> follows,
            IEnumerable<RuleSet> ruleSets,
            TranslationTable translationTable,
            IReadOnlyDictionary<string, PrecedenceRule> precedenceByTerminalName,
            IReadOnlyDictionary<string, Resolve> shiftReduceResolutions,
            GrammarBuilder.ReductionStep0? onReductionStep0,
            GrammarBuilder.ReductionStep1? onReductionStep1
        )
        {
            _root = root;
            _exProductions = exProductions;
            _follows = follows;
            _ruleSets = ruleSets;
            _translationTable = translationTable;
            _precedenceByTerminalName = precedenceByTerminalName;
            _shiftReduceResolutions = shiftReduceResolutions;
            OnReductionStep0 = onReductionStep0;
            OnReductionStep1 = onReductionStep1;
        }

        public ActionAndGotoTable Build()
        {
            if (_actionAndGotoTable != null)
            {
                return _actionAndGotoTable;
            }

            _actionAndGotoTable = Initialize();
            BuildGotos();
            BuildShifts();
            ReductionsSubStep1();
            BuildReductions();

            return _actionAndGotoTable;
        }

        private void ReductionsSubStep1()
        {
            if (OnReductionStep0 != null)
            {
                var ruleToFollowSet = new Dictionary<ExProduction, IReadOnlyCollection<ITerm>>(_exProductions.Count);
                foreach (var exProduction in _exProductions)
                {
                    ruleToFollowSet[exProduction] = _follows[exProduction.ExLeftNonTerminal];
                }

                OnReductionStep0(ruleToFollowSet);
            }

            var mergeGroups = new Dictionary<ReductionMergeKey, ReductionMergeAccumulator>(_exProductions.Count);
            var orderedGroups = new List<ReductionMergeAccumulator>(_exProductions.Count);
            foreach (var exProduction in _exProductions)
            {
                var finalTerm = exProduction.ExProductionDefinition[^1];
                var isEpsilon = finalTerm.Term is IEmptyTerm;
                var finalSet = isEpsilon
                    ? finalTerm.From
                    : finalTerm.To ??
                    throw new InvalidOperationException("Final set cannot be null when building merged reduction rows.");

                var key = new ReductionMergeKey(
                    exProduction.ExLeftNonTerminal.NonTerminal,
                    finalTerm.Term,
                    finalSet,
                    isEpsilon);

                if (!mergeGroups.TryGetValue(key, out var group))
                {
                    group = new ReductionMergeAccumulator(exProduction, finalSet, isEpsilon);
                    mergeGroups[key] = group;
                    orderedGroups.Add(group);
                }

                group.PreMergedRules.Add(exProduction);
                foreach (var followTerm in _follows[exProduction.ExLeftNonTerminal])
                {
                    group.FollowSet.Add(followTerm);
                }
            }

            _mergedRows = new List<MergedRow>(orderedGroups.Count);
            foreach (var group in orderedGroups)
            {
                _mergedRows.Add(new MergedRow
                {
                    FinalSet = group.FinalSet,
                    IsEpsilon = group.IsEpsilon,
                    Production = new Production(
                        group.FirstProduction.ExLeftNonTerminal.NonTerminal,
                        group.FirstProduction.Production.ProductionDefinition),
                    FollowSet = group.FollowSet.ToList(),
                    PreMergedRules = group.PreMergedRules
                });
            }

            OnReductionStep1?.Invoke(_mergedRows);
        }

        /// <summary>
        ///     Add a column for the end of input, labeled $.
        ///     Place an "accept" in the $ column whenever the item set contains an item where the pointer is at the end of the
        ///     starting rule
        ///     (in our example "S → N •").
        /// </summary>
        private ActionAndGotoTable Initialize()
        {
            var actionAndGotoTable = new ActionAndGotoTable(_root);
            foreach (var ruleSet in _ruleSets)
            {
                if (ContainStartingRuleWithPointerAtTheEnd(ruleSet))
                {
                    actionAndGotoTable.MutableActionTable[new KeyValuePair<ITerm, RuleSet>(EofTerminal.Instance, ruleSet)] =
                        AcceptAction.Instance;
                }
            }

            return actionAndGotoTable;
        }

        /// <summary>
        ///     Directly copy the Translation Table's non-terminal columns as GOTOs
        /// </summary>
        private void BuildGotos()
        {
            var actionAndGotoTable = _actionAndGotoTable ?? throw new System.InvalidOperationException("Action and goto table is not initialized.");

            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is INonTerminal nonTerminal)
                {
                    actionAndGotoTable.MutableGotoTable
                        [new KeyValuePair<INonTerminal, RuleSet>(nonTerminal, record.Key.Value)] = record.Value;
                }
            }
        }

        /// <summary>
        ///     Copy the terminal columns as shift actions to the number determined from the Translation Table.
        /// </summary>
        private void BuildShifts()
        {
            var actionAndGotoTable = _actionAndGotoTable ?? throw new System.InvalidOperationException("Action and goto table is not initialized.");

            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is ITerminal terminal)
                {
                    actionAndGotoTable.MutableActionTable[new KeyValuePair<ITerm, RuleSet>(terminal, record.Key.Value)] =
                        new ShiftAction(record.Value);
                }
            }
        }

        private void BuildReductions()
        {
            var actionAndGotoTable = _actionAndGotoTable ?? throw new System.InvalidOperationException("Action and goto table is not initialized.");
            var mergedRows = _mergedRows ?? throw new System.InvalidOperationException("Merged rows are not initialized.");

            foreach (var mergedRow in mergedRows)
            {
                // Skip the starting rule (AcceptAction is already set for it in Initialize)
                if (mergedRow.Production.LeftNonTerminal == _root)
                {
                    continue;
                }

                // For epsilon productions (X → Empty), pop 0 items — Empty was never shifted.
                var popLength = mergedRow.IsEpsilon ? 0 : mergedRow.Production.ProductionDefinition.Count;
                var reduceAction = new ReduceAction(mergedRow.Production, popLength);

                foreach (var followTerm in mergedRow.FollowSet)
                {
                    if (followTerm is ITerminal terminal)
                    {
                        var key = new KeyValuePair<ITerm, RuleSet>(terminal, mergedRow.FinalSet);

                        if (actionAndGotoTable.MutableActionTable.TryGetValue(key, out var existingAction))
                        {
                            if (existingAction is ShiftAction)
                            {
                                if (TryResolveShiftReduceConflict(terminal, mergedRow.Production, out var resolution) &&
                                    resolution == Resolve.Reduce)
                                {
                                    actionAndGotoTable.MutableActionTable[key] = reduceAction;
                                }

                                continue;
                            }

                            var conflictType = "reduce/reduce";

                            System.Diagnostics.Debug.WriteLine(
                                $"Conflict detected: {conflictType} conflict for terminal '{terminal.Name}' " +
                                $"in state {mergedRow.FinalSet.SetNumber}. " +
                                $"Existing: {existingAction}, New: {reduceAction}. " +
                                $"Merged from {mergedRow.PreMergedRules.Count} rules.");

                            continue;
                        }

                        actionAndGotoTable.MutableActionTable[key] = reduceAction;
                    }
                }
            }
        }

        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == _root);
        }

        private bool TryResolveShiftReduceConflict(ITerminal lookaheadTerminal, Production reduceProduction, out Resolve resolution)
        {
            if (TryResolveByExplicitRule(lookaheadTerminal, reduceProduction, out resolution))
            {
                return true;
            }

            return TryResolveByPrecedence(lookaheadTerminal, reduceProduction, out resolution);
        }

        private bool TryResolveByExplicitRule(ITerminal lookaheadTerminal, Production reduceProduction, out Resolve resolution)
        {
            var key = BuildShiftReduceRuleKey(reduceProduction.LeftNonTerminal.Name, lookaheadTerminal.Name);
            return _shiftReduceResolutions.TryGetValue(key, out resolution);
        }

        private bool TryResolveByPrecedence(ITerminal lookaheadTerminal, Production reduceProduction, out Resolve resolution)
        {
            if (!TryGetTerminalPrecedence(lookaheadTerminal.Name, out var lookaheadPrecedence))
            {
                resolution = default;
                return false;
            }

            if (!TryGetProductionPrecedence(reduceProduction, out var reducePrecedence))
            {
                resolution = default;
                return false;
            }

            if (lookaheadPrecedence.Level > reducePrecedence.Level)
            {
                resolution = Resolve.Shift;
                return true;
            }

            if (lookaheadPrecedence.Level < reducePrecedence.Level)
            {
                resolution = Resolve.Reduce;
                return true;
            }

            switch (lookaheadPrecedence.Associativity)
            {
                case Assoc.Left:
                    resolution = Resolve.Reduce;
                    return true;
                case Assoc.Right:
                    resolution = Resolve.Shift;
                    return true;
                default:
                    resolution = default;
                    return false;
            }
        }

        private bool TryGetTerminalPrecedence(string terminalName, out PrecedenceRule precedence)
        {
            return _precedenceByTerminalName.TryGetValue(terminalName, out precedence);
        }

        private bool TryGetProductionPrecedence(Production reduceProduction, out PrecedenceRule precedence)
        {
            for (var index = reduceProduction.ProductionDefinition.Count - 1; index >= 0; index--)
            {
                if (reduceProduction.ProductionDefinition[index] is ITerminal terminal &&
                    TryGetTerminalPrecedence(terminal.Name, out precedence))
                {
                    return true;
                }
            }

            precedence = default;
            return false;
        }

        private static string BuildShiftReduceRuleKey(string nonTerminalName, string lookaheadTerminalName)
        {
            return $"{nonTerminalName.Trim()}\u001f{lookaheadTerminalName.Trim()}";
        }

        private readonly struct ReductionMergeKey : IEquatable<ReductionMergeKey>
        {
            private readonly INonTerminal _leftNonTerminal;
            private readonly ITerm _finalTerm;
            private readonly RuleSet _finalSet;
            private readonly bool _isEpsilon;

            public ReductionMergeKey(INonTerminal leftNonTerminal, ITerm finalTerm, RuleSet finalSet, bool isEpsilon)
            {
                _leftNonTerminal = leftNonTerminal;
                _finalTerm = finalTerm;
                _finalSet = finalSet;
                _isEpsilon = isEpsilon;
            }

            public bool Equals(ReductionMergeKey other)
            {
                return ReferenceEquals(_leftNonTerminal, other._leftNonTerminal) &&
                    ReferenceEquals(_finalTerm, other._finalTerm) &&
                    ReferenceEquals(_finalSet, other._finalSet) &&
                    _isEpsilon == other._isEpsilon;
            }

            public override bool Equals(object? obj)
            {
                return obj is ReductionMergeKey other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    var hash = RuntimeHelpers.GetHashCode(_leftNonTerminal);
                    hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(_finalTerm);
                    hash = (hash * 397) ^ RuntimeHelpers.GetHashCode(_finalSet);
                    return (hash * 397) ^ (_isEpsilon ? 1 : 0);
                }
            }
        }

        private sealed class ReductionMergeAccumulator
        {
            public ReductionMergeAccumulator(ExProduction firstProduction, RuleSet finalSet, bool isEpsilon)
            {
                FirstProduction = firstProduction;
                FinalSet = finalSet;
                IsEpsilon = isEpsilon;
            }

            public ExProduction FirstProduction { get; }
            public RuleSet FinalSet { get; }
            public bool IsEpsilon { get; }
            public List<ExProduction> PreMergedRules { get; } = [];
            public HashSet<ITerm> FollowSet { get; } = [];
        }

        public class MergedRow
        {
            public RuleSet FinalSet { get; init; } = null!;
            public bool IsEpsilon { get; init; }
            public IReadOnlyList<ExProduction> PreMergedRules { get; init; } = null!;
            public Production Production { get; init; } = null!;
            public IReadOnlyCollection<ITerm> FollowSet { get; init; } = null!;
        }
    }
}

