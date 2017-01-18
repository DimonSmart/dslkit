using DSLKIT.Terminals;

namespace DSLKIT.Tokens
{
    public interface IToken : IPosition
    {
        int Length { get; }
        string OriginalString { get; }
        object Value { get; }
        ITerminal Terminal { get; }
    }
}