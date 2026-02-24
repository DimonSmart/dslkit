namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class AssemblyLoadReport
{
    public required int SelectedFileCount { get; init; }
    public required int ProcessedAssemblyFileCount { get; init; }
    public required int RegisteredProviderCount { get; init; }
    public required IReadOnlyList<string> Messages { get; init; }
}
