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

        public void AddKeyword(string keyword)
        {
            var terminal = _grammarBuilder.AddTerminalBody(new KeywordTerminal(keyword));
            _ruleDefinition.Add(terminal);
        }

        public GrammarBuilder Build()
        {
            var nonTerminal = _grammarBuilder.AddNonTerminal(_nonTerminalName);
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
                        AddKeyword(keyword);
                        break;
                    case ITerminal terminal:
                        AddTerminal(terminal);
                        break;

                    case NonTerminal nonTerminal:
                        _grammarBuilder.AddNonTerminal(nonTerminal);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Rule definition contains no [Terminal, NonTerminal, string] object");
                }
            }

            return Build();
        }

        private void AddTerminal(ITerminal terminal)
        {
            _grammarBuilder.AddTerminal(terminal);
        }
    }
}