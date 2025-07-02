using DSLKIT.Base;
using DSLKIT.NonTerminals;
using System;
using System.Collections.Generic;

namespace DSLKIT.Parser
{
    public sealed class NonTerminalNode : ParseTreeNode
    {
        public INonTerminal NonTerminal { get; }

        public override ITerm Term => NonTerminal;

        public NonTerminalNode(INonTerminal nonTerminal, IEnumerable<ParseTreeNode> children)
            : base(children)
        {
            NonTerminal = nonTerminal ?? throw new ArgumentNullException(nameof(nonTerminal));
        }
    }
}
