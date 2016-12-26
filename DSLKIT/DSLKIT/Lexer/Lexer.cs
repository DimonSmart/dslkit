using System.Collections.Generic;
using System.Linq;
using DSLKIT.Terminals;
using DSLKIT.Tokens;

namespace DSLKIT
{
    public class Lexer
    {
        private readonly ITerminal _eofTerminal;
        private readonly LexerSettings _lexerData;

        public Lexer(LexerSettings lexerData)
        {
            _lexerData = lexerData;
            _eofTerminal = _lexerData.EofTerminal;

            if (_eofTerminal == null)
            {
                _eofTerminal = new EofTerminal();
            }
        }

        public IEnumerable<Token> GetTokens(ISourceStream source)
        {
            while (true)
            {
                Token eofToken;
                if (_eofTerminal.TryMatch(source, out eofToken))
                {
                    yield return eofToken;
                    yield break;
                }

                IEnumerable<ITerminal> possibleTerminals;
                if (_lexerData.UsePreviewChar)
                {
                    possibleTerminals = _lexerData.Where(terminal => terminal.CanStartWith(source.Peek())).ToList();
                }
                else
                {
                    possibleTerminals = _lexerData;
                }

                var matches = GetAllMatches(possibleTerminals, source).ToList();
                var bestMatches =
                    matches.Where(m => m.Length == matches.Max(a => a.Length))
                        .OrderByDescending(b => b.Terminal.Priority);
                if (!bestMatches.Any())
                {
                    yield return new ErrorToken
                    {
                        Length = 0,
                        Position = source.Position
                    };
                    yield break;
                }

                var bestMatch = bestMatches.First();
                yield return bestMatch;
                source.Seek(source.Position + bestMatch.Length);
            }
        }

        private static IEnumerable<Token> GetAllMatches(IEnumerable<ITerminal> lexerData, ISourceStream source)
        {
            foreach (var terminal in lexerData)
            {
                Token token;
                if (terminal.TryMatch(source, out token))
                {
                    yield return token;
                }
            }
        }
    }
}