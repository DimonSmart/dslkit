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

        public virtual TerminalPriority Priority => TerminalPriority.Low;

        public bool CanStartWith(char c)
        {
            if (_previewChar == null)
            {
                return true;
            }

            return _previewChar == c;
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
            {
                return false;
            }

            token = CreateToken(source.Position, result.Length, result.Value, result.Value, this);
            return true;
        }


        protected virtual IToken CreateToken(
            int position,
            int length,
            string originalString,
            object value,
            ITerminal terminal
        )
        {
            // var instance = (IToken)Activator.CreateInstance(typeof(KeywordToken));
            return new KeywordToken
            {
                Position = position,
                Length = length,
                Terminal = this,
                OriginalString = originalString,
                Value = value
            };
        }
    }
}