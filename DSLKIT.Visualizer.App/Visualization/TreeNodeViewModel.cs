namespace DSLKIT.Visualizer.App.Visualization;

public sealed class TreeNodeViewModel
{
    public required string NodeId { get; init; }
    public required string Label { get; init; }
    public required TreeNodeKind Kind { get; init; }
    public string? Description { get; init; }
    public string? TypeName { get; init; }
    public required IReadOnlyList<TreeNodeViewModel> Children { get; init; }
}
