using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class ItemSetsBuilder
    {
        private readonly IEnumerable<Production> _productions;
        private readonly INonTerminal _root;
        private readonly IList<RuleSet> _sets = new List<RuleSet>();

        public ItemSetsBuilder(IEnumerable<Production> productions, INonTerminal root)
        {
            _productions = productions;
            _root = root;
        }

        public ICollection<RuleSet> Build()
        {
            var startProduction = _productions.FirstOrDefault(i => i.LeftNonTerminal == _root);
            if (startProduction is null)
            {
                throw new InvalidOperationException($"No start production found for root non-terminal '{_root.Name}'.");
            }

            _sets.Add(new RuleSet(_sets.Count, new Rule(startProduction)));
            FillRuleSet(_sets[0]);
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

            foreach (var set in _sets.ToList())
            {
                foreach (var rule in set.Rules.Where(rule => !rule.IsFinished))
                {
                    var newRules = set.Rules
                        .Where(r => !r.IsFinished && r.NextTerm == rule.NextTerm)
                        .Select(r => r.MoveDot())
                        .ToList();

                    var existsSet = GetSetBySetDefinitionRules(newRules);
                    if (existsSet != null)
                    {
                        set.SetArrow(rule.NextTerm, existsSet);
                        continue;
                    }

                    var newRuleSet = new RuleSet(_sets.Count, newRules);
                    _sets.Add(newRuleSet);
                    set.SetArrow(rule.NextTerm, newRuleSet);

                    anyChanges = true;
                }
            }

            return anyChanges;
        }

        private RuleSet? GetSetBySetDefinitionRules(IEnumerable<Rule> newRules)
        {
            return _sets.SingleOrDefault(s => s.StartsFrom(newRules));
        }

        private bool FillRuleSet(RuleSet set)
        {
            bool changed;
            var anyChanges = false;
            do
            {
                changed = false;
                foreach (var rule in set.Rules.Where(r => !r.IsFinished).ToList())
                {
                    var nextTerm = rule.NextTerm;
                    if (!(nextTerm is INonTerminal nextNonTerminal))
                    {
                        continue;
                    }

                    var toAdd = _productions
                        .Where(p => p.LeftNonTerminal == nextNonTerminal)
                        .Select(i => new Rule(i))
                        .ToList();
                    if (!toAdd.Any())
                    {
                        throw new Exception($"No productions for non terminal:{nextNonTerminal}");
                    }

                    toAdd = toAdd.Except(set.Rules).ToList();
                    if (toAdd.Any())
                    {
                        foreach (var item in toAdd)
                        {
                            set.AddRule(item);
                        }

                        changed = true;
                        anyChanges = true;
                    }
                }
            } while (changed);

            return anyChanges;
        }
    }
}
