using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public sealed class NonTerminalNode : ParseTreeNode
    {
        public INonTerminal NonTerminal { get; }
        public Production Production { get; }

        public override ITerm Term => NonTerminal;

        public NonTerminalNode(INonTerminal nonTerminal, Production production, IEnumerable<ParseTreeNode> children)
            : base(children)
        {
            NonTerminal = nonTerminal;
            Production = production;
        }
    }
}
