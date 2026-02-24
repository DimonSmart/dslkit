using DSLKIT.Lexer;
using DSLKIT.Parser;

namespace DSLKIT.Visualizer.Abstractions;

public interface IDslGrammarProvider
{
    string Id { get; }
    string DisplayName { get; }
    int ApiVersion { get; }
    IGrammar BuildGrammar();
    LexerSettings CreateLexerSettings(IGrammar grammar);
}
