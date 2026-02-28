using System;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using DSLKIT.Helpers;
using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public abstract class RegExpTerminalBase : ITerminal
    {
        private static readonly ConcurrentDictionary<(string Pattern, RegexOptions Options), Regex> RegexCache = [];
        private readonly char? _previewChar;
        private readonly Regex _regex;

        protected RegExpTerminalBase(string pattern, char? previewChar)
        {
            _previewChar = previewChar;
            var options = GetRegexOptions();
            _regex = RegexCache.GetOrAdd((pattern, options), key => new Regex(key.Pattern, key.Options));
        }

        public abstract string Name { get; }
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

        public bool TryMatch(
            ISourceStream source,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IToken? token)
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

        public abstract string DictionaryKey { get; }

        protected virtual IToken CreateToken(
            int position,
            int length,
            string originalString,
            object value,
            ITerminal terminal
        )
        {
            return new KeywordToken(
                Position: position,
                Length: length,
                OriginalString: originalString,
                Value: value,
                Terminal: this);
        }

        private static RegexOptions GetRegexOptions()
        {
            return RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.Compiled;
        }
    }
}

