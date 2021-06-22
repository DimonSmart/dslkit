using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class ExTerminal : ExBase, IExTerminal
    {
        public ITerm Term { get { return Terminal; } }
        public ITerminal Terminal { get; }

        public ExTerminal(ITerminal terminal, RuleSet from, RuleSet to) : base(from, to)
        {
            Terminal = terminal;
        }
    }

    public static class ExTermFactory
    {
        public static IExTerm ToExTerm(this ITerm term, RuleSet from, RuleSet to)
        {
            switch (term)
            {
                case ITerminal terminal:
                    return terminal.ToExTerminal(from, to);
                case INonTerminal nonTerminal:
                    return nonTerminal.ToExNonTerminal(from, to);
                default:
                    throw new System.InvalidOperationException(nameof(ToExTerm));
            }
        }

        public static IExTerminal ToExTerminal(this ITerminal terminal, RuleSet from, RuleSet to)
        {
            return new ExTerminal(terminal, from, to);
        }

        public static IExNonTerminal ToExNonTerminal(this INonTerminal nonTerminal, RuleSet from, RuleSet to)
        {
            return new ExNonTerminal(nonTerminal, from, to);
        }
    }
}