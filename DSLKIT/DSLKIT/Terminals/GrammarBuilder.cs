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

        public INonTerminal AddNonTerminal(INonTerminal nonTerminal)
        {
            return _nonTerminals.GetOrAdd(nonTerminal.Name, i => nonTerminal);
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

        public Grammar BuildGrammar(string rootProductionName = null)
        {
            INonTerminal root;
            if (!string.IsNullOrEmpty(rootProductionName))
            {
                root = _productions.Where(i => i.LeftNonTerminal.Name == rootProductionName).SingleOrDefault()?.LeftNonTerminal;
            }
            else
            {
                root = _productions.First().LeftNonTerminal;
            }
            return new Grammar(_name, _terminals.Values, _nonTerminals.Values.AsEnumerable(), _productions, root);
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