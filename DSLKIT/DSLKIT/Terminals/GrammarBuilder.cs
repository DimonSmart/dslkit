using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using static DSLKIT.SpecialTerms.EmptyTerm;

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
                root = _productions.SingleOrDefault(i => i.LeftNonTerminal.Name == rootProductionName)?.LeftNonTerminal;
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

        public GrammarBuilder AddProductionFromString(string productionDefinition)
        {
            var production = productionDefinition.Split('→');
            if (production.Length != 2)
            {
                throw new ArgumentException($"{productionDefinition} should be in form A→zxcA with → as delimiter");
            }
            var left = production[0].Trim();
            var productionBuilder = AddProduction(left);
            var definition = new List<ITerm>();
            foreach (var item in production[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item == "ε")
                {
                    definition.Add(Empty);
                    continue;
                }

                if (char.IsUpper(item[0]))
                {
                    definition.Add(item.AsNonTerminal());
                    continue;
                }

                definition.Add(item.AsKeywordTerminal());
            }
            productionBuilder.AddProductionDefinition(definition.ToArray());
            return this;
        }

        public GrammarBuilder AddProductionsFromString(string productions, string[] delimiters = null)
        {
            if (delimiters == null)
            {
                delimiters = new[] { Environment.NewLine, ";" };
            }
            var lines = productions.Split(delimiters, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                AddProductionFromString(line);
            }
            return this;
        }
    }
}