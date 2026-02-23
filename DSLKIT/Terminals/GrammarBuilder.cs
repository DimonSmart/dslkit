using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using DSLKIT.Ast;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;
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

        private readonly Dictionary<Production, AstNodeBinding> _productionAstBindings =
            new Dictionary<Production, AstNodeBinding>();

        private readonly Dictionary<INonTerminal, AstNodeBinding> _nonTerminalAstBindings =
            new Dictionary<INonTerminal, AstNodeBinding>();

        private IEofTerminal _eof = EofTerminal.Instance;

        private string _name = string.Empty;

        public INonTerminal GetOrAddNonTerminal(string nonTerminalName)
        {
            return _nonTerminals.GetOrAdd(nonTerminalName, i => new NonTerminal(nonTerminalName));
        }

        public INonTerminal AddNonTerminal(INonTerminal nonTerminal)
        {
            return _nonTerminals.GetOrAdd(nonTerminal.Name, i => nonTerminal);
        }

        public NonTerminalBindingBuilder NT(string nonTerminalName)
        {
            var nonTerminal = GetOrAddNonTerminal(nonTerminalName);
            return new NonTerminalBindingBuilder(this, nonTerminal);
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

        public Grammar BuildGrammar(string? rootProductionName = null)
        {
            var root = GetRootNonTerminal(rootProductionName);
            var ruleSets = new ItemSetsBuilder(_productions, root).Build().ToList();
            OnRuleSetCreated?.Invoke(ruleSets);

            var translationTable = TranslationTableBuilder.Build(ruleSets);
            OnTranslationTableCreated?.Invoke(translationTable);

            var exProductions = ExtendedGrammarBuilder.Build(translationTable).ToList();
            OnExtendedGrammarCreated?.Invoke(exProductions);

            var firsts = new FirstsCalculator(exProductions).Calculate();
            OnFirstsCreated?.Invoke(firsts);

            var follows = new FollowCalculator(root, _eof, exProductions, firsts).Calculate();
            OnFollowsCreated?.Invoke(follows);

            var actionAndGotoTable = new ActionAndGotoTableBuilder(
                    root,
                    exProductions,
                    follows,
                    ruleSets,
                    translationTable,
                    OnReductionStep0,
                    OnReductionStep1)
                .Build();

            var astBindings = new AstBindings(_productionAstBindings, _nonTerminalAstBindings);

            return new Grammar(_name,
                root,
                _terminals.Values,
                _nonTerminals.Values.AsEnumerable(),
                _productions,
                exProductions,
                new ReadOnlyDictionary<IExNonTerminal, IList<ITerm>>(firsts),
                new ReadOnlyDictionary<IExNonTerminal, IList<ITerm>>(follows),
                ruleSets,
                translationTable,
                actionAndGotoTable,
                _eof,
                astBindings);
        }

        private INonTerminal GetRootNonTerminal(string? rootProductionName)
        {
            INonTerminal? root;
            if (!string.IsNullOrEmpty(rootProductionName))
            {
                root = _productions.SingleOrDefault(i => i.LeftNonTerminal.Name == rootProductionName)?.LeftNonTerminal;
            }
            else
            {
                root = _productions.First().LeftNonTerminal;
            }

            if (root is null)
            {
                throw new InvalidOperationException($"Root non-terminal '{rootProductionName}' was not found.");
            }

            return root;
        }

        public void AddProduction(Production production)
        {
            _productions.Add(production);
        }

        public ProductionBuilder Prod(string ruleName)
        {
            return AddProduction(ruleName);
        }

        public ProductionBuilder AddProduction(string ruleName)
        {
            return new ProductionBuilder(this, ruleName);
        }

        /// <summary>
        /// Defines a "zero or more" list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | List Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, INonTerminal repeatedNonTerminal)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedNonTerminal);
        }

        /// <summary>
        /// Defines a "zero or more" list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | List Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, ITerminal repeatedTerminal)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedTerminal);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, INonTerminal repeatedNonTerminal, ITerminal delimiter)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedNonTerminal, delimiter);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, ITerminal repeatedTerminal, ITerminal delimiter)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedTerminal, delimiter);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, INonTerminal repeatedNonTerminal, string delimiter)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedNonTerminal, CreateDelimiterTerminal(delimiter));
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(string listNonTerminalName, ITerminal repeatedTerminal, string delimiter)
        {
            var listNonTerminal = GetRequiredListNonTerminal(listNonTerminalName);
            return Star(listNonTerminal, repeatedTerminal, CreateDelimiterTerminal(delimiter));
        }

        /// <summary>
        /// Defines a "zero or more" list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | List Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, INonTerminal repeatedNonTerminal)
        {
            return AddStarWithoutDelimiter(listNonTerminal, repeatedNonTerminal);
        }

        /// <summary>
        /// Defines a "zero or more" list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | List Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, ITerminal repeatedTerminal)
        {
            return AddStarWithoutDelimiter(listNonTerminal, repeatedTerminal);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, INonTerminal repeatedNonTerminal, ITerminal delimiter)
        {
            return AddStarWithDelimiter(listNonTerminal, repeatedNonTerminal, delimiter);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, ITerminal repeatedTerminal, ITerminal delimiter)
        {
            return AddStarWithDelimiter(listNonTerminal, repeatedTerminal, delimiter);
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, INonTerminal repeatedNonTerminal, string delimiter)
        {
            return AddStarWithDelimiter(listNonTerminal, repeatedNonTerminal, CreateDelimiterTerminal(delimiter));
        }

        /// <summary>
        /// Defines a "zero or more" delimited list production, similar to Irony's MakeStarRule.
        /// Example: List -> Empty | Item | List Delimiter Item
        /// </summary>
        public GrammarBuilder Star(INonTerminal listNonTerminal, ITerminal repeatedTerminal, string delimiter)
        {
            return AddStarWithDelimiter(listNonTerminal, repeatedTerminal, CreateDelimiterTerminal(delimiter));
        }

        public GrammarBuilder BindAst(INonTerminal nonTerminal, Type astType)
        {
            return BindAst(nonTerminal, new AstNodeBinding(astType));
        }

        public GrammarBuilder BindAst<TAst>(INonTerminal nonTerminal)
            where TAst : IAstNode
        {
            return BindAst(nonTerminal, typeof(TAst));
        }

        public GrammarBuilder BindAst(INonTerminal nonTerminal, Func<AstBuildContext, IAstNode> factory)
        {
            return BindAst(nonTerminal, new AstNodeBinding(factory));
        }

        public GrammarBuilder BindAst(Production production, Type astType)
        {
            return BindAst(production, new AstNodeBinding(astType));
        }

        public GrammarBuilder BindAst<TAst>(Production production)
            where TAst : IAstNode
        {
            return BindAst(production, typeof(TAst));
        }

        public GrammarBuilder BindAst(Production production, Func<AstBuildContext, IAstNode> factory)
        {
            return BindAst(production, new AstNodeBinding(factory));
        }

        internal GrammarBuilder BindAst(Production production, AstNodeBinding binding)
        {
            _productionAstBindings[production] = binding;
            return this;
        }

        internal GrammarBuilder BindAst(INonTerminal nonTerminal, AstNodeBinding binding)
        {
            var normalizedNonTerminal = GetOrAddNonTerminal(nonTerminal.Name);
            _nonTerminalAstBindings[normalizedNonTerminal] = binding;
            return this;
        }

        public GrammarBuilder WithEof(IEofTerminal eof)
        {
            _eof = eof;
            return this;
        }

        public GrammarBuilder WithOnRuleSetCreated(RuleSetCreated evt)
        {
            OnRuleSetCreated += evt;
            return this;
        }

        public GrammarBuilder WithOnTranslationTableCreated(TranslationTableCreated evt)
        {
            OnTranslationTableCreated += evt;
            return this;
        }

        public GrammarBuilder WithOnExtendedGrammarCreated(ExtendedGrammarCreated evt)
        {
            OnExtendedGrammarCreated += evt;
            return this;
        }

        public GrammarBuilder WithOnFirstsCreated(FirstsCreated firstsCreated)
        {
            OnFirstsCreated += firstsCreated;
            return this;
        }

        public GrammarBuilder WithOnFollowsCreated(FollowsCreated followsCreated)
        {
            OnFollowsCreated += followsCreated;
            return this;
        }

        public GrammarBuilder WithOnReductionStep0(ReductionStep0 reductionStep0)
        {
            OnReductionStep0 += reductionStep0;
            return this;
        }

        public GrammarBuilder WithOnReductionStep1(ReductionStep1 reductionStep1)
        {
            OnReductionStep1 += reductionStep1;
            return this;
        }

        public GrammarBuilder AddProductionFromString(string productionDefinition)
        {
            var production = productionDefinition.Split('→');
            if (production.Length != 2)
            {
                throw new ArgumentException($"{productionDefinition} should be in form A→zxcA with → as delimiter");
            }

            var left = production[0].Trim();
            if (left.StartsWith("<") && left.EndsWith(">"))
            {
                left = left.Substring(1, left.Length - 2);
            }

            var productionBuilder = AddProduction(left);
            var definition = new List<ITerm>();

            foreach (var item in production[1].Trim().Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries))
            {
                if (item == "ε")
                {
                    definition.Add(Empty);
                    continue;
                }

                if (item.StartsWith("<") && item.EndsWith(">"))
                {
                    var ntName = item.Substring(1, item.Length - 2);
                    definition.Add(ntName.AsNonTerminal());
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

        public GrammarBuilder AddProductionsFromString(string productions, string[]? delimiters = null)
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

        private INonTerminal GetRequiredListNonTerminal(string listNonTerminalName)
        {
            if (string.IsNullOrWhiteSpace(listNonTerminalName))
            {
                throw new ArgumentException("Non-terminal name must be specified.", nameof(listNonTerminalName));
            }

            return GetOrAddNonTerminal(listNonTerminalName);
        }

        private GrammarBuilder AddStarWithoutDelimiter(INonTerminal listNonTerminal, ITerm repeatedTerm)
        {
            if (listNonTerminal == null)
            {
                throw new ArgumentNullException(nameof(listNonTerminal));
            }

            if (repeatedTerm == null)
            {
                throw new ArgumentNullException(nameof(repeatedTerm));
            }

            AddProduction(listNonTerminal.Name).Is(Empty);
            AddProduction(listNonTerminal.Name).Is(listNonTerminal, repeatedTerm);
            return this;
        }

        private GrammarBuilder AddStarWithDelimiter(INonTerminal listNonTerminal, ITerm repeatedTerm, ITerminal delimiter)
        {
            if (listNonTerminal == null)
            {
                throw new ArgumentNullException(nameof(listNonTerminal));
            }

            if (repeatedTerm == null)
            {
                throw new ArgumentNullException(nameof(repeatedTerm));
            }

            if (delimiter == null)
            {
                throw new ArgumentNullException(nameof(delimiter));
            }

            AddProduction(listNonTerminal.Name).Is(Empty);
            AddProduction(listNonTerminal.Name).Is(repeatedTerm);
            AddProduction(listNonTerminal.Name).Is(listNonTerminal, delimiter, repeatedTerm);
            return this;
        }

        private static ITerminal CreateDelimiterTerminal(string delimiter)
        {
            if (delimiter == null)
            {
                throw new ArgumentNullException(nameof(delimiter));
            }

            if (delimiter.Length == 0)
            {
                throw new ArgumentException("Delimiter must not be empty.", nameof(delimiter));
            }

            return new KeywordTerminal(delimiter);
        }

        #region Events

        public delegate void RuleSetCreated(List<RuleSet> ruleSets);

        public event RuleSetCreated? OnRuleSetCreated;

        public delegate void TranslationTableCreated(TranslationTable translationTable);

        public event TranslationTableCreated? OnTranslationTableCreated;

        public delegate void ExtendedGrammarCreated(List<ExProduction> exProductions);

        public event ExtendedGrammarCreated? OnExtendedGrammarCreated;

        public delegate void FirstsCreated(IDictionary<IExNonTerminal, IList<ITerm>> firsts);

        public event FirstsCreated? OnFirstsCreated;

        public delegate void FollowsCreated(IDictionary<IExNonTerminal, IList<ITerm>> follows);

        public event FollowsCreated? OnFollowsCreated;

        public delegate void ReductionStep0(Dictionary<ExProduction, IList<ITerm>> rule2FollowSet);

        public event ReductionStep0? OnReductionStep0;

        public delegate void ReductionStep1(IEnumerable<ActionAndGotoTableBuilder.MergedRow> mergedRows);

        public event ReductionStep1? OnReductionStep1;

        #endregion
    }
}
