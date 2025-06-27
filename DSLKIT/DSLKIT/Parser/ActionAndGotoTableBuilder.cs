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
        private readonly List<ExProduction> _exProductions;
        private readonly IDictionary<IExNonTerminal, IList<ITerm>> _follows;
        private readonly INonTerminal _root;
        private readonly IEnumerable<RuleSet> _ruleSets;
        private readonly TranslationTable _translationTable;
        private ActionAndGotoTable _actionAndGotoTable;

        public GrammarBuilder.ReductionStep0 OnReductionStep0 { get; }
        public GrammarBuilder.ReductionStep1 OnReductionStep1 { get; }

        public ActionAndGotoTableBuilder(INonTerminal root,
            List<ExProduction> exProductions,
            IDictionary<IExNonTerminal, IList<ITerm>> follows,
            IEnumerable<RuleSet> ruleSets,
            TranslationTable translationTable,
            GrammarBuilder.ReductionStep0 onReductionStep0,
            GrammarBuilder.ReductionStep1 onReductionStep1
        )
        {
            _root = root;
            _exProductions = exProductions;
            _follows = follows;
            _ruleSets = ruleSets;
            _translationTable = translationTable;
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
            BuildReductions();
            ReductionsSubStep1();
            return _actionAndGotoTable;
        }

        private void ReductionsSubStep1()
        {
            var rule2FollowSet = new Dictionary<ExProduction, IList<ITerm>>();
            foreach (var exProduction in _exProductions)
            {
                rule2FollowSet[exProduction] = _follows[exProduction.ExLeftNonTerminal];
            }

            OnReductionStep0?.Invoke(rule2FollowSet);

            var mergedRows = new List<MergedRow>();
            var prods = new List<ExProduction>(_exProductions);
            do
            {
                var exProduction = prods.FirstOrDefault();
                if (exProduction == null)
                {
                    break;
                }

                var group = prods
                    .Where(p =>
                        p.ExLeftNonTerminal.NonTerminal == exProduction.ExLeftNonTerminal.NonTerminal &&
                        // p.ExLeftNonTerminal.To == exProduction.ExLeftNonTerminal.To &&
                        exProduction.ExProductionDefinition[^1].Term == p.ExProductionDefinition[^1].Term &&
                        exProduction.ExProductionDefinition[^1].To == p.ExProductionDefinition[^1].To).ToList();

                var mergedRow = new MergedRow
                {
                    FinalSet = group.First().ExProductionDefinition[^1].To,
                    Production =
                        new Production(group.First().ExLeftNonTerminal.NonTerminal,
                            new List<ITerm> { group.First().ExProductionDefinition[^1].Term }),
                    FollowSet = rule2FollowSet[group.First()],
                    PreMergedRules = group
                };

                mergedRows.Add(mergedRow);

                foreach (var g in group)
                {
                    prods.Remove(g);
                }
            } while (true);

            OnReductionStep1?.Invoke(mergedRows);
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
                    actionAndGotoTable.ActionTable[new KeyValuePair<ITerm, RuleSet>(EofTerminal.Instance, ruleSet)] =
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
            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is INonTerminal nonTerminal)
                {
                    _actionAndGotoTable.GotoTable
                        [new KeyValuePair<INonTerminal, RuleSet>(nonTerminal, record.Key.Value)] = record.Value;
                }
            }
        }

        /// <summary>
        ///     Copy the terminal columns as shift actions to the number determined from the Translation Table.
        /// </summary>
        private void BuildShifts()
        {
            foreach (var record in _translationTable.GetAllRecords())
            {
                if (record.Key.Key is ITerminal terminal)
                {
                    _actionAndGotoTable.ActionTable[new KeyValuePair<ITerm, RuleSet>(terminal, record.Key.Value)] =
                        new ShiftAction(record.Value);
                }
            }
        }

        /// <summary>
        ///     For each RuleSet, find rules of the form A → α •. For such rules:
        ///     Get FOLLOW(A) from computed follow sets and add reduce actions to the ActionTable.
        /// </summary>
        private void BuildReductions()
        {
            foreach (var ruleSet in _ruleSets)
            {
                // Find rules where the dot is at the end (A → α •)
                var finishedRules = ruleSet.Rules.Where(rule => rule.IsFinished);
                
                foreach (var rule in finishedRules)
                {
                    // Skip the starting rule (it should have AcceptAction, not ReduceAction)
                    if (rule.Production.LeftNonTerminal == _root)
                    {
                        continue;
                    }

                    // Find the corresponding ExNonTerminal for this rule's left side
                    var exNonTerminal = _exProductions
                        .Where(exProd => exProd.ExLeftNonTerminal.NonTerminal == rule.Production.LeftNonTerminal)
                        .Select(exProd => exProd.ExLeftNonTerminal)
                        .FirstOrDefault();

                    if (exNonTerminal == null)
                    {
                        continue;
                    }

                    // Get FOLLOW set for this non-terminal
                    if (!_follows.TryGetValue(exNonTerminal, out var followSet))
                    {
                        continue;
                    }

                    // Create reduce action
                    var reduceAction = new ReduceAction(rule.Production, rule.Production.ProductionDefinition.Count);

                    // Add reduce action for each terminal in FOLLOW set
                    foreach (var followTerm in followSet)
                    {
                        if (followTerm is ITerminal terminal)
                        {
                            var key = new KeyValuePair<ITerm, RuleSet>(terminal, ruleSet);
                            
                            // Check for conflicts
                            if (_actionAndGotoTable.ActionTable.ContainsKey(key))
                            {
                                var existingAction = _actionAndGotoTable.ActionTable[key];
                                var conflictType = existingAction is ShiftAction ? "shift/reduce" : "reduce/reduce";
                                
                                // Output diagnostic about conflict
                                System.Diagnostics.Debug.WriteLine(
                                    $"Conflict detected: {conflictType} conflict for terminal '{terminal.Name}' " +
                                    $"in state {ruleSet.SetNumber}. " +
                                    $"Existing: {existingAction}, New: {reduceAction}");
                                
                                // For now, keep the existing action (could be configurable)
                                continue;
                            }

                            _actionAndGotoTable.ActionTable[key] = reduceAction;
                        }
                    }
                }
            }
        }

        private bool ContainStartingRuleWithPointerAtTheEnd(RuleSet ruleSet)
        {
            return ruleSet.Rules.Any(rule => rule.IsFinished && rule.Production.LeftNonTerminal == _root);
        }

        public class MergedRow
        {
            public RuleSet FinalSet { get; set; }
            public List<ExProduction> PreMergedRules { get; set; }
            public Production Production { get; set; }
            public IList<ITerm> FollowSet { get; set; }
        }
    }
}