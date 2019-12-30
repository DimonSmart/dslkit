using System.Collections.Concurrent;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        private readonly ConcurrentDictionary<string, KeywordTerminal> _terminals =
            new ConcurrentDictionary<string, KeywordTerminal>();
        private readonly ITerminal Empty = new EmptyTerminal();

        public ITerminal Eof { get; } = new EofTerminal();
        public NonTerminal Root { get; set; }
    }
}