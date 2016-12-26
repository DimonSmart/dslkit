using System.Collections.Generic;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        private readonly Dictionary<string, KeywordTerminal> _keywords = new Dictionary<string, KeywordTerminal>();
        public readonly ITerminal Empty = new EmptyTerminal();
        public ITerminal Eof { get; } = new EofTerminal();
        public NonTerminal Root { get; set; }

        public KeywordTerminal ToKeywordTerminal(string text)
        {
            KeywordTerminal keywordTerminal;
            if (_keywords.TryGetValue(text, out keywordTerminal))
            {
                return keywordTerminal;
            }

            keywordTerminal = new KeywordTerminal(text);
            _keywords.Add(text, keywordTerminal);
            return keywordTerminal;
        }
    }
}