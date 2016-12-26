using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class SpaceTerminalBase : ITerminal
    {
        public TermFlags Flags => TermFlags.Space;
        public string Name => "Space";
        public TerminalPriority Priority => TerminalPriority.High;
        public bool CanStartWith(char c)
        {
            return IsSpace(c);
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            var previewChar = source.Peek();
            if (!IsSpace(previewChar))
            {
                token = null;
                return false;
            }

            token = new Token
            {
                Length = 1,
                Position = source.Position,
                Terminal = this,
                StringValue = previewChar.ToString(),
                Value = previewChar
            };
            return true;
        }
        protected abstract bool IsSpace(char c);
    }
}