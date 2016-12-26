using System;

namespace DSLKIT.Parser
{
    public class NonTerminal : Term
    {
        public Rule Rule;

        public NonTerminal(string name, Rule expression)
        {
            Name = name;
            Rule = expression;
        }

        public NonTerminal(string name, TermList expression)
        {
            Name = name;
            Rule = new Rule(expression);
        }

        public override string Name { get; }

        public static implicit operator NonTerminal(Rule rule)
        {
            return new NonTerminal("AutoName " + Guid.NewGuid(), rule);
        }

        public override string ToString()
        {
            return Name;
        }
    }
}