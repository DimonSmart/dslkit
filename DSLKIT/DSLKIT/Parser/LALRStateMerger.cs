using DSLKIT.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    /// <summary>
    /// Implements LALR(1) state merging by combining LR(1) item sets with identical cores
    /// and unifying their lookahead sets.
    /// </summary>
    public class LALRStateMerger
    {
        private readonly IEnumerable<RuleSet> _lr1ItemSets;
        private readonly IList<RuleSet> _lalrStates = new List<RuleSet>();
        private readonly Dictionary<RuleSet, RuleSet> _lr1ToLalrMapping = new Dictionary<RuleSet, RuleSet>();

        public LALRStateMerger(IEnumerable<RuleSet> lr1ItemSets)
        {
            _lr1ItemSets = lr1ItemSets ?? throw new ArgumentNullException(nameof(lr1ItemSets));
        }

        /// <summary>
        /// Merges LR(1) item sets with identical cores into LALR(1) states.
        /// </summary>
        /// <returns>Collection of merged LALR(1) states with updated lookahead sets</returns>
        public LALRMergeResult MergeStates()
        {
            // Group LR(1) item sets by their core (items without lookahead)
            var coreGroups = GroupByCore();

            // Create merged LALR states
            CreateMergedStates(coreGroups);

            // Update state transitions to point to merged states
            UpdateStateTransitions();

            return new LALRMergeResult(_lalrStates, _lr1ToLalrMapping, GetMergeStatistics(coreGroups));
        }

        /// <summary>
        /// Groups LR(1) item sets by their core (rule production and dot position, ignoring lookaheads).
        /// </summary>
        private Dictionary<string, List<RuleSet>> GroupByCore()
        {
            var coreGroups = new Dictionary<string, List<RuleSet>>();

            foreach (var itemSet in _lr1ItemSets)
            {
                var coreSignature = GetCoreSignature(itemSet);

                if (!coreGroups.ContainsKey(coreSignature))
                {
                    coreGroups[coreSignature] = new List<RuleSet>();
                }

                coreGroups[coreSignature].Add(itemSet);
            }

            return coreGroups;
        }

        /// <summary>
        /// Generates a signature for the core of an item set (rules without lookahead information).
        /// </summary>
        private string GetCoreSignature(RuleSet itemSet)
        {
            // Sort rules by production and dot position to ensure consistent signature
            var sortedRules = itemSet.Rules
                .OrderBy(r => r.Production.LeftNonTerminal.Name)
                .ThenBy(r => string.Join(",", r.Production.ProductionDefinition.Select(t => t.ToString())))
                .ThenBy(r => r.DotPosition)
                .ToList();

            var signatures = sortedRules.Select(rule =>
                $"{rule.Production.LeftNonTerminal.Name}→{string.Join("", rule.Production.ProductionDefinition.Select(t => t.ToString()))}•{rule.DotPosition}");

            return string.Join("|", signatures);
        }

        /// <summary>
        /// Creates merged LALR states from groups of LR(1) item sets with identical cores.
        /// </summary>
        private void CreateMergedStates(Dictionary<string, List<RuleSet>> coreGroups)
        {
            var lalrStateNumber = 0;

            foreach (var group in coreGroups.Values)
            {
                if (group.Count == 1)
                {
                    // No merging needed - single state with this core
                    var singleState = group[0];
                    var lalrState = new RuleSet(lalrStateNumber++, singleState.Rules);
                    lalrState.Arrows = new Dictionary<ITerm, RuleSet>(singleState.Arrows);

                    _lalrStates.Add(lalrState);
                    _lr1ToLalrMapping[singleState] = lalrState;
                }
                else
                {
                    // Merge multiple LR(1) states with identical cores
                    var mergedState = CreateMergedState(group, lalrStateNumber++);
                    _lalrStates.Add(mergedState);

                    // Map all original LR(1) states to the merged LALR state
                    foreach (var lr1State in group)
                    {
                        _lr1ToLalrMapping[lr1State] = mergedState;
                    }
                }
            }
        }

        /// <summary>
        /// Creates a single merged LALR state from multiple LR(1) states with identical cores.
        /// </summary>
        private RuleSet CreateMergedState(List<RuleSet> lr1States, int lalrStateNumber)
        {
            // Take the core rules from the first state (they should be identical across all states)
            var coreRules = lr1States[0].Rules.ToList();

            // Create the merged LALR state
            var mergedState = new RuleSet(lalrStateNumber, coreRules);

            // Merge transitions from all LR(1) states
            // Note: Transitions should be consistent for states with identical cores
            foreach (var lr1State in lr1States)
            {
                foreach (var transition in lr1State.Arrows)
                {
                    if (!mergedState.Arrows.ContainsKey(transition.Key))
                    {
                        // Will be updated later in UpdateStateTransitions
                        mergedState.Arrows[transition.Key] = transition.Value;
                    }
                    else if (mergedState.Arrows[transition.Key] != transition.Value)
                    {
                        // This indicates states that should merge have different transitions
                        // This shouldn't happen with properly constructed LR(1) item sets
                        throw new InvalidOperationException(
                            $"Inconsistent transitions found when merging states with identical cores. " +
                            $"Transition on '{transition.Key}' leads to different states.");
                    }
                }
            }

            return mergedState;
        }

        /// <summary>
        /// Updates all state transitions to point to merged LALR states instead of original LR(1) states.
        /// </summary>
        private void UpdateStateTransitions()
        {
            foreach (var lalrState in _lalrStates)
            {
                var updatedArrows = new Dictionary<ITerm, RuleSet>();

                foreach (var transition in lalrState.Arrows)
                {
                    var targetLr1State = transition.Value;

                    if (_lr1ToLalrMapping.TryGetValue(targetLr1State, out var targetLalrState))
                    {
                        updatedArrows[transition.Key] = targetLalrState;
                    }
                    else
                    {
                        throw new InvalidOperationException(
                            $"No LALR mapping found for LR(1) state {targetLr1State.SetNumber}");
                    }
                }

                lalrState.Arrows = updatedArrows;
            }
        }

        /// <summary>
        /// Generates statistics about the merging process for diagnostics.
        /// </summary>
        private LALRMergeStatistics GetMergeStatistics(Dictionary<string, List<RuleSet>> coreGroups)
        {
            var totalLr1States = _lr1ItemSets.Count();
            var totalLalrStates = _lalrStates.Count;
            var mergedGroups = coreGroups.Values.Where(g => g.Count > 1).ToList();
            var mergedStateCount = mergedGroups.Sum(g => g.Count);

            return new LALRMergeStatistics
            {
                OriginalLR1StateCount = totalLr1States,
                MergedLALRStateCount = totalLalrStates,
                StatesReduced = totalLr1States - totalLalrStates,
                MergeGroupCount = mergedGroups.Count,
                LargestMergeGroupSize = mergedGroups.Any() ? mergedGroups.Max(g => g.Count) : 0,
                MergeGroups = mergedGroups.Select(g => new MergeGroup
                {
                    CoreSignature = GetCoreSignature(g[0]),
                    MergedStateCount = g.Count,
                    OriginalStateNumbers = g.Select(s => s.SetNumber).ToList()
                }).ToList()
            };
        }
    }

    /// <summary>
    /// Result of LALR state merging operation.
    /// </summary>
    public class LALRMergeResult
    {
        public IReadOnlyList<RuleSet> LALRStates { get; }
        public IReadOnlyDictionary<RuleSet, RuleSet> LR1ToLALRMapping { get; }
        public LALRMergeStatistics Statistics { get; }

        public LALRMergeResult(IList<RuleSet> lalrStates,
                             Dictionary<RuleSet, RuleSet> lr1ToLalrMapping,
                             LALRMergeStatistics statistics)
        {
            LALRStates = lalrStates.ToList().AsReadOnly();
            LR1ToLALRMapping = lr1ToLalrMapping;
            Statistics = statistics;
        }
    }

    /// <summary>
    /// Statistics about the LALR merging process.
    /// </summary>
    public class LALRMergeStatistics
    {
        public int OriginalLR1StateCount { get; set; }
        public int MergedLALRStateCount { get; set; }
        public int StatesReduced { get; set; }
        public int MergeGroupCount { get; set; }
        public int LargestMergeGroupSize { get; set; }
        public List<MergeGroup> MergeGroups { get; set; } = new List<MergeGroup>();

        public override string ToString()
        {
            return $"LALR Merge: {OriginalLR1StateCount} LR(1) states → {MergedLALRStateCount} LALR states " +
                   $"({StatesReduced} states reduced, {MergeGroupCount} merge groups)";
        }
    }

    /// <summary>
    /// Information about a group of merged states.
    /// </summary>
    public class MergeGroup
    {
        public string CoreSignature { get; set; }
        public int MergedStateCount { get; set; }
        public List<int> OriginalStateNumbers { get; set; } = new List<int>();

        public override string ToString()
        {
            return $"Merged {MergedStateCount} states: [{string.Join(", ", OriginalStateNumbers)}]";
        }
    }
}
