using System.Text;
using System.Text.RegularExpressions;
using DSLKIT.Helpers;
using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class StringTerminal : ITerminal
    {
        private readonly string _end;
        private readonly Regex _regex;
        private readonly string _start;

        public StringTerminal() : this(@"""")
        {
        }

        public StringTerminal(string quote) : this(quote, quote)
        {
        }

        public StringTerminal(string start, string end)
        {
            _start = start;
            _end = end;
            var escapedStart = start.Escape();
            var escapedEnd = end.Escape();

            var stringStartPattern = @"(?<OpenQuote>" + escapedStart + ")";
            var stringEndPattern = @"(?<CloseQuote>" + escapedEnd + ")";

            var innerPattern = new StringBuilder();
            innerPattern.Append(@"(?:");
            innerPattern.AppendFormat(@"\\{0}|", escapedStart);
            if (start != end)
            {
                innerPattern.AppendFormat(@"\\{0}|", escapedEnd);
            }

            innerPattern.AppendFormat(@"[^{0}", escapedStart);
            if (start != end)
            {
                innerPattern.Append(escapedEnd);
            }

            innerPattern.Append(@"]|");

            innerPattern.AppendFormat(@"{0}{0}", escapedStart);
            if (start != end)
            {
                innerPattern.AppendFormat(@"|{0}{0}", escapedEnd);
            }

            innerPattern.Append(@")*");
            var stringBodyPattern = @"(?<StringBody>" + innerPattern + ")";

            var pattern = @"\G" + stringStartPattern + stringBodyPattern + stringEndPattern;

            _regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public string Name => "String";
        public TermFlags Flags => TermFlags.Const;
        public TerminalPriority Priority => TerminalPriority.Normal;

        public bool CanStartWith(char c)
        {
            if (_start.Length == 0)
            {
                return true;
            }

            return _start[0] == c;
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
            {
                return false;
            }

            var openQuote = result.Groups["OpenQuote"].Value;
            var stringBody = result.Groups["StringBody"].Value;
            var closeQuote = result.Groups["CloseQuote"].Value;

            token = new StringToken
            {
                Position = source.Position,
                Length = result.Length,
                Terminal = this,
                OriginalString = result.Value,
                Value = stringBody
            };

            return true;
        }

        public string DictionaryKey => Name;

        public override string ToString()
        {
            return _start + "string" + _end;
        }
    }
}