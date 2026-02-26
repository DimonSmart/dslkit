using DSLKIT.GrammarExamples;
using DSLKIT.Lexer;
using DSLKIT.Parser;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class ExpressionGrammarProvider : IDslGrammarProvider
{
    private static readonly IReadOnlyList<DslGrammarExample> BuiltInExamples =
    [
        new DslGrammarExample
        {
            Id = "assignments-precedence",
            Name = "Assignments And Precedence",
            Description = "Variables, assignment, and operator precedence in one program.",
            SourceText = "x=2+3*4;x+1"
        },
        new DslGrammarExample
        {
            Id = "functions-power",
            Name = "Functions And Power",
            Description = "Unary functions and right-associative power operator.",
            SourceText = "a=2;b=sin(0)+cos(0);a**3+b"
        }
    ];

    public string Id => "expression";
    public string DisplayName => "Expression Demo";
    public int ApiVersion => DslGrammarProviderApi.CurrentVersion;
    public string Description =>
        "Arithmetic expression grammar with assignments, precedence (+, -, *, /, **), unary operators, and built-in math functions.";
    public string? DocumentationUrl => null;
    public IReadOnlyList<DslGrammarExample> Examples => BuiltInExamples;

    public IGrammar BuildGrammar()
    {
        return ExpressionGrammarExample.BuildGrammar();
    }

    public LexerSettings CreateLexerSettings(IGrammar grammar)
    {
        return ExpressionGrammarExample.CreateLexerSettings(grammar);
    }
}
