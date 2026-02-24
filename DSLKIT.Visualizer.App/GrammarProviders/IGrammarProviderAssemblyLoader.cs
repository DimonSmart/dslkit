using Microsoft.AspNetCore.Components.Forms;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public interface IGrammarProviderAssemblyLoader
{
    Task<AssemblyLoadReport> LoadProvidersAsync(IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken = default);
}
