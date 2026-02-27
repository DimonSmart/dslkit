using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using DSLKIT.Visualizer.Abstractions;
using DSLKIT.Visualizer.App;
using DSLKIT.Visualizer.App.GrammarProviders;
using DSLKIT.Visualizer.App.Visualization;
using MudBlazor.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();
builder.Services.AddSingleton<IDslGrammarProvider, ExpressionGrammarProvider>();
builder.Services.AddSingleton<IDslGrammarProvider, IniGrammarProvider>();
builder.Services.AddSingleton<IDslGrammarProvider, SJacksonGrammarProvider>();
builder.Services.AddSingleton<IGrammarProviderCatalog, GrammarProviderCatalog>();
builder.Services.AddSingleton<IGrammarProviderAssemblyLoader, GrammarProviderAssemblyLoader>();
builder.Services.AddSingleton<IGrammarSnapshotMapper, GrammarSnapshotMapper>();

await builder.Build().RunAsync();
