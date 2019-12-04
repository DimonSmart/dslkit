using System.Collections.Concurrent;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        private readonly ConcurrentDictionary<string, KeywordTerminal> _keywords =
            new ConcurrentDictionary<string, KeywordTerminal>();

        private readonly ITerminal Empty = new EmptyTerminal();

        public ITerminal Eof { get; } = new EofTerminal();
        public NonTerminal Root { get; set; }

        /// <summary>
        ///     Add terminal to collection or get existing to avoid duplicates
        ///     Used to easily write ToTerm("x") + "x" + "x" and get the same x terminal
        /// </summary>
        /// <param name="keyword"></param>
        /// <returns></returns>
        public KeywordTerminal ToTerm(string keyword)
        {
            return _keywords.GetOrAdd(keyword, s => new KeywordTerminal(s));
        }
    }
}