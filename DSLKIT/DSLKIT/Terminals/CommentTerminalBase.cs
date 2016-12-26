using System.Text.RegularExpressions;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class CommentTerminalBase : ITerminal
    {
        private readonly Regex _regex;
        private readonly char _startChar;

        protected CommentTerminalBase(Regex regex, char startChar)
        {
            _regex = regex;
            _startChar = startChar;
        }

        public TermFlags Flags => TermFlags.Comment;

        public TerminalPriority Priority => TerminalPriority.Normal;

        public bool CanStartWith(char c)
        {
            return c == _startChar;
        }

        public bool TryMatch(ISourceStream source, out Token token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
                return false;
            var commentBody = result.Groups["CommentBody"].Value;

            token = new StringToken
            {
                Position = source.Position,
                Length = result.Length,
                Terminal = this,
                StringValue = result.Value,
                Value = commentBody
            };

            return true;
        }
    }
}