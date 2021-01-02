using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class EofTerminal : ITerminal
    {
        public string Name => "Eof";
        public TermFlags Flags => TermFlags.None;
        public TerminalPriority Priority => TerminalPriority.High;

        public bool CanStartWith(char c)
        {
            return false;
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            token = null;
            if (source.Position != source.Length)
            {
                return false;
            }

            token = new EofToken
            {
                Position = source.Position,
                Length = 0,
                OriginalString = "EOF",
                Terminal = this,
                Value = null
            };

            return true;
        }

        public string DictionaryKey => Name;

        public override string ToString()
        {
            return "$(EOF)";
        }
    }
}