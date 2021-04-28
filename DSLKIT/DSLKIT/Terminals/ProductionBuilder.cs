using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;
using System;
using System.Collections.Generic;

namespace DSLKIT.Terminals
{
    public class ProductionBuilder
    {
        private readonly GrammarBuilder _grammarBuilder;
        private readonly string _leftNonTerminalName;
        private readonly List<ITerm> _ruleDefinition = new List<ITerm>();

        public ProductionBuilder(GrammarBuilder grammarBuilder, string leftNonTerminalName)
        {
            _grammarBuilder = grammarBuilder;
            _leftNonTerminalName = leftNonTerminalName;
        }

        public ProductionBuilder(GrammarBuilder grammarBuilder) : this(grammarBuilder, $"NT_{Guid.NewGuid()}")
        {
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
                    case INonTerminal nonTerminal:
                        _ruleDefinition.Add(_grammarBuilder.AddNonTerminal(nonTerminal));
                        break;
                    case EmptyTerm emptyTerm:
                        _ruleDefinition.Add(EmptyTerm.Empty);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Rule definition contains no [Terminal, NonTerminal, string] object");
                }
            }

            var leftNonTerminal = _grammarBuilder.GetOrAddNonTerminal(_leftNonTerminalName);
            _grammarBuilder.AddProduction(new Production(leftNonTerminal, _ruleDefinition));
            return _grammarBuilder;
        }
    }
}