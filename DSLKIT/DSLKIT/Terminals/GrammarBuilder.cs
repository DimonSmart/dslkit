using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;

namespace DSLKIT.Terminals
{
    public class GrammarBuilder
    {
        private readonly ConcurrentDictionary<string, ITerminal> _allTerminals =
            new ConcurrentDictionary<string, ITerminal>();

        private readonly ConcurrentDictionary<string, NonTerminal> NonTerminals =
            new ConcurrentDictionary<string, NonTerminal>();

        private readonly IList<Production> Productions = new List<Production>();
        private string _name;

        public NonTerminal AddNonTerminal(string nonTerminalName)
        {
            return NonTerminals.GetOrAdd(nonTerminalName, i => new NonTerminal(nonTerminalName));
        }

        public NonTerminal AddNonTerminal(NonTerminal nonTerminal)
        {
            return NonTerminals.GetOrAdd(nonTerminal.Name, i => nonTerminal);
        }

        public GrammarBuilder AddKeyword(string keyword, TermFlags flags = TermFlags.None)
        {
            AddTerminal(new KeywordTerminal(keyword, flags));
            return this;
        }

        public GrammarBuilder AddTerminal(ITerminal terminal)
        {
            AddTerminalBody(terminal);
            return this;
        }

        public ITerminal AddTerminalBody(ITerminal terminal)
        {
            var newTerminal = _allTerminals.GetOrAdd(terminal.DictionaryKey, i => terminal);
           
            if (terminal.Flags != newTerminal.Flags)
            {
                throw new InvalidOperationException(
                    $"Different flags for same terminal:[{terminal}] Expected flag:[{terminal.Flags}], Got:[{newTerminal.Flags}]");
            }

            return newTerminal;
        }

        public GrammarBuilder WithGrammarName(string name)
        {
            _name = name;
            return this;
        }

        public Grammar BuildGrammar()
        {
            return new Grammar(_name, _allTerminals.Values, NonTerminals.Values.AsEnumerable());
        }

        public void AddProduction(Production production)
        {
            Productions.Add(production);
        }

        public ProductionBuilder AddProduction(string ruleName)
        {
            return new ProductionBuilder(this, ruleName);
        }
    }
}