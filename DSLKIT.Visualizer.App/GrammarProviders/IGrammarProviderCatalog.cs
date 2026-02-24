using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public interface IGrammarProviderCatalog
{
    IReadOnlyList<IDslGrammarProvider> GetAll();
    IDslGrammarProvider? FindById(string providerId);
    bool TryAdd(IDslGrammarProvider provider, out string errorMessage);
}
