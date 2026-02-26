using DSLKIT.GrammarExamples.Ini;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class IniGrammarProvider : IDslGrammarProvider
{
    private static readonly IReadOnlyList<DslGrammarExample> BuiltInExamples =
    [
        new DslGrammarExample
        {
            Id = "basic-ini",
            Name = "Basic INI",
            Description = "Simple sections and key=value properties.",
            SourceText = """
[app]
name=DSLKIT
version=1.0

[features]
enabled=true
"""
        },
        new DslGrammarExample
        {
            Id = "missing-equals-recovery",
            Name = "Recovery: Missing Equals",
            Description = "The 'port 5432' line is parsed through the recovery production.",
            SourceText = """
[database]
host=localhost
port 5432
enabled=true
"""
        }
    ];

    public string Id => "ini";
    public string DisplayName => "INI Demo";
    public int ApiVersion => DslGrammarProviderApi.CurrentVersion;
    public string Description =>
        "INI grammar with section headers, key/value pairs, and recovery for missing '=' in property lines.";
    public IReadOnlyList<DslGrammarExample> Examples => BuiltInExamples;

    public IGrammar BuildGrammar()
    {
        return IniGrammarExample.BuildGrammar();
    }

    public LexerSettings CreateLexerSettings(IGrammar grammar)
    {
        return IniGrammarExample.CreateLexerSettings(grammar);
    }
}
