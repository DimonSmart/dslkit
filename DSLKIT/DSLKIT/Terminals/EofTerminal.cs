using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class EofTerminal : ITerminal
    {
        public TermFlags Flags => TermFlags.None;
        public string Name => "Eof";
        public TerminalPriority Priority => TerminalPriority.High;
        public bool CanStartWith(char c)
        {
            return false;
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            token = null;
            if (source.Position != source.Length) return false;
            token = new Token
            {
                Position = source.Position,
                Length = 0,
                Terminal = this,
                StringValue = "EOF",
                Value = null
            };
            return true;
        }
    }
}