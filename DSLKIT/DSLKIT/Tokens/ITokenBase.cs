using DSLKIT.Terminals;

namespace DSLKIT.Tokens
{
    public interface ITokenBase : IPosition
    {
        int Length { get; }
        string OriginalString { get; }
        object Value { get; }
        ITerminal Terminal { get; }
    }
}