using DSLKIT.NonTerminals;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public class ItemSetsBuilder
    {
        public delegate void SetBuilderStep(object sender, IEnumerable<RuleSet> sets, string grammarName);

        private readonly IGrammar _grammar;
        private readonly IList<RuleSet> _sets = new List<RuleSet>();

        public ItemSetsBuilder(IGrammar grammar)
        {
            _grammar = grammar;
        }

        public event SetBuilderStep StepEvent;

        private void Step()
        {
            StepEvent?.Invoke(this, _sets, _grammar.Name);
        }

        public ICollection<RuleSet> Build()
        {
            // TODO: Move to grammar
            var startProduction = _grammar.Productions.FirstOrDefault(i => i.LeftNonTerminal == _grammar.Root);
            _sets.Add(new RuleSet(_sets.Count, new Rule(startProduction)));
            FillRuleSet(_sets[0]);
            Step();
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
                        set.Arrows[rule.NextTerm] = existsSet;
                        continue;
                    }

                    var newRuleSet = new RuleSet(_sets.Count, newRules);
                    _sets.Add(newRuleSet);
                    set.Arrows[rule.NextTerm] = newRuleSet;

                    anyChanges = true;
                    Step();
                }
            }

            return anyChanges;
        }

        private RuleSet GetSetBySetDefinitionRules(IEnumerable<Rule> newRules)
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

                    var toAdd = _grammar.Productions
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
                            set.Rules.Add(item);
                        }

                        changed = true;
                        anyChanges = true;
                    }
                }
            } while (changed);

            Step();
            return anyChanges;
        }
    }
}