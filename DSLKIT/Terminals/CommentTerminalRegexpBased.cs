using System.Text.RegularExpressions;
using DSLKIT.Helpers;
using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class CommentTerminalRegexpBased : CommentTerminalBase
    {
        private readonly Regex _regex;
        private readonly char _startChar;

        protected CommentTerminalRegexpBased(Regex regex, char startChar)
        {
            _regex = regex;
            _startChar = startChar;
        }

        public override bool CanStartWith(char c)
        {
            return c == _startChar;
        }

        public override bool TryMatch(
            ISourceStream source,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IToken? token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
            {
                return false;
            }

            var commentBody = result.Groups["CommentBody"].Value;

            token = new CommentToken(
                Position: source.Position,
                Length: result.Length,
                OriginalString: result.Value,
                Value: commentBody,
                Terminal: this);

            return true;
        }
    }
}
