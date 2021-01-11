using DSLKIT.NonTerminals;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public class SetBuilder
    {
        private readonly IGrammar _grammar;
        private readonly IList<RuleSet> _sets = new List<RuleSet>();

        public SetBuilder(IGrammar grammar)
        {
            _grammar = grammar;
        }

        public void Build()
        {

            // TODO: Move to grammar
            var startProduction = _grammar.Productions.FirstOrDefault(i => i.LeftNonTerminal == _grammar.Root);
            var set0 = new RuleSet(0);
            set0.Rules.Add(new Rule(startProduction));
            _sets.Add(new RuleSet(0));
        }

        public void FillRuleSet(RuleSet set)
        {
            bool changed;

            do
            {
                changed = false;
                foreach (var rule in set.Rules)
                {
                    if (rule.IsFinished())
                    {
                        continue;
                    }

                    var nextTerm = rule.ProductionDefinition[rule.DotPosition];
                    var nextNonTerminal = nextTerm as INonTerminal;
                    if (nextTerm == null)
                    {
                        continue;
                    };


                    var toAdd = _grammar.Productions.Where(p => p.LeftNonTerminal == nextNonTerminal);
                    if (!toAdd.Any())
                    {
                        throw new System.Exception($"No productions for nonterminal:{nextNonTerminal}");
                    }

                    foreach (var item in toAdd)
                    {
                        set.Rules.Add(new Rule(item));
                    }
                    changed = true;
                    break;
                }
            } while (changed);
        }
    }
}
