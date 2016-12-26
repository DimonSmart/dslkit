namespace DSLKIT.Terminals
{
    public class RegExpTerminal : RegExpTerminalBase
    {
        public RegExpTerminal(string name, string pattern, char? previewChar, TermFlags flags)
            : base(pattern, previewChar)
        {
            Name = name;
            Flags = flags;
        }

        public override TermFlags Flags { get; }
        public override string Name { get; }
    }
}