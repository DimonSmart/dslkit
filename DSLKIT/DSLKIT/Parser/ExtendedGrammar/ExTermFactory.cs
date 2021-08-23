using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser.ExtendedGrammar
{
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
                case IEmptyTerm empty:
                    return empty.ToExEmpty(from, to);
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

        public static IExEmptyTerm ToExEmpty(this IEmptyTerm emptyTerm, RuleSet from, RuleSet to)
        {
            return new ExEmptyTerm(emptyTerm, from, to);
        }
    }
}