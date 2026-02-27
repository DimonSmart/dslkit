using System;

namespace DSLKIT.Terminals
{
    public enum NewLineStyle
    {
        CrlfOrCrOrLf
    }

    public class NewLineTerminal : RegExpTerminalBase
    {
        private readonly bool _collapseConsecutive;

        public NewLineTerminal(
            string name = "NewLine",
            NewLineStyle style = NewLineStyle.CrlfOrCrOrLf,
            bool collapseConsecutive = true)
            : base(BuildPattern(style, collapseConsecutive), previewChar: null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Terminal name must not be empty.", nameof(name));
            }

            Name = name;
            Style = style;
            _collapseConsecutive = collapseConsecutive;
        }

        public NewLineStyle Style { get; }
        public override string Name { get; }
        public override TermFlags Flags => TermFlags.None;
        public override TerminalPriority Priority => TerminalPriority.Low;

        public override string DictionaryKey =>
            $"NewLine[{Name}|{Style}|collapse:{_collapseConsecutive}]";

        private static string BuildPattern(NewLineStyle style, bool collapseConsecutive)
        {
            var basePattern = style switch
            {
                NewLineStyle.CrlfOrCrOrLf => @"(?:\r\n|\r|\n)",
                _ => throw new InvalidOperationException($"Unsupported newline style '{style}'.")
            };

            return collapseConsecutive ? $@"\G{basePattern}+" : $@"\G{basePattern}";
        }
    }
}
