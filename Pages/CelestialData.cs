using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace AboveMe.Pages
{
    public class CelestialData
    {
        // Helper to determine if the selected country/timezone is US or US timezone
        public static bool IsUSLocale() => true;

        // Format time as 12-hour with AM/PM for US, otherwise return as-is
        public static string FormatTime(string? time, bool showSeconds = false)
        {
            if (string.IsNullOrWhiteSpace(time)) return string.Empty;
            DateTime dt;
            if (DateTime.TryParseExact(time, new[] { "H:mm", "HH:mm", "H:mm:ss", "HH:mm:ss", "H:mm:ss.fff", "HH:mm:ss.fff" }, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out dt))
            {
                return showSeconds ? dt.ToString(format: "h:mm:ss tt") : dt.ToString(format: "h:mm tt");
            }
            return time;
        }

        public static List<string> Countries { get; } = new() { "United States", "Canada", "United Kingdom", "Australia", "China" };
        public static List<(string Id, string Display, string Description)> Timezones { get; } = new()
        {
            ("Eastern Standard Time", "Eastern", "UTC-5"),
            ("Central Standard Time", "Central", "UTC-6"),
            ("Mountain Standard Time", "Mountain", "UTC-7"),
            ("Pacific Standard Time", "Pacific", "UTC-8"),
            ("Greenwich Mean Time", "Greenwich Mean Time", "UTC+0"),
            ("Central European Time", "Central European Time", "UTC+1"),
            ("Australian Eastern Time", "Australian Eastern Time", "UTC+10"),
            ("China Standard Time", "China Standard Time", "UTC+8")
        };

        public class GeolocationResult
        {
            public double Latitude { get; set; }
            public double Longitude { get; set; }
            public string? Country { get; set; }
            public string? City { get; set; }
            public string? Timezone { get; set; }
        }

        public class AstronomyData
        {
            [JsonPropertyName("moon_phase")]
            public string? MoonPhase { get; set; }
            [JsonPropertyName("moonrise")]
            public string? Moonrise { get; set; }
            [JsonPropertyName("moonset")]
            public string? Moonset { get; set; }
            [JsonPropertyName("sunrise")]
            public string? Sunrise { get; set; }
            [JsonPropertyName("sunset")]
            public string? Sunset { get; set; }
            [JsonPropertyName("current_time")]
            public string? CurrentTime { get; set; }
            [JsonPropertyName("solar_noon")]
            public string? SolarNoon { get; set; }
            [JsonPropertyName("day_length")]
            public string? DayLength { get; set; }
            [JsonPropertyName("country_code")]
            public string? CountryCode { get; set; }
            [JsonPropertyName("location")]
            public object? Location { get; set; }
            [JsonPropertyName("moon_illumination_percentage")]
            public string? MoonIlluminationPercentage { get; set; }
        }

    // CometInfo (legacy) and GetNextComets() below have been removed;
    // comet data is now served by AboveMe.Services.CometService loaded
    // from wwwroot/comets-forecasts.json (JPL Horizons-derived catalog).

    public class NasaApodData
        {
            [JsonPropertyName("copyright")]
            public string? Copyright { get; set; }
            [JsonPropertyName("date")]
            public string? Date { get; set; }
            [JsonPropertyName("explanation")]
            public string? Explanation { get; set; }
            [JsonPropertyName("hdurl")]
            public string? HdUrl { get; set; }
            [JsonPropertyName("media_type")]
            public string? MediaType { get; set; }
            [JsonPropertyName("service_version")]
            public string? ServiceVersion { get; set; }
            [JsonPropertyName("title")]
            public string? Title { get; set; }
            [JsonPropertyName("url")]
            public string? Url { get; set; }
        }

        public static (string, string) GetCurrentMoonPhase()
        {
            (string, string)[] phases = new[]
            {
                ("New Moon", "moonphases/newmoon.png"),
                ("Waxing Crescent", "moonphases/waxingcrescent.png"),
                ("First Quarter", "moonphases/firstquarter.png"),
                ("Waxing Gibbous", "moonphases/waxinggibbous.png"),
                ("Full Moon", "moonphases/fullmoon.png"),
                ("Waning Gibbous", "moonphases/waninggibbous.png"),
                ("Last Quarter", "moonphases/lastquarter.png"),
                ("Waning Crescent", "moonphases/waningcrescent.png")
            };
            DateTime now = DateTime.UtcNow;
            double synodicMonth = 29.53058867;
            DateTime knownNewMoon = new DateTime(2000, 1, 6, 18, 14, 0, DateTimeKind.Utc);
            double daysSince = (now - knownNewMoon).TotalDays;
            double currentPhase = daysSince % synodicMonth;
            int phaseIndex = (int)Math.Floor((currentPhase / synodicMonth) * phases.Length) % phases.Length;
            return phases[phaseIndex];
        }

        public static string GetMoonPhaseImage(string? moonPhase)
        {
            if (string.IsNullOrWhiteSpace(moonPhase)) return "moonphases/newmoon.png";
            return moonPhase.ToUpperInvariant() switch
            {
                "NEW_MOON" => "moonphases/newmoon.png",
                "WAXING_CRESCENT" => "moonphases/waxingcrescent.png",
                "FIRST_QUARTER" => "moonphases/firstquarter.png",
                "WAXING_GIBBOUS" => "moonphases/waxinggibbous.png",
                "FULL_MOON" => "moonphases/fullmoon.png",
                "WANING_GIBBOUS" => "moonphases/waninggibbous.png",
                "LAST_QUARTER" => "moonphases/lastquarter.png",
                "WANING_CRESCENT" => "moonphases/waningcrescent.png",
                _ => "moonphases/newmoon.png"
            };
        }

        public static List<string> GetVisibleConstellations(string? country, string? timezone)
        {
            if (!string.IsNullOrEmpty(country))
            {
                return country switch
                {
                    "United States" => new() { "Orion", "Ursa Major", "Cassiopeia", "Cygnus" },
                    "Canada" => new() { "Draco", "Cepheus", "Ursa Minor", "Perseus" },
                    "United Kingdom" => new() { "Pegasus", "Andromeda", "Perseus", "Cassiopeia" },
                    "Australia" => new() { "Crux", "Centaurus", "Carina", "Pavo" },
                    _ => new() { "Ursa Major", "Cassiopeia", "Cygnus", "Perseus" }
                };
            }
            else if (!string.IsNullOrEmpty(timezone))
            {
                return timezone switch
                {
                    "Eastern Standard Time" => new() { "Orion", "Gemini", "Canis Major" },
                    "Central Standard Time" => new() { "Leo", "Virgo", "Cancer" },
                    "Mountain Standard Time" => new() { "Aquila", "Lyra", "Sagittarius" },
                    "Pacific Standard Time" => new() { "Pegasus", "Pisces", "Andromeda" },
                    "Greenwich Mean Time" => new() { "Cassiopeia", "Perseus", "Andromeda" },
                    "Central European Time" => new() { "Orion", "Taurus", "Auriga" },
                    "Australian Eastern Time" => new() { "Crux", "Carina", "Centaurus" },
                    _ => new() { "Ursa Major", "Cassiopeia", "Cygnus", "Perseus" }
                };
            }
            else
            {
                return new();
            }
        }

        // Legacy hardcoded GetNextComets(country, timezone) has been removed.
        // Comet forecasts are now produced by AboveMe.Services.CometService
        // (see FetchCometsAsync in Pages/Home.razor).
    }
}
