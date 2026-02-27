using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class KeywordTerminal : RegExpTerminalBase
    {
        public static IReadOnlyDictionary<string, TermFlags> PredefinedFlags { get; } = new Dictionary<string, TermFlags>
        {
            { "(", TermFlags.OpenBrace },
            { ")", TermFlags.CloseBrace },
            { "[", TermFlags.OpenBrace },
            { "]", TermFlags.CloseBrace },
            { "{", TermFlags.OpenBrace },
            { "}", TermFlags.CloseBrace },
            { "<", TermFlags.OpenBrace },
            { ">", TermFlags.CloseBrace }
        };

        private readonly bool _wholeWord;
        private readonly bool _ignoreCase;

        public KeywordTerminal(
            string keyword,
            TermFlags flags = TermFlags.None,
            bool wholeWord = false,
            bool ignoreCase = false,
            string? name = null)
            : base(CreatePattern(keyword, wholeWord, ignoreCase), CreatePreviewChar(keyword, ignoreCase))
        {
            Keyword = keyword;
            Name = string.IsNullOrWhiteSpace(name) ? keyword : name;
            Flags = GetFlag(keyword, flags);
            _wholeWord = wholeWord;
            _ignoreCase = ignoreCase;
        }

        public override string Name { get; }
        public override TermFlags Flags { get; }
        public override TerminalPriority Priority => TerminalPriority.Normal;
        public override string DictionaryKey
        {
            get
            {
                if (!_wholeWord && !_ignoreCase && string.Equals(Name, Keyword, StringComparison.Ordinal))
                {
                    return $"Keyword[{Keyword}]";
                }

                return $"Keyword[{Keyword}|wholeWord:{_wholeWord}|ignoreCase:{_ignoreCase}|name:{Name}]";
            }
        }

        private string Keyword { get; }

        public static TermFlags GetFlag(string keyword, TermFlags flags = TermFlags.None)
        {
            if (flags == TermFlags.None)
            {
                PredefinedFlags.TryGetValue(keyword, out flags);
            }

            return flags;
        }

        public static implicit operator KeywordTerminal(string keyword)
        {
            return new KeywordTerminal(keyword);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Keyword) ? "[Empty]" : Keyword;
        }

        private static string CreatePattern(string keyword, bool wholeWord, bool ignoreCase)
        {
            if (string.IsNullOrWhiteSpace(keyword))
            {
                throw new ArgumentException("Keyword must not be empty.", nameof(keyword));
            }

            var escapedKeyword = Regex.Escape(keyword);
            var keywordPattern = wholeWord
                ? $@"(?<!\w){escapedKeyword}(?!\w)"
                : escapedKeyword;

            if (ignoreCase)
            {
                keywordPattern = $"(?i:{keywordPattern})";
            }

            return $@"\G{keywordPattern}";
        }

        private static char? CreatePreviewChar(string keyword, bool ignoreCase)
        {
            if (ignoreCase || string.IsNullOrEmpty(keyword))
            {
                return null;
            }

            return keyword[0];
        }
    }
}
