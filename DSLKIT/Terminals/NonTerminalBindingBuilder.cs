using System;
using DSLKIT.Ast;
using DSLKIT.NonTerminals;

namespace DSLKIT.Terminals
{
    public sealed class NonTerminalBindingBuilder : INonTerminal
    {
        private readonly GrammarBuilder _grammarBuilder;

        public NonTerminalBindingBuilder(GrammarBuilder grammarBuilder, INonTerminal nonTerminal)
        {
            _grammarBuilder = grammarBuilder;
            NonTerminal = nonTerminal;
        }

        public INonTerminal NonTerminal { get; }

        public string Name => NonTerminal.Name;

        public INonTerminal Ast(Type nodeType)
        {
            _grammarBuilder.BindAst(NonTerminal, nodeType);
            return NonTerminal;
        }

        public INonTerminal Ast<TAst>()
            where TAst : IAstNode
        {
            return Ast(typeof(TAst));
        }

        public INonTerminal Ast(Func<AstBuildContext, IAstNode> factory)
        {
            _grammarBuilder.BindAst(NonTerminal, factory);
            return NonTerminal;
        }
    }
}
