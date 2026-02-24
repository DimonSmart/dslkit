namespace DSLKIT.Visualizer.App.Visualization;

public sealed class GrammarSnapshotDto
{
    public required string GrammarName { get; init; }
    public required string RootName { get; init; }
    public required int TerminalCount { get; init; }
    public required int NonTerminalCount { get; init; }
    public required int RuleSetCount { get; init; }
    public required IReadOnlyList<ProductionRowDto> Productions { get; init; }
    public required TableDto TranslationTable { get; init; }
    public required TableDto ActionAndGotoTable { get; init; }
    public required TableDto FirstsTable { get; init; }
    public required TableDto FollowsTable { get; init; }
}

public sealed class ProductionRowDto
{
    public required int Number { get; init; }
    public required string Left { get; init; }
    public required string Right { get; init; }
}

public sealed class TableDto
{
    public required string Name { get; init; }
    public required IReadOnlyList<string> Columns { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> Rows { get; init; }
}
