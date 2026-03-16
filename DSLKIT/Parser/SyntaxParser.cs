using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DSLKIT.SpecialTerms;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT.Parser
{
    public class SyntaxParser
    {
        private static readonly ConditionalWeakTable<IGrammar, IntParserRuntimeData> RuntimeDataCache = new();
        private readonly IGrammar _grammar;
        private readonly IntParserDiagnosticsData _diagnostics;
        private readonly IntParserTables _tables;

        public SyntaxParser(IGrammar grammar)
        {
            _grammar = grammar;
            var runtimeData = RuntimeDataCache.GetValue(grammar, IntParserRuntimeData.Create);
            _tables = runtimeData.Tables;
            _diagnostics = runtimeData.Diagnostics;
        }

        public ParseResult Parse(IEnumerable<IToken> tokens)
        {
            var tokenList = tokens.ToList();
            var inputPosition = 0;
            var output = new List<int>();
            var stateStack = new Stack<int>();
            var nodeStack = new Stack<ParseTreeNode>();

            stateStack.Push(_tables.StartStateId);

            while (true)
            {
                var currentStateId = stateStack.Peek();
                var currentToken = GetCurrentToken(tokenList, inputPosition);
                if (currentToken.Terminal == null)
                {
                    return new ParseResult
                    {
                        Error = new ParseErrorDescription
                        {
                            Message = "Token terminal is null.",
                            ErrorPosition = currentToken.Position,
                            ActualTokenText = currentToken.OriginalString
                        },
                        Productions = output.ToArray()
                    };
                }

                if (!_tables.TryGetTerminalId(currentToken.Terminal, out var terminalId))
                {
                    return BuildNoActionResult(currentToken, currentStateId, output);
                }

                var encodedAction = _tables.ActionTable.GetActionCode(currentStateId, terminalId);
                if (encodedAction == IntParserActionEncoding.Error)
                {
                    return BuildNoActionResult(currentToken, currentStateId, output);
                }

                if (encodedAction == IntParserActionEncoding.Accept)
                {
                    return ProcessAccept(nodeStack, output);
                }

                if (encodedAction > IntParserActionEncoding.Error)
                {
                    nodeStack.Push(new TerminalNode(currentToken));
                    stateStack.Push(IntParserActionEncoding.DecodeShiftStateId(encodedAction));
                    inputPosition++;
                    continue;
                }

                var productionId = IntParserActionEncoding.DecodeReduceProductionId(encodedAction);
                var production = _tables.GetProduction(productionId);
                output.Add(productionId);

                var popCount = _tables.GetPopLength(productionId);
                var children = new List<ParseTreeNode>();
                for (var i = 0; i < popCount; i++)
                {
                    stateStack.Pop();
                    children.Insert(0, nodeStack.Pop());
                }

                var parent = new NonTerminalNode(production.LeftNonTerminal, production, children);
                nodeStack.Push(parent);

                var newCurrentStateId = stateStack.Peek();
                var leftNonTerminalId = _tables.GetLeftNonTerminalId(productionId);
                var gotoStateId = _tables.GotoTable.GetGotoStateId(newCurrentStateId, leftNonTerminalId);
                if (gotoStateId < 0)
                {
                    throw new System.InvalidOperationException(
                        $"No goto found for non-terminal '{production.LeftNonTerminal.Name}' in state {_diagnostics.GetStateSetNumber(newCurrentStateId)}");
                }

                stateStack.Push(gotoStateId);
            }
        }

        private ParseResult ProcessAccept(Stack<ParseTreeNode> nodeStack, List<int> output)
        {
            var result = new ParseResult
            {
                Productions = output.ToArray()
            };

            if (nodeStack.Count > 0)
            {
                result.ParseTree = nodeStack.Pop();
            }

            return result;
        }

        private ParseResult BuildNoActionResult(IToken currentToken, int currentStateId, List<int> output)
        {
            var expectedTokens = GetExpectedTokenDescriptions(currentStateId);

            return new ParseResult
            {
                Error = new ParseErrorDescription
                {
                    Message = BuildUnexpectedTokenMessage(currentToken, expectedTokens),
                    ErrorPosition = currentToken.Position,
                    ActualTokenText = currentToken.Terminal is IEofTerminal ? null : currentToken.OriginalString,
                    ExpectedTokens = expectedTokens
                },
                Productions = output.ToArray()
            };
        }

        private IReadOnlyList<string> GetExpectedTokenDescriptions(int currentStateId)
        {
            var expectedTokens = new List<string>();
            var seen = new HashSet<string>(System.StringComparer.OrdinalIgnoreCase);

            foreach (var terminal in _diagnostics.GetExpectedTerminals(currentStateId))
            {
                var description = DescribeTerminal(terminal);
                if (!seen.Add(description))
                {
                    continue;
                }

                expectedTokens.Add(description);
            }

            return expectedTokens;
        }

        private static string BuildUnexpectedTokenMessage(IToken currentToken, IReadOnlyList<string> expectedTokens)
        {
            var tokenText = currentToken.Terminal is IEofTerminal
                ? "Unexpected end of input."
                : $"Unexpected token '{CreateTokenPreview(currentToken.OriginalString)}'.";
            if (expectedTokens.Count == 0)
            {
                return tokenText;
            }

            return $"{tokenText} Expected: {FormatExpectedTokens(expectedTokens)}.";
        }

        private static string DescribeTerminal(ITerminal terminal)
        {
            if (terminal is IEofTerminal)
            {
                return "end of input";
            }

            if (terminal.Flags == TermFlags.Identifier)
            {
                return "identifier";
            }

            return terminal.Name switch
            {
                "Id" => "identifier",
                "String" => "string literal",
                "Number" => "number",
                var name when name.Contains("Identifier", System.StringComparison.OrdinalIgnoreCase) => "identifier",
                var name when name.Contains("Variable", System.StringComparison.OrdinalIgnoreCase) => "variable",
                _ => terminal.Name
            };
        }

        private static string CreateTokenPreview(string? tokenText)
        {
            if (string.IsNullOrEmpty(tokenText))
            {
                return "?";
            }

            var normalizedToken = tokenText
                .Replace("\r", "\\r")
                .Replace("\n", "\\n")
                .Replace("\t", "\\t");

            const int maxLength = 24;
            return normalizedToken.Length <= maxLength
                ? normalizedToken
                : $"{normalizedToken[..maxLength]}...";
        }

        private static string FormatExpectedTokens(IReadOnlyList<string> expectedTokens)
        {
            const int maxTokenCount = 8;
            if (expectedTokens.Count <= maxTokenCount)
            {
                return string.Join(", ", expectedTokens);
            }

            var visibleTokens = string.Join(", ", expectedTokens.Take(maxTokenCount));
            var remainingCount = expectedTokens.Count - maxTokenCount;
            return $"{visibleTokens}, and {remainingCount} more";
        }

        private IToken GetCurrentToken(IList<IToken> tokens, int position)
        {
            if (position >= tokens.Count)
            {
                // Return EOF token if we've reached the end
                var lastToken = tokens.LastOrDefault();
                var eofPosition = lastToken?.Position + lastToken?.Length ?? 0;

                return new EofToken(
                    Position: eofPosition,
                    Length: 0,
                    OriginalString: string.Empty,
                    Value: null,
                    Terminal: _grammar.Eof);
            }

            return tokens[position];
        }
    }
}
