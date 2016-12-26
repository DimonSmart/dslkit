using System.Text.RegularExpressions;
using DSLKIT.Parser;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class RegExpTerminalBase : Term, ITerminal
    {
        private readonly char? _previewChar;
        private readonly Regex _regex;

        protected RegExpTerminalBase(string pattern, char? previewChar)
        {
            _previewChar = previewChar;
            _regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public abstract TermFlags Flags { get; }
        public TerminalPriority Priority => TerminalPriority.Low;


        public bool CanStartWith(char c)
        {
            if (_previewChar == null)
            {
                return true;
            }
            return _previewChar == c;
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
                return false;

            token = new Token
            {
                Position = source.Position,
                Length = result.Length,
                Terminal = this,
                StringValue = result.Value,
                Value = result.Value
            };

            return true;
        }
    }
}