using System;
using System.Collections.Generic;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;

namespace DSLKIT.Terminals
{
    public class ProductionBuilder
    {
        private readonly GrammarBuilder _grammarBuilder;
        private readonly string _nonTerminalName;
        private readonly List<ITerm> _ruleDefinition = new List<ITerm>();

        public ProductionBuilder(GrammarBuilder grammarBuilder, string nonTerminalName)
        {
            _grammarBuilder = grammarBuilder;
            _nonTerminalName = nonTerminalName;
        }

        public ProductionBuilder(GrammarBuilder grammarBuilder) : this(grammarBuilder, $"NT_{Guid.NewGuid()}")
        {
        }

        public GrammarBuilder Build()
        {
            var nonTerminal = _grammarBuilder.GetOrAddNonTerminal(_nonTerminalName);
            _grammarBuilder.AddProduction(new Production(nonTerminal, _ruleDefinition));
            return _grammarBuilder;
        }

        public GrammarBuilder AddProductionDefinition(params object[] terms)
        {
            foreach (var term in terms)
            {
                switch (term)
                {
                    case string keyword:
                        _ruleDefinition.Add(_grammarBuilder.AddTerminalBody(new KeywordTerminal(keyword)));
                        break;
                    case ITerminal terminal:
                        _ruleDefinition.Add(_grammarBuilder.AddTerminalBody(terminal));
                        break;
                    case NonTerminal nonTerminal:
                        _ruleDefinition.Add(_grammarBuilder.GetOrAddNonTerminal(nonTerminal));
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Rule definition contains no [Terminal, NonTerminal, string] object");
                }
            }

            return Build();
        }
    }
}