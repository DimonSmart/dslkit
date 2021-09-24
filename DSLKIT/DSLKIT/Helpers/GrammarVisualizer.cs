using System.Linq;
using System.Text;
using DSLKIT.Parser;

namespace DSLKIT.Helpers
{
    public static class GrammarVisualizer
    {
        public static string DumpGrammar(IGrammar grammar)
        {
            var sb = new StringBuilder();
            sb.Append("Grammar name:").AppendLine(grammar.Name);

            DumpNonTerminals(grammar, sb);
            DumpTerminals(grammar, sb);
            DumpProductions(grammar, sb);
            DumpFirsts(grammar, sb);
            DumpFollow(grammar, sb);

            return sb.ToString();
        }

        private static void DumpProductions(IGrammar grammar, StringBuilder sb)
        {
            sb.Append("Productions: ").Append(grammar.Productions.Count).AppendLine();
            var line = 1;
            foreach (var production in grammar.Productions)
            {
                sb.AppendLine($"{line++:D2} {production}");
            }
        }

        private static void DumpTerminals(IGrammar grammar, StringBuilder sb)
        {
            sb.Append("Terminals: ").Append(grammar.Terminals.Count).AppendLine();
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

        private static void DumpFirsts(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Firsts: {grammar.Firsts.Count}");
            foreach (var first in grammar.Firsts)
            {
                var firstsSet = string.Join(",", first.Value.Select(i => i.Name));
                sb.AppendLine($"{first.Key.Term.Name} : \t{firstsSet}");
            }
        }

        private static void DumpFollow(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Follow: {grammar.Follows.Count}");
            foreach (var follow in grammar.Follows)
            {
                var followSet = string.Join(",", follow.Value.Select(i => i.Name));
                sb.AppendLine($"{follow.Key} : \t{followSet}");
            }
        }
    }
}