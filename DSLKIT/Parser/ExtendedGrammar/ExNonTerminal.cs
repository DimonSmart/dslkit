using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public class ExNonTerminal : ExBase, IExNonTerminal
    {
        public ExNonTerminal(INonTerminal t, RuleSet from, RuleSet? to) : base(from, to)
        {
            NonTerminal = t;
        }

        public INonTerminal NonTerminal { get; }
        public override ITerm Term => NonTerminal;
    }
}
