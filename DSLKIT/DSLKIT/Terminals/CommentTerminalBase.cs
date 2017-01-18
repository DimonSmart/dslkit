using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class CommentTerminalBase : ITerminal
    {
        public TermFlags Flags => TermFlags.Comment;
        public TerminalPriority Priority => TerminalPriority.Normal;
        public abstract bool CanStartWith(char c);
        public abstract bool TryMatch(ISourceStream source, out IToken token);
    }
}