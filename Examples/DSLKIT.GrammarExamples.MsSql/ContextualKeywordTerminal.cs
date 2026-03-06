using System;
using DSLKIT.Lexer;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed class ContextualKeywordTerminal : ITerminal
    {
        private readonly string[] _followingKeywords;

        public ContextualKeywordTerminal(string name, string dictionaryKey, params string[] followingKeywords)
        {
            Name = name;
            DictionaryKey = dictionaryKey;
            _followingKeywords = followingKeywords ?? throw new ArgumentNullException(nameof(followingKeywords));
        }

        public string Name { get; }
        public string DictionaryKey { get; }
        public TermFlags Flags => TermFlags.None;
        public TerminalPriority Priority => TerminalPriority.High;

        public bool CanStartWith(char c)
        {
            return char.ToUpperInvariant(c) == char.ToUpperInvariant(Name[0]);
        }

        public bool TryMatch(
            ISourceStream source,
            [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IToken? token)
        {
            token = null;

            var text = source.GetText();
            var index = source.Position;
            if (!MatchesWord(text, index, Name))
            {
                return false;
            }

            index += Name.Length;
            foreach (var followingKeyword in _followingKeywords)
            {
                if (!TrySkipTrivia(text, ref index) || !MatchesWord(text, index, followingKeyword))
                {
                    return false;
                }

                index += followingKeyword.Length;
            }

            token = new KeywordToken(
                Position: source.Position,
                Length: Name.Length,
                OriginalString: text.Substring(source.Position, Name.Length),
                Value: Name,
                Terminal: this);
            return true;
        }

        private static bool MatchesWord(string text, int index, string keyword)
        {
            if (index < 0 || index + keyword.Length > text.Length)
            {
                return false;
            }

            if (!text.AsSpan(index, keyword.Length).Equals(keyword, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var endIndex = index + keyword.Length;
            return endIndex >= text.Length || !IsWordChar(text[endIndex]);
        }

        private static bool TrySkipTrivia(string text, ref int index)
        {
            var skippedTrivia = false;

            while (index < text.Length)
            {
                if (char.IsWhiteSpace(text[index]))
                {
                    skippedTrivia = true;
                    index++;
                    continue;
                }

                if (index + 1 >= text.Length)
                {
                    break;
                }

                if (text[index] == '/' && text[index + 1] == '*')
                {
                    var commentEnd = text.IndexOf("*/", index + 2, StringComparison.Ordinal);
                    if (commentEnd < 0)
                    {
                        return false;
                    }

                    skippedTrivia = true;
                    index = commentEnd + 2;
                    continue;
                }

                if (text[index] == '-' && text[index + 1] == '-')
                {
                    skippedTrivia = true;
                    index += 2;
                    while (index < text.Length && text[index] != '\r' && text[index] != '\n')
                    {
                        index++;
                    }

                    if (index < text.Length && text[index] == '\r')
                    {
                        index++;
                    }

                    if (index < text.Length && text[index] == '\n')
                    {
                        index++;
                    }

                    continue;
                }

                break;
            }

            return skippedTrivia;
        }

        private static bool IsWordChar(char value)
        {
            return char.IsLetterOrDigit(value) || value == '_';
        }
    }
}
