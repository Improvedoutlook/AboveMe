# AboveMe — Agent Rules

## Project
.NET Blazor WebAssembly app: astronomy-themed UI, twinkling starry background, real-time location (ipgeolocation) and NASA Images API (Webb).

## Core Rules
1. **Simplicity first.** Pick the most straightforward solution that meets the requirement. If it needs long explanation, there's probably a simpler one.
2. **DRY.** No duplicated logic — extract to services, base classes, or parameterized components. No magic strings/numbers; centralize config.
3. **Naming:** PascalCase for classes/methods/Razor files, camelCase for locals/params, `_camelCase` for injected private fields. Names describe intent (e.g. `FetchWebbDataAsync`).
4. **Async I/O everywhere** with cancellation tokens; debounce user input that triggers API calls.
5. **API calls** via injected `HttpClient`; cache when appropriate; retry on transient failures; handle rate limiting. Wrap in try/catch, log details, show user-friendly errors (never stack traces).
6. **No hardcoded secrets.** Use `appsettings.json` / env vars; keep them out of git.
7. **Default HTML encoding is on — don't switch to `MarkupString` unless the content is fully trusted and sanitized.**

## Components
- Razor files: put code in a single `@code` block at the bottom.
- Using directives used across multiple files → move to `_Imports.razor`.
- Null-conditional (`?.`) and null-coalescing (`??`) preferred.
- Implement error boundaries.
- Favor built-in .NET/Blazor features over third-party libs.

## UI/UX — "Surprise and Delight"
- **Not** a generic interface. Palette: deep-space blues/purples/whites.
- Starry background: CSS animations over JS; reduce particle count on mobile; respect `prefers-reduced-motion`.
- Glassmorphism panels (translucent + backdrop blur) for cards.
- Skeletons/elegant spinners — never blank flashes.
- Loading, success, and error states all feel intentional.
- **Accessibility is non-negotiable:** WCAG 2.1 AA, keyboard nav, ARIA labels, ≥44×44px touch targets, contrast against the starry bg.

## Performance
- `@key` on lists; virtualize long ones.
- Code-split / lazy-load heavy components.
- Dispose `IDisposable`; unsubscribe events; leak-check `IJSRuntime` usage.

## Testing
- xUnit + bUnit; mock `HttpClient` / `IJSRuntime`.
- Arrange-Act-Assert, one logical assertion per test, descriptive names.

## Git
- `main` = prod, `develop-main` = dev, `feature/*`, `fix/*`.
- Commits: `[Type] Brief description` (Feature / Fix / Refactor / Docs / Style / Test / Chore).

## APIs In Use
- **ipgeolocation** — user location
- **NASA Images API** — Webb imagery

## PR / Commit Checklist
- [ ] Simplest viable solution
- [ ] No duplication
- [ ] Naming conventions
- [ ] Error handling in place
- [ ] Tests for critical paths
- [ ] Accessible & responsive
- [ ] No hardcoded secrets
