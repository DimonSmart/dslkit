using System.Collections.Generic;
using System.Linq;
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
            var rule2FollowSet = new Dictionary<ExProduction, IReadOnlyCollection<ITerm>>(_exProductions.Count);
            foreach (var exProduction in _exProductions)
            {
                rule2FollowSet[exProduction] = _follows[exProduction.ExLeftNonTerminal];
            }

            OnReductionStep0?.Invoke(rule2FollowSet);

            _mergedRows = new List<MergedRow>(_exProductions.Count);
            var prods = new List<ExProduction>(_exProductions);
            do
            {
                var exProduction = prods.FirstOrDefault();
                if (exProduction == null)
                {
                    break;
                }

                var isEpsilon = exProduction.ExProductionDefinition[^1].Term is IEmptyTerm;
                var group = prods
                    .Where(p =>
                        p.ExLeftNonTerminal.NonTerminal == exProduction.ExLeftNonTerminal.NonTerminal &&
                        // p.ExLeftNonTerminal.To == exProduction.ExLeftNonTerminal.To &&
                        exProduction.ExProductionDefinition[^1].Term == p.ExProductionDefinition[^1].Term &&
                        (isEpsilon
                            ? exProduction.ExProductionDefinition[^1].From == p.ExProductionDefinition[^1].From
                            : exProduction.ExProductionDefinition[^1].To == p.ExProductionDefinition[^1].To)).ToList();
                // For epsilon productions (X → Empty), the reduce action must fire in the predecessor state
                // (where "• Empty" appears), not the successor state (which the parser never reaches).
                var finalSet = isEpsilon
                    ? group.First().ExProductionDefinition[^1].From
                    : (group.First().ExProductionDefinition[^1].To
                        ?? throw new System.InvalidOperationException("Final set cannot be null when building merged reduction rows."));
                // FOLLOW is a set by definition. When merged rows share the same terminal in FOLLOW,
                // keep one copy to avoid duplicate reduction attempts and noisy false conflict logs.
                var mergedFollowSet = group
                    .SelectMany(p => rule2FollowSet[p])
                    .Distinct()
                    .ToList();

                var mergedRow = new MergedRow
                {
                    FinalSet = finalSet,
                    IsEpsilon = isEpsilon,
                    Production = new Production(group.First().ExLeftNonTerminal.NonTerminal, group.First().Production.ProductionDefinition),
                    FollowSet = mergedFollowSet,
                    PreMergedRules = group
                };

                _mergedRows.Add(mergedRow);

                foreach (var g in group)
                {
                    prods.Remove(g);
                }
            } while (true);

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

                        if (actionAndGotoTable.ActionTable.TryGetValue(key, out var existingAction))
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
