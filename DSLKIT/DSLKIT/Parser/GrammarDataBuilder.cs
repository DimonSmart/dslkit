using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public static class GrammarDataBuilder
    {
        public static IEnumerable<NonTerminal> GetAllNonTerminals(NonTerminal root)
        {
            var nonterminals = new List<NonTerminal> {root};
            var rootRule = root.Rule.Data.OfType<NonTerminal>();
            foreach (var nonTerminal in rootRule)
            {
                nonterminals.AddRange(GetAllNonTerminals(nonTerminal));
            }

            return nonterminals;
        }

        public static NonTerminal CreateAugmentedRoot(IGrammar grammar)
        {
            return grammar.Root;
            // TODO: Fix later
            // var result = new NonTerminal(grammar.Root.Name + "'", grammar.Root + (ITerm) grammar.Eof);
            // return result;
        }
    }
}