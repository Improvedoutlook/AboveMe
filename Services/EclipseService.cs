using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AboveMe.Services
{
    /// <summary>
    /// Builds the combined Solar + Lunar eclipse event timeline shown in the
    /// "Eclipse Data" panel.
    ///
    /// Live data source: <c>https://aa.usno.navy.mil/api/eclipses/solar/year?year=YYYY</c>
    /// (USNO Astronomical Applications API, v4.0.1). No key, no rate limit, and
    /// CORS-enabled (<c>Access-Control-Allow-Origin: *</c> confirmed via response
    /// inspection on 10 Jul 2026). The per-year endpoint gives us the
    /// authoritative date + descriptive title for every solar eclipse within
    /// the year.
    ///
    /// Static source: <c>wwwroot/eclipses-catalog.json</c>. A curated catalog
    /// of upcoming solar + lunar eclipses (2026–2035) derived from the NASA
    /// GSFC Five Millennium Canon (Fred Espenak). Used for two purposes:
    ///   1. The sole source of upcoming <strong>lunar</strong> eclipse data,
    ///      because the USNO API does not expose a documented REST endpoint
    ///      for lunar eclipses (the form-based Lunar Eclipse Computer is
    ///      not programmatically accessible).
    ///   2. A defense-in-depth fallback for solar eclipses if the USNO
    ///      endpoint is unreachable or has been retired (USNO was deprecated
    ///      once in late 2023 before being restored).
    ///
    /// Caching: each per-year USNO response is cached in <c>localStorage</c>
    /// under <c>eclipseSolar_v1:{year}</c>. Each cached entry stores the raw
    /// JSON string the API returned. Cache is checked first; if absent (or
    /// forced-refresh), the live call is made with a 15-second timeout and
    /// the response is persisted back to localStorage. Solar-eclipse lists
    /// for a given calendar year do not change once published, so the cache
    /// has no time-to-live; it is only invalidated manually via the
    /// <c>v1</c> key-version bump if the USNO schema ever changes.
    /// </summary>
    public class EclipseService
    {
        private const string CatalogPath = "eclipses-catalog.json";
        private const string SolarCacheKeyPrefix = "eclipseSolar_v1:";
        private const int EarthEclipseNetworkTimeoutSeconds = 15;

        private readonly HttpClient _http;
        private readonly LocalStorageService _localStorage;
        private List<EclipseEvent>? _catalogCache;

        public EclipseService(HttpClient http, LocalStorageService localStorage)
        {
            _http = http;
            _localStorage = localStorage;
        }

        /// <summary>
        /// Case-insensitive substring tokens that, when found anywhere in an
        /// <see cref="EclipseEvent.RegionHint"/>, imply the eclipse is best
        /// viewed from the Northern Hemisphere. Stays focused on the phrases
        /// that actually appear in the curated catalog so we don't over-match.
        /// </summary>
        private static readonly string[] NorthernKeywords = new[]
        {
            "north america", "central america", "northern", "arctic", "alaska",
            "canada", "greenland", "iceland", "scandinavia", "russia", "europe",
            "asia", "middle east", "united kingdom", "china", "japan", "india",
            "korea", "spain", "portugal", "morocco", "algeria", "tunisia",
            "libya", "egypt", "saudi arabia", "mexico", "iran", "iraq",
            "turkey", "pakistan", "mediterranean", "caribbean"
        };

        /// <summary>
        /// Mirror of <see cref="NorthernKeywords"/> for Southern Hemisphere
        /// visibility hints. Keeping both lists explicit (rather than
        /// negative-keywords) makes the matching logic obvious to the next
        /// reader and easy to extend when the catalog grows.
        /// </summary>
        private static readonly string[] SouthernKeywords = new[]
        {
            "south america", "southern", "antarctica", "australia",
            "new zealand", "south africa", "south pacific",
            "argentina", "chile", "southern indian ocean"
        };

        /// <summary>
        /// Tighter sub-region matchers keyed by <c>selectedTimezone</c> (the
        /// options in <c>Home.razor</c>'s Timezones list). When geolocation
        /// is unavailable the user still picks a timezone, so we use this
        /// map as a secondary filter inside an already-hemisphere-correct
        /// bucket. Each value is matched as a case-insensitive substring
        /// against <see cref="EclipseEvent.RegionHint"/>.
        /// </summary>
        private static readonly Dictionary<string, string[]> TimezoneRegionKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            // North American zones — favor Americas + Atlantic + the
            // transatlantic viewable areas that share a working-day clock.
            ["Eastern Standard Time"]     = new[] { "north america", "europe", "atlantic", "south america" },
            ["Central Standard Time"]     = new[] { "north america", "europe", "atlantic", "south america" },
            ["Mountain Standard Time"]    = new[] { "north america", "europe" },
            ["Pacific Standard Time"]     = new[] { "north america", "europe" },
            // Greenwich — the listed catch-all for Atlantic + Western Europe.
            ["Greenwich Mean Time"]       = new[] { "europe", "africa", "atlantic" },
            // Central Europe — extends east to the Middle East.
            ["Central European Time"]    = new[] { "europe", "africa", "middle east", "asia" },
            // Australian East Time — Southern Hemisphere primary, with a
            // wide asymmetric footprint (NZ, Pacific, parts of Asia).
            ["Australian Eastern Time"]   = new[] { "australia", "new zealand", "south pacific", "indonesia" },
            // China Standard Time — wide Asia coverage.
            ["China Standard Time"]       = new[] { "china", "asia", "india", "indonesia", "japan", "korea" }
        };

        /// <summary>
        /// Country-level fallback filter to use when there's no timezone
        /// selection (or it's missing from <see cref="TimezoneRegionKeywords"/>).
        /// Mirrors the Country options in the Home.razor dropdown.
        /// </summary>
        private static readonly Dictionary<string, string[]> CountryRegionKeywords = new(StringComparer.OrdinalIgnoreCase)
        {
            ["United States"]  = new[] { "north america", "europe", "atlantic" },
            ["Canada"]         = new[] { "north america", "europe", "arctic", "atlantic" },
            ["United Kingdom"] = new[] { "united kingdom", "europe", "africa", "atlantic" },
            ["Australia"]      = new[] { "australia", "new zealand", "south pacific", "indonesia" },
            ["China"]          = new[] { "china", "asia", "india", "indonesia", "japan", "korea" }
        };

        // ---- Public API -----------------------------------------------

        /// <summary>
        /// Returns the merged, future-only list of upcoming solar + lunar
        /// eclipses starting today (UTC), sorted by date ascending — and
        /// splits the result into <c>VisibleFromYourHemisphere</c> vs.
        /// <c>Elsewhere</c> when the caller supplies a usable
        /// <paramref name="userLatitude"/>.
        /// <para>
        /// Live USNO solar-year responses are fetched in parallel with the
        /// bundled catalog and overlaid onto matching solar rows so the user
        /// gets the precise USNO phrasing whenever the live endpoint succeeds.
        /// </para>
        /// <para>
        /// The split is a substring match against <see cref="EclipseEvent.RegionHint"/>
        /// — Northern keywording wins ties, mixed regions default to the user's
        /// hemisphere (because they're at least partially visible from there),
        /// and ambiguous/empty hints default to the user's hemisphere for the
        /// same reason. This is a lightweight heuristic, not a precise
        /// eclipse-visibility calculation; users at equatorial latitudes may
        /// see overlap by design.
        /// </para>
        /// </summary>
        /// <param name="userLatitude">
        /// Signed latitude from the browser geolocation API. <c>+ve</c> is
        /// treated as Northern, <c>-ve</c> as Southern, and <c>null</c> means
        /// we have no location hint so the timeline is returned as a single
        /// unsplit list (HasLocationHint = false).
        /// </param>
        /// <param name="timezoneId">
        /// Optional Win-style timezone id (matches the values in
        /// <see cref="TimezoneRegionKeywords"/>) used as a SECONDARY filter
        /// when geolocation isn't shared. Picks a tighter sub-region so
        /// the "Visible from your hemisphere" list reflects where the user
        /// actually is — not the whole hemisphere. Falls back to country.
        /// </param>
        /// <param name="countryName">
        /// Optional country selection. Used only if <paramref name="timezoneId"/>
        /// is null or not in <see cref="TimezoneRegionKeywords"/>.
        /// </param>
        /// <param name="userLongitude">
        /// Signed longitude (-180..180) from the browser geolocation API. Required
        /// only for the per-location "local circumstances" overlay (USNO
        /// <c>/solar/date</c>); safe to leave <c>null</c> for users who share
        /// region/timezone but not coordinates — the panel will gracefully fall
        /// back to the bundled <c>region_hint</c> text for the affected events.
        /// </param>
        public async Task<EclipseTimeline> GetUpcomingEclipsesAsync(
            double? userLatitude = null,
            double? userLongitude = null,
            string? timezoneId = null,
            string? countryName = null)
        {
            var catalog = await GetCatalogAsync();
            var today = DateTime.UtcNow.Date;

            // Take everything ≥ today, sort by event date ascending. Cap at
            // 60 entries so the UI doesn't blow up if the catalog grows.
            var future = catalog
                .Where(e => e.Date.Date >= today)
                .OrderBy(e => e.Date.Date)
                .Take(60)
                .ToList();

            // Pull USNO solar list for this year and next year, in parallel,
            // and overlay its descriptive titles onto matching solar rows
            // (matched by Y/M/D) so we surface USNO's authoritative phrasing.
            var overlayTasks = new List<Task<UsnoSolarYearResponse?>>();
            int currentYear = today.Year;
            int lookupSpan = 2;
            for (int y = currentYear; y <= currentYear + lookupSpan; y++)
            {
                overlayTasks.Add(FetchSolarYearAsync(y));
            }

            var yearPayloads = await Task.WhenAll(overlayTasks);
            var overlays = yearPayloads
                .Where(p => p != null)
                .SelectMany(p => p!.EclipsesInYear ?? new List<UsnoSolarEclipse>())
                .ToList();

            foreach (var solar in future.Where(e => e.Type.Equals("Solar", StringComparison.OrdinalIgnoreCase)))
            {
                UsnoSolarEclipse? match = overlays.FirstOrDefault(o =>
                    o.Year == solar.Date.Year &&
                    o.Month == solar.Date.Month &&
                    o.Day == solar.Date.Day);
                if (match != null && !string.IsNullOrWhiteSpace(match.Event))
                {
                    solar.Title = match.Event;
                    solar.Source = "USNO";
                }
            }

            string[]? regionFilter = ResolveRegionFilter(userLatitude, timezoneId, countryName);
            var timeline = SplitByHemisphere(future, userLatitude, regionFilter);

            // Sole source of "M:% obscuration from your location @ …
            // UT": fires one USNO per-location call per visible solar eclipse
            // (skips lunar + opposite-hemisphere events) so the panel only
            // surfaces overlay data the user can plausibly act on. Best-effort
            // overlay — failures collapse silently to <c>LocationOverlay = null</c>
            // so the UI falls back to <see cref="EclipseEvent.RegionHint"/>.
            await OverlayLocalCircumstancesAsync(timeline, userLatitude, userLongitude);

            return timeline;
        }

        /// <summary>
        /// Per-event USNO <c>/solar/date</c> overlay for solar eclipses that
        /// landed in <see cref="EclipseTimeline.VisibleFromYourHemisphere"/>
        /// AND are within the next 10 years (USNO's per-date parser is
        /// unreliable past roughly 2027; skipping events we already know will
        /// 500 keeps the panel responsive). Lunar events and "Elsewhere" solar
        /// events are deliberately skipped — the per-date math gives 0%
        /// obscuration for everyone outside the path, so there's no useful
        /// signal to surface. Parallel-capped at 3 in-flight requests to be
        /// courteous to the USNO server while keeping cold-cache latency bounded.
        /// </summary>
        async Task OverlayLocalCircumstancesAsync(
            EclipseTimeline timeline,
            double? userLat,
            double? userLon)
        {
            if (!userLat.HasValue || !userLon.HasValue) return;

            int maxYear = DateTime.UtcNow.Year + 10;
            var targets = timeline.VisibleFromYourHemisphere
                .Where(e => e.Type.Equals("Solar", StringComparison.OrdinalIgnoreCase) &&
                            e.Date.Year <= maxYear)
                .ToList();
            if (targets.Count == 0) return;

            // `Parallel.ForEachAsync` matches the .NET 9 idiom and replaces the
            // earlier SemaphoreSlim+Task.WhenAll boilerplate. MaxDegreeOfParallelism
            // caps in-flight calls at 3 so we stay courteous to USNO while
            // staying fast on cold cache miss. We forward the lambda's
            // CancellationToken to FetchSolarDateAsync so a user-initiated
            // cancellation (panel collapse mid-fetch) actually short-circuits
            // the in-flight requests instead of waiting the full 15s.
            await Parallel.ForEachAsync(
                targets,
                new ParallelOptions { MaxDegreeOfParallelism = 3 },
                async (e, cancellationToken) =>
                {
                    e.LocationOverlay = await FetchSolarDateAsync(
                        e.Date.Year, e.Date.Month, e.Date.Day,
                        userLat.Value, userLon.Value,
                        cancellationToken);
                });
        }

        /// <summary>
        /// Buckets a flat sorted timeline into the user's-hemisphere-visible
        /// vs. elsewhere splits when a latitude is supplied. See
        /// <see cref="GetUpcomingEclipsesAsync"/> for the matching rules.
        /// <para>
        /// When a secondary <paramref name="regionFilter"/> is supplied
        /// (from the timezone / country lookup), it further narrows the
        /// visible bucket so users who picked e.g. <c>Pacific Standard Time</c>
        /// don't get Northern Hemisphere entries from Asia they can't see.
        /// Events that don't match the filter but were hemisphere-visible
        /// flow into <see cref="EclipseTimeline.Elsewhere"/>.
        /// </para>
        /// </summary>
        EclipseTimeline SplitByHemisphere(List<EclipseEvent> events, double? userLatitude, string[]? regionFilter)
        {
            if (!userLatitude.HasValue)
            {
                // No hemisphere split; render the unsplit list. If a
                // filter is supplied, narrow it so the panel still gives
                // a "near you" feel when only timezone/country is known.
                if (regionFilter != null && regionFilter.Length > 0)
                {
                    return new EclipseTimeline
                    {
                        HasLocationHint = false,
                        Hemisphere = "Selected region",
                        VisibleFromYourHemisphere = events
                            .Where(e => RegionMatchesAny(e.RegionHint, regionFilter))
                            .ToList(),
                        Elsewhere = events
                            .Where(e => !RegionMatchesAny(e.RegionHint, regionFilter))
                            .ToList()
                    };
                }
                return new EclipseTimeline
                {
                    HasLocationHint = false,
                    Hemisphere = null,
                    VisibleFromYourHemisphere = events,
                    Elsewhere = new List<EclipseEvent>()
                };
            }

            bool isNorthern = userLatitude.Value > 0;
            var visible = new List<EclipseEvent>();
            var elsewhere = new List<EclipseEvent>();
            foreach (var e in events)
            {
                bool hasNorthern = RegionMatchesAny(e.RegionHint, NorthernKeywords);
                bool hasSouthern = RegionMatchesAny(e.RegionHint, SouthernKeywords);

                // Decision rule (see class summary for rationale):
                //   - explicit Northern-only  -> only Northern users see "Visible"
                //   - explicit Southern-only  -> only Southern users see "Visible"
                //   - mixed or ambiguous hits -> default to the user's hemisphere
                //     (at least one bucket will see this eclipse, so we lean in)
                bool isNorthernVisible = hasNorthern || !hasSouthern;
                bool isSouthernVisible = hasSouthern || !hasNorthern;
                bool visibleHere = isNorthern ? isNorthernVisible : isSouthernVisible;
                if (!visibleHere)
                {
                    elsewhere.Add(e);
                    continue;
                }

                // Secondary regional filter (from timezone or country).
                // Only narrows the visible bucket — never widens it.
                if (regionFilter != null && regionFilter.Length > 0 &&
                    !RegionMatchesAny(e.RegionHint, regionFilter))
                {
                    elsewhere.Add(e);
                    continue;
                }

                visible.Add(e);
            }

            return new EclipseTimeline
            {
                HasLocationHint = true,
                Hemisphere = isNorthern ? "Northern" : "Southern",
                VisibleFromYourHemisphere = visible,
                Elsewhere = elsewhere
            };
        }

        /// <summary>
        /// Picks the secondary region-keyword set to apply on top of the
        /// hemisphere split. Returns <c>null</c> when we should fall through
        /// (no user signal at all). Order of preference: explicit timezone
        /// (best signal), explicit country (middle signal), then hemisphere
        /// default (no-op when both are unset, North America when only
        /// latitude is available and Northern, Oceania when Southern).
        /// </summary>
        string[]? ResolveRegionFilter(double? userLatitude, string? timezoneId, string? countryName)
        {
            if (!string.IsNullOrWhiteSpace(timezoneId) &&
                TimezoneRegionKeywords.TryGetValue(timezoneId, out var tzKw))
            {
                return tzKw;
            }
            if (!string.IsNullOrWhiteSpace(countryName) &&
                CountryRegionKeywords.TryGetValue(countryName, out var cKw))
            {
                return cKw;
            }
            // Fall back to a hemisphere-style default so even an unset
            // timezone/country user gets a slightly tighter list than
            // the whole hemisphere — only Northern Hemisphere cases
            // get a default filter; Southern Hemisphere expansion
            // would otherwise exclude every visible event.
            if (userLatitude.HasValue && userLatitude.Value >= 0)
            {
                return TimezoneRegionKeywords["Eastern Standard Time"];
            }
            return null;
        }

        /// <summary>
        /// True if <paramref name="region"/> contains any whitespace-
        /// insensitive substring from <paramref name="keywords"/>. Empty
        /// or whitespace-only text never matches. Case-insensitive so the
        /// catalog (which uses Title Case) and any future lower-case
        /// overlays work the same way.
        /// </summary>
        static bool RegionMatchesAny(string? region, string[] keywords)
        {
            if (string.IsNullOrWhiteSpace(region)) return false;
            foreach (var k in keywords)
            {
                if (region.IndexOf(k, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Loads the bundled eclipses catalog from <c>wwwroot/eclipses-catalog.json</c>.
        /// Result is memoized in memory for the session so repeat toggles
        /// don't re-fetch the file. Errors collapse to an empty list rather
        /// than throwing — the UI handles "no events" gracefully.
        /// </summary>
        public async Task<List<EclipseEvent>> GetCatalogAsync()
        {
            if (_catalogCache != null)
            {
                return _catalogCache;
            }

            try
            {
                var result = await _http.GetFromJsonAsync<List<EclipseEvent>>(CatalogPath);
                _catalogCache = result ?? new List<EclipseEvent>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"eclipses catalog load failed: {ex.Message}");
                _catalogCache = new List<EclipseEvent>();
            }

            return _catalogCache;
        }

        // ---- USNO live fetch -----------------------------------------

        /// <summary>
        /// Fetches the USNO <c>year</c> endpoint for the requested year.
        /// Attempts <c>localStorage</c> cache first; on miss, calls the live
        /// API with a 15-second timeout and stores the raw JSON for next time.
        /// Returns <c>null</c> if the call fails or USNO returns anything
        /// other than a 200 OK with a parseable body — callers use this as a
        /// pure additive overlay, never as a blocking requirement.
        /// </summary>
        async Task<UsnoSolarYearResponse?> FetchSolarYearAsync(int year)
        {
            string cacheKey = SolarCacheKeyPrefix + year;

            try
            {
                string? cachedJson = await _localStorage.GetItemAsync<string>(cacheKey);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<UsnoSolarYearResponse>(cachedJson);
                    if (cached != null)
                    {
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"eclipses localStorage read failed ({year}): {ex.Message}");
            }

            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(EarthEclipseNetworkTimeoutSeconds));
                string url = $"https://aa.usno.navy.mil/api/eclipses/solar/year?year={year}&height=0";
                HttpResponseMessage response = await _http.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"USNO returned {(int)response.StatusCode} for {year}. Falling back to bundled catalog.");
                    return null;
                }

                string rawJson = await response.Content.ReadAsStringAsync();
                var parsed = JsonSerializer.Deserialize<UsnoSolarYearResponse>(rawJson);
                if (parsed == null) return null;

                try
                {
                    await _localStorage.SetItemAsync(cacheKey, rawJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"eclipses localStorage write failed ({year}): {ex.Message}");
                }

                return parsed;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"USNO request for {year} timed out after {EarthEclipseNetworkTimeoutSeconds}s. Using bundled catalog.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USNO request for {year} failed: {ex.Message}. Using bundled catalog.");
                return null;
            }
        }

        /// <summary>
        /// Fetches the USNO per-location <c>/solar/date</c> endpoint for one
        /// event at one observer point. Caches by <c>(date, lat@F2, lon@F2)</c>
        /// in <c>localStorage</c> under <c>eclipseSolarDate_v1:*</c> — magnitudes
        /// and obscurations are deterministic functions of (date, coords) so the
        /// cache has no time-to-live; the <c>v1</c> key-version bump is the only
        /// invalidation lever if USNO changes the field names.
        /// <para>
        /// Failures are swallowed: HTTP 500, timeout, malformed JSON, the
        /// <c>"error"</c> short-circuit (USNO returns <c>{"error": "..."}</c> for
        /// both "no eclipse that day" and out-of-bounds dates), and missing
        /// <c>local_data</c> all return <c>null</c>. Only successful responses
        /// are persisted to <c>localStorage</c> so a transient server hiccup
        /// doesn't pin a negative result into cache indefinitely.
        /// </para>
        /// </summary>
        async Task<UsnoSolarDateResponse?> FetchSolarDateAsync(
            int year, int month, int day, double lat, double lon,
            CancellationToken externalToken = default)
        {
            string dateStr = $"{year}-{month:D2}-{day:D2}";
            string cacheKey = $"eclipseSolarDate_v1:{dateStr}:{lat:F2},{lon:F2}";

            try
            {
                string? cachedJson = await _localStorage.GetItemAsync<string>(cacheKey);
                if (!string.IsNullOrWhiteSpace(cachedJson))
                {
                    var cached = JsonSerializer.Deserialize<UsnoSolarDateResponse>(cachedJson);
                    if (cached?.Properties?.LocalData != null && cached.Properties.LocalData.Count > 0)
                    {
                        return cached;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"eclipses solar-date localStorage read failed ({dateStr}): {ex.Message}");
            }

            try
            {
                // Compose the per-row 15-second timeout with whatever outer
                // cancellation token the caller supplies (e.g. when the user
                // collapses the Eclipse panel mid-fetch). Either source fires
                // -> HttpClient.GetAsync throws OperationCanceledException
                // and we return null silently. Without the linked source the
                // outer token would be ignored.
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(externalToken);
                cts.CancelAfter(TimeSpan.FromSeconds(EarthEclipseNetworkTimeoutSeconds));

                string url = $"https://aa.usno.navy.mil/api/eclipses/solar/date?date={dateStr}&coords={lat:F2},{lon:F2}&height=0";
                HttpResponseMessage response = await _http.GetAsync(url, cts.Token);
                if (!response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"USNO solar-date for {dateStr} returned {(int)response.StatusCode}. Falling back to region hint.");
                    return null;
                }

                string rawJson = await response.Content.ReadAsStringAsync();
                // USNO returns either {"error":"..."} (human-readable string)
                // or {"error":true} (boolean). Both shapes mean "no eclipse at
                // this point on this date" — treat as silent null and DON'T cache
                // so future fixes / spec changes can retry. We parse once and
                // re-use the same JsonDocument for both the error check and the
                // typed deserialize so a successful response only pays one parse
                // pass. We inspect the parsed tree (rather than substring-matching
                // the raw JSON) so a future USNO field whose VALUE contains the
                // literal `"error"` (e.g. an `error_margin` entry in `local_data`)
                // doesn't false-positive.
                using var doc = JsonDocument.Parse(rawJson);
                if (doc.RootElement.ValueKind == JsonValueKind.Object &&
                    doc.RootElement.TryGetProperty("error", out _))
                {
                    return null;
                }

                var parsed = doc.RootElement.Deserialize<UsnoSolarDateResponse>();
                if (parsed?.Properties?.LocalData == null || parsed.Properties.LocalData.Count == 0)
                {
                    return null;
                }

                try
                {
                    await _localStorage.SetItemAsync(cacheKey, rawJson);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"eclipses solar-date localStorage write failed ({dateStr}): {ex.Message}");
                }

                return parsed;
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine($"USNO solar-date for {dateStr} cancelled or timed out after {EarthEclipseNetworkTimeoutSeconds}s. Using region hint.");
                return null;
            }
            catch (JsonException ex)
            {
                Console.WriteLine($"USNO solar-date for {dateStr} returned malformed JSON: {ex.Message}. Using region hint.");
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"USNO solar-date for {dateStr} failed: {ex.Message}. Using region hint.");
                return null;
            }
        }
    }

    // ---- View model ------------------------------------------------

    /// <summary>
    /// Return value of <see cref="EclipseService.GetUpcomingEclipsesAsync"/>.
    /// Splits the upcoming eclipse list into two buckets so the UI can
    /// surface "Visible from your hemisphere" vs. "Elsewhere" without
    /// having to re-run the substring match itself.
    /// </summary>
    public class EclipseTimeline
    {
        /// <summary>
        /// True when a usable <c>userLatitude</c> was supplied to
        /// <see cref="EclipseService.GetUpcomingEclipsesAsync"/>. False
        /// means the timeline is returned unsplit and the UI should fall
        /// back to a single flat list rendering.
        /// </summary>
        public bool HasLocationHint { get; set; }

        /// <summary>
        /// "Northern" or "Southern", in display capitalization. Null when
        /// <see cref="HasLocationHint"/> is false.
        /// </summary>
        public string? Hemisphere { get; set; }

        /// <summary>
        /// Events best (or at least partially) viewed from the user's
        /// hemisphere, sorted ascending by date. Empty when the latitude
        /// parse failed and the unsplit list is returned in this bucket.
        /// </summary>
        public List<EclipseEvent> VisibleFromYourHemisphere { get; set; } = new();

        /// <summary>
        /// Events that the substring-match heuristic flagged as primarily
        /// visible from the opposite hemisphere. Empty when
        /// <see cref="HasLocationHint"/> is false.
        /// </summary>
        public List<EclipseEvent> Elsewhere { get; set; } = new();
    }

    /// <summary>
    /// Unified Solar / Lunar eclipse event used by both the bundled catalog
    /// and the live USNO overlay. The <see cref="Source"/> field lets the UI
    /// attribute each entry correctly ("USNO" vs "NASA catalog").
    /// </summary>
    public class EclipseEvent
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;   // "Solar" or "Lunar"

        [JsonPropertyName("kind")]
        public string Kind { get; set; } = string.Empty;   // "Total", "Annular", "Partial", "Penumbral"

        [JsonPropertyName("date")]
        public DateTime Date { get; set; }

        [JsonPropertyName("region_hint")]
        public string RegionHint { get; set; } = string.Empty;

        /// <summary>Descriptive title (overridden by USNO when applicable).</summary>
        [JsonIgnore]
        public string Title { get; set; } = string.Empty;

        /// <summary>Where the authoritative row came from for this event.</summary>
        [JsonIgnore]
        public string Source { get; set; } = "NASA catalog";

        /// <summary>One-line presentation string for the compact list view.</summary>
        [JsonIgnore]
        public string Subtitle =>
            string.IsNullOrWhiteSpace(Kind) ? Type : $"{Kind} {Type.ToLowerInvariant()}";

        /// <summary>
        /// Per-location local-circumstances overlay populated by
        /// <see cref="EclipseService.GetUpcomingEclipsesAsync"/> when the user
        /// shares coordinates AND <see cref="Type"/> is <c>Solar</c>. Null when
        /// the live USNO <c>/solar/date</c> call failed, was out of range, or
        /// hasn't been attempted yet (e.g. lunar eclipses). UI consumers should
        /// fall back to <see cref="RegionHint"/> whenever this is null.
        /// </summary>
        [JsonIgnore]
        public UsnoSolarDateResponse? LocationOverlay { get; set; }

        /// <summary>
        /// Compact formatted version of <see cref="LocationOverlay"/> for display
        /// in the eclipse-item row. Format:
        /// <c>M:0.382 · 38.2% obscuration from your location @ 2026-08-12 17:30 UT</c>
        /// — followed by altitude at maximum if returned by USNO. Returns
        /// <c>null</c> when no usable "Maximum Eclipse" entry is present so the
        /// Razor template can branch cleanly between "with overlay" and
        /// "region hint only".
        /// </summary>
        [JsonIgnore]
        public string? OverlayDisplay
        {
            get
            {
                if (LocationOverlay?.Properties?.LocalData == null) return null;
                var max = LocationOverlay.Properties.LocalData
                    .FirstOrDefault(d => d.Phenomenon
                        .Equals("Maximum Eclipse", StringComparison.OrdinalIgnoreCase));
                if (max == null ||
                    !max.Magnitude.HasValue ||
                    !max.Obscuration.HasValue ||
                    max.Obscuration.Value <= 0)
                {
                    return null;
                }

                string altitude = max.Altitude.HasValue ? $" · max altitude {max.Altitude.Value:F0}°" : string.Empty;
                return $"M:{max.Magnitude.Value:F3} · {max.Obscuration.Value:F1}% obscuration from your location @ {Date:yyyy-MM-dd} {max.Time} UT{altitude}";
            }
        }
    }

    // ---- USNO DTOs --------------------------------------------------

    /// <summary>
    /// Subset of the USNO <c>/api/eclipses/solar/year</c> response. Only the
    /// fields we actually use are mapped; everything else is ignored so
    /// future USNO schema additions don't break us.
    /// </summary>
    public class UsnoSolarYearResponse
    {
        [JsonPropertyName("apiversion")]
        public string? ApiVersion { get; set; }

        [JsonPropertyName("year")]
        public int? Year { get; set; }

        [JsonPropertyName("eclipses_in_year")]
        public List<UsnoSolarEclipse>? EclipsesInYear { get; set; }
    }

    public class UsnoSolarEclipse
    {
        [JsonPropertyName("year")]
        public int Year { get; set; }

        [JsonPropertyName("month")]
        public int Month { get; set; }

        [JsonPropertyName("day")]
        public int Day { get; set; }

        [JsonPropertyName("event")]
        public string Event { get; set; } = string.Empty;
    }

    /// <summary>
    /// Subset of the USNO <c>/api/eclipses/solar/date?date=YYYY-MM-DD&amp;coords=LAT,LON</c>
    /// response — per-location local circumstances for one event at one observer
    /// point. Field set is intentionally narrow (we only surface magnitude,
    /// obscuration, max-time, altitude); future additions to USNO's schema are
    /// ignored. <see cref="Error"/> is a string because USNO returns either a
    /// human-readable error string OR a <c>{"error": true}</c> literal — both
    /// are treated as a silent no-eclipse response by the caller.
    /// </summary>
    public class UsnoSolarDateResponse
    {
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        [JsonPropertyName("properties")]
        public UsnoSolarProperties? Properties { get; set; }
    }

    public class UsnoSolarProperties
    {
        [JsonPropertyName("local_data")]
        public List<UsnoSolarLocalData>? LocalData { get; set; }
    }

    public class UsnoSolarLocalData
    {
        [JsonPropertyName("phenomenon")]
        public string Phenomenon { get; set; } = string.Empty;

        [JsonPropertyName("time")]
        public string Time { get; set; } = string.Empty;

        [JsonPropertyName("altitude")]
        public double? Altitude { get; set; }

        [JsonPropertyName("azimuth")]
        public double? Azimuth { get; set; }

        [JsonPropertyName("magnitude")]
        public double? Magnitude { get; set; }

        [JsonPropertyName("obscuration")]
        public double? Obscuration { get; set; }
    }
}
