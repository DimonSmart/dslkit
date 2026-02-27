using DSLKIT.GrammarExamples.SJackson;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class SJacksonGrammarProvider : IDslGrammarProvider
{
    private static readonly IReadOnlyList<DslGrammarExample> BuiltInExamples =
    [
        new DslGrammarExample
        {
            Id = "assignment-pointer",
            Name = "Assignment With Prefix Star",
            Description = "Classic tutorial sample input.",
            SourceText = "x=*x"
        },
        new DslGrammarExample
        {
            Id = "nested-prefix-star",
            Name = "Nested Prefix Star",
            Description = "Shows recursive V -> * E production.",
            SourceText = "x=***x"
        }
    ];

    public string Id => "sjackson";
    public string DisplayName => "LALR Tutorial (SJackson)";
    public int ApiVersion => DslGrammarProviderApi.CurrentVersion;
    public string Description =>
        "Grammar from the LALR(1) tutorial: S -> N, N -> V=E | E, E -> V, V -> x | *E.";
    public IReadOnlyList<DslGrammarExample> Examples => BuiltInExamples;

    public IGrammar BuildGrammar()
    {
        return SJacksonGrammarExample.BuildGrammar();
    }

    public LexerSettings CreateLexerSettings(IGrammar grammar)
    {
        return SJacksonGrammarExample.CreateLexerSettings(grammar);
    }
}
