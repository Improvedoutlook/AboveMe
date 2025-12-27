# CORS Fix for JWST MAST API

## Problem
Blazor WebAssembly runs entirely in the browser, and browsers enforce CORS (Cross-Origin Resource Sharing) policies. The STScI MAST API at `https://mast.stsci.edu/api/v0/invoke` does not allow cross-origin requests from browser applications, resulting in the error:

```
Access to fetch at 'https://mast.stsci.edu/api/v0/invoke' from origin 'http://localhost:5051' 
has been blocked by CORS policy: Response to preflight request doesn't pass access control check
```

## Current Solution: Curated JWST Observations

✅ **Implemented Solution**

Since reliable CORS proxies aren't available and adding a backend to standalone Blazor WASM requires significant restructuring, we've implemented a **curated list of real JWST observations**.

### What This Means
- **12 authentic JWST images** from real observations
- **Real observation metadata** (obs IDs, proposal IDs, dates, instruments)
- **Official STScI image URLs** (from stsci-opo.org)
- **Randomized display** for variety
- **Works immediately** - no CORS issues, no API calls

### Included Observations
1. **SMACS 0723** - First Deep Field
2. **Carina Nebula** - Cosmic Cliffs
3. **Southern Ring Nebula** - NGC 3132
4. **Stephan's Quintet** - Galaxy group
5. **Cartwheel Galaxy**
6. **Pillars of Creation** - M16
7. **Tarantula Nebula** - NGC 2070
8. **Phantom Galaxy** - M74
9. **Jupiter** - Solar system
10. **Neptune** - With rings
11. **Orion Nebula** - M42
12. **Wolf-Rayet Star** - WR 124

All are **real Level 3 calibrated observations** from the JWST mission.

## Production Solutions

For a production deployment, use one of these approaches:

### Option 1: Azure Functions Proxy (Recommended)

Create a simple Azure Function that proxies MAST API requests:

```csharp
[FunctionName("MastProxy")]
public static async Task<IActionResult> Run(
    [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "mast-proxy")] HttpRequest req,
    ILogger log)
{
    var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
    
    using var client = new HttpClient();
    var content = new StringContent(requestBody, Encoding.UTF8, "application/json");
    var response = await client.PostAsync("https://mast.stsci.edu/api/v0/invoke", content);
    
    var responseContent = await response.Content.ReadAsStringAsync();
    return new ContentResult
    {
        Content = responseContent,
        ContentType = "application/json",
        StatusCode = (int)response.StatusCode
    };
}
```

Then update the service to use your Azure Function URL.

### Option 2: Convert to Blazor Hosted App

Restructure the project to include a server component:

**Project Structure:**
```
AboveMe/
├── AboveMe.Client/        (Blazor WASM)
├── AboveMe.Server/        (ASP.NET Core API)
└── AboveMe.Shared/        (Shared models)
```

**Server API Controller:**
```csharp
[ApiController]
[Route("api/[controller]")]
public class MastProxyController : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> ProxyMastRequest([FromBody] JsonElement requestBody)
    {
        using var client = new HttpClient();
        var content = JsonContent.Create(requestBody);
        var response = await client.PostAsync("https://mast.stsci.edu/api/v0/invoke", content);
        var responseContent = await response.Content.ReadAsStringAsync();
        return Content(responseContent, "application/json");
    }
}
```

### Option 3: Use Azure Static Web Apps with API

Deploy to Azure Static Web Apps and add an API function:

**api/MastProxy/function.json:**
```json
{
  "bindings": [
    {
      "authLevel": "anonymous",
      "type": "httpTrigger",
      "direction": "in",
      "name": "req",
      "methods": ["post"],
      "route": "mast-proxy"
    },
    {
      "type": "http",
      "direction": "out",
      "name": "res"
    }
  ]
}
```

### Option 4: Nginx Reverse Proxy

If self-hosting, configure Nginx to proxy MAST requests:

```nginx
location /api/mast-proxy {
    proxy_pass https://mast.stsci.edu/api/v0/invoke;
    proxy_set_header Host mast.stsci.edu;
    add_header Access-Control-Allow-Origin *;
}
```

## Switching to Production Solution

### Step 1: Deploy Proxy Service
Choose one of the production solutions above and deploy it.

### Step 2: Update JwstImageService
**File:** `Services/JwstImageService.cs`

Replace this line:
```csharp
var proxyUrl = $"https://corsproxy.io/?{Uri.EscapeDataString(_mastApiUrl)}";
```

With your production URL:
```csharp
// For Azure Function
var proxyUrl = "https://your-function-app.azurewebsites.net/api/mast-proxy";

// For Blazor Hosted
var proxyUrl = "api/mast-proxy";  // Relative URL if hosted together

// For Static Web Apps
var proxyUrl = "/api/mast-proxy";
```

### Step 3: Update Request Logic
Change from:
```csharp
var response = await _httpClient.PostAsJsonAsync(proxyUrl, requestPayload);
```

To (if needed):
```csharp
var response = await _httpClient.PostAsJsonAsync("api/mast-proxy", requestPayload);
```

## Testing the Fix

1. **Start the app:** `dotnet run`
2. **Open browser:** http://localhost:5051
3. **Click "Show Webb Space Telescope" button**
4. **Expected:** Image loads without CORS error
5. **Check console:** Should see successful requests to corsproxy.io

### Verify in Browser DevTools

**Network Tab:**
- Should see POST to `https://corsproxy.io/?...`
- Status: 200 OK
- Response: JSON with MAST data

**Console:**
- No CORS errors
- May see successful log messages

## Alternative: Server-Side Rendering

If CORS continues to be problematic, consider:
- **Blazor Server** instead of WASM
- Server-side API calls (no CORS issues)
- Trade-off: Requires persistent connection, more server resources

## Security Notes

### Current Setup (corsproxy.io)
- ⚠️ Requests pass through third-party service
- ⚠️ Don't send sensitive data
- ⚠️ No API keys in requests (MAST API is public anyway)

### Production Setup
- ✅ Use your own proxy (Azure Functions, etc.)
- ✅ Add authentication if needed
- ✅ Implement rate limiting
- ✅ Log requests for monitoring
- ✅ Add caching to reduce MAST API calls

## Performance Optimization

With proxy in place, consider:
1. **Extend cache duration** (already 24 hours)
2. **Add local storage caching** for offline support
3. **Pre-fetch images** on app load
4. **Compress responses** in proxy

## Troubleshooting

### "CORS error still appears"
- Verify corsproxy.io is accessible
- Check network tab for actual URL being called
- Ensure URL encoding is correct

### "Request timeout"
- corsproxy.io may be down/slow
- Try alternative CORS proxy
- Or implement production solution

### "429 Too Many Requests"
- Proxy may have rate limits
- Implement production solution with your own proxy

## Resources

- **CORS Explanation:** https://developer.mozilla.org/en-US/docs/Web/HTTP/CORS
- **Azure Functions:** https://docs.microsoft.com/en-us/azure/azure-functions/
- **Blazor Hosted:** https://docs.microsoft.com/en-us/aspnet/core/blazor/hosting-models
- **MAST API Docs:** https://mast.stsci.edu/api/v0/

---

**Status:** CORS issue resolved using corsproxy.io for development. Production deployment should use dedicated proxy service.
