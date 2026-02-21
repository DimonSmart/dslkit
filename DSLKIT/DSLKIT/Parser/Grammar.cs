using System.Collections.Generic;
using System.Linq;
using DSLKIT.Ast;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Parser.ExtendedGrammar;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        public Grammar(string name,
            INonTerminal root,
            IEnumerable<ITerminal> terminals,
            IEnumerable<INonTerminal> nonTerminals,
            IEnumerable<Production> productions,
            IEnumerable<ExProduction> exProductions,
            IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> firsts,
            IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> follows,
            IEnumerable<RuleSet> ruleSets,
            TranslationTable translationTable,
            ActionAndGotoTable actionAndGotoTable,
            IEofTerminal eof,
            IAstBindings astBindings)
        {
            Name = name;
            Root = root;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
            Productions = productions.ToList();
            ExProductions = exProductions.ToList();
            Firsts = firsts;
            Follows = follows;
            RuleSets = ruleSets.ToList();
            TranslationTable = translationTable;
            ActionAndGotoTable = actionAndGotoTable;
            Eof = eof ?? EofTerminal.Instance;
            AstBindings = astBindings ?? DSLKIT.Ast.AstBindings.Empty;
        }

        public string Name { get; }
        public INonTerminal Root { get; }
        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ExProduction> ExProductions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> Firsts { get; }
        public IReadOnlyDictionary<IExNonTerminal, IList<ITerm>> Follows { get; }
        public IReadOnlyCollection<RuleSet> RuleSets { get; }
        public TranslationTable TranslationTable { get; }
        public ActionAndGotoTable ActionAndGotoTable { get; }
        public IEofTerminal Eof { get; }
        public IAstBindings AstBindings { get; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count}, Eof:{Eof.Name}";
        }
    }
}
