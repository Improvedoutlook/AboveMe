using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using AboveMe;
using AboveMe.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HttpClient with base address
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Register LocalStorageService for persistent user preferences
builder.Services.AddScoped<LocalStorageService>();

// Register CometService for fetching the curated comet visibility catalog
builder.Services.AddScoped<CometService>();

// Register EclipseService for the combined Solar + Lunar eclipse data panel.
// Live data from USNO AA (solar only) is merged with the bundled NASA GSFC
// catalog (solar fallback + all lunar) in wwwroot/eclipses-catalog.json.
builder.Services.AddScoped<EclipseService>();

// Load environment-specific configuration only in Development
// In Production (GitHub Pages), API keys are injected into appsettings.json by the CI/CD workflow
if (builder.HostEnvironment.IsDevelopment())
{
    using var httpClient = new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) };
    try
    {
        var response = await httpClient.GetAsync("appsettings.Development.json");
        if (response.IsSuccessStatusCode)
        {
            using var stream = await response.Content.ReadAsStreamAsync();
            builder.Configuration.AddJsonStream(stream);
        }
    }
    catch
    {
        // Development config not found, use base appsettings.json
    }
}

await builder.Build().RunAsync();
