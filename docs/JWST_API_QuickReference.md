# JWST MAST API - Quick Reference

## Service Usage

```csharp
// Inject the service
[Inject] JwstImageService JwstService { get; set; } = default!;

// Get a random JWST image
var image = await JwstService.GetRandomJwstImageAsync();

// Get multiple images
var images = await JwstService.GetJwstImagesAsync(count: 20);
```

## JwstImageData Properties

```csharp
public class JwstImageData
{
    string ObsId             // e.g., "jw02731-o001_t001_nircam_clear-f200w"
    string ProposalId        // e.g., "2731"
    string TargetName        // e.g., "NGC 3324"
    string InstrumentName    // e.g., "NIRCam", "MIRI", "NIRSpec"
    string Filter            // e.g., "F200W", "F444W"
    DateTime ObservationDate // When observed
    
    // Image URLs
    string ThumbnailUrl      // Small preview (~150px)
    string PreviewUrl        // Medium preview (~500px) - DEFAULT
    string FullSizeUrl       // Full resolution (large file)
    
    // Computed properties
    string FormattedDate     // "January 15, 2023"
    string DisplayTitle      // "NGC 3324 (NIRCam)"
    string BestImageUrl      // Returns PreviewUrl (fallback in UI)
}
```

## MAST API Endpoint

**URL:** `https://mast.stsci.edu/api/v0/invoke`  
**Method:** POST  
**Auth:** None (public API)

## Request Payload

```json
{
  "service": "Mast.Caom.Filtered.Jwst",
  "format": "json",
  "params": {
    "columns": "target_name,obs_id,dataURL,t_min,filters,instrument_name,proposal_id",
    "filters": [
      { "paramName": "obs_collection", "values": ["JWST"] },
      { "paramName": "dataproduct_type", "values": ["image"] },
      { "paramName": "calib_level", "values": [3] }
    ]
  }
}
```

## Image URL Pattern

```
https://mast.stsci.edu/portal/Download/file/JWST/{proposal_id}/{obs_id}/{obs_id}_{size}.jpg

Sizes:
- _thumb.jpg    (thumbnail)
- _preview.jpg  (medium - recommended)
- _i2d.jpg      (full resolution)
```

## Common JWST Instruments

- **NIRCam** - Near Infrared Camera (most common)
- **MIRI** - Mid-Infrared Instrument
- **NIRSpec** - Near Infrared Spectrograph
- **NIRISS** - Near Infrared Imager and Slitless Spectrograph
- **FGS** - Fine Guidance Sensor

## Common Filters

- **F090W, F115W, F150W, F200W** - Near-IR wide band
- **F277W, F356W, F444W** - Mid-IR wide band
- **F560W, F770W, F1000W, F1130W, F1280W** - Mid-IR (MIRI)

## Calibration Levels

- **Level 1** - Raw, uncalibrated
- **Level 2** - Calibrated, single exposure
- **Level 3** - Mosaic, combined exposures (✅ **We use this**)

## Modified Julian Date Conversion

```csharp
// MJD to DateTime
double mjd = 59812.5234;
var unixTimestamp = (mjd - 40587) * 86400 * 1000;
var dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)unixTimestamp).DateTime;

// Reference: MJD 40587 = Unix epoch (Jan 1, 1970)
```

## Error Handling

```csharp
try
{
    var image = await JwstService.GetRandomJwstImageAsync();
    if (image == null)
    {
        // No images found
    }
}
catch (HttpRequestException ex)
{
    // Network error
}
catch (Exception ex)
{
    // Other error
}
```

## Caching Behavior

- **Duration:** 24 hours
- **Scope:** Per service instance (scoped lifetime)
- **Clearing:** Restart app or wait for expiration

## Performance Tips

1. **Prefer `GetRandomJwstImageAsync()`** - Uses cache, no repeated API calls
2. **Don't call `GetJwstImagesAsync()` repeatedly** - Results are cached
3. **Cache is automatic** - No manual cache management needed
4. **Image URLs are static** - Safe to cache/reuse

## Troubleshooting

### "No JWST images found"
- MAST API returned empty result
- Very rare - thousands of observations available
- Check API status at https://mast.stsci.edu

### Image 404 Error
- Preview not yet generated
- Use fallback: thumbnail or full size
- Implemented automatically in UI

### Slow First Load
- Normal - API call takes 2-3 seconds
- Subsequent loads use cache (<1 sec)

### CORS Error
- MAST API supports CORS
- Should not occur in Blazor WASM
- Check browser console for details

## JWST Mission Timeline

- **Launch:** December 25, 2021
- **First Images:** July 12, 2022
- **Observations:** All dates should be 2022 or later
- **If seeing earlier dates:** Likely test/calibration data

## Resources

- **MAST Portal:** https://mast.stsci.edu
- **MAST API Docs:** https://mast.stsci.edu/api/v0/
- **JWST Mission:** https://webb.nasa.gov
- **Our Docs:** See `docs/WebbFeatureRefactorDocumentation.md`

---

**Quick Copy-Paste Test:**

```csharp
// In Home.razor @code block
private async Task TestJwstService()
{
    var image = await JwstService.GetRandomJwstImageAsync();
    Console.WriteLine($"Target: {image.TargetName}");
    Console.WriteLine($"Instrument: {image.InstrumentName}");
    Console.WriteLine($"Date: {image.FormattedDate}");
    Console.WriteLine($"Image: {image.PreviewUrl}");
}
```
