using DSLKIT.Tokens;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace DSLKIT.Parser
{
    public class SyntaxParser
    {
        protected readonly IGrammar _grammar;

        public SyntaxParser(IGrammar grammar)
        {
            _grammar = grammar;
        }

        public ParseResult Parse(IEnumerable<IToken> tokens)
        {
            var tokenList = tokens.ToList();
            var inputPosition = 0;
            var output = new List<int>();
            var stateStack = new Stack<RuleSet>();
            var nodeStack = new Stack<ParseTreeNode>();

            var initialState = _grammar.RuleSets.First(rs => rs.SetNumber == 0);
            stateStack.Push(initialState);

            while (true)
            {
                var currentState = stateStack.Peek();
                var currentToken = GetCurrentToken(tokenList, inputPosition);

                if (!_grammar.ActionAndGotoTable.TryGetActionValue(currentToken.Terminal, currentState, out var action))
                {
                    return new ParseResult
                    {
                        Error = new ParseErrorDescription($"No action found for terminal '{currentToken.Terminal.Name}' in state {currentState.SetNumber}", currentToken.Position),
                        Productions = output
                    };
                }

                switch (action)
                {
                    case ShiftAction shiftAction:
                        ProcessShift(shiftAction, currentToken, ref inputPosition, stateStack, nodeStack);
                        break;

                    case ReduceAction reduceAction:
                        ProcessReduce(reduceAction, currentToken, stateStack, nodeStack, output);
                        break;

                    case AcceptAction _:
                        return ProcessAccept(nodeStack, output);

                    default:
                        return new ParseResult
                        {
                            Error = new ParseErrorDescription($"Unknown action type: {action.GetType().Name}", currentToken.Position),
                            Productions = output
                        };
                }
            }
        }

        private void ProcessShift(ShiftAction shiftAction, IToken currentToken, ref int inputPosition,
                                Stack<RuleSet> stateStack, Stack<ParseTreeNode> nodeStack)
        {
            nodeStack.Push(new TerminalNode(currentToken));
            stateStack.Push(shiftAction.RuleSet);
            inputPosition++;
        }

        private void ProcessReduce(ReduceAction reduceAction, IToken currentToken,
                                 Stack<RuleSet> stateStack, Stack<ParseTreeNode> nodeStack, List<int> output)
        {
            var production = reduceAction.Production;
            var productionNumber = GetProductionNumber(production);
            output.Add(productionNumber);

            var popCount = reduceAction.PopLength;

            var children = new List<ParseTreeNode>();
            for (int i = 0; i < popCount; i++)
            {
                stateStack.Pop();
                children.Insert(0, nodeStack.Pop());
            }

            var parent = new NonTerminalNode(production.LeftNonTerminal, children);
            nodeStack.Push(parent);

            var newCurrentState = stateStack.Peek();
            var leftNonTerminal = production.LeftNonTerminal;

            if (!_grammar.ActionAndGotoTable.TryGetGotoValue(leftNonTerminal, newCurrentState, out var gotoState))
            {
                throw new System.InvalidOperationException($"No goto found for non-terminal '{leftNonTerminal.Name}' in state {newCurrentState.SetNumber}");
            }

            stateStack.Push(gotoState);
        }

        private ParseResult ProcessAccept(Stack<ParseTreeNode> nodeStack, List<int> output)
        {
            var result = new ParseResult
            {
                Productions = output
            };

            if (nodeStack.Count > 0)
            {
                result.ParseTree = nodeStack.Pop();
            }

            return result;
        }

        protected IToken GetCurrentToken(IList<IToken> tokens, int position)
        {
            if (position >= tokens.Count)
            {
                // Return EOF token if we've reached the end
                var lastToken = tokens.LastOrDefault();
                var eofPosition = lastToken?.Position + lastToken?.Length ?? 0;

                return new Token
                {
                    Terminal = _grammar.Eof,
                    Position = eofPosition,
                    Length = 0,
                    OriginalString = string.Empty,
                    Value = null
                };
            }

            return tokens[position];
        }

        protected int GetProductionNumber(Production production)
        {
            var productions = _grammar.Productions.ToList();
            var index = productions.IndexOf(production);
            Debug.Assert(index != -1, "Production not found in grammar.Productions. This should not happen in a correct grammar.");
            return index;
        }
    }
}
