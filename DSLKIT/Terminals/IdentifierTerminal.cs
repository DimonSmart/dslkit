namespace DSLKIT.Terminals
{
    public class IdentifierTerminal : WordTerminal
    {
        private readonly bool _allowDot;

        public IdentifierTerminal(bool allowDot = true)
            : base(name: "Id", style: WordStyle.CLikeIdentifier, options: CreateOptions(allowDot))
        {
            _allowDot = allowDot;
        }

        public override string DictionaryKey => _allowDot ? Name : $"{Name}|allowDot:false";

        private static WordOptions CreateOptions(bool allowDot)
        {
            return new WordOptions
            {
                AllowDot = allowDot,
                CaseInsensitive = true,
                UnicodeLetters = true,
                StartRule = WordStartRule.LetterOrUnderscore
            };
        }
    }
}
