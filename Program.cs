using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AboveMe;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Load environment-specific configuration
// In Development, this will load appsettings.Development.json which contains real API keys
// In Production (GitHub Pages), only appsettings.json with DEMO_KEY will be used
var environment = builder.HostEnvironment.Environment;
using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };

// Try to load environment-specific settings (e.g., appsettings.Development.json)
try
{
    var envConfigUrl = $"appsettings.{environment}.json";
    var response = await httpClient.GetAsync(envConfigUrl);
    if (response.IsSuccessStatusCode)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        builder.Configuration.AddJsonStream(stream);
    }
}
catch
{
    // Environment-specific config not found, use base appsettings.json
}

await builder.Build().RunAsync();
