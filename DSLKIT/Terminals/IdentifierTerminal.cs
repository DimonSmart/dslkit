namespace DSLKIT.Terminals
{
    public class IdentifierTerminal : WordTerminal
    {
        private static readonly WordOptions IdentifierOptions = new()
        {
            AllowDot = true,
            CaseInsensitive = true,
            UnicodeLetters = true,
            StartRule = WordStartRule.LetterOrUnderscore
        };

        public IdentifierTerminal()
            : base(name: "Id", style: WordStyle.CLikeIdentifier, options: IdentifierOptions)
        {
        }

        public override string DictionaryKey => Name;
    }
}
