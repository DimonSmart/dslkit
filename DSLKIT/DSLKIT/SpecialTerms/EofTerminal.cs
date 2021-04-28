using DSLKIT.Terminals;
using DSLKIT.Tokens;
using System;

namespace DSLKIT.SpecialTerms
{
    public class EofTerminal : IEofTerminal
    {
        private static readonly Lazy<IEofTerminal>
          _lazy = new Lazy<IEofTerminal>(() => new EofTerminal());
        public static IEofTerminal Instance => _lazy.Value;

        public string Name => "Eof";
        public TermFlags Flags => TermFlags.None;
        public TerminalPriority Priority => TerminalPriority.High;
        private EofTerminal()
        {
        }

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