using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using AboveMe.Services;
using Microsoft.AspNetCore.Components;

namespace AboveMe.Pages
{
    /// <summary>
    /// Code-behind partial class for <see cref="Home"/>. The aurora borealis
    /// feature lives here so the markup file stays compact and easy to scan.
    /// All members share private accessibility with the main @code block in
    /// Home.razor (same partial class), so state fields like
    /// <c>userLatitude</c> / <c>userLongitude</c> continue to be the single
    /// source of truth for location-aware features.
    /// </summary>
    public partial class Home
    {
        // ---- Aurora Borealis state (NOAA SWPC OVATION) --------------------

        private bool showAurora = false;
        private bool isLoadingAurora = false;
        private string AuroraError = string.Empty;
        private AuroraData? auroraData = null;

        // Single-key cache: localStorage never accumulates multiple entries.
        // The in-memory `lastAuroraFetchUtc` records the UTC date of the last
        // successful network fetch so the panel knows when to re-download.
        private const string AuroraRawJsonKey = "auroraFeed_v1:latest";
        private DateTime lastAuroraFetchUtc = DateTime.MinValue;

        /// <summary>
        /// Aurora viewing probability for the user's nearest OVATION grid
        /// point. Cached per UTC day so repeated toggles within the same day
        /// are instant and resilient to transient network blips.
        /// </summary>
        public class AuroraData
        {
            /// <summary>0..100, probability of aurora overhead at the grid point.</summary>
            public int Probability { get; set; }

            /// <summary>Latitude of the nearest grid point chosen.</summary>
            public double GridLat { get; set; }

            /// <summary>Longitude (-180..180) of the nearest grid point.</summary>
            public double GridLon { get; set; }

            /// <summary>NOAA model observation timestamp (UTC). <c>null</c> if NOAA did not return one.</summary>
            public DateTime? ObservationTimeUtc { get; set; }

            /// <summary>NOAA model forecast timestamp (UTC). <c>null</c> if NOAA did not return one.</summary>
            public DateTime? ForecastTimeUtc { get; set; }

            /// <summary>Latitude that was used for lookup (signed).</summary>
            public double UserLat { get; set; }

            /// <summary>Longitude that was used for lookup (signed -180..180).</summary>
            public double UserLon { get; set; }
        }

        // ---- Toggle handler ---------------------------------------------

        /// <summary>
        /// Show / hide the aurora panel. Triggers a fetch on first open,
        /// or reuses the in-memory result if it's already up to date.
        /// </summary>
        async Task ToggleAurora()
        {
            showAurora = !showAurora;
            if (showAurora)
            {
                await FetchAuroraAsync();
            }
        }

        // ---- Fetch + cache ----------------------------------------------

        /// <summary>
        /// Loads NOAA SWPC's OVATION aurora forecast from the open
        /// https://services.swpc.noaa.gov/json/ovation_aurora_latest.json grid
        /// (no API key required; CORS-friendly).
        ///
        /// Caching strategy:
        /// <list type="bullet">
        ///   <item><description>The raw JSON blob is stored at a single
        ///   localStorage key (<c>auroraFeed_v1:latest</c>), so the cache size
        ///   is bounded — no per-day-key accumulation.</description></item>
        ///   <item><description>The in-memory <c>lastAuroraFetchUtc</c>
        ///   records the day of the last successful fetch and forces a
        ///   re-download when the UTC date rolls over.</description></item>
        /// </list>
        /// The network call is wrapped in a 15-second <see cref="CancellationTokenSource"/>
        /// so a hung NOAA response can't leave <c>isLoadingAurora</c> stuck at
        /// <c>true</c> forever.
        /// </summary>
        async Task FetchAuroraAsync()
        {
            if (isLoadingAurora) return;

            // Resolve lat/lon with sensible fallbacks (shared geolocation
            // beats selected city/country).
            (double lat, double lon, string source)? coords = TryResolveAuroraCoordinates();
            if (coords == null)
            {
                auroraData = null;
                AuroraError = "To see aurora viewing probability, share your location or select a country/city above.";
                // Mark today's fetch as resolved so we don't loop on every
                // subsequent component re-render.
                lastAuroraFetchUtc = DateTime.UtcNow.Date;
                StateHasChanged();
                return;
            }

            double userLat = coords.Value.lat;
            double userLon = coords.Value.lon;

            // Try localStorage cached raw grid first.
            string? rawJson = null;
            try
            {
                rawJson = await LocalStorage.GetItemAsync<string>(AuroraRawJsonKey);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"aurora localStorage read failed: {ex.Message}");
            }

            DateTime today = DateTime.UtcNow.Date;
            bool cacheIsFresh = lastAuroraFetchUtc.Date == today && !string.IsNullOrWhiteSpace(rawJson);

            if (!cacheIsFresh)
            {
                isLoadingAurora = true;
                AuroraError = string.Empty;
                StateHasChanged();

                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    const string url = "https://services.swpc.noaa.gov/json/ovation_aurora_latest.json";
                    HttpResponseMessage response = await Http.GetAsync(url, cts.Token);
                    if (!response.IsSuccessStatusCode)
                    {
                        AuroraError = $"NOAA SWPC returned {(int)response.StatusCode}. Try again later.";
                        isLoadingAurora = false;
                        StateHasChanged();
                        return;
                    }

                    rawJson = await response.Content.ReadAsStringAsync();
                    try
                    {
                        await LocalStorage.SetItemAsync(AuroraRawJsonKey, rawJson);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"aurora localStorage write failed (non-fatal): {ex.Message}");
                    }
                    lastAuroraFetchUtc = today;
                }
                catch (OperationCanceledException)
                {
                    AuroraError = "Request to NOAA SWPC timed out. Try again in a few minutes.";
                    isLoadingAurora = false;
                    StateHasChanged();
                    return;
                }
                catch (Exception ex)
                {
                    AuroraError = $"Error loading aurora data: {ex.Message}";
                    isLoadingAurora = false;
                    StateHasChanged();
                    return;
                }
                finally
                {
                    isLoadingAurora = false;
                }
            }

            // Single-pass parse — extraction of Observation/Forecast
            // timestamps plus nearest-grid lookup happen in one walk so the
            // ~150 KB JSON document isn't parsed twice.
            (int probability, double gridLat, double gridLon360, DateTime? obsUtc, DateTime? fcstUtc)?
                resolved = null;
            try
            {
                resolved = ParseAuroraGrid(rawJson!, userLat, userLon);
            }
            catch (Exception ex)
            {
                AuroraError = $"Error parsing aurora data: {ex.Message}";
                StateHasChanged();
                return;
            }

            if (resolved == null)
            {
                auroraData = null;
                AuroraError = "NOAA OVATION model returned no grid points. Try again later.";
                StateHasChanged();
                return;
            }

            auroraData = new AuroraData
            {
                Probability = resolved.Value.probability,
                GridLat = resolved.Value.gridLat,
                GridLon = NormalizeLongitudeSigned(resolved.Value.gridLon360),
                ObservationTimeUtc = resolved.Value.obsUtc,
                ForecastTimeUtc = resolved.Value.fcstUtc,
                UserLat = userLat,
                UserLon = userLon
            };
            AuroraError = string.Empty;
            StateHasChanged();
        }

        // ---- Location resolution ----------------------------------------

        /// <summary>
        /// Pick the most precise coordinates available for aurora resolution.
        /// Order of preference: (1) shared browser geolocation, (2) city
        /// lookup for items in <c>Cities</c> / timezone-derived cities, (3)
        /// country centroid. Returns <c>null</c> when nothing usable can be
        /// derived so the UI can prompt the user to share location.
        /// </summary>
        (double lat, double lon, string source)? TryResolveAuroraCoordinates()
        {
            // 1) Browser geolocation — most accurate.
            if (!string.IsNullOrWhiteSpace(userLatitude) &&
                double.TryParse(userLatitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var geoLat) &&
                !string.IsNullOrWhiteSpace(userLongitude) &&
                double.TryParse(userLongitude, NumberStyles.Float, CultureInfo.InvariantCulture, out var geoLon))
            {
                return (geoLat, geoLon, "geolocation");
            }

            // 2) City lookup — kept tight on purpose and aligned with the
            // city options + timezone→city mapping in Home.razor so this
            // feature doesn't drift from the rest of the app.
            var cityCoords = new Dictionary<string, (double lat, double lon)>(StringComparer.OrdinalIgnoreCase)
            {
                { "New York",      ( 40.7128,  -74.0060) },
                { "Los Angeles",   ( 34.0522, -118.2437) },
                { "Chicago",       ( 41.8781,  -87.6298) },
                { "Toronto",       ( 43.6532,  -79.3832) },
                { "Vancouver",     ( 49.2827, -123.1207) },
                { "Montreal",      ( 45.5019,  -73.5674) },
                { "London",        ( 51.5074,   -0.1278) },
                { "Manchester",    ( 53.4808,   -2.2426) },
                { "Birmingham",    ( 52.4862,   -1.8904) },
                { "Sydney",        (-33.8688, 151.2093)  },
                { "Melbourne",     (-37.8136, 144.9631)  },
                { "Brisbane",      (-27.4698, 153.0251)  },
                { "Beijing",       ( 39.9042, 116.4074)  },
                { "Berlin",        ( 52.5200,  13.4050)  },
                { "Kansas City",   ( 39.0473,  -94.5573) },
                { "Denver",        ( 39.7392, -104.9903) }
            };

            if (!string.IsNullOrWhiteSpace(selectedCity) &&
                cityCoords.TryGetValue(selectedCity, out var cc))
            {
                return (cc.lat, cc.lon, $"city:{selectedCity}");
            }

            // 3) Country centroid — last resort.
            var countryCenters = new Dictionary<string, (double lat, double lon)>(StringComparer.OrdinalIgnoreCase)
            {
                { "United States",  ( 39.8283,  -98.5795) },
                { "Canada",         ( 56.1304, -106.3468) },
                { "United Kingdom", ( 54.0000,   -2.0000) },
                { "Australia",      (-25.2744,  133.7751) },
                { "China",          ( 35.8617,  104.1954) }
            };

            if (!string.IsNullOrWhiteSpace(selectedCountry) &&
                countryCenters.TryGetValue(selectedCountry, out var countryCoord))
            {
                return (countryCoord.lat, countryCoord.lon, $"country:{selectedCountry}");
            }

            return null;
        }

        // ---- Grid lookup ------------------------------------------------

        /// <summary>
        /// Single-pass parse of NOAA's OVATION JSON. Extracts
        /// <c>Observation Time</c> / <c>Forecast Time</c> (nullable when the
        /// fields are missing or unparseable so the UI doesn't lie about when
        /// the data was produced), then walks the <c>coordinates</c> array to
        /// find the [lon, lat, probability] grid point closest to the user.
        /// We normalize the user longitude to 0..360 once and use a wrap-aware
        /// delta for the longitude component so users near the antimeridian
        /// aren't routed across the globe. Squared L2 distance suffices.
        /// </summary>
        (int probability, double gridLat, double gridLon360, DateTime? obsUtc, DateTime? fcstUtc)?
            ParseAuroraGrid(string json, double userLat, double userLonSigned)
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            DateTime? obsUtc = null;
            DateTime? fcstUtc = null;
            if (root.TryGetProperty("Observation Time", out var obsEl) &&
                obsEl.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(obsEl.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var obsParsed))
            {
                obsUtc = obsParsed;
            }
            if (root.TryGetProperty("Forecast Time", out var fcstEl) &&
                fcstEl.ValueKind == JsonValueKind.String &&
                DateTime.TryParse(fcstEl.GetString(), CultureInfo.InvariantCulture,
                    DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var fcstParsed))
            {
                fcstUtc = fcstParsed;
            }

            if (!root.TryGetProperty("coordinates", out var coords) ||
                coords.ValueKind != JsonValueKind.Array)
            {
                return null;
            }

            // Normalize user longitude into NOAA's 0..360 reference frame.
            double userLon360 = userLonSigned < 0 ? userLonSigned + 360.0 : userLonSigned;
            if (userLon360 >= 360.0) userLon360 -= 360.0;

            double bestScore = double.MaxValue;
            int bestProb = 0;
            double bestLat = userLat;
            double bestLon = userLon360;
            int seen = 0;

            foreach (var point in coords.EnumerateArray())
            {
                seen++;
                if (point.ValueKind != JsonValueKind.Array || point.GetArrayLength() < 3) continue;
                if (!point[0].TryGetDouble(out double lon)) continue;
                if (!point[1].TryGetDouble(out double lat)) continue;
                if (!point[2].TryGetDouble(out double prob)) continue;

                double dLon = Math.Abs(userLon360 - lon);
                // Shortest signed distance around the 0/360 longitude seam.
                if (dLon > 180.0) dLon = 360.0 - dLon;

                double dLat = userLat - lat;
                double score = dLat * dLat + dLon * dLon;
                if (score < bestScore)
                {
                    bestScore = score;
                    bestProb = (int)Math.Round(prob);
                    bestLat = lat;
                    bestLon = lon;
                }
            }

            if (seen == 0) return null;
            return (bestProb, bestLat, bestLon, obsUtc, fcstUtc);
        }

        /// <summary>Convert NOAA 0..360 longitude into conventional -180..180 for display.</summary>
        private static double NormalizeLongitudeSigned(double lon360)
            => lon360 > 180.0 ? lon360 - 360.0 : lon360;

        // ---- Presentation helpers ---------------------------------------

        /// <summary>
        /// Friendly label, color-band, and tip for a 0..100 aurora probability.
        /// Thresholds chosen to match NOAA's viewline guidance
        /// (https://www.swpc.noaa.gov/products/aurora-viewline-forecast-and-prediction).
        /// </summary>
        (string label, string color, string tip) GetAuroraLevel(int probability)
        {
            if (probability <= 10)
                return ("Quiet",   "#6c757d", "Aurora activity is low. Best chance is far-northern latitudes on clear, dark nights.");
            if (probability <= 30)
                return ("Minor",   "#0dcaf0", "Minor overhead activity near the auroral oval. Faint on the horizon at high latitudes.");
            if (probability <= 50)
                return ("Moderate","#0d6efd", "Moderate activity. Auroras may be visible low on the horizon at high latitudes.");
            if (probability <= 70)
                return ("Active",  "#6610f2", "Active aurora — likely overhead at high latitudes; visible on the horizon at mid-latitudes.");
            if (probability <= 85)
                return ("Strong",  "#d63384", "Strong activity. Aurora may be visible overhead from mid-latitudes (e.g. northern US, southern Australia).");
            return ("Storm", "#dc3545", "Geomagnetic storm levels. Aurora may be visible from unusually low latitudes — get away from city lights!");
        }

        /// <summary>
        /// Hemisphere-specific viewing direction hint. Northerners look north,
        /// southerners look south. Keeps the guidance clear regardless of the
        /// user's land hemisphere or country.
        /// </summary>
        string GetAuroraViewDirectionHint()
        {
            if (auroraData == null) return string.Empty;
            return auroraData.UserLat >= 0
                ? "Look toward the northern horizon for the best chance of seeing aurora from your latitude."
                : "Look toward the southern horizon for the best chance of seeing aurora from your latitude.";
        }

        // ---- Solar + Lunar Eclipse panel -------------------------------

        /// <summary>DI for the eclipse data service. Solar data lives at USNO, lunar data at the bundled catalog.</summary>
        [Inject] AboveMe.Services.EclipseService EclipseSvc { get; set; } = default!;

        private bool showEclipse = false;
        private bool isLoadingEclipse = false;
        private string EclipseError = string.Empty;
        private AboveMe.Services.EclipseTimeline eclipses = new();

        /// <summary>
        /// Toggles the Eclipse Data panel. On open, fetches the upcoming
        /// solar + lunar timeline. Unlike the aurora/comet flows there's no
        /// coordinate-gating — eclipses are global — so no permission prompt
        /// is required.
        /// </summary>
        async Task ToggleEclipse()
        {
            showEclipse = !showEclipse;
            if (showEclipse)
            {
                await FetchEclipsesAsync();
            }
        }

        /// <summary>
        /// Loads the merged upcoming-eclipse timeline from
        /// <see cref="EclipseSvc"/>, which overlays live USNO solar-year
        /// responses onto the bundled NASA GSFC catalog (sole lunar source +
        /// solar fallback). Network failures are non-fatal: the service
        /// silently falls back to the bundled catalog for solar eclipses.
        /// <para>
        /// The service uses up to three location signals in priority order:
        /// (1) shared browser geolocation (best), (2) timezone id from the
        /// dropdown, (3) country name. With latitude, the panel splits the
        /// timeline into "Visible from your hemisphere" vs. "Elsewhere",
        /// optionally narrowed further by timezone/country so users who
        /// only set a timezone don't get a hemisphere-wide dump of events
        /// halfway across the world.
        /// </para>
        /// </summary>
        async Task FetchEclipsesAsync()
        {
            if (isLoadingEclipse) return;

            isLoadingEclipse = true;
            EclipseError = string.Empty;
            StateHasChanged();

            try
            {
                double? userLat = null;
                if (!string.IsNullOrWhiteSpace(userLatitude) &&
                    double.TryParse(userLatitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLat))
                {
                    userLat = parsedLat;
                }
                double? userLon = null;
                if (!string.IsNullOrWhiteSpace(userLongitude) &&
                    double.TryParse(userLongitude, NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedLon))
                {
                    userLon = parsedLon;
                }

                eclipses = await EclipseSvc.GetUpcomingEclipsesAsync(
                    userLat,
                    userLon,
                    timezoneId: selectedTimezone,
                    countryName: selectedCountry);
                int visibleCount = eclipses?.VisibleFromYourHemisphere?.Count ?? 0;
                int elsewhereCount = eclipses?.Elsewhere?.Count ?? 0;
                if (eclipses == null || (visibleCount == 0 && elsewhereCount == 0))
                {
                    EclipseError = "No upcoming eclipse data available right now. Try again later.";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"eclipses fetch failed: {ex.Message}");
                EclipseError = $"Error loading eclipse data: {ex.Message}";
            }
            finally
            {
                isLoadingEclipse = false;
                StateHasChanged();
            }
        }

        /// <summary>
        /// Single-string render mode the Razor template dispatches on via
        /// <c>@switch (EclipseRenderMode)</c>. Centralizing the conditions
        /// keeps the markup free of multi-line boolean expressions which
        /// were tripping up the Razor parser in the previous implementation.
        /// </summary>
        private string EclipseRenderMode
        {
            get
            {
                if (isLoadingEclipse) return "loading";
                if (!string.IsNullOrEmpty(EclipseError)) return "error";

                int visibleCount = eclipses?.VisibleFromYourHemisphere?.Count ?? 0;
                int elsewhereCount = eclipses?.Elsewhere?.Count ?? 0;
                if (eclipses == null || (visibleCount == 0 && elsewhereCount == 0)) return "empty";

                // The service populates HasLocationHint=true when latitude is
                // supplied; otherwise the timeline is unsplit (everything in
                // VisibleFromYourHemisphere, nowhere in Elsewhere).
                return eclipses!.HasLocationHint ? "split" : "flat";
            }
        }
    }
}
