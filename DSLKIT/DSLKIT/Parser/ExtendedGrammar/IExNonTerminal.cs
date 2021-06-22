using DSLKIT.NonTerminals;

namespace DSLKIT.Parser
{
    public interface IExNonTerminal : IExTerm
    {
        INonTerminal NonTerminal { get; }
    }
}