using DSLKIT.Terminals;

namespace DSLKIT.Tokens
{
    public interface IToken : IPosition
    {
        int Length { get; }
        string StringValue { get; }
        ITerminal Terminal { get; }
        object Value { get; }
    }
}