using DSLKIT.Base;
using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public class ExNonTerminal : ExBase, IExNonTerminal
    {
        public INonTerminal NonTerminal { get; }
        public ITerm Term { get { return NonTerminal; } }

        public ExNonTerminal(INonTerminal t, RuleSet from, RuleSet to) : base(from, to)
        {
            NonTerminal = t;
        }
    }
}