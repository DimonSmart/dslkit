namespace DSLKIT.Terminals
{
    public class IdentifierTerminal : RegExpTerminalBase
    {
        private const string TerminalPattern = @"\G(?i)\b[a-z_][\.\p{L}\p{Nl}0-9]*";

        public override TermFlags Flags => TermFlags.Identifier;
        public override string DictionaryKey => Name;
        public override string Name => "Id";

        public IdentifierTerminal() : base(TerminalPattern, null)
        {
        }
    }
}