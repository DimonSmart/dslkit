using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DSLKIT.Lexer;
using DSLKIT.Tokens;

namespace DSLKIT.Terminals
{
    public enum StringStyle
    {
        DoubleOrSingle,
        IniQuoted,
        SqlSingleQuoted,
        JsonString
    }

    public enum QuoteKinds
    {
        Single,
        Double,
        Both
    }

    public enum StringEscapeMode
    {
        Backslash,
        DoubleQuoteEscape
    }

    public sealed class StringOptions
    {
        public QuoteKinds? QuoteKinds { get; init; }
        public StringEscapeMode? EscapeMode { get; init; }
        public bool? AllowMultiline { get; init; }
        public IReadOnlyCollection<string>? Prefixes { get; init; }
    }

    public class QuotedStringTerminal : ITerminal
    {
        private readonly Configuration _configuration;
        private readonly IReadOnlyList<string> _prefixes;

        public QuotedStringTerminal(
            string name = "String",
            StringStyle style = StringStyle.DoubleOrSingle,
            StringOptions? options = null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Terminal name must not be empty.", nameof(name));
            }

            Name = name;
            _configuration = BuildConfiguration(style, options);
            _prefixes = BuildPrefixList(_configuration.Prefixes);
        }

        public string Name { get; }
        public TermFlags Flags => TermFlags.Const;
        public TerminalPriority Priority => TerminalPriority.Normal;

        public string DictionaryKey =>
            $"QuotedString[{Name}|{_configuration.Style}|quotes:{_configuration.QuoteKinds}|escape:{_configuration.EscapeMode}|multiline:{_configuration.AllowMultiline}|prefixes:{string.Join(",", _configuration.Prefixes)}]";

        public bool CanStartWith(char c)
        {
            if (IsQuoteChar(c))
            {
                return true;
            }

            return _prefixes.Any(prefix => prefix.Length > 0 && prefix[0] == c);
        }

        public bool TryMatch(ISourceStream source, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out IToken? token)
        {
            token = null;
            var text = source.GetText();
            var start = source.Position;
            if (start >= text.Length)
            {
                return false;
            }

            foreach (var prefix in _prefixes)
            {
                if (!StartsWith(text, start, prefix))
                {
                    continue;
                }

                var quoteIndex = start + prefix.Length;
                if (quoteIndex >= text.Length)
                {
                    continue;
                }

                var quote = text[quoteIndex];
                if (!IsQuoteChar(quote))
                {
                    continue;
                }

                if (!TryReadBody(text, start, quoteIndex, quote, out var length, out var body))
                {
                    continue;
                }

                token = new StringToken(
                    Position: start,
                    Length: length,
                    OriginalString: text.Substring(start, length),
                    Value: body,
                    Terminal: this);

                return true;
            }

            return false;
        }

        private static Configuration BuildConfiguration(StringStyle style, StringOptions? options)
        {
            var defaults = GetDefaults(style);
            var resolvedPrefixes = options?.Prefixes is { Count: > 0 }
                ? options.Prefixes.Where(prefix => !string.IsNullOrEmpty(prefix)).ToArray()
                : defaults.Prefixes;

            return new Configuration(
                Style: style,
                QuoteKinds: options?.QuoteKinds ?? defaults.QuoteKinds,
                EscapeMode: options?.EscapeMode ?? defaults.EscapeMode,
                AllowMultiline: options?.AllowMultiline ?? defaults.AllowMultiline,
                Prefixes: resolvedPrefixes);
        }

        private static Configuration GetDefaults(StringStyle style)
        {
            return style switch
            {
                StringStyle.IniQuoted => new Configuration(
                    Style: style,
                    QuoteKinds: QuoteKinds.Both,
                    EscapeMode: StringEscapeMode.Backslash,
                    AllowMultiline: false,
                    Prefixes: Array.Empty<string>()),
                StringStyle.SqlSingleQuoted => new Configuration(
                    Style: style,
                    QuoteKinds: QuoteKinds.Single,
                    EscapeMode: StringEscapeMode.DoubleQuoteEscape,
                    AllowMultiline: true,
                    Prefixes: new[] { "N", "n" }),
                StringStyle.JsonString => new Configuration(
                    Style: style,
                    QuoteKinds: QuoteKinds.Double,
                    EscapeMode: StringEscapeMode.Backslash,
                    AllowMultiline: false,
                    Prefixes: Array.Empty<string>()),
                _ => new Configuration(
                    Style: style,
                    QuoteKinds: QuoteKinds.Both,
                    EscapeMode: StringEscapeMode.Backslash,
                    AllowMultiline: false,
                    Prefixes: Array.Empty<string>())
            };
        }

        private static IReadOnlyList<string> BuildPrefixList(IReadOnlyCollection<string> prefixes)
        {
            var result = prefixes
                .Where(prefix => !string.IsNullOrEmpty(prefix))
                .Distinct(StringComparer.Ordinal)
                .OrderByDescending(prefix => prefix.Length)
                .ToList();

            result.Add(string.Empty);
            return result;
        }

        private static bool StartsWith(string text, int position, string value)
        {
            if (value.Length == 0)
            {
                return true;
            }

            if (position + value.Length > text.Length)
            {
                return false;
            }

            return string.Compare(text, position, value, 0, value.Length, StringComparison.Ordinal) == 0;
        }

        private bool TryReadBody(
            string text,
            int start,
            int quoteIndex,
            char quote,
            out int length,
            out string body)
        {
            var sb = new StringBuilder();
            var position = quoteIndex + 1;
            while (position < text.Length)
            {
                var current = text[position];
                if (current == quote)
                {
                    if (_configuration.EscapeMode == StringEscapeMode.DoubleQuoteEscape &&
                        position + 1 < text.Length &&
                        text[position + 1] == quote)
                    {
                        sb.Append(quote);
                        sb.Append(quote);
                        position += 2;
                        continue;
                    }

                    length = position - start + 1;
                    body = sb.ToString();
                    return true;
                }

                if (!_configuration.AllowMultiline && (current == '\r' || current == '\n'))
                {
                    length = 0;
                    body = string.Empty;
                    return false;
                }

                if (_configuration.EscapeMode == StringEscapeMode.Backslash && current == '\\')
                {
                    if (position + 1 >= text.Length)
                    {
                        length = 0;
                        body = string.Empty;
                        return false;
                    }

                    var escaped = text[position + 1];
                    if (!_configuration.AllowMultiline && (escaped == '\r' || escaped == '\n'))
                    {
                        length = 0;
                        body = string.Empty;
                        return false;
                    }

                    sb.Append(current);
                    sb.Append(escaped);
                    position += 2;
                    continue;
                }

                sb.Append(current);
                position++;
            }

            length = 0;
            body = string.Empty;
            return false;
        }

        private bool IsQuoteChar(char c)
        {
            return _configuration.QuoteKinds switch
            {
                QuoteKinds.Single => c == '\'',
                QuoteKinds.Double => c == '"',
                QuoteKinds.Both => c == '\'' || c == '"',
                _ => false
            };
        }

        private sealed record Configuration(
            StringStyle Style,
            QuoteKinds QuoteKinds,
            StringEscapeMode EscapeMode,
            bool AllowMultiline,
            IReadOnlyCollection<string> Prefixes);
    }
}
