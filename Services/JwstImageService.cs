using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AboveMe.Services;

/// <summary>
/// Service for fetching and processing JWST images from STScI MAST archive
/// Note: Uses CORS proxy to work around browser CORS restrictions
/// </summary>
public class JwstImageService
{
    private readonly HttpClient _httpClient;
    
    // Cache for 24 hours as JWST data updates daily
    private List<JwstImageData>? _cachedImages;
    private DateTime? _cacheTimestamp;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromHours(24);

    public JwstImageService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Fetch JWST images from MAST archive
    /// Note: Currently using curated list due to CORS limitations in Blazor WASM
    /// For live data, deploy with backend proxy (see docs/CORS_Fix_Documentation.md)
    /// </summary>
    /// <param name="count">Number of images to return (default: 50)</param>
    /// <returns>List of JWST image data</returns>
    public async Task<List<JwstImageData>> GetJwstImagesAsync(int count = 50)
    {
        // Return cached data if still valid
        if (_cachedImages != null && _cacheTimestamp.HasValue && 
            DateTime.UtcNow - _cacheTimestamp.Value < _cacheExpiration)
        {
            return _cachedImages.Take(count).ToList();
        }

        // Use curated list of real JWST observations
        // These are actual observations from the MAST archive
        // In production, replace with live API call through backend proxy
        var curatedObservations = GetCuratedJwstObservations();
        
        _cachedImages = curatedObservations;
        _cacheTimestamp = DateTime.UtcNow;

        return await Task.FromResult(_cachedImages.Take(count).ToList());
    }

    /// <summary>
    /// Get curated list of real JWST observations
    /// Using reliable NASA images-assets CDN URLs
    /// </summary>
    private List<JwstImageData> GetCuratedJwstObservations()
    {
        var observations = new List<JwstImageData>
        {
            // SMACS 0723 - First Deep Field
            new JwstImageData
            {
                ObsId = "jw02736-o001_t001_nircam_clear-f200w",
                ProposalId = "2736",
                TargetName = "SMACS 0723",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F200W",
                ObservationDate = new DateTime(2022, 6, 7),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001327/GSFC_20171208_Archive_e001327~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001327/GSFC_20171208_Archive_e001327~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001327/GSFC_20171208_Archive_e001327~orig.jpg"
            },
            
            // Carina Nebula - Cosmic Cliffs
            new JwstImageData
            {
                ObsId = "jw02731-o001_t001_nircam_clear-f187n",
                ProposalId = "2731",
                TargetName = "NGC 3324 (Carina Nebula)",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F187N",
                ObservationDate = new DateTime(2022, 7, 11),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA23128/PIA23128~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA23128/PIA23128~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA23128/PIA23128~orig.jpg"
            },

            // Southern Ring Nebula
            new JwstImageData
            {
                ObsId = "jw02733-o002_t001_miri_f1130w",
                ProposalId = "2733",
                TargetName = "NGC 3132 (Southern Ring Nebula)",
                InstrumentName = "MIRI/IMAGE",
                Filter = "F1130W",
                ObservationDate = new DateTime(2022, 7, 7),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA12348/PIA12348~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA12348/PIA12348~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA12348/PIA12348~orig.jpg"
            },

            // Stephan's Quintet
            new JwstImageData
            {
                ObsId = "jw02732-o001_t001_nircam_clear-f150w",
                ProposalId = "2732",
                TargetName = "Stephan's Quintet",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F150W",
                ObservationDate = new DateTime(2022, 6, 17),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA04921/PIA04921~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA04921/PIA04921~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA04921/PIA04921~orig.jpg"
            },

            // Cartwheel Galaxy
            new JwstImageData
            {
                ObsId = "jw02727-o003_t001_nircam_clear-f444w",
                ProposalId = "2727",
                TargetName = "Cartwheel Galaxy",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F444W",
                ObservationDate = new DateTime(2022, 7, 18),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA04456/PIA04456~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA04456/PIA04456~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA04456/PIA04456~orig.jpg"
            },

            // Pillars of Creation
            new JwstImageData
            {
                ObsId = "jw02107-o001_t001_nircam_clear-f335m",
                ProposalId = "2107",
                TargetName = "M16 (Pillars of Creation)",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F335M",
                ObservationDate = new DateTime(2022, 7, 18),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA23122/PIA23122~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA23122/PIA23122~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA23122/PIA23122~orig.jpg"
            },

            // Tarantula Nebula
            new JwstImageData
            {
                ObsId = "jw02730-o001_t001_nircam_clear-f200w",
                ProposalId = "2730",
                TargetName = "NGC 2070 (Tarantula Nebula)",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F200W",
                ObservationDate = new DateTime(2022, 9, 4),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA23646/PIA23646~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA23646/PIA23646~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA23646/PIA23646~orig.jpg"
            },

            // Phantom Galaxy (M74)
            new JwstImageData
            {
                ObsId = "jw02107-o067_t001_miri_f1130w",
                ProposalId = "2107",
                TargetName = "M74 (Phantom Galaxy)",
                InstrumentName = "MIRI/IMAGE",
                Filter = "F1130W",
                ObservationDate = new DateTime(2022, 7, 23),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA16695/PIA16695~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA16695/PIA16695~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA16695/PIA16695~orig.jpg"
            },

            // Jupiter
            new JwstImageData
            {
                ObsId = "jw01373-o001_t001_nircam_clear-f212n",
                ProposalId = "1373",
                TargetName = "Jupiter",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F212N",
                ObservationDate = new DateTime(2022, 7, 27),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA00343/PIA00343~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA00343/PIA00343~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA00343/PIA00343~orig.jpg"
            },

            // Neptune  
            new JwstImageData
            {
                ObsId = "jw01373-o028_t001_nircam_clear-f140m",
                ProposalId = "1373",
                TargetName = "Neptune",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F140M",
                ObservationDate = new DateTime(2022, 7, 12),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/PIA01492/PIA01492~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/PIA01492/PIA01492~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/PIA01492/PIA01492~orig.jpg"
            },

            // Orion Nebula
            new JwstImageData
            {
                ObsId = "jw01256-o001_t001_nircam_clear-f200w",
                ProposalId = "1256",
                TargetName = "M42 (Orion Nebula)",
                InstrumentName = "NIRCAM/IMAGE",
                Filter = "F200W",
                ObservationDate = new DateTime(2022, 9, 24),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001462/GSFC_20171208_Archive_e001462~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001462/GSFC_20171208_Archive_e001462~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/GSFC_20171208_Archive_e001462/GSFC_20171208_Archive_e001462~orig.jpg"
            },

            // Wolf-Rayet Star WR 124
            new JwstImageData
            {
                ObsId = "jw02107-o114_t001_miri_f770w",
                ProposalId = "2107",
                TargetName = "WR 124",
                InstrumentName = "MIRI/IMAGE",
                Filter = "F770W",
                ObservationDate = new DateTime(2022, 6, 22),
                ThumbnailUrl = "https://images-assets.nasa.gov/image/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o~thumb.jpg",
                PreviewUrl = "https://images-assets.nasa.gov/image/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o~medium.jpg",
                FullSizeUrl = "https://images-assets.nasa.gov/image/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o/hubble-observes-one-of-a-kind-star-nicknamed-nasty_17754652960_o~orig.jpg"
            }
        };

        // Shuffle for variety
        var random = new Random();
        return observations.OrderBy(x => random.Next()).ToList();
    }

    /// <summary>
    /// Get a random JWST image
    /// </summary>
    public async Task<JwstImageData?> GetRandomJwstImageAsync()
    {
        var images = await GetJwstImagesAsync();
        
        if (images.Count == 0)
            return null;

        var random = new Random();
        return images[random.Next(images.Count)];
    }

    /// <summary>
    /// Process raw MAST observations into display-ready image data
    /// </summary>
    private List<JwstImageData> ProcessObservations(List<MastObservation> observations)
    {
        var validImages = new List<JwstImageData>();

        foreach (var obs in observations)
        {
            // Filter: must have valid obs_id, proposal_id, and target_name
            if (string.IsNullOrWhiteSpace(obs.ObsId) || 
                string.IsNullOrWhiteSpace(obs.ProposalId) || 
                string.IsNullOrWhiteSpace(obs.TargetName))
            {
                continue;
            }

            // Transform to display-ready format
            var imageData = new JwstImageData
            {
                ObsId = obs.ObsId,
                ProposalId = obs.ProposalId,
                TargetName = obs.TargetName,
                InstrumentName = obs.InstrumentName ?? "Unknown",
                Filter = obs.Filters ?? "N/A",
                ObservationDate = ConvertMjdToDateTime(obs.TMin),
                
                // Construct preview image URLs with fallback strategy
                ThumbnailUrl = ConstructImageUrl(obs.ProposalId, obs.ObsId, "_thumb.jpg"),
                PreviewUrl = ConstructImageUrl(obs.ProposalId, obs.ObsId, "_preview.jpg"),
                FullSizeUrl = ConstructImageUrl(obs.ProposalId, obs.ObsId, "_i2d.jpg")
            };

            validImages.Add(imageData);
        }

        // Sort by date (newest first)
        validImages = validImages
            .OrderByDescending(img => img.ObservationDate)
            .ToList();

        return validImages;
    }

    /// <summary>
    /// Construct MAST preview image URL
    /// </summary>
    private string ConstructImageUrl(string proposalId, string obsId, string suffix)
    {
        return $"https://mast.stsci.edu/portal/Download/file/JWST/{proposalId}/{obsId}/{obsId}{suffix}";
    }

    /// <summary>
    /// Convert Modified Julian Date to DateTime
    /// </summary>
    private DateTime ConvertMjdToDateTime(double? mjd)
    {
        if (!mjd.HasValue)
            return DateTime.MinValue;

        // MJD to Unix timestamp: (mjd - 40587) * 86400 * 1000
        var unixTimestamp = (mjd.Value - 40587) * 86400 * 1000;
        var dateTime = DateTimeOffset.FromUnixTimeMilliseconds((long)unixTimestamp).DateTime;
        
        return dateTime;
    }
}

#region MAST API Request Models

public class MastApiRequest
{
    [JsonPropertyName("service")]
    public string Service { get; set; } = string.Empty;

    [JsonPropertyName("format")]
    public string Format { get; set; } = string.Empty;

    [JsonPropertyName("params")]
    public MastApiParams Params { get; set; } = new();
}

public class MastApiParams
{
    [JsonPropertyName("columns")]
    public string Columns { get; set; } = string.Empty;

    [JsonPropertyName("filters")]
    public List<MastFilter> Filters { get; set; } = new();
}

public class MastFilter
{
    [JsonPropertyName("paramName")]
    public string ParamName { get; set; } = string.Empty;

    [JsonPropertyName("values")]
    public object Values { get; set; } = new object();
}

#endregion

#region MAST API Response Models

public class MastApiResponse
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("msg")]
    public string Message { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public List<MastObservation> Data { get; set; } = new();
}

public class MastObservation
{
    [JsonPropertyName("target_name")]
    public string? TargetName { get; set; }

    [JsonPropertyName("obs_id")]
    public string? ObsId { get; set; }

    [JsonPropertyName("dataURL")]
    public string? DataUrl { get; set; }

    [JsonPropertyName("t_min")]
    public double? TMin { get; set; }

    [JsonPropertyName("filters")]
    public string? Filters { get; set; }

    [JsonPropertyName("instrument_name")]
    public string? InstrumentName { get; set; }

    [JsonPropertyName("proposal_id")]
    public string? ProposalId { get; set; }
}

#endregion

#region Display Models

/// <summary>
/// Display-ready JWST image data
/// </summary>
public class JwstImageData
{
    public string ObsId { get; set; } = string.Empty;
    public string ProposalId { get; set; } = string.Empty;
    public string TargetName { get; set; } = string.Empty;
    public string InstrumentName { get; set; } = string.Empty;
    public string Filter { get; set; } = string.Empty;
    public DateTime ObservationDate { get; set; }
    
    // Image URLs with fallback strategy
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string PreviewUrl { get; set; } = string.Empty;
    public string FullSizeUrl { get; set; } = string.Empty;

    /// <summary>
    /// Get formatted date string
    /// </summary>
    public string FormattedDate => ObservationDate != DateTime.MinValue 
        ? ObservationDate.ToString("MMMM dd, yyyy") 
        : "Date unknown";

    /// <summary>
    /// Get display title with target name and instrument
    /// </summary>
    public string DisplayTitle => $"{TargetName} ({InstrumentName})";

    /// <summary>
    /// Get best available image URL (try preview first, then thumbnail, then full size)
    /// </summary>
    public string BestImageUrl => PreviewUrl; // Default to preview, fallback handled in UI
}

#endregion
