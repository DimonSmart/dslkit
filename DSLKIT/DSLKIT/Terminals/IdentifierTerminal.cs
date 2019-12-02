namespace DSLKIT.Terminals
{
    public class IdentifierTerminal : RegExpTerminalBase
    {
        private const string TerminalPattern = @"\G(?i)\b[a-z_][\.\p{L}\p{Nl}0-9]*";

        public IdentifierTerminal(string name) : base(TerminalPattern, null)
        {
        }

        public IdentifierTerminal() : this("Variable")
        {
        }

        public override TermFlags Flags => TermFlags.Identifier;
        
        public override string Name => "Identifier";
    }
}