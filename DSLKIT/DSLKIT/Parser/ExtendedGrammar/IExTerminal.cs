using DSLKIT.Terminals;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExTerminal : IExTerm
    {
        ITerminal Terminal { get; }
    }
}