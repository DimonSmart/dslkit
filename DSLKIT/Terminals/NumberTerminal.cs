using System;

namespace DSLKIT.Terminals
{
    public enum NumberStyle
    {
        Decimal,
        IniNumber,
        SqlNumber,
        IntegerOnly
    }

    public enum LeadingZeroPolicy
    {
        Allow,
        DisallowExceptZero
    }

    public sealed class NumberOptions
    {
        public bool? AllowFraction { get; init; }
        public bool? AllowExponent { get; init; }
        public bool? AllowLeadingDot { get; init; }
        public bool? AllowSign { get; init; }
        public LeadingZeroPolicy? LeadingZeroPolicy { get; init; }
    }

    public class NumberTerminal : RegExpTerminalBase
    {
        private readonly Configuration _configuration;

        public NumberTerminal(
            string name = "Number",
            NumberStyle style = NumberStyle.Decimal,
            NumberOptions? options = null)
            : this(name, BuildConfiguration(style, options))
        {
        }

        private NumberTerminal(string name, Configuration configuration)
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
        public override TermFlags Flags => TermFlags.Const;
        public override TerminalPriority Priority => TerminalPriority.Normal;

        public override string DictionaryKey =>
            $"Number[{Name}|{_configuration.Style}|fraction:{_configuration.AllowFraction}|exp:{_configuration.AllowExponent}|leadingDot:{_configuration.AllowLeadingDot}|sign:{_configuration.AllowSign}|leadingZero:{_configuration.LeadingZeroPolicy}]";

        private static Configuration BuildConfiguration(NumberStyle style, NumberOptions? options)
        {
            var defaults = GetDefaults(style);
            var resolved = ResolveOptions(defaults, options);
            var pattern = BuildPattern(resolved);

            return new Configuration(
                Style: style,
                Pattern: pattern,
                AllowFraction: resolved.AllowFraction,
                AllowExponent: resolved.AllowExponent,
                AllowLeadingDot: resolved.AllowLeadingDot,
                AllowSign: resolved.AllowSign,
                LeadingZeroPolicy: resolved.LeadingZeroPolicy);
        }

        private static ResolvedOptions GetDefaults(NumberStyle style)
        {
            return style switch
            {
                NumberStyle.IniNumber => new ResolvedOptions(
                    AllowFraction: true,
                    AllowExponent: false,
                    AllowLeadingDot: false,
                    AllowSign: false,
                    LeadingZeroPolicy: LeadingZeroPolicy.DisallowExceptZero,
                    RequireFractionDigits: true),
                NumberStyle.SqlNumber => new ResolvedOptions(
                    AllowFraction: true,
                    AllowExponent: true,
                    AllowLeadingDot: true,
                    AllowSign: false,
                    LeadingZeroPolicy: LeadingZeroPolicy.Allow,
                    RequireFractionDigits: false),
                NumberStyle.IntegerOnly => new ResolvedOptions(
                    AllowFraction: false,
                    AllowExponent: false,
                    AllowLeadingDot: false,
                    AllowSign: false,
                    LeadingZeroPolicy: LeadingZeroPolicy.Allow,
                    RequireFractionDigits: false),
                _ => new ResolvedOptions(
                    AllowFraction: true,
                    AllowExponent: true,
                    AllowLeadingDot: false,
                    AllowSign: false,
                    LeadingZeroPolicy: LeadingZeroPolicy.Allow,
                    RequireFractionDigits: false)
            };
        }

        private static ResolvedOptions ResolveOptions(ResolvedOptions defaults, NumberOptions? options)
        {
            if (options == null)
            {
                return defaults;
            }

            return new ResolvedOptions(
                AllowFraction: options.AllowFraction ?? defaults.AllowFraction,
                AllowExponent: options.AllowExponent ?? defaults.AllowExponent,
                AllowLeadingDot: options.AllowLeadingDot ?? defaults.AllowLeadingDot,
                AllowSign: options.AllowSign ?? defaults.AllowSign,
                LeadingZeroPolicy: options.LeadingZeroPolicy ?? defaults.LeadingZeroPolicy,
                RequireFractionDigits: defaults.RequireFractionDigits);
        }

        private static string BuildPattern(ResolvedOptions options)
        {
            var integerPart = options.LeadingZeroPolicy switch
            {
                LeadingZeroPolicy.Allow => @"\d+",
                LeadingZeroPolicy.DisallowExceptZero => @"(?:0(?!\d)|[1-9]\d*)",
                _ => throw new InvalidOperationException($"Unsupported leading zero policy '{options.LeadingZeroPolicy}'.")
            };

            string core;
            if (options.AllowFraction)
            {
                var fractionPart = options.RequireFractionDigits ? @"\.\d+" : @"\.\d*";
                var integerWithOptionalFraction = $"{integerPart}(?:{fractionPart})?";

                if (options.AllowLeadingDot)
                {
                    core = $@"(?:{integerWithOptionalFraction}|\.\d+)";
                }
                else
                {
                    core = integerWithOptionalFraction;
                }
            }
            else
            {
                core = integerPart;
            }

            if (options.AllowExponent)
            {
                core += @"(?:[eE][+-]?\d+)?";
            }

            if (options.AllowSign)
            {
                core = $@"[+-]?{core}";
            }

            return $@"\G{core}";
        }

        private sealed record Configuration(
            NumberStyle Style,
            string Pattern,
            bool AllowFraction,
            bool AllowExponent,
            bool AllowLeadingDot,
            bool AllowSign,
            LeadingZeroPolicy LeadingZeroPolicy);

        private sealed record ResolvedOptions(
            bool AllowFraction,
            bool AllowExponent,
            bool AllowLeadingDot,
            bool AllowSign,
            LeadingZeroPolicy LeadingZeroPolicy,
            bool RequireFractionDigits);
    }
}
