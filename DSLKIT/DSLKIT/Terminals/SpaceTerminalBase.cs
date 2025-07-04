using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class SpaceTerminalBase : ITerminal
    {
        public string Name => "Space";
        public TermFlags Flags => TermFlags.Space;
        public TerminalPriority Priority => TerminalPriority.High;

        public bool CanStartWith(char c)
        {
            return IsSpace(c);
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            var previewChar = source.Peek();
            if (!IsSpace(previewChar))
            {
                token = null;
                return false;
            }

            token = new SpaceToken(
                Position: source.Position,
                Length: 1,
                OriginalString: previewChar.ToString(),
                Value: previewChar,
                Terminal: this);
            return true;
        }

        public string DictionaryKey => "Terminal[space]";

        protected abstract bool IsSpace(char c);
    }
}