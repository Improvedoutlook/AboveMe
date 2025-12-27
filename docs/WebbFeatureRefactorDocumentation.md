# Webb Space Telescope Feature - Implementation Documentation

## Overview
The Webb Space Telescope feature has been refactored to fetch **actual JWST observations** from the official STScI MAST (Mikulski Archive for Space Telescopes) archive instead of using generic NASA image searches.

## What Changed

### Previous Implementation
- Used NASA Images API with keyword searches
- Returned mixed results (sometimes non-telescope content, Hubble images, etc.)
- Required complex filtering to exclude non-relevant images (conferences, people, etc.)
- No guarantee of actual JWST content

### New Implementation
- Uses **STScI MAST Archive API** - the official source for JWST data
- Returns **only** authenticated JWST observations
- Filters for:
  - `obs_collection`: JWST (excludes Hubble and other missions)
  - `dataproduct_type`: image (excludes spectroscopy, catalogs)
  - `calib_level`: 3 (fully processed, science-ready images)
- Provides actual observation metadata (target name, instrument, filters, dates)

## Architecture

### New Service: `JwstImageService`
**Location:** `Services/JwstImageService.cs`

**Key Features:**
- Fetches observations from MAST API
- Implements 24-hour caching (JWST data updates daily)
- Converts FITS file paths to browser-compatible preview images
- Transforms Modified Julian Dates (MJD) to readable DateTime
- Sorts observations by date (newest first)
- Provides both random image selection and full list retrieval

### API Integration

**Endpoint:** `https://mast.stsci.edu/api/v0/invoke`

**Request Structure:**
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

**Response Processing:**
1. Validates response status is "COMPLETE"
2. Extracts observation data array
3. Filters for valid entries (non-null obs_id, proposal_id, target_name)
4. Constructs preview image URLs
5. Converts MJD dates to DateTime
6. Sorts by observation date (descending)

### Image URL Construction

MAST generates preview images for all observations:

- **Thumbnail:** `https://mast.stsci.edu/portal/Download/file/JWST/{proposal_id}/{obs_id}/{obs_id}_thumb.jpg`
- **Preview:** `https://mast.stsci.edu/portal/Download/file/JWST/{proposal_id}/{obs_id}/{obs_id}_preview.jpg`
- **Full Size:** `https://mast.stsci.edu/portal/Download/file/JWST/{proposal_id}/{obs_id}/{obs_id}_i2d.jpg`

**Fallback Strategy:** If preview image fails, automatically tries thumbnail, then full size.

## Data Models

### `JwstImageData` (Display Model)
```csharp
public class JwstImageData
{
    public string ObsId { get; set; }           // Unique observation ID
    public string ProposalId { get; set; }      // Science program ID
    public string TargetName { get; set; }      // Celestial object name
    public string InstrumentName { get; set; }  // NIRCam, MIRI, etc.
    public string Filter { get; set; }          // Filter used (F200W, etc.)
    public DateTime ObservationDate { get; set; } // When observed
    
    public string ThumbnailUrl { get; set; }    // Small preview
    public string PreviewUrl { get; set; }      // Medium preview
    public string FullSizeUrl { get; set; }     // Full resolution
}
```

## UI Updates

### Display Information
The Webb button now shows:
- **Target Name:** e.g., "NGC 3324"
- **Instrument:** e.g., "NIRCam"
- **Filter:** e.g., "F200W"
- **Observation Date:** e.g., "July 12, 2022"
- **Observation ID:** Full unique identifier

### Error Handling
- Network failures are caught and displayed
- Image loading errors trigger automatic fallback
- Empty results show user-friendly message
- Console logging for debugging

### Image Fallback
The `HandleImageError()` method automatically tries:
1. Preview image (default)
2. Thumbnail image (if preview fails)
3. Full size image (if thumbnail fails)
4. Error message (if all fail)

## Configuration

### Service Registration
**File:** `Program.cs`
```csharp
builder.Services.AddScoped<JwstImageService>();
```

### Component Injection
**File:** `Pages/Home.razor`
```csharp
[Inject] JwstImageService JwstService { get; set; } = default!;
```

## Performance Optimizations

1. **Caching:** Results cached for 24 hours
2. **Lazy Loading:** Images only fetched when button clicked
3. **Efficient Filtering:** Server-side filtering via MAST API
4. **Random Selection:** Client-side randomization (no repeated API calls)

## Testing

### Success Criteria ✓
- [x] API returns status "COMPLETE"
- [x] At least 10 valid observations retrieved
- [x] All observations have valid image URLs
- [x] Images display correctly in browser
- [x] No Hubble or non-JWST images in results
- [x] No conference photos or press event images
- [x] Dates within JWST operational period (2022-present)

### Edge Cases Handled
- [x] Empty results array
- [x] Observations without preview images (fallback)
- [x] Malformed obs_id or proposal_id
- [x] Network failures
- [x] Invalid dates (handles DateTime.MinValue)

## Future Enhancements

Potential improvements:
1. **Filter by Target Type:** Allow users to select nebulae, galaxies, planets, etc.
2. **Date Range Selection:** Show observations from specific time periods
3. **Instrument Filter:** Let users choose NIRCam vs MIRI vs NIRSpec
4. **Gallery View:** Show multiple images instead of one random
5. **Deep Links:** Link to full observation details on MAST portal
6. **Download Support:** Allow users to download full-resolution images

## Technical Notes

### Modified Julian Date (MJD) Conversion
Formula: `Unix Timestamp = (MJD - 40587) × 86400 × 1000`

This converts astronomical time format to standard DateTime.

### MAST API Details
- **No authentication required** (public API)
- **No rate limiting** for reasonable use
- **All JWST data is public domain**
- Preview images may take days to generate after observation

### CORS
The MAST API supports cross-origin requests, so no proxy needed in Blazor WASM.

## Resources

- **MAST API Documentation:** https://mast.stsci.edu/api/v0/
- **JWST Mission:** https://webb.nasa.gov/
- **STScI MAST Portal:** https://mast.stsci.edu/

---

## Rollback Information

If you need to revert to the old implementation, the previous code used:
- NASA Images API: `https://images-api.nasa.gov/search`
- Keyword-based searches with exclusion filters
- `WebbData` class instead of `JwstImageData`
- No service layer (direct HTTP calls in component)
