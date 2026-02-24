namespace DSLKIT.Visualizer.App.Visualization;

public sealed class TokenRowDto
{
    public required int Index { get; init; }
    public required string Kind { get; init; }
    public required string Terminal { get; init; }
    public required string Text { get; init; }
    public required string Value { get; init; }
    public required int Position { get; init; }
    public required int Length { get; init; }
    public required bool IsIgnoredForParsing { get; init; }
}
