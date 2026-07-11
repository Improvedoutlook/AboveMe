# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

> Note: a `CLAUDE.md` exists one level up in `C:\Users\HP\Desktop\` — it belongs to a **different** project (ElevitaCore, a Razor Pages/ASP.NET Core app) and does **not** apply here. AboveMe is Blazor WebAssembly. Use this file.

## What this is

AboveMe is a **Blazor WebAssembly (.NET 9)** single-page app that uses the visitor's location to surface astronomy data: moon phases, Solar + Lunar eclipses, NASA APOD, a curated James Webb gallery, latitude-filtered comet forecasts, meteor showers, near-Earth asteroids, and a real-time NOAA OVATION aurora probability — all over an animated starry background. It ships to **GitHub Pages** as a fully static client-side app; there is no server backend.

## Commands

```powershell
dotnet run                                       # run dev server (https://localhost:5001)
dotnet build --configuration Release             # build
dotnet publish --configuration Release -o publish # produce the static site (what CI ships)
```

There is **no test project** in the repo despite the copilot conventions mentioning xUnit/bUnit — do not assume `dotnet test` works. The `.vscode` task "Run AboveMe Blazor App" just runs `dotnet run`.

## Deployment

- Push to the **`develop-main`** branch triggers `.github/workflows/deploy.yml`, which builds, publishes, rewrites `<base href>` to `/AboveMe/`, adds `.nojekyll`, copies `index.html`→`404.html`, and deploys to GitHub Pages.
- `main` is prod, `develop-main` is dev. Commit style (from copilot conventions): `[Type] Brief description` (Feature / Fix / Refactor / Docs / Style / Test / Chore).
- The `docs/` directory holds a **previously published build artifact** (static output, generated files like `AboveMe.staticwebassets.endpoints.json`) plus project docs — do not hand-edit the generated files there.

## Architecture

Everything is client-side. Each feature is a collapsible toggle section on a single page.

- **`Pages/Home.razor`** — the entire UI: every toggle section's markup and most feature logic live here in the `@code` block. It's a sizable file (~3,900 lines), so when editing, find the relevant section rather than scanning the whole thing.
- **`Pages/Home.razor.cs`** — code-behind `partial class Home` (same class as the `.razor` `@code` block). The **Aurora (NOAA OVATION)** feature lives here. Location state fields (`userLatitude`, `userLongitude`) declared in `Home.razor` are the single source of truth shared across both partial halves.
- **`Pages/CelestialData.cs`** — shared helper models.
- **`Services/`** (all registered `Scoped` in `Program.cs`):
  - `LocalStorageService` — JS-interop wrapper over `localStorage` for persisted user location prefs and per-section response caches.
  - `CometService` — loads `wwwroot/comets-forecasts.json`, filters by the visitor's latitude using each entry's `MinLatitudeVisible`/`MaxLatitudeVisible`.
  - `EclipseService` — builds the merged Solar+Lunar timeline: live USNO AA solar data overlaid on the bundled `wwwroot/eclipses-catalog.json` (solar fallback + all lunar). Hemisphere split keywords live in the `NorthernKeywords`/`SouthernKeywords` arrays at the top of the file.
- **`Program.cs`** — DI registration + in Development only, fetches `appsettings.Development.json` and merges it into `IConfiguration` (production keys are baked into `appsettings.json` by CI).
- **JS interop** — `wwwroot/getUserLocation.js` (browser geolocation), `wwwroot/starry.js` (animated background).

### Data & caching conventions

- External APIs are called via the injected `HttpClient`. Responses are cached in `localStorage` per feature, keyed by UTC day (e.g. `asteroidFeed_v1:YYYYMMDD`, `auroraFeed_v1:latest`) to spare rate-limited endpoints. Follow this pattern for new data sections.
- Curated static catalogs (`comets-forecasts.json`, `eclipses-catalog.json`) let sections work offline and degrade gracefully when a live API is unreachable — always provide a fallback rather than letting a failed fetch break the panel.
- Cross-origin APIs without CORS headers are routed through **corsproxy.io** (see `docs/CORS_Fix_Documentation.md`).

### API keys

- Keys live under `ApiKeys` in `wwwroot/appsettings.json` (`NasaApiKey`, `AstronomyApiKey`); `IPGEO_API_KEY` env var also honored. Missing keys fall back to `DEMO_KEY`.
- **Never commit real keys.** `wwwroot/appsettings.Development.json` is gitignored and excluded from publish (see `AboveMe.csproj`); production keys are injected from repo secrets by CI.

## Project conventions (from `.github/copilot-instructions.md`)

- **Simplicity first, DRY** — extract shared logic to services; no magic strings/numbers.
- **Naming:** PascalCase for classes/methods/Razor files, camelCase for locals/params, `_camelCase` for injected private fields.
- **Async I/O everywhere** with cancellation tokens; debounce user input that triggers API calls; wrap calls in try/catch and show user-friendly errors, never stack traces.
- **Output encoding:** default HTML encoding stays on — do not switch to `MarkupString`/`Html.Raw` unless content is fully trusted and sanitized.
- **Razor:** one `@code` block at the bottom; shared `@using` go in `_Imports.razor`; prefer `?.`/`??`; dispose `IDisposable` and unsubscribe `IJSRuntime`/event handlers to avoid leaks.
- **UI/UX ("surprise and delight"):** deep-space blue/purple/white palette, glassmorphism panels, skeletons/spinners over blank flashes. CSS animations over JS for the starry background; reduce particle count on mobile; respect `prefers-reduced-motion`. Accessibility is required — WCAG 2.1 AA, keyboard nav, ARIA labels, ≥44×44px touch targets. Theme/starry styling lives in `wwwroot/app.css` and `wwwroot/starry.js`.

## Adding / updating content

- **Webb gallery** — drop images into `wwwroot/Webb Space Telescope Images/` and append to the `WebbImages` list in `Home.razor`.
- **Meteor showers** — update `AllMeteorShowers` in `Home.razor`; each entry keeps `HemisphereVisibility` (Northern/Southern/All).
- **Comet catalog** — refresh `wwwroot/comets-forecasts.json` (source: JPL SBDB/Horizons; each entry keeps its `JplCommand`).
- **Eclipse catalog** — extend `wwwroot/eclipses-catalog.json`; each entry has `type` (Solar/Lunar), `kind` (Total/Annular/Partial/Penumbral), `date` (ISO), `region_hint`. New solar entries get USNO's authoritative title overlaid automatically when USNO is reachable.

## Housekeeping

`scripts/` contains ad-hoc `dump_*.txt` debugging snapshots, and there is a stray `et --hard` file in the repo root (an accidental capture of `git reset --hard` output) — these are scratch artifacts, not part of the app.
