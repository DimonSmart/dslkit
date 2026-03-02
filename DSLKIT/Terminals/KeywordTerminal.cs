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

        private readonly string _dictionaryKey;

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
            _dictionaryKey = BuildDictionaryKey(keyword, Name, wholeWord, ignoreCase);
        }

        public override string Name { get; }
        public override TermFlags Flags { get; }
        public override TerminalPriority Priority => TerminalPriority.Normal;
        public override string DictionaryKey => _dictionaryKey;

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

        private static string BuildDictionaryKey(string keyword, string name, bool wholeWord, bool ignoreCase)
        {
            if (!wholeWord && !ignoreCase && string.Equals(name, keyword, StringComparison.Ordinal))
            {
                return $"Keyword[{keyword}]";
            }

            return $"Keyword[{keyword}|wholeWord:{wholeWord}|ignoreCase:{ignoreCase}|name:{name}]";
        }
    }
}
