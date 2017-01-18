using System.Text.RegularExpressions;

namespace DSLKIT.Terminals
{
    public class KeywordTerminal : RegExpTerminalBase
    {
        public KeywordTerminal(string keyword) : base(@"\G" + Regex.Escape(keyword), keyword[0])
        {
            Keyword = keyword;
            Flags = TermFlags.None;
            Name = Keyword;
        }

        public KeywordTerminal(string keyword, TermFlags flags) : this(keyword)
        {
            Keyword = keyword;
            Flags = flags;
        }

        public override TermFlags Flags { get; }
        public override string Name { get; }
        private string Keyword { get; }

        public override string ToString()
        {
            return string.IsNullOrEmpty(Keyword) ? "[Empty]" : Keyword;
        }
    }
}