namespace DSLKIT.Visualizer.Abstractions;

public sealed record DslGrammarExample
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public required string Description { get; init; }
    public required string SourceText { get; init; }
}
