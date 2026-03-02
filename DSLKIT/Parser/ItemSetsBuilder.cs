using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class ItemSetsBuilder
    {
        private readonly IReadOnlyList<Production> _productions;
        private readonly Dictionary<INonTerminal, IReadOnlyList<Production>> _productionsByLeft;
        private readonly INonTerminal _root;
        private readonly List<RuleSet> _sets;
        private readonly Dictionary<int, List<RuleSet>> _setsByKernelHash;

        public ItemSetsBuilder(IEnumerable<Production> productions, INonTerminal root)
        {
            _productions = productions as IReadOnlyList<Production> ?? productions.ToList();
            _productionsByLeft = BuildProductionsByLeft(_productions);
            var estimatedSetCapacity = Math.Max(16, _productions.Count * 2);
            _sets = new List<RuleSet>(estimatedSetCapacity);
            _setsByKernelHash = new Dictionary<int, List<RuleSet>>(estimatedSetCapacity);
            _root = root;
        }

        public ICollection<RuleSet> Build()
        {
            Production? startProduction = null;
            foreach (var production in _productions)
            {
                if (production.LeftNonTerminal == _root)
                {
                    startProduction = production;
                    break;
                }
            }

            if (startProduction is null)
            {
                throw new InvalidOperationException($"No start production found for root non-terminal '{_root.Name}'.");
            }

            var startSet = new RuleSet(_sets.Count, new Rule(startProduction));
            _sets.Add(startSet);
            RegisterSet(startSet);

            FillRuleSet(startSet);
            bool changes;
            do
            {
                changes = false;
                changes |= TryFormNewSets();
                foreach (var set in _sets)
                {
                    changes |= FillRuleSet(set);
                }
            } while (changes);

            return _sets;
        }

        private bool TryFormNewSets()
        {
            var anyChanges = false;

            var setCount = _sets.Count;
            for (var setIndex = 0; setIndex < setCount; setIndex++)
            {
                var set = _sets[setIndex];
                var groupedByNextTerm = new Dictionary<ITerm, List<Rule>>(Math.Clamp(set.Rules.Count, 4, 32));

                foreach (var rule in set.Rules)
                {
                    if (rule.IsFinished)
                    {
                        continue;
                    }

                    if (!groupedByNextTerm.TryGetValue(rule.NextTerm, out var group))
                    {
                        group = [];
                        groupedByNextTerm[rule.NextTerm] = group;
                    }

                    group.Add(rule);
                }

                foreach (var group in groupedByNextTerm)
                {
                    var newRules = new List<Rule>(group.Value.Count);
                    foreach (var rule in group.Value)
                    {
                        newRules.Add(rule.MoveDot());
                    }

                    var existsSet = GetSetBySetDefinitionRules(newRules);
                    if (existsSet != null)
                    {
                        set.SetArrow(group.Key, existsSet);
                        continue;
                    }

                    var newRuleSet = new RuleSet(_sets.Count, newRules);
                    _sets.Add(newRuleSet);
                    RegisterSet(newRuleSet);
                    set.SetArrow(group.Key, newRuleSet);

                    anyChanges = true;
                }
            }

            return anyChanges;
        }

        private RuleSet? GetSetBySetDefinitionRules(IReadOnlyCollection<Rule> newRules)
        {
            var kernelHash = ComputeKernelHash(newRules);
            if (!_setsByKernelHash.TryGetValue(kernelHash, out var candidates))
            {
                return null;
            }

            foreach (var candidate in candidates)
            {
                if (candidate.StartsFrom(newRules))
                {
                    return candidate;
                }
            }

            return null;
        }

        private bool FillRuleSet(RuleSet set)
        {
            var anyChanges = false;
            for (var ruleIndex = 0; ruleIndex < set.Rules.Count; ruleIndex++)
            {
                var rule = set.Rules[ruleIndex];
                if (rule.IsFinished || rule.NextTerm is not INonTerminal nextNonTerminal)
                {
                    continue;
                }

                if (!_productionsByLeft.TryGetValue(nextNonTerminal, out var productions))
                {
                    throw new InvalidOperationException(
                        $"No productions found for non-terminal '{nextNonTerminal.Name}'.");
                }

                foreach (var production in productions)
                {
                    if (set.AddRule(new Rule(production)))
                    {
                        anyChanges = true;
                    }
                }
            }

            return anyChanges;
        }

        private static Dictionary<INonTerminal, IReadOnlyList<Production>> BuildProductionsByLeft(
            IReadOnlyList<Production> productions)
        {
            var result = new Dictionary<INonTerminal, IReadOnlyList<Production>>(Math.Max(4, productions.Count));
            foreach (var grouping in productions.GroupBy(production => production.LeftNonTerminal))
            {
                result[grouping.Key] = grouping.ToList();
            }

            return result;
        }

        private void RegisterSet(RuleSet set)
        {
            var kernelHash = ComputeKernelHash(set.KernelRules);
            if (!_setsByKernelHash.TryGetValue(kernelHash, out var bucket))
            {
                bucket = [];
                _setsByKernelHash[kernelHash] = bucket;
            }

            bucket.Add(set);
        }

        private static int ComputeKernelHash(IReadOnlyCollection<Rule> rules)
        {
            var xor = 0;
            var sum = 0;

            foreach (var rule in rules)
            {
                var hash = rule.GetHashCode();
                xor ^= hash;
                sum = unchecked(sum + hash * 397);
            }

            return HashCode.Combine(rules.Count, xor, sum);
        }
    }
}

