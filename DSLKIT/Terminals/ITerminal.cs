using DSLKIT.Base;
using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public interface ITerminal : ITerm
    {
        TermFlags Flags { get; }

        TerminalPriority Priority { get; }

        bool CanStartWith(char c);
        bool TryMatch(ISourceStream source, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IToken? token);
    }
}
