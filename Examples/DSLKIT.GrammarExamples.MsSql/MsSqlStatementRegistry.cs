using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.NonTerminals;

namespace DSLKIT.GrammarExamples.MsSql
{
    internal sealed class MsSqlStatementRegistry
    {
        private readonly List<StatementSpec> _statements = [];

        public void Add(
            INonTerminal symbol,
            INonTerminal? implicitSymbol = null,
            bool allowedInFunctionPrelude = true)
        {
            ArgumentNullException.ThrowIfNull(symbol);

            _statements.Add(new StatementSpec(
                symbol,
                implicitSymbol ?? symbol,
                allowedInFunctionPrelude));
        }

        public object[] CreateTopLevelAlternatives()
        {
            return [.. _statements.Select(static statement => (object)statement.Symbol)];
        }

        public object[] CreateImplicitAlternatives()
        {
            return [.. _statements.Select(static statement => (object)statement.ImplicitSymbol)];
        }

        public object[] CreateFunctionPreludeAlternatives()
        {
            return [.. _statements
                .Where(static statement => statement.AllowedInFunctionPrelude)
                .Select(static statement => (object)statement.Symbol)];
        }

        public object[] CreateFunctionImplicitPreludeAlternatives()
        {
            return [.. _statements
                .Where(static statement => statement.AllowedInFunctionPrelude)
                .Select(static statement => (object)statement.ImplicitSymbol)];
        }

        private sealed record StatementSpec(
            INonTerminal Symbol,
            INonTerminal ImplicitSymbol,
            bool AllowedInFunctionPrelude);
    }
}
