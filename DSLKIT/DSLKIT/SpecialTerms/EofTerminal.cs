using System;
using DSLKIT.Lexer;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.SpecialTerms
{
    public class EofTerminal : IEofTerminal
    {
        private static readonly Lazy<IEofTerminal> LazyEofTerminal =
            new Lazy<IEofTerminal>(() => new EofTerminal());

        public static IEofTerminal Instance => LazyEofTerminal.Value;

        private EofTerminal()
        {
        }

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

            token = new EofToken(
                Position: source.Position,
                Length: 0,
                OriginalString: "EOF",
                Value: null,
                Terminal: this);

            return true;
        }

        public string DictionaryKey => Name;

        public override string ToString()
        {
            return "$(EOF)";
        }
    }
}