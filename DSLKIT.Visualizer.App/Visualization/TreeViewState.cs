namespace DSLKIT.Visualizer.App.Visualization;

public sealed class TreeViewState
{
    public HashSet<string> ExpandedNodeIds { get; } = new(StringComparer.Ordinal);
    public string? SelectedNodeId { get; set; }
}
