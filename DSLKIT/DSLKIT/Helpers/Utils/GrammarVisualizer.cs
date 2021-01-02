﻿using DSLKIT.Parser;
using System.Linq;
using System.Text;

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
            DumpFirsts(grammar, sb);
            DumpFollow(grammar, sb);

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

        private static void DumpFirsts(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Firsts: {grammar.Firsts.Count}");
            foreach (var first in grammar.Firsts)
            {
                var firstsSet = string.Join(",", first.Value.Select(i => i.Name));
                sb.AppendLine($"{first.Key.Name} : \t{firstsSet}");
            }
        }
        private static void DumpFollow(IGrammar grammar, StringBuilder sb)
        {
            sb.AppendLine($"Follow: {grammar.Follow.Count}");
            foreach (var follow in grammar.Follow)
            {
                var followSet = string.Join(",", follow.Value.Select(i => i.Name));
                sb.AppendLine($"{follow.Key.Name} : \t{followSet}");
            }
        }
    }
}
