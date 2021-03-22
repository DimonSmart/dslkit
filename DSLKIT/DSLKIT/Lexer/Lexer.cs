using DSLKIT.Terminals;
using DSLKIT.Tokens;
using System.Collections.Generic;
using System.Linq;

namespace DSLKIT
{
    public class Lexer
    {
        private readonly ITerminal _eofTerminal;
        private readonly LexerSettings _lexerSettings;

        public Lexer(LexerSettings lexerSettings)
        {
            _lexerSettings = lexerSettings;
            _eofTerminal = _lexerSettings.EofTerminal ?? new EofTerminal();
        }

        public IEnumerable<IToken> GetTokens(ISourceStream source)
        {
            while (true)
            {
                // Check for EOF terminal. For some grammars it could be a keyword 'end.' instead of real end of file
                if (_eofTerminal.TryMatch(source, out var eofToken))
                {
                    yield return eofToken;
                    yield break;
                }

                // Check for all possible terminals with could be started from current char
                var previewChar = source.Peek();
                var possibleTerminals = _lexerSettings
                    .Where(i => i.CanStartWith(previewChar))
                    .ToList()
                    .AsReadOnly();

                var matches = GetAllMatches(possibleTerminals, source)
                    .ToList()
                    .AsReadOnly();
                if (!matches.Any())
                {
                    yield return new ErrorToken("No terminal found")
                    {
                        Length = 0,
                        Position = source.Position
                    };
                    yield break;
                }

                var maxMatchLength = matches.Max(i => i.Length);
                matches = matches
                    .Where(i => i.Length == maxMatchLength)
                    .OrderByDescending(b => b.Terminal.Priority)
                    .ToList()
                    .AsReadOnly();

                var maxMatchedTerminalPriority = matches.Max(i => i.Terminal.Priority);
                matches = matches
                    .Where(i => i.Terminal.Priority == maxMatchedTerminalPriority)
                    .ToList()
                    .AsReadOnly();

                if (matches.Count > 1)
                {
                    var msg = string.Join(",", matches);
                    yield return new ErrorToken(
                        $"Many terminals found at same position. Possible duplicates or priority error : {msg}")
                    {
                        Length = 0,
                        Position = source.Position
                    };
                }

                var bestMatch = matches.Single();
                yield return bestMatch;
                source.Seek(source.Position + bestMatch.Length);
            }
        }

        private static IEnumerable<IToken> GetAllMatches(IEnumerable<ITerminal> lexerData, ISourceStream source)
        {
            foreach (var terminal in lexerData)
            {
                if (terminal.TryMatch(source, out var token))
                {
                    yield return token;
                }
            }
        }
    }
}