using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using DSLKIT.Parser;

namespace DSLKIT.Terminals
{
    public class GrammarBuilder
    {
        private readonly ConcurrentDictionary<string, ITerminal> _allTerminals =
            new ConcurrentDictionary<string, ITerminal>();
        private readonly IList<Production> Productions;
        private string _name;

        public GrammarBuilder AddKeywordTerminal(string keyword, TermFlags flags = TermFlags.None)
        {
            AddTerminal(new KeywordTerminal(keyword, flags));
            return this;
        }

        public GrammarBuilder AddTerminal(ITerminal terminal)
        {
            var existTerminal = _allTerminals.GetOrAdd(terminal.DictionaryKey, i => terminal);
            if (terminal != existTerminal)
            {
                return this;
            }

            Console.WriteLine($"Terminal with key:{terminal.DictionaryKey} already exists in a grammar");
            if (terminal.Flags != existTerminal.Flags)
            {
                throw new InvalidOperationException(
                    $"Different flags for same terminal:[{terminal}] Expected flag:[{terminal.Flags}], Got:[{existTerminal.Flags}]");
            }

            return this;
        }

        public GrammarBuilder AddTerminals(IEnumerable<ITerminal> terminals)
        {
            foreach (var terminal in terminals)
            {
                AddTerminal(terminal);
            }

            return this;
        }

        public GrammarBuilder WithName(string name)
        {
            _name = name;
            return this;
        }

        public Grammar Build()
        {
            return new Grammar(_name, _allTerminals.Values);
        }

        public GrammarBuilder AddProduction(Production production)
        {
            Productions.Add(production);
            return this;
        }
    }
}