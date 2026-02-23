using System;
using System.Collections.Generic;
using DSLKIT.Ast;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.SpecialTerms;

namespace DSLKIT.Terminals
{
    public class ProductionBuilder
    {
        private readonly GrammarBuilder _grammarBuilder;
        private readonly string _leftNonTerminalName;
        private AstNodeBinding? _pendingAstBinding;

        public ProductionBuilder(GrammarBuilder grammarBuilder, string leftNonTerminalName)
        {
            _grammarBuilder = grammarBuilder;
            _leftNonTerminalName = leftNonTerminalName;
        }

        public ProductionBuilder(GrammarBuilder grammarBuilder) : this(grammarBuilder, $"NT_{Guid.NewGuid()}")
        {
        }

        public Production? Production { get; private set; }

        public ProductionBuilder Ast(Type nodeType)
        {
            return Ast(new AstNodeBinding(nodeType));
        }

        public ProductionBuilder Ast<TAst>()
            where TAst : IAstNode
        {
            return Ast(typeof(TAst));
        }

        public ProductionBuilder Ast(Func<AstBuildContext, IAstNode> factory)
        {
            return Ast(new AstNodeBinding(factory));
        }

        public ProductionBuilder Ast(AstNodeBinding binding)
        {
            if (Production != null)
            {
                _grammarBuilder.BindAst(Production, binding);
            }
            else
            {
                _pendingAstBinding = binding;
            }

            return this;
        }

        /// <summary>
        /// Defines a production and returns grammar builder for fluent grammar declarations.
        /// </summary>
        public GrammarBuilder Is(params object[] terms)
        {
            Define(terms);
            return Done();
        }

        public GrammarBuilder AddProductionDefinition(params object[] terms)
        {
            return Is(terms);
        }

        /// <summary>
        /// Defines a production and keeps the production builder for further configuration.
        /// </summary>
        public ProductionBuilder Define(params object[] terms)
        {
            if (Production != null)
            {
                throw new InvalidOperationException("Production has already been defined for this builder instance.");
            }

            var leftNonTerminal = _grammarBuilder.GetOrAddNonTerminal(_leftNonTerminalName);
            var ruleDefinition = BuildRuleDefinition(terms);

            Production = new Production(leftNonTerminal, ruleDefinition);
            _grammarBuilder.AddProduction(Production);

            if (_pendingAstBinding != null)
            {
                _grammarBuilder.BindAst(Production, _pendingAstBinding);
                _pendingAstBinding = null;
            }

            return this;
        }

        public GrammarBuilder Done()
        {
            if (Production == null)
            {
                throw new InvalidOperationException("Production is not defined. Call Is(...) or Define(...) before Done().");
            }

            return _grammarBuilder;
        }

        private List<ITerm> BuildRuleDefinition(params object[] terms)
        {
            var ruleDefinition = new List<ITerm>();

            foreach (var term in terms)
            {
                switch (term)
                {
                    case string keyword:
                        ruleDefinition.Add(_grammarBuilder.AddTerminalBody(new KeywordTerminal(keyword)));
                        break;
                    case ITerminal terminal:
                        ruleDefinition.Add(_grammarBuilder.AddTerminalBody(terminal));
                        break;
                    case INonTerminal nonTerminal:
                        ruleDefinition.Add(_grammarBuilder.AddNonTerminal(nonTerminal));
                        break;
                    case EmptyTerm _:
                        ruleDefinition.Add(EmptyTerm.Empty);
                        break;
                    default:
                        throw new InvalidOperationException(
                            "Rule definition contains no [Terminal, NonTerminal, string] object");
                }
            }

            return ruleDefinition;
        }
    }
}
