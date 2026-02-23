using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using DSLKIT.NonTerminals;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    public sealed class AstBindings : IAstBindings
    {
        private static readonly AstBindings EmptyInstance = new AstBindings();

        private readonly IReadOnlyDictionary<Production, AstNodeBinding> _productionBindings;
        private readonly IReadOnlyDictionary<INonTerminal, AstNodeBinding> _nonTerminalBindings;

        public AstBindings(
            IReadOnlyDictionary<Production, AstNodeBinding>? productionBindings = null,
            IReadOnlyDictionary<INonTerminal, AstNodeBinding>? nonTerminalBindings = null)
        {
            _productionBindings = productionBindings != null
                ? new Dictionary<Production, AstNodeBinding>(productionBindings)
                : new Dictionary<Production, AstNodeBinding>();

            _nonTerminalBindings = nonTerminalBindings != null
                ? new Dictionary<INonTerminal, AstNodeBinding>(nonTerminalBindings)
                : new Dictionary<INonTerminal, AstNodeBinding>();
        }

        public static IAstBindings Empty => EmptyInstance;

        public bool TryGet(Production? production, [NotNullWhen(true)] out AstNodeBinding? binding)
        {
            if (production is null)
            {
                binding = null;
                return false;
            }

            return _productionBindings.TryGetValue(production, out binding);
        }

        public bool TryGet(INonTerminal? nonTerminal, [NotNullWhen(true)] out AstNodeBinding? binding)
        {
            if (nonTerminal is null)
            {
                binding = null;
                return false;
            }

            return _nonTerminalBindings.TryGetValue(nonTerminal, out binding);
        }
    }
}
