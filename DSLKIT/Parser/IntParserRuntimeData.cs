using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    internal sealed class IntParserRuntimeData
    {
        private IntParserRuntimeData(IntParserTables tables, IntParserDiagnosticsData diagnostics)
        {
            Tables = tables;
            Diagnostics = diagnostics;
        }

        public IntParserTables Tables { get; }

        public IntParserDiagnosticsData Diagnostics { get; }

        public static IntParserRuntimeData Create(IGrammar grammar)
        {
            ArgumentNullException.ThrowIfNull(grammar);

            var statesById = grammar.RuleSets
                .OrderBy(ruleSet => ruleSet.SetNumber)
                .ToList();
            var stateToId = new Dictionary<RuleSet, int>(statesById.Count, ReferenceEqualityComparer.Instance);
            for (var stateId = 0; stateId < statesById.Count; stateId++)
            {
                stateToId[statesById[stateId]] = stateId;
            }

            var terminals = new List<ITerminal>(grammar.Terminals.Count + 1);
            var terminalToId = new Dictionary<ITerminal, int>(grammar.Terminals.Count + 1, ReferenceEqualityComparer.Instance);
            IntParserTables.RegisterTerminalSet(grammar, terminals, terminalToId);

            var nonTerminals = new List<INonTerminal>(grammar.NonTerminals.Count + 1);
            var nonTerminalToId = new Dictionary<INonTerminal, int>(grammar.NonTerminals.Count + 1, ReferenceEqualityComparer.Instance);
            IntParserTables.RegisterNonTerminalSet(grammar, nonTerminals, nonTerminalToId);

            var productionsById = grammar.Productions.ToList();
            var productionToId = new Dictionary<Production, int>(productionsById.Count);
            var popLengthByProductionId = new int[productionsById.Count];
            var leftNonTerminalIdByProductionId = new int[productionsById.Count];
            for (var productionId = 0; productionId < productionsById.Count; productionId++)
            {
                var production = productionsById[productionId];
                productionToId[production] = productionId;
                popLengthByProductionId[productionId] = production.ProductionDefinition.Count;
                if (!nonTerminalToId.TryGetValue(production.LeftNonTerminal, out var leftNonTerminalId))
                {
                    throw new InvalidOperationException(
                        $"Production references unknown non-terminal '{production.LeftNonTerminal.Name}'.");
                }

                leftNonTerminalIdByProductionId[productionId] = leftNonTerminalId;
            }

            var actionTable = IntParserTables.BuildActionTable(
                grammar,
                stateToId,
                terminalToId,
                productionToId,
                popLengthByProductionId);

            var gotoTable = IntParserTables.BuildGotoTable(
                grammar,
                stateToId,
                nonTerminalToId);

            var initialState = statesById.FirstOrDefault(ruleSet => ruleSet.SetNumber == 0)
                ?? throw new InvalidOperationException("State with SetNumber=0 was not found.");
            if (!stateToId.TryGetValue(initialState, out var startStateId))
            {
                throw new InvalidOperationException("Failed to resolve start state id.");
            }

            var tables = new IntParserTables(
                productionsById,
                terminalToId,
                nonTerminalToId,
                popLengthByProductionId,
                leftNonTerminalIdByProductionId,
                actionTable,
                gotoTable,
                startStateId);

            var diagnostics = IntParserDiagnosticsData.Create(statesById, terminals, actionTable);
            return new IntParserRuntimeData(tables, diagnostics);
        }
    }

    internal sealed class IntParserDiagnosticsData
    {
        private readonly IReadOnlyList<int> _stateSetNumbersById;
        private readonly IReadOnlyList<IReadOnlyList<ITerminal>> _expectedTerminalsByState;

        private IntParserDiagnosticsData(
            IReadOnlyList<int> stateSetNumbersById,
            IReadOnlyList<IReadOnlyList<ITerminal>> expectedTerminalsByState)
        {
            _stateSetNumbersById = stateSetNumbersById;
            _expectedTerminalsByState = expectedTerminalsByState;
        }

        public int GetStateSetNumber(int stateId)
        {
            return _stateSetNumbersById[stateId];
        }

        public IReadOnlyList<ITerminal> GetExpectedTerminals(int stateId)
        {
            return _expectedTerminalsByState[stateId];
        }

        public static IntParserDiagnosticsData Create(
            IReadOnlyList<RuleSet> statesById,
            IReadOnlyList<ITerminal> terminalsById,
            IntActionTable actionTable)
        {
            var stateSetNumbers = statesById
                .Select(ruleSet => ruleSet.SetNumber)
                .ToArray();
            var expectedTerminalsByState = BuildExpectedTerminalsByState(statesById.Count, terminalsById, actionTable);

            return new IntParserDiagnosticsData(stateSetNumbers, expectedTerminalsByState);
        }

        private static IReadOnlyList<IReadOnlyList<ITerminal>> BuildExpectedTerminalsByState(
            int stateCount,
            IReadOnlyList<ITerminal> terminalsById,
            IntActionTable actionTable)
        {
            var expectedTerminalsByState = new IReadOnlyList<ITerminal>[stateCount];
            for (var stateId = 0; stateId < stateCount; stateId++)
            {
                var expectedTerminals = new List<ITerminal>();
                for (var terminalId = 0; terminalId < terminalsById.Count; terminalId++)
                {
                    if (actionTable.GetActionCode(stateId, terminalId) != IntParserActionEncoding.Error)
                    {
                        expectedTerminals.Add(terminalsById[terminalId]);
                    }
                }

                expectedTerminalsByState[stateId] = expectedTerminals;
            }

            return expectedTerminalsByState;
        }
    }
}
