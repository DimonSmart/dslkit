using System.Reflection;
using DSLKIT.Visualizer.Abstractions;
using Microsoft.AspNetCore.Components.Forms;

namespace DSLKIT.Visualizer.App.GrammarProviders;

public sealed class GrammarProviderAssemblyLoader : IGrammarProviderAssemblyLoader
{
    private const long MaxUploadFileSize = 50 * 1024 * 1024;
    private readonly IGrammarProviderCatalog _providerCatalog;

    public GrammarProviderAssemblyLoader(IGrammarProviderCatalog providerCatalog)
    {
        _providerCatalog = providerCatalog;
    }

    public async Task<AssemblyLoadReport> LoadProvidersAsync(
        IReadOnlyList<IBrowserFile> files,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<string>();
        var registeredProviderCount = 0;
        var processedAssemblyFileCount = 0;

        if (files.Count == 0)
        {
            messages.Add("No files were selected.");
            return new AssemblyLoadReport
            {
                SelectedFileCount = files.Count,
                ProcessedAssemblyFileCount = processedAssemblyFileCount,
                RegisteredProviderCount = registeredProviderCount,
                Messages = messages
            };
        }

        foreach (var file in files)
        {
            if (!file.Name.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                messages.Add($"[{file.Name}] Skipped: expected .dll file.");
                continue;
            }

            processedAssemblyFileCount++;

            try
            {
                var assembly = await LoadAssemblyAsync(file, cancellationToken);
                var discoveryResult = DiscoverProviderTypes(assembly);

                foreach (var discoveryMessage in discoveryResult.Messages)
                {
                    messages.Add($"[{file.Name}] {discoveryMessage}");
                }

                if (discoveryResult.ProviderTypes.Count == 0)
                {
                    messages.Add($"[{file.Name}] No IDslGrammarProvider implementations were found.");
                    continue;
                }

                foreach (var providerType in discoveryResult.ProviderTypes)
                {
                    if (!TryCreateProvider(providerType, out var provider, out var createErrorMessage))
                    {
                        messages.Add($"[{file.Name}] Type '{providerType.FullName}': {createErrorMessage}");
                        continue;
                    }

                    if (provider!.ApiVersion != DslGrammarProviderApi.CurrentVersion)
                    {
                        messages.Add(
                            $"[{file.Name}] Type '{providerType.FullName}': unsupported API version {provider.ApiVersion}; expected {DslGrammarProviderApi.CurrentVersion}.");
                        continue;
                    }

                    if (!_providerCatalog.TryAdd(provider, out var addErrorMessage))
                    {
                        messages.Add($"[{file.Name}] Type '{providerType.FullName}': {addErrorMessage}");
                        continue;
                    }

                    registeredProviderCount++;
                    messages.Add(
                        $"[{file.Name}] Registered '{provider.DisplayName}' ({provider.Id}) from type '{providerType.FullName}'.");
                }
            }
            catch (Exception ex)
            {
                messages.Add($"[{file.Name}] Failed to load assembly: {ex.Message}");
            }
        }

        return new AssemblyLoadReport
        {
            SelectedFileCount = files.Count,
            ProcessedAssemblyFileCount = processedAssemblyFileCount,
            RegisteredProviderCount = registeredProviderCount,
            Messages = messages
        };
    }

    private static async Task<Assembly> LoadAssemblyAsync(IBrowserFile file, CancellationToken cancellationToken)
    {
        await using var readStream = file.OpenReadStream(MaxUploadFileSize, cancellationToken);
        using var memoryStream = new MemoryStream();
        await readStream.CopyToAsync(memoryStream, cancellationToken);
        return Assembly.Load(memoryStream.ToArray());
    }

    private static ProviderTypeDiscoveryResult DiscoverProviderTypes(Assembly assembly)
    {
        var providerType = typeof(IDslGrammarProvider);

        try
        {
            var providerTypes = assembly.GetTypes()
                .Where(type => IsConcreteProviderType(type, providerType))
                .ToList();

            return new ProviderTypeDiscoveryResult(providerTypes, []);
        }
        catch (ReflectionTypeLoadException ex)
        {
            var providerTypes = ex.Types
                .Where(type => type != null && IsConcreteProviderType(type, providerType))
                .Cast<Type>()
                .ToList();

            var loaderMessages = ex.LoaderExceptions
                .Where(loaderException => loaderException != null)
                .Select(loaderException => loaderException!.Message)
                .Distinct(StringComparer.Ordinal)
                .Select(message => $"Type load warning: {message}")
                .ToList();

            return new ProviderTypeDiscoveryResult(providerTypes, loaderMessages);
        }
        catch (Exception ex)
        {
            return new ProviderTypeDiscoveryResult([], [$"Failed to inspect assembly types: {ex.Message}"]);
        }
    }

    private static bool TryCreateProvider(
        Type providerType,
        out IDslGrammarProvider? provider,
        out string errorMessage)
    {
        try
        {
            if (providerType.GetConstructor(Type.EmptyTypes) == null)
            {
                provider = null;
                errorMessage = "Public parameterless constructor was not found.";
                return false;
            }

            if (Activator.CreateInstance(providerType) is not IDslGrammarProvider createdProvider)
            {
                provider = null;
                errorMessage = "Type instance does not implement IDslGrammarProvider.";
                return false;
            }

            provider = createdProvider;
            errorMessage = string.Empty;
            return true;
        }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            provider = null;
            errorMessage = $"Constructor failed: {ex.InnerException.Message}";
            return false;
        }
        catch (Exception ex)
        {
            provider = null;
            errorMessage = ex.Message;
            return false;
        }
    }

    private static bool IsConcreteProviderType(Type type, Type providerType)
    {
        return providerType.IsAssignableFrom(type) &&
               !type.IsAbstract &&
               !type.IsInterface;
    }

    private sealed record ProviderTypeDiscoveryResult(
        IReadOnlyList<Type> ProviderTypes,
        IReadOnlyList<string> Messages);
}
