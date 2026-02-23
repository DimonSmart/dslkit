namespace DSLKIT.Terminals
{
    public class RegExpTerminal : RegExpTerminalBase
    {
        public override TermFlags Flags { get; }
        public override string DictionaryKey => Name;
        public override string Name { get; }

        public RegExpTerminal(string name, string pattern, char? previewChar, TermFlags flags)
            : base(pattern, previewChar)
        {
            Name = name;
            Flags = flags;
        }
    }
}