using System;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Base;
using DSLKIT.NonTerminals;
using DSLKIT.Terminals;

namespace DSLKIT.Parser
{
    public sealed class IntParserTables
    {
        private readonly IReadOnlyList<RuleSet> _statesById;
        private readonly IReadOnlyList<Production> _productionsById;
        private readonly IReadOnlyDictionary<ITerminal, int> _terminalToId;
        private readonly IReadOnlyDictionary<INonTerminal, int> _nonTerminalToId;
        private readonly int[] _popLengthByProductionId;
        private readonly int[] _leftNonTerminalIdByProductionId;

        private IntParserTables(
            IReadOnlyList<RuleSet> statesById,
            IReadOnlyList<Production> productionsById,
            IReadOnlyDictionary<ITerminal, int> terminalToId,
            IReadOnlyDictionary<INonTerminal, int> nonTerminalToId,
            int[] popLengthByProductionId,
            int[] leftNonTerminalIdByProductionId,
            IntActionTable actionTable,
            IntGotoTable gotoTable,
            int startStateId)
        {
            _statesById = statesById;
            _productionsById = productionsById;
            _terminalToId = terminalToId;
            _nonTerminalToId = nonTerminalToId;
            _popLengthByProductionId = popLengthByProductionId;
            _leftNonTerminalIdByProductionId = leftNonTerminalIdByProductionId;
            ActionTable = actionTable;
            GotoTable = gotoTable;
            StartStateId = startStateId;
        }

        public IntActionTable ActionTable { get; }
        public IntGotoTable GotoTable { get; }
        public int StartStateId { get; }

        public bool TryGetTerminalId(ITerminal terminal, out int terminalId)
        {
            return _terminalToId.TryGetValue(terminal, out terminalId);
        }

        public int GetStateSetNumber(int stateId)
        {
            return _statesById[stateId].SetNumber;
        }

        public Production GetProduction(int productionId)
        {
            return _productionsById[productionId];
        }

        public int GetPopLength(int productionId)
        {
            return _popLengthByProductionId[productionId];
        }

        public int GetLeftNonTerminalId(int productionId)
        {
            return _leftNonTerminalIdByProductionId[productionId];
        }

        public static IntParserTables Create(IGrammar grammar)
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
            RegisterTerminalSet(grammar, terminals, terminalToId);

            var nonTerminals = new List<INonTerminal>(grammar.NonTerminals.Count + 1);
            var nonTerminalToId = new Dictionary<INonTerminal, int>(grammar.NonTerminals.Count + 1, ReferenceEqualityComparer.Instance);
            RegisterNonTerminalSet(grammar, nonTerminals, nonTerminalToId);

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

            var actionTable = BuildActionTable(
                grammar,
                stateToId,
                terminalToId,
                productionToId,
                popLengthByProductionId);

            var gotoTable = BuildGotoTable(
                grammar,
                stateToId,
                nonTerminalToId);

            var initialState = statesById.FirstOrDefault(ruleSet => ruleSet.SetNumber == 0)
                ?? throw new InvalidOperationException("State with SetNumber=0 was not found.");
            if (!stateToId.TryGetValue(initialState, out var startStateId))
            {
                throw new InvalidOperationException("Failed to resolve start state id.");
            }

            return new IntParserTables(
                statesById,
                productionsById,
                terminalToId,
                nonTerminalToId,
                popLengthByProductionId,
                leftNonTerminalIdByProductionId,
                actionTable,
                gotoTable,
                startStateId);
        }

        private static void RegisterTerminalSet(
            IGrammar grammar,
            List<ITerminal> terminals,
            Dictionary<ITerminal, int> terminalToId)
        {
            foreach (var terminal in grammar.Terminals)
            {
                RegisterTerminal(terminal, terminals, terminalToId);
            }

            RegisterTerminal(grammar.Eof, terminals, terminalToId);
            foreach (var actionKey in grammar.ActionAndGotoTable.ActionTable.Keys)
            {
                if (actionKey.Key is ITerminal terminal)
                {
                    RegisterTerminal(terminal, terminals, terminalToId);
                }
            }
        }

        private static void RegisterNonTerminalSet(
            IGrammar grammar,
            List<INonTerminal> nonTerminals,
            Dictionary<INonTerminal, int> nonTerminalToId)
        {
            foreach (var nonTerminal in grammar.NonTerminals)
            {
                RegisterNonTerminal(nonTerminal, nonTerminals, nonTerminalToId);
            }

            RegisterNonTerminal(grammar.Root, nonTerminals, nonTerminalToId);
            foreach (var gotoKey in grammar.ActionAndGotoTable.GotoTable.Keys)
            {
                RegisterNonTerminal(gotoKey.Key, nonTerminals, nonTerminalToId);
            }
        }

        private static IntActionTable BuildActionTable(
            IGrammar grammar,
            IReadOnlyDictionary<RuleSet, int> stateToId,
            IReadOnlyDictionary<ITerminal, int> terminalToId,
            IReadOnlyDictionary<Production, int> productionToId,
            int[] popLengthByProductionId)
        {
            var actionTable = new IntActionTable(stateToId.Count, terminalToId.Count);
            foreach (var actionRecord in grammar.ActionAndGotoTable.ActionTable)
            {
                if (actionRecord.Key.Key is not ITerminal terminal)
                {
                    continue;
                }

                if (!stateToId.TryGetValue(actionRecord.Key.Value, out var fromStateId))
                {
                    throw new InvalidOperationException(
                        $"Action table references unknown state {actionRecord.Key.Value.SetNumber}.");
                }

                if (!terminalToId.TryGetValue(terminal, out var terminalId))
                {
                    throw new InvalidOperationException(
                        $"Action table references unknown terminal '{terminal.Name}'.");
                }

                var encodedAction = actionRecord.Value switch
                {
                    ShiftAction shiftAction => EncodeShift(shiftAction, stateToId),
                    ReduceAction reduceAction => EncodeReduce(reduceAction, productionToId, popLengthByProductionId),
                    AcceptAction _ => IntParserActionEncoding.Accept,
                    _ => throw new InvalidOperationException(
                        $"Unsupported action type '{actionRecord.Value.GetType().Name}'.")
                };

                actionTable.SetActionCode(fromStateId, terminalId, encodedAction);
            }

            return actionTable;
        }

        private static IntGotoTable BuildGotoTable(
            IGrammar grammar,
            IReadOnlyDictionary<RuleSet, int> stateToId,
            IReadOnlyDictionary<INonTerminal, int> nonTerminalToId)
        {
            var gotoTable = new IntGotoTable(stateToId.Count, nonTerminalToId.Count);
            foreach (var gotoRecord in grammar.ActionAndGotoTable.GotoTable)
            {
                if (!stateToId.TryGetValue(gotoRecord.Key.Value, out var fromStateId))
                {
                    throw new InvalidOperationException(
                        $"Goto table references unknown source state {gotoRecord.Key.Value.SetNumber}.");
                }

                if (!stateToId.TryGetValue(gotoRecord.Value, out var toStateId))
                {
                    throw new InvalidOperationException(
                        $"Goto table references unknown destination state {gotoRecord.Value.SetNumber}.");
                }

                if (!nonTerminalToId.TryGetValue(gotoRecord.Key.Key, out var nonTerminalId))
                {
                    throw new InvalidOperationException(
                        $"Goto table references unknown non-terminal '{gotoRecord.Key.Key.Name}'.");
                }

                gotoTable.SetGotoStateId(fromStateId, nonTerminalId, toStateId);
            }

            return gotoTable;
        }

        private static int EncodeShift(ShiftAction shiftAction, IReadOnlyDictionary<RuleSet, int> stateToId)
        {
            if (!stateToId.TryGetValue(shiftAction.RuleSet, out var targetStateId))
            {
                throw new InvalidOperationException(
                    $"Shift action references unknown state {shiftAction.RuleSet.SetNumber}.");
            }

            return IntParserActionEncoding.EncodeShift(targetStateId);
        }

        private static int EncodeReduce(
            ReduceAction reduceAction,
            IReadOnlyDictionary<Production, int> productionToId,
            int[] popLengthByProductionId)
        {
            if (!productionToId.TryGetValue(reduceAction.Production, out var productionId))
            {
                throw new InvalidOperationException("Reduce action references unknown production.");
            }

            popLengthByProductionId[productionId] = reduceAction.PopLength;
            return IntParserActionEncoding.EncodeReduce(productionId);
        }

        private static void RegisterTerminal(
            ITerminal terminal,
            List<ITerminal> terminals,
            Dictionary<ITerminal, int> terminalToId)
        {
            if (terminalToId.ContainsKey(terminal))
            {
                return;
            }

            var terminalId = terminals.Count;
            terminals.Add(terminal);
            terminalToId[terminal] = terminalId;
        }

        private static void RegisterNonTerminal(
            INonTerminal nonTerminal,
            List<INonTerminal> nonTerminals,
            Dictionary<INonTerminal, int> nonTerminalToId)
        {
            if (nonTerminalToId.ContainsKey(nonTerminal))
            {
                return;
            }

            var nonTerminalId = nonTerminals.Count;
            nonTerminals.Add(nonTerminal);
            nonTerminalToId[nonTerminal] = nonTerminalId;
        }
    }

    public sealed class IntActionTable
    {
        private readonly int[] _actionCodes;
        private readonly int _terminalCount;

        public IntActionTable(int stateCount, int terminalCount)
        {
            if (stateCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stateCount));
            }

            if (terminalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(terminalCount));
            }

            _terminalCount = terminalCount;
            _actionCodes = new int[stateCount * terminalCount];
        }

        public int GetActionCode(int stateId, int terminalId)
        {
            return _actionCodes[(stateId * _terminalCount) + terminalId];
        }

        public void SetActionCode(int stateId, int terminalId, int actionCode)
        {
            _actionCodes[(stateId * _terminalCount) + terminalId] = actionCode;
        }
    }

    public sealed class IntGotoTable
    {
        private readonly int[] _gotoStateIds;
        private readonly int _nonTerminalCount;

        public IntGotoTable(int stateCount, int nonTerminalCount)
        {
            if (stateCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(stateCount));
            }

            if (nonTerminalCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(nonTerminalCount));
            }

            _nonTerminalCount = nonTerminalCount;
            _gotoStateIds = new int[stateCount * nonTerminalCount];
            Array.Fill(_gotoStateIds, -1);
        }

        public int GetGotoStateId(int stateId, int nonTerminalId)
        {
            return _gotoStateIds[(stateId * _nonTerminalCount) + nonTerminalId];
        }

        public void SetGotoStateId(int stateId, int nonTerminalId, int gotoStateId)
        {
            _gotoStateIds[(stateId * _nonTerminalCount) + nonTerminalId] = gotoStateId;
        }
    }

    internal static class IntParserActionEncoding
    {
        internal const int Error = 0;
        internal const int Accept = int.MinValue;

        public static int EncodeShift(int targetStateId)
        {
            return targetStateId + 1;
        }

        public static int EncodeReduce(int productionId)
        {
            return -(productionId + 1);
        }

        public static int DecodeShiftStateId(int encodedAction)
        {
            return encodedAction - 1;
        }

        public static int DecodeReduceProductionId(int encodedAction)
        {
            return -encodedAction - 1;
        }
    }
}
