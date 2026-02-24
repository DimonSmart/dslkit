using DSLKIT.GrammarExamples;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class ExpressionGrammarProvider : IDslGrammarProvider
{
    public string Id => "expression";
    public string DisplayName => "Expression Demo";
    public int ApiVersion => DslGrammarProviderApi.CurrentVersion;

    public IGrammar BuildGrammar()
    {
        return ExpressionGrammarExample.BuildGrammar();
    }

    public LexerSettings CreateLexerSettings(IGrammar grammar)
    {
        return ExpressionGrammarExample.CreateLexerSettings(grammar);
    }
}
