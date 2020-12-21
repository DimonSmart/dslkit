using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class KeywordTerminal : RegExpTerminalBase
    {
        public static Dictionary<string, TermFlags> PredefinedFlags = new Dictionary<string, TermFlags>
        {
            {"(", TermFlags.OpenBrace},
            {")", TermFlags.CloseBrace},
            {"[", TermFlags.OpenBrace},
            {"]", TermFlags.CloseBrace},
            {"{", TermFlags.OpenBrace},
            {"}", TermFlags.CloseBrace},
            {"<", TermFlags.OpenBrace},
            {">", TermFlags.CloseBrace}
        };

        private KeywordTerminal(string keyword) : base(@"\G" + Regex.Escape(keyword), keyword[0])
        {
            Keyword = keyword;
            Flags = TermFlags.None;
            Name = Keyword;
        }

        public KeywordTerminal(string keyword, TermFlags flags = TermFlags.None) : this(keyword)
        {
            Keyword = keyword;
            Flags = GetFlag(keyword, flags);
        }

        public static TermFlags GetFlag(string keyword, TermFlags flags = TermFlags.None)
        {
            if (flags == TermFlags.None)
            {
                PredefinedFlags.TryGetValue(keyword, out flags);
            }
            return flags;
        }


        public override TerminalPriority Priority => TerminalPriority.Normal;
        public override string DictionaryKey => $"Keyword[{Keyword}]";

        public override string Name { get; }
        public override TermFlags Flags { get; }
        private string Keyword { get; }

        public static implicit operator KeywordTerminal(string keyword)
        {
            return new KeywordTerminal(keyword);
        }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Keyword) ? "[Empty]" : Keyword;
        }
    }
}