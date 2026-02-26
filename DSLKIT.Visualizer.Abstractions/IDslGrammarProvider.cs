using DSLKIT.Lexer;
using DSLKIT.Parser;

namespace DSLKIT.Visualizer.Abstractions;

public interface IDslGrammarProvider
{
    string Id { get; }
    string DisplayName { get; }
    int ApiVersion { get; }
    string Description => DisplayName;
    string? DocumentationUrl => null;
    IReadOnlyList<DslGrammarExample> Examples => [];
    IGrammar BuildGrammar();
    LexerSettings CreateLexerSettings(IGrammar grammar);
}
