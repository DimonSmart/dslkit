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
        private readonly string _dictionaryKey;

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
            _dictionaryKey = $"NewLine[{Name}|{Style}|collapse:{_collapseConsecutive}]";
        }

        public NewLineStyle Style { get; }
        public override string Name { get; }
        public override TermFlags Flags => TermFlags.None;
        public override TerminalPriority Priority => TerminalPriority.Low;

        public override string DictionaryKey => _dictionaryKey;

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
