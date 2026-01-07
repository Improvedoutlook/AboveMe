# API Key Configuration

## Overview
API keys for external services (NASA, ipgeolocation) are stored in configuration files to keep them secure and prevent exposure in the browser console.

## Configuration Files

### appsettings.json
Located in `wwwroot/appsettings.json`, this file contains placeholder values for production:

```json
{
  "ApiKeys": {
    "NasaApiKey": "DEMO_KEY",
    "AstronomyApiKey": "DEMO_KEY"
  }
}
```

### appsettings.Development.json
Located in `wwwroot/appsettings.Development.json`, this file contains your actual API keys for local development. **This file is excluded from version control** via `.gitignore`.

```json
{
  "ApiKeys": {
    "NasaApiKey": "your-nasa-api-key-here",
    "AstronomyApiKey": "your-astronomy-api-key-here"
  }
}
```

## How It Works

1. The application loads configuration from `wwwroot/appsettings.json` by default (built into Blazor WebAssembly)
2. In development mode, `appsettings.Development.json` overrides the base settings
3. API keys are injected via `IConfiguration` in components
4. Keys are never logged to the browser console

## Security Best Practices

✅ **DO:**
- Keep actual API keys in `appsettings.Development.json`
- Verify `.gitignore` excludes `appsettings.Development.json`
- Use environment-specific configuration files
- Use "DEMO_KEY" or placeholder values in version-controlled files

❌ **DON'T:**
- Hardcode API keys in source code
- Log API keys to console
- Commit `appsettings.Development.json` to version control
- Expose API keys in client-side code that can be inspected

## Usage in Code

```csharp
[Inject] IConfiguration Configuration { get; set; } = default!;

protected override async Task OnInitializedAsync()
{
    // Read API keys from configuration
    nasaApiKey = Configuration["ApiKeys:NasaApiKey"] ?? "DEMO_KEY";
    astronomyApiKey = Configuration["ApiKeys:AstronomyApiKey"] ?? "DEMO_KEY";
}
```

## Getting API Keys

- **NASA API**: Get a free key at https://api.nasa.gov/
- **ipgeolocation**: Get a free key at https://ipgeolocation.io/

## Deployment

For production deployment (e.g., Azure Static Web Apps):
1. Set API keys as environment variables or in Azure App Configuration
2. Update the build/deployment process to inject values into `appsettings.json`
3. Never expose production API keys in client-side code that can be inspected by users
