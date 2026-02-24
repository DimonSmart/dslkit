using System.Text.RegularExpressions;
using DSLKIT.Visualizer.Abstractions;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class GrammarProviderCatalog : IGrammarProviderCatalog
{
    private static readonly Regex ProviderIdRegex = new(
        "^[a-z0-9](?:[a-z0-9._-]{0,127})$",
        RegexOptions.CultureInvariant | RegexOptions.Compiled);

    private readonly Dictionary<string, IDslGrammarProvider> _providersById = new(StringComparer.Ordinal);

    public GrammarProviderCatalog(IEnumerable<IDslGrammarProvider> builtInProviders)
    {
        foreach (var provider in builtInProviders)
        {
            if (!TryAdd(provider, out var errorMessage))
            {
                throw new InvalidOperationException($"Failed to register built-in provider '{provider.GetType().FullName}': {errorMessage}");
            }
        }
    }

    public IReadOnlyList<IDslGrammarProvider> GetAll()
    {
        return _providersById.Values
            .OrderBy(provider => provider.DisplayName, StringComparer.Ordinal)
            .ThenBy(provider => provider.Id, StringComparer.Ordinal)
            .ToList();
    }

    public IDslGrammarProvider? FindById(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            return null;
        }

        _providersById.TryGetValue(providerId, out var provider);
        return provider;
    }

    public bool TryAdd(IDslGrammarProvider provider, out string errorMessage)
    {
        if (provider == null)
        {
            errorMessage = "Provider cannot be null.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(provider.Id))
        {
            errorMessage = "Provider id cannot be empty.";
            return false;
        }

        if (!ProviderIdRegex.IsMatch(provider.Id))
        {
            errorMessage = $"Provider id '{provider.Id}' is invalid. Use lowercase letters, digits, '.', '_' or '-', max length 128.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(provider.DisplayName))
        {
            errorMessage = $"Provider '{provider.Id}' must have a non-empty display name.";
            return false;
        }

        if (provider.ApiVersion != DslGrammarProviderApi.CurrentVersion)
        {
            errorMessage = $"Provider '{provider.Id}' declares API version {provider.ApiVersion}, expected {DslGrammarProviderApi.CurrentVersion}.";
            return false;
        }

        if (_providersById.ContainsKey(provider.Id))
        {
            errorMessage = $"Provider with id '{provider.Id}' already exists.";
            return false;
        }

        _providersById[provider.Id] = provider;
        errorMessage = string.Empty;
        return true;
    }
}
