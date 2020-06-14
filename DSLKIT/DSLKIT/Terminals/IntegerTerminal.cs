using System.Text.RegularExpressions;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public class IntegerTerminal : ITerminal
    {
        private readonly Regex _regex;

        public IntegerTerminal()
        {
            _regex = new Regex(@"\G\d+");
        }

        public string Name => "Integer";
        public TermFlags Flags => TermFlags.Const;

        public TerminalPriority Priority => TerminalPriority.Normal;

        public bool CanStartWith(char c)
        {
            return char.IsDigit(c);
        }

        public bool TryMatch(ISourceStream source, out IToken token)
        {
            token = null;
            var result = _regex.Match(source);
            if (!result.Success)
            {
                return false;
            }

            var stringBody = result.Value;
            if (!int.TryParse(stringBody, out var intValue))
            {
                return false;
            }

            token = new IntegerToken
            {
                Position = source.Position,
                Length = result.Length,
                Terminal = this,
                OriginalString = stringBody,
                Value = intValue
            };

            return true;
        }

        public string DictionaryKey => Name;
    }
}