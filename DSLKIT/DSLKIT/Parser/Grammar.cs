using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT.Parser
{
    public class Grammar : IGrammar
    {
        public Grammar(string name,
            IEnumerable<ITerminal> terminals,
            IEnumerable<INonTerminal> nonTerminals,
            IEnumerable<Production> productions,
            INonTerminal root)
        {
            Name = name;
            Terminals = terminals.ToList();
            NonTerminals = nonTerminals.ToList();
            Productions = productions.ToList();
            Root = root;
        }

        private IReadOnlyDictionary<INonTerminal, IList<ITerm>> _firsts;
        private IReadOnlyDictionary<INonTerminal, IList<ITerm>> _follow;

        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public IReadOnlyDictionary<INonTerminal, IList<ITerm>> Firsts
        {
            get
            {
                if (_firsts != null)
                {
                    return _firsts;
                }

                var sets = new ItemSetsBuilder(this).Build().ToList();
                var translationTable = TranslationTableBuilder.Build(sets);
                var extendedGrammar = ExtendedGrammarBuilder.Build(translationTable).ToList();
                _firsts = new FirstsCalculator(extendedGrammar).Calculate();
                return _firsts;
            }
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerm>> Follow
        {
            get
            {
                if (_follow != null)
                {
                    return _follow;
                }
                return _follow = new FollowCalculator(this).Calculate();
            }
        }

        public IEofTerminal Eof { get; } = EofTerminal.Instance;
        public string Name { get; }

        public INonTerminal Root { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count}, Eof:{Eof.Name}";
        }
    }
}