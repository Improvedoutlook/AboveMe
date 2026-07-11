# AboveMe

AboveMe is a Blazor WebAssembly (ASP.NET Core 9) app that uses your location to surface astronomy data you'll actually look at: lunar phases, upcoming Solar + Lunar eclipses, NASA's Astronomy Picture of the Day, a curated gallery of James Webb Space Telescope images, latitude-filtered comet forecasts, the upcoming meteor showers visible from your hemisphere, and a real-time NOAA OVATION aurora viewing probability. The whole UI sits on top of an animated, twinkling starry background.

## Features

Each section is a toggle button — open only what you want, and the rest stays collapsed.

- **Moon Data** — current phase (with image), illumination percentage, moonrise / moonset, sunrise / sunset, solar noon, day length, and the local time for your coordinates. Powered by the ipgeolocation.io astronomy endpoint.
- **Eclipse Data** — merged Solar + Lunar eclipse timeline (10 years, ~40 events) from USNO AA's live `year` endpoint for the authoritative Solar eclipse titles, overlaid on a curated NASA GSFC Five Millennium Canon-style catalog (2026–2035) bundled in `wwwroot/eclipses-catalog.json` that also covers every Lunar eclipse (USNO does not expose a public lunar JSON endpoint). When you share your location, the list is split into **Likely visible from your location** vs. **Elsewhere**. The first pass is a case-insensitive substring match against each event's `region_hint` (Northern: "North America", "Europe", "Asia", …; Southern: "Australia", "South America", "Antarctica", …), optionally narrowed further by your selected timezone/country; without a latitude it falls back to a single flat list. Each row shows the event kind (Total, Annular, Partial, Penumbral), date, USNO descriptive title when live, and a region hint. **Solar eclipses also get a per-location overlay row** (📍 `M:0.382 · 38.2% obscuration from your location @ 2026-08-12 17:30 UT`) sourced from USNO's `/api/eclipses/solar/date?date=YYYY-MM-DD&coords=LAT,LON`, fired in parallel (capped at 3 in-flight) for solar eclipses in your hemisphere within the next 10 years, cached per (date, lat@F2, lon@F2) in `localStorage`, and silently falling back to `region_hint` when USNO 500s/times out/returns no-event. **That same per-coordinate call also filters the list:** when USNO definitively reports the eclipse is outside your path (an `error` response or a zero-obscuration reading), the solar event is demoted from *Likely visible* to *Elsewhere*, while a network failure or out-of-range date is treated as unknown and never hides an event. The catalog is bundled as `wwwroot/eclipses-catalog.json` (same `eclipses-catalog.json`-keyed localStorage cache pattern as the comet list), so the panel works offline and degrades gracefully if USNO is unreachable.
- **Astronomy Picture of the Day** — NASA's APOD for today, with full image, title, and explanation. Video APODs render as a `<video>` tag for direct media, an embedded `iframe` for YouTube, or a fallback link when the host blocks framing.
- **James Webb Space Telescope Gallery** — 30 curated JWST images with descriptions, each loaded from the project's `wwwroot/Webb Space Telescope Images/` directory. Toggling the section advances to the next image in the list, so repeatedly clicking "Show Webb Space Telescope" walks you through the gallery.
- **Comet Data** — curated forecast catalog sourced from JPL Horizons with named comets (Tsuchinshan-ATLAS, ATLAS, Encke, Olbers, Churyumov-Gerasimenko, …). Each entry shows designation, status pill (`Upcoming` / `Active` / `Past` derived from the current UTC date), perihelion date, peak window, peak magnitude, summary, and notes. The list is filtered to the comets visible from your latitude.
- **Meteor Showers** — American Meteor Society 2025–2026 calendar (Quadrantids, Lyrids, Eta Aquarids, Perseids, Orionids, Leonids, Geminids, Ursids, plus Taurids/Alpha Capricornids/Delta Aquarids). Hemispheres are filtered to your location, peak times are shifted into your timezone, paginated 5 at a time with a "Show More" button.
- **Asteroid Data** — NASA Near-Earth Object Web Service (NeoWs) feed for the next 7 days, ordered first by upcoming approach date, then by closest miss distance within each day. Each card shows the object's name, a red ⚠ badge if it's potentially hazardous, close-approach date, estimated diameter range (km), miss distance in lunar distances plus km, and relative velocity (km/s). A "NASA JPL →" link per object opens the JPL SBDB lookup page for that designation. The toggle loads on demand, the day's response is cached in `localStorage` (keyed by `asteroidFeed_v1:YYYYMMDD` UTC) to spare the rate-limited endpoint, and the list paginates 10 at a time via a "Show More" button.

## Tech Stack

- **.NET 9 / Blazor WebAssembly** (Microsoft.AspNetCore.Components.WebAssembly 9.0.5)
- **HttpClient + JS Interop** for browser geolocation (`getUserLocation.js` in `wwwroot/`)
- **LocalStorage service** (`Services/LocalStorageService.cs`) for persistent user location preferences and per-section caching
- **Static assets shipped in `wwwroot/`** — moon phase images, JWST image catalog, curated comet forecast catalog
- **Corsproxy.io** for routes that hit MAST / external CDNs without CORS headers (per `docs/CORS_Fix_Documentation.md`)

## Data Sources

| Section | Source | Auth |
| --- | --- | --- |
| Moon Data | `https://api.ipgeolocation.io/v2/astronomy` | API key |
| Eclipse Data (Solar) | `https://aa.usno.navy.mil/api/eclipses/solar/year?year=YYYY` (live) + `wwwroot/eclipses-catalog.json` (fallback) | — (no key; CORS-enabled) |
| Eclipse Data (Lunar) | `wwwroot/eclipses-catalog.json` (curated from NASA GSFC) | — |
| NASA APOD | `https://api.nasa.gov/planetary/apod` | API key (`DEMO_KEY` fallback) |
| Asteroid Data | `https://api.nasa.gov/neo/rest/v1/feed` | API key (same `NasaApiKey`, `DEMO_KEY` fallback) |
| Webb gallery | `wwwroot/Webb Space Telescope Images/` (curated 1–30) | — |
| Comet forecasts | `wwwroot/comets-forecasts.json` (curated from JPL Horizons/SBDB) | — |
| Meteor showers | American Meteor Society 2025–2026 calendar (static) | — |

API keys fall back to `DEMO_KEY` when missing; revenue-tier keys are wired in through `wwwroot/appsettings.json` (Development) or repository secrets (Production / GitHub Pages), see `docs/API_KEY_CONFIGURATION.md`.

## Getting Started

1. Clone the repo and open it in Visual Studio or VS Code.
2. (Optional) Create `wwwroot/appsettings.Development.json` with your own `ApiKeys:NasaApiKey` and `ApiKeys:AstronomyApiKey`.
3. Run the app:
   - `dotnet run` (CLI), or
   - press F5 in Visual Studio, or
   - use the **Run AboveMe** task in `.vscode/tasks.json`.
4. Visit the local URL printed in the terminal (typically `https://localhost:5001`).
5. Pick a country / timezone / city, or hit **Share my location** 📍 to use the browser's geolocation.

## Configuration

API keys live in `wwwroot/appsettings.json` and are loaded via `IConfiguration` in `Program.cs`:

```json
{
  "ApiKeys": {
    "NasaApiKey": "<your_nasa_key>",
    "AstronomyApiKey": "<your_ipgeolocation_key>"
  }
}
```

The `IPGEO_API_KEY` environment variable is also honored. Missing keys fall back to `DEMO_KEY` so the app still runs out of the box — rate limits just apply.

For production deployment, `.github/workflows/deploy.yml` injects real keys from repository secrets into the published `appsettings.json`.

## Deployment

Pushed to `develop-main` on GitHub, the workflow at `.github/workflows/deploy.yml` builds and publishes the Blazor app to **GitHub Pages** automatically. The `docs/` directory exists as the published artifact location. See `docs/CORS_Fix_Documentation.md` for the reasoning behind the corsproxy.io routing for cross-origin APIs.

## Project Structure

```
AboveMe/
├── Pages/
│   ├── Home.razor          # Single-page UI with all toggle sections
│   ├── CelestialData.cs    # Helper models (CometForecast moved to Services)
│   ├── Counter.razor       # Default Blazor template scaffold
│   └── Weather.razor       # Default Blazor template scaffold
├── Services/
│   ├── CometService.cs     # Loads & latitude-filters the comet catalog
│   ├── EclipseService.cs   # Builds merged Solar+Lunar eclipse timeline (USNO live + bundled NASA catalog)
│   └── LocalStorageService.cs
├── wwwroot/
│   ├── stars/              # Starry background assets
│   ├── moonphases/         # Moon phase image set
│   ├── Webb Space Telescope Images/   # JWST gallery (numbered 1–30 + Descriptions/)
│   ├── comets-forecasts.json          # Curated JPL Horizons-derived comet catalog
│   ├── eclipses-catalog.json          # Curated NASA GSFC Solar+Lunar eclipse catalog (2026–2035)
│   ├── appsettings.json    # API keys (injected by CI for production)
│   ├── getUserLocation.js  # Browser geolocation JS Interop
│   └── starry.js           # Animated background
├── docs/                   # GitHub Pages publish target + project docs
├── .github/
│   ├── copilot-instructions.md
│   └── workflows/deploy.yml
└── AboveMe.csproj
```

## Customization

- **Comet catalog** — refresh `wwwroot/comets-forecasts.json` quarterly with data pulled from `https://ssd-api.jpl.nasa.gov/sbdb.api` (each entry keeps its `JplCommand` so a future live-enrichment layer can call JPL Horizons).
- **Webb gallery** — drop new images into `wwwroot/Webb Space Telescope Images/` and append a matching entry to the `WebbImages` list in `Pages/Home.razor`.
- **Meteor showers** — update `AllMeteorShowers` in `Pages/Home.razor` with the latest AMS calendar each year. Each entry keeps `HemisphereVisibility` (Northern, Southern, or All) so the filter still works.
- **Eclipse catalog** — extend `wwwroot/eclipses-catalog.json` to add upcoming events. Each entry has `type` (`Solar` or `Lunar`), `kind` (`Total` / `Annular` / `Partial` / `Penumbral`), `date` (ISO `YYYY-MM-DD`), and `region_hint` (free text describing visibility). New solar entries in the catalog get overlaid with USNO's authoritative title automatically when USNO is reachable.
- **Eclipse hemisphere split** — the substring-match logic lives in the `NorthernKeywords` and `SouthernKeywords` arrays at the top of `Services/EclipseService.cs`. Add new phrases to either array (lowercase or title case both work) and the splitter will route matching `region_hint` entries into the right bucket the next time the panel opens.

- **Styling** — theme colors and the starry background live in `wwwroot/app.css` and `wwwroot/starry.js`.

## Notes for Contributors

- See `.github/copilot-instructions.md` for the project's engineering conventions (async, DI, accessibility, security around API keys).
- API keys must never be committed; rely on `appsettings.Development.json` (gitignored) and CI secrets.
- The comet catalog uses `MinLatitudeVisible` / `MaxLatitudeVisible` ranges per entry rather than a binary hemisphere split — high-declination comets can be visible far from the equator.

---

This project was bootstrapped with `dotnet new blazorwasm`.

<a href="https://app.daily.dev/improvedoutlook"><img src="https://api.daily.dev/devcards/v2/a3jXrhNvqTvcDRfL6sXIV.png?type=default&r=zcw" width="356" alt="Mark Owens's Dev Card"/></a>
