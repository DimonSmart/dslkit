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
        private readonly List<Production> _definedProductions = new List<Production>();

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
                foreach (var production in _definedProductions)
                {
                    _grammarBuilder.BindAst(production, binding);
                }
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

            if (terms == null)
            {
                throw new ArgumentNullException(nameof(terms));
            }

            var leftNonTerminal = _grammarBuilder.GetOrAddNonTerminal(_leftNonTerminalName);
            var uniqueProductions = new HashSet<Production>();

            foreach (var expandedTerms in ExpandInlinePatterns(terms))
            {
                var definition = BuildRuleDefinition(
                    expandedTerms.Length == 0 && terms.Length > 0
                        ? new object[] { EmptyTerm.Empty }
                        : expandedTerms);
                var production = new Production(leftNonTerminal, definition);
                if (!uniqueProductions.Add(production))
                {
                    continue;
                }

                Production ??= production;
                _definedProductions.Add(production);
                _grammarBuilder.AddProduction(production);

                if (_pendingAstBinding != null)
                {
                    _grammarBuilder.BindAst(production, _pendingAstBinding);
                }
            }

            _pendingAstBinding = null;
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

        private List<object[]> ExpandInlinePatterns(object[] terms)
        {
            var expanded = new List<object[]> { Array.Empty<object>() };

            foreach (var term in terms)
            {
                List<object[]> variants;
                switch (term)
                {
                    case RulePattern pattern:
                        variants = ExpandInlinePatterns(pattern.Terms);
                        break;
                    case OptionalRulePattern optional:
                        variants = ExpandInlinePatterns(optional.Terms);
                        variants.Insert(0, Array.Empty<object>());
                        break;
                    case ChoiceRulePattern choice:
                        variants = new List<object[]>();
                        foreach (var alternative in choice.Alternatives)
                        {
                            variants.AddRange(ExpandInlinePatterns(new[] { alternative }));
                        }
                        break;
                    case RepetitionRulePattern repetition:
                        variants = new List<object[]>
                        {
                            new object[] { _grammarBuilder.MaterializeRepetition(repetition) }
                        };
                        break;
                    case EmptyTerm _:
                        variants = new List<object[]> { Array.Empty<object>() };
                        break;
                    default:
                        variants = new List<object[]> { new[] { term } };
                        break;
                }

                var next = new List<object[]>();
                foreach (var prefix in expanded)
                {
                    foreach (var variant in variants)
                    {
                        var combination = new object[prefix.Length + variant.Length];
                        Array.Copy(prefix, 0, combination, 0, prefix.Length);
                        Array.Copy(variant, 0, combination, prefix.Length, variant.Length);
                        next.Add(combination);
                    }
                }

                expanded = next;
            }

            return expanded;
        }

        private List<ITerm> BuildRuleDefinition(params object[] terms)
        {
            var ruleDefinition = new List<ITerm>(terms.Length);

            foreach (var term in terms)
            {
                switch (term)
                {
                    case RulePattern pattern:
                        foreach (var nestedTerm in BuildRuleDefinition(pattern.Terms))
                        {
                            ruleDefinition.Add(nestedTerm);
                        }
                        break;
                    case string keyword:
                        ruleDefinition.Add(_grammarBuilder.GetOrAddKeywordTerminal(keyword));
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
