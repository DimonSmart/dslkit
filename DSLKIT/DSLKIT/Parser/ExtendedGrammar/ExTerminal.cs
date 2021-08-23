using DSLKIT.Base;
using DSLKIT.Terminals;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public class ExTerminal : ExBase, IExTerminal
    {
        public override ITerm Term => Terminal;
        public ITerminal Terminal { get; }

        public ExTerminal(ITerminal terminal, RuleSet from, RuleSet to) : base(from, to)
        {
            Terminal = terminal;
        }
    }
}