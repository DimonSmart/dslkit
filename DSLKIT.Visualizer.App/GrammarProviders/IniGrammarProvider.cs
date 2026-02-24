using DSLKIT.GrammarExamples.Ini;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class IniGrammarProvider : IDslGrammarProvider
{
    public string Id => "ini";
    public string DisplayName => "INI Demo";
    public int ApiVersion => DslGrammarProviderApi.CurrentVersion;

    public IGrammar BuildGrammar()
    {
        return IniGrammarExample.BuildGrammar();
    }

    public LexerSettings CreateLexerSettings(IGrammar grammar)
    {
        return IniGrammarExample.CreateLexerSettings(grammar);
    }
}
