using System;
using System.Text;

namespace DSLKIT.Terminals
{
    public enum WordStyle
    {
        Identifier,
        IniWord,
        SqlIdentifier,
        CLikeIdentifier
    }

    public enum WordStartRule
    {
        LetterOrUnderscore,
        Letter,
        Custom
    }

    public sealed class WordOptions
    {
        public bool? AllowDash { get; init; }
        public bool? AllowDot { get; init; }
        public bool? AllowDollar { get; init; }
        public bool? AllowHash { get; init; }
        public bool? CaseInsensitive { get; init; }
        public bool? UnicodeLetters { get; init; }
        public WordStartRule? StartRule { get; init; }
        public string? CustomStartCharacterClass { get; init; }
    }

    public class WordTerminal : RegExpTerminalBase
    {
        private readonly Configuration _configuration;

        public WordTerminal(
            string name = "Word",
            WordStyle style = WordStyle.Identifier,
            WordOptions? options = null)
            : this(name, BuildConfiguration(style, options))
        {
        }

        private WordTerminal(string name, Configuration configuration)
            : base(configuration.Pattern, previewChar: null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Terminal name must not be empty.", nameof(name));
            }

            Name = name;
            _configuration = configuration;
        }

        public override string Name { get; }
        public override TermFlags Flags => TermFlags.Identifier;
        public override TerminalPriority Priority => TerminalPriority.Low;

        public override string DictionaryKey =>
            $"Word[{Name}|{_configuration.Style}|dash:{_configuration.AllowDash}|dot:{_configuration.AllowDot}|dollar:{_configuration.AllowDollar}|hash:{_configuration.AllowHash}|caseInsensitive:{_configuration.CaseInsensitive}|unicode:{_configuration.UnicodeLetters}|start:{_configuration.StartRule}|custom:{_configuration.CustomStartCharacterClass}]";

        private static Configuration BuildConfiguration(WordStyle style, WordOptions? options)
        {
            var defaults = GetDefaults(style);
            var resolved = ResolveOptions(defaults, options);
            var startClass = BuildStartClass(resolved);
            var bodyClass = BuildBodyClass(resolved);
            var pattern = BuildPattern(startClass, bodyClass, resolved.CaseInsensitive);

            return new Configuration(
                Style: style,
                Pattern: pattern,
                AllowDash: resolved.AllowDash,
                AllowDot: resolved.AllowDot,
                AllowDollar: resolved.AllowDollar,
                AllowHash: resolved.AllowHash,
                CaseInsensitive: resolved.CaseInsensitive,
                UnicodeLetters: resolved.UnicodeLetters,
                StartRule: resolved.StartRule,
                CustomStartCharacterClass: resolved.CustomStartCharacterClass);
        }

        private static ResolvedOptions GetDefaults(WordStyle style)
        {
            return style switch
            {
                WordStyle.IniWord => new ResolvedOptions(
                    AllowDash: true,
                    AllowDot: true,
                    AllowDollar: false,
                    AllowHash: false,
                    CaseInsensitive: false,
                    UnicodeLetters: false,
                    StartRule: WordStartRule.LetterOrUnderscore,
                    CustomStartCharacterClass: null),
                WordStyle.SqlIdentifier => new ResolvedOptions(
                    AllowDash: false,
                    AllowDot: false,
                    AllowDollar: true,
                    AllowHash: true,
                    CaseInsensitive: true,
                    UnicodeLetters: false,
                    StartRule: WordStartRule.LetterOrUnderscore,
                    CustomStartCharacterClass: null),
                WordStyle.CLikeIdentifier => new ResolvedOptions(
                    AllowDash: false,
                    AllowDot: false,
                    AllowDollar: false,
                    AllowHash: false,
                    CaseInsensitive: false,
                    UnicodeLetters: false,
                    StartRule: WordStartRule.LetterOrUnderscore,
                    CustomStartCharacterClass: null),
                _ => new ResolvedOptions(
                    AllowDash: false,
                    AllowDot: false,
                    AllowDollar: false,
                    AllowHash: false,
                    CaseInsensitive: true,
                    UnicodeLetters: true,
                    StartRule: WordStartRule.LetterOrUnderscore,
                    CustomStartCharacterClass: null)
            };
        }

        private static ResolvedOptions ResolveOptions(ResolvedOptions defaults, WordOptions? options)
        {
            if (options == null)
            {
                return defaults;
            }

            var customStart = string.IsNullOrWhiteSpace(options.CustomStartCharacterClass)
                ? defaults.CustomStartCharacterClass
                : options.CustomStartCharacterClass;

            return new ResolvedOptions(
                AllowDash: options.AllowDash ?? defaults.AllowDash,
                AllowDot: options.AllowDot ?? defaults.AllowDot,
                AllowDollar: options.AllowDollar ?? defaults.AllowDollar,
                AllowHash: options.AllowHash ?? defaults.AllowHash,
                CaseInsensitive: options.CaseInsensitive ?? defaults.CaseInsensitive,
                UnicodeLetters: options.UnicodeLetters ?? defaults.UnicodeLetters,
                StartRule: options.StartRule ?? defaults.StartRule,
                CustomStartCharacterClass: customStart);
        }

        private static string BuildPattern(string startClass, string bodyClass, bool caseInsensitive)
        {
            var pattern = $@"\G[{startClass}][{bodyClass}]*";
            return caseInsensitive ? $@"\G(?i:[{startClass}][{bodyClass}]*)" : pattern;
        }

        private static string BuildStartClass(ResolvedOptions options)
        {
            return options.StartRule switch
            {
                WordStartRule.Letter => BuildLetterClass(options.UnicodeLetters),
                WordStartRule.LetterOrUnderscore => BuildLetterClass(options.UnicodeLetters) + "_",
                WordStartRule.Custom => ResolveCustomStartClass(options.CustomStartCharacterClass),
                _ => throw new InvalidOperationException($"Unsupported start rule '{options.StartRule}'.")
            };
        }

        private static string BuildBodyClass(ResolvedOptions options)
        {
            var body = new StringBuilder();
            body.Append(BuildLetterClass(options.UnicodeLetters));
            body.Append("0-9_");

            if (options.AllowDot)
            {
                body.Append(@"\.");
            }

            if (options.AllowDash)
            {
                body.Append(@"\-");
            }

            if (options.AllowDollar)
            {
                body.Append(@"\$");
            }

            if (options.AllowHash)
            {
                body.Append("#");
            }

            return body.ToString();
        }

        private static string BuildLetterClass(bool unicodeLetters)
        {
            return unicodeLetters ? @"A-Za-z\p{L}\p{Nl}" : "A-Za-z";
        }

        private static string ResolveCustomStartClass(string? customStartCharacterClass)
        {
            if (string.IsNullOrWhiteSpace(customStartCharacterClass))
            {
                throw new ArgumentException(
                    "CustomStartCharacterClass must be provided when StartRule is Custom.",
                    nameof(customStartCharacterClass));
            }

            return customStartCharacterClass;
        }

        private sealed record Configuration(
            WordStyle Style,
            string Pattern,
            bool AllowDash,
            bool AllowDot,
            bool AllowDollar,
            bool AllowHash,
            bool CaseInsensitive,
            bool UnicodeLetters,
            WordStartRule StartRule,
            string? CustomStartCharacterClass);

        private sealed record ResolvedOptions(
            bool AllowDash,
            bool AllowDot,
            bool AllowDollar,
            bool AllowHash,
            bool CaseInsensitive,
            bool UnicodeLetters,
            WordStartRule StartRule,
            string? CustomStartCharacterClass);
    }
}
