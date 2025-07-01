using DSLKIT.Tokens;
using System.Collections.Generic;
using System.Linq;
using DSLKIT.Helpers;

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
                        stateStack.Push(shiftAction.RuleSet);
                        inputPosition++;
                        break;

                    case ReduceAction reduceAction:
                        var productionNumber = GetProductionNumber(reduceAction.Production);
                        output.Add(productionNumber);

                        // Pop states according to production length
                        stateStack.PopMany(reduceAction.PopLength);

                        // Goto action with left-hand side non-terminal
                        var newCurrentState = stateStack.Peek();
                        var leftNonTerminal = reduceAction.Production.LeftNonTerminal;

                        if (!_grammar.ActionAndGotoTable.TryGetGotoValue(leftNonTerminal, newCurrentState, out var gotoState))
                        {
                            return new ParseResult
                            {
                                Error = new ParseErrorDescription($"No goto found for non-terminal '{leftNonTerminal.Name}' in state {newCurrentState.SetNumber}", currentToken.Position),
                                Productions = output
                            };
                        }

                        stateStack.Push(gotoState);
                        break;

                    case AcceptAction _:
                        return new ParseResult
                        {
                            Productions = output
                        };

                    default:
                        return new ParseResult
                        {
                            Error = new ParseErrorDescription($"Unknown action type: {action.GetType().Name}", currentToken.Position),
                            Productions = output
                        };
                }
            }
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
            
            // Используем встроенный метод поиска с переопределенным Equals
            var index = productions.IndexOf(production);
            
            return index; // Вернет -1 если не найдено (что не должно происходить в корректной грамматике)
        }
    }
}
