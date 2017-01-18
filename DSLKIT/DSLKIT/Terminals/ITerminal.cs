using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public interface ITerminal
    {
        TermFlags Flags { get; }
        TerminalPriority Priority { get; }
        bool CanStartWith(char c);
        bool TryMatch(ISourceStream source, out IToken token);
    }
}