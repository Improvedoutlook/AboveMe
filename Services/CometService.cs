using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json.Serialization;

namespace AboveMe.Services
{
    /// <summary>
    /// Loads the curated comet visibility forecast catalog
    /// (comets-forecasts.json) and returns entries filtered by
    /// the user's latitude.
    ///
    /// Forecasts are pre-computed from JPL Horizons orbital
    /// + ephemeris data and shipped with the app. They can be
    /// refreshed quarterly by the project maintainer without
    /// requiring any runtime API call, which avoids CORS issues
    /// and third-party rate-limit dependencies.
    ///
    /// Optional live Horizons calls could be added on top
    /// (e.g., via corsproxy.io) to enrich altitudes at runtime.
    ///
    /// COBS (Comet Observation Database, https://cobs.si/) was
    /// investigated as a secondary source for community-reported
    /// actual magnitudes but is deferred: their public REST
    /// endpoints are inconsistent / registration-gated. Revisit
    /// if a stable, CORS-enabled endpoint becomes available.
    /// </summary>
    public class CometService
    {
        private const string CatalogPath = "comets-forecasts.json";
        private readonly HttpClient _http;
        private List<CometForecast>? _catalogCache;

        public CometService(HttpClient http)
        {
            _http = http;
        }

        /// <summary>
        /// Returns the raw catalog (all comets, regardless of
        /// location). Memoized in-memory for the session.
        /// </summary>
        public async Task<List<CometForecast>> GetCatalogAsync()
        {
            if (_catalogCache != null)
            {
                return _catalogCache;
            }

            try
            {
                var result = await _http.GetFromJsonAsync<List<CometForecast>>(CatalogPath);
                _catalogCache = result ?? new List<CometForecast>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"comet catalog load failed: {ex.Message}");
                _catalogCache = new List<CometForecast>();
            }

            return _catalogCache;
        }

        /// <summary>
        /// Returns forecasts the user can plausibly see from
        /// <paramref name="latitude"/>. If latitude is null,
        /// returns the full catalog sorted by closeness to the
        /// current UTC date so the strongest upcoming / recently
        /// past comets surface first.
        /// </summary>
        /// <remarks>
        /// Filtering uses each forecast's <c>MinLatitudeVisible</c>
        /// and <c>MaxLatitudeVisible</c> range rather than a
        /// binary "Northern/Southern Hemisphere" enum \u2014 a much
        /// more accurate heuristic for comets that may only be
        /// observable from a latitude band rather than a full
        /// hemisphere.
        /// </remarks>
        public async Task<List<CometForecast>> GetVisibleForecastsAsync(double? latitude)
        {
            var catalog = await GetCatalogAsync();
            var today = DateTime.UtcNow.Date;

            IEnumerable<CometForecast> filtered = catalog;

            if (latitude.HasValue)
            {
                double lat = latitude.Value;
                filtered = catalog.Where(c =>
                    lat >= c.MinLatitudeVisible &&
                    lat <= c.MaxLatitudeVisible);
            }

            return filtered
                // Sort by closeness to today's date (upcoming
                // first, then recently past, then distant future).
                .OrderBy(c => Math.Abs((c.PeakWindowStart - today).TotalDays))
                .ToList();
        }
    }

    /// <summary>
    /// One pre-computed comet visibility forecast entry.
    /// Shape mirrors <c>wwwroot/comets-forecasts.json</c>.
    /// </summary>
    public class CometForecast
    {
        [JsonPropertyName("designation")]
        public string Designation { get; set; } = string.Empty;

        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("jpl_command")]
        public string JplCommand { get; set; } = string.Empty;

        [JsonPropertyName("perihelion_date")]
        public DateTime PerihelionDate { get; set; }

        [JsonPropertyName("peak_window_start")]
        public DateTime PeakWindowStart { get; set; }

        [JsonPropertyName("peak_window_end")]
        public DateTime PeakWindowEnd { get; set; }

        [JsonPropertyName("peak_magnitude")]
        public double PeakMagnitude { get; set; }

        [JsonPropertyName("min_latitude_visible")]
        public double MinLatitudeVisible { get; set; } = -90;

        [JsonPropertyName("max_latitude_visible")]
        public double MaxLatitudeVisible { get; set; } = 90;

        /// <summary>
        /// One of: "Past", "Active", "Upcoming".
        /// </summary>
        [JsonPropertyName("status")]
        public string Status { get; set; } = string.Empty;

        [JsonPropertyName("summary")]
        public string Summary { get; set; } = string.Empty;

        [JsonPropertyName("notes")]
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Dynamically derived visibility status based on the current UTC date:
        ///   • "Upcoming" — peak window hasn't started yet
        ///   • "Active"   — within peak window or up to 30 days past (still
        ///                  observable / fading)
        ///   • "Past"     — peak window ended more than 30 days ago
        /// Used by the UI instead of the cached <see cref="Status"/> so the
        /// badge stays accurate as time advances.
        /// </summary>
        [JsonIgnore]
        public string DisplayStatus
        {
            get
            {
                var today = DateTime.UtcNow.Date;
                var graceWindowEnd = PeakWindowEnd.AddDays(30);
                if (today > graceWindowEnd) return "Past";
                if (today >= PeakWindowStart) return "Active";
                return "Upcoming";
            }
        }
    }
}
