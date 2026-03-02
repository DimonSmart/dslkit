namespace DSLKIT.Terminals
{
    public class IdentifierTerminal : WordTerminal
    {
        private readonly bool _allowDot;
        private readonly string _dictionaryKey;

        public IdentifierTerminal(bool allowDot = true)
            : base(name: "Id", style: WordStyle.CLikeIdentifier, options: CreateOptions(allowDot))
        {
            _allowDot = allowDot;
            _dictionaryKey = _allowDot ? Name : $"{Name}|allowDot:false";
        }

        public override string DictionaryKey => _dictionaryKey;

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
