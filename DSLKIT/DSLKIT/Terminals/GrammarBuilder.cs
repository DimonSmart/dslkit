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
        private readonly ConcurrentDictionary<string, INonTerminal> _nonTerminals =
            new ConcurrentDictionary<string, INonTerminal>();

        private readonly IList<Production> _productions = new List<Production>();

        private readonly ConcurrentDictionary<string, ITerminal> _terminals =
            new ConcurrentDictionary<string, ITerminal>();

        private string _name;

        public INonTerminal GetOrAddNonTerminal(string nonTerminalName)
        {
            return _nonTerminals.GetOrAdd(nonTerminalName, i => new NonTerminal(nonTerminalName));
        }

        public INonTerminal GetOrAddNonTerminal(INonTerminal nonTerminal)
        {
            return _nonTerminals.GetOrAdd(nonTerminal.Name, i => nonTerminal);
        }

        public ITerm GetOrAddTerm(ITerm term)
        {
            switch (term)
            {
                case ITerminal terminal:
                    return AddTerminalBody(terminal);
                case INonTerminal nonTerminal:
                    return GetOrAddNonTerminal(nonTerminal);
            }

            throw new InvalidOperationException($"term {term?.GetType()} must be an ITerminal or INonTerminal");
        }

        public GrammarBuilder AddTerminal(ITerminal terminal)
        {
            AddTerminalBody(terminal);
            return this;
        }

        public ITerminal AddTerminalBody(ITerminal terminal)
        {
            var newTerminal = _terminals.GetOrAdd(terminal.DictionaryKey, i => terminal);

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
            return new Grammar(_name, _terminals.Values, _nonTerminals.Values.AsEnumerable(), _productions);
        }

        public void AddProduction(Production production)
        {
            _productions.Add(production);
        }

        public ProductionBuilder AddProduction(string ruleName)
        {
            return new ProductionBuilder(this, ruleName);
        }
    }
}