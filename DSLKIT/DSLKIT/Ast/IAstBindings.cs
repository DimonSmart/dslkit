using DSLKIT.NonTerminals;
using DSLKIT.Parser;
using System.Diagnostics.CodeAnalysis;

namespace DSLKIT.Ast
{
    public interface IAstBindings
    {
        bool TryGet(Production? production, [NotNullWhen(true)] out AstNodeBinding? binding);
        bool TryGet(INonTerminal? nonTerminal, [NotNullWhen(true)] out AstNodeBinding? binding);
    }
}
