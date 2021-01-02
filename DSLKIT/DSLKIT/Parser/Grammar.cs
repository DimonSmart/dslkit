using DSLKIT.NonTerminals;
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

        private IReadOnlyDictionary<INonTerminal, IList<ITerminal>> _firsts;
        private IReadOnlyDictionary<INonTerminal, IList<ITerminal>> _follow;

        public IReadOnlyCollection<Production> Productions { get; }
        public IReadOnlyCollection<ITerminal> Terminals { get; }
        public IReadOnlyCollection<INonTerminal> NonTerminals { get; }
        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Firsts
        {
            get
            {
                if (_firsts != null)
                {
                    return _firsts;
                }
                return _firsts = new FirstsCalculator(Productions).Calculate();
            }
        }

        public IReadOnlyDictionary<INonTerminal, IList<ITerminal>> Follow
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

        public ITerminal Eof { get; } = new EofTerminal();
        public string Name { get; }

        public INonTerminal Root { get; set; }

        public override string ToString()
        {
            return $"Name: {Name}, Terminals:{Terminals.Count}, Eof:{Eof.Name}";
        }
    }
}