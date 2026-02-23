using DSLKIT.NonTerminals;

namespace DSLKIT.Parser.ExtendedGrammar
{
    public interface IExNonTerminal : IExTerm
    {
        INonTerminal NonTerminal { get; }
    }
}