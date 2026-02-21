using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using DSLKIT.Parser;

namespace DSLKIT.Ast
{
    /// <summary>
    /// Builds AST from a fully constructed parse tree.
    /// Binding resolution order:
    /// 1) production binding
    /// 2) non-terminal binding
    /// 3) fallback (single-child passthrough, otherwise GenericAstNode)
    /// </summary>
    public sealed class AstBuilder
    {
        private readonly IAstBindings _bindings;

        public AstBuilder(IAstBindings? bindings)
        {
            _bindings = bindings ?? AstBindings.Empty;
        }

        public IAstNode Build(ParseTreeNode root, string? sourceText = null)
        {
            return BuildInternal(root, sourceText);
        }

        private IAstNode BuildInternal(ParseTreeNode parseNode, string? sourceText)
        {
            if (parseNode is TerminalNode terminalNode)
            {
                return new AstTokenNode(terminalNode, sourceText);
            }

            if (parseNode is not NonTerminalNode nonTerminalNode)
            {
                throw new InvalidOperationException($"Unsupported parse tree node type: {parseNode.GetType().Name}");
            }

            var astChildren = nonTerminalNode.Children
                .Select(child => BuildInternal(child, sourceText))
                .ToList();

            var context = new AstBuildContext(nonTerminalNode, astChildren, sourceText);

            if (TryResolveBinding(nonTerminalNode, out var binding))
            {
                return CreateBoundNode(context, binding);
            }

            // Default fallback: collapse transparent wrappers.
            if (astChildren.Count == 1)
            {
                return astChildren[0];
            }

            // Keep structure explicit when there are multiple children.
            return new GenericAstNode(context, astChildren);
        }

        private bool TryResolveBinding(NonTerminalNode nonTerminalNode, [NotNullWhen(true)] out AstNodeBinding? binding)
        {
            if (_bindings.TryGet(nonTerminalNode.Production, out binding))
            {
                return true;
            }

            return _bindings.TryGet(nonTerminalNode.NonTerminal, out binding);
        }

        private static IAstNode CreateBoundNode(AstBuildContext context, AstNodeBinding binding)
        {
            if (binding.Factory != null)
            {
                var createdWithFactory = binding.Factory(context);
                if (createdWithFactory == null)
                {
                    throw new InvalidOperationException("AST binding factory returned null.");
                }

                return createdWithFactory;
            }

            if (binding.NodeType == null)
            {
                throw new InvalidOperationException("AST binding must specify either NodeType or Factory.");
            }

            var nodeType = binding.NodeType;
            var instance = CreateWithSupportedConstructors(nodeType, context);
            if (instance == null)
            {
                throw new InvalidOperationException(
                    $"Cannot create AST node '{nodeType.FullName}'. Supported constructors: (AstBuildContext, IReadOnlyList<IAstNode>), (AstBuildContext), ()");
            }

            return instance;
        }

        private static IAstNode? CreateWithSupportedConstructors(Type nodeType, AstBuildContext context)
        {
            var ctorWithContextAndChildren = nodeType.GetConstructor(new[] { typeof(AstBuildContext), typeof(IReadOnlyList<IAstNode>) });
            if (ctorWithContextAndChildren != null)
            {
                return (IAstNode)ctorWithContextAndChildren.Invoke(new object[] { context, context.AstChildren });
            }

            var ctorWithContext = nodeType.GetConstructor(new[] { typeof(AstBuildContext) });
            if (ctorWithContext != null)
            {
                return (IAstNode)ctorWithContext.Invoke(new object[] { context });
            }

            var defaultCtor = nodeType.GetConstructor(Type.EmptyTypes);
            if (defaultCtor != null)
            {
                return (IAstNode)defaultCtor.Invoke(Array.Empty<object>());
            }

            return null;
        }
    }
}
