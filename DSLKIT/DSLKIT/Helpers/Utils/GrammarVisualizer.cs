using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Utils
{
    public static class GrammarVisualizer
    {
        public static string DumpGrammar(IGrammar grammar)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Grammar name:{grammar.Name}");
            
            DumpNonTerminals(grammar, sb);
            DumpTerminals(grammar, sb);
            DumpProductions(grammar, sb);

            return sb.ToString();
        }

        private static void DumpProductions(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Productions: {grammar.Productions.Count}");
            var line = 1;
            foreach (var production in grammar.Productions)
            {
                sb.AppendLine($"{line++:D2} {production}");
            }
        }

        private static void DumpTerminals(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Terminals: {grammar.Terminals.Count}");
            foreach (var terminal in grammar.Terminals)
            {
                sb.AppendLine($"{terminal.Name}\t{terminal.GetType().Name}");
            }
        }

        private static void DumpNonTerminals(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Non terminals: {grammar.NonTerminals.Count}");
            foreach (var nonTerminal in grammar.NonTerminals)
            {
                sb.AppendLine($"{nonTerminal.Name}\t{nonTerminal.GetType().Name}");
            }
        }
    }
}
