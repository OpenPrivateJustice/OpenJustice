---
phase: 11-extractor-foundation-and-tjgo-search
plan: "02"
subsystem: extractor
tags: [playwright, tjgo, browser-automation, worker, scraping]

# Dependency graph
requires:
  - phase: 11-extractor-foundation-and-tjgo-search
    provides: BrazilExtractor project foundation with validated config
provides:
  - Playwright Chromium runtime boundary (IPlaywrightBrowserFactory)
  - TJGO ConsultaPublicacao navigation and form submission service
  - Single-day date query semantics with same-day DataInicial/DataFinal
  - Worker orchestration wired to execute TJGO search job
affects: [11-extractor-foundation-and-tjgo-search]

# Tech tracking
tech-stack:
  added: [Microsoft.Playwright]
  patterns: [IAsyncDisposable, BackgroundService with scoped services, locator-first form submission]

key-files:
  created:
    - src/OpenJustice.BrazilExtractor/Services/Browser/IPlaywrightBrowserFactory.cs
    - src/OpenJustice.BrazilExtractor/Services/Browser/PlaywrightBrowserFactory.cs
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchQuery.cs
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/ITjgoSearchService.cs
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs
    - src/OpenJustice.BrazilExtractor/Services/Jobs/ITjgoSearchJob.cs
    - src/OpenJustice.BrazilExtractor/Services/Jobs/TjgoSearchJob.cs
  modified:
    - src/OpenJustice.BrazilExtractor/Program.cs
    - src/OpenJustice.BrazilExtractor/Worker.cs
    - src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptions.cs
    - src/OpenJustice.BrazilExtractor/appsettings.json

key-decisions:
  - Used singleton IPlaywrightBrowserFactory for centralized browser lifecycle management
  - Scoped services (ITjgoSearchService, ITjgoSearchJob) inside BackgroundService using IServiceScopeFactory
  - Single-day queries set both DataInicial and DataFinal to same dd/MM/yyyy value
  - Headless mode configurable via BrazilExtractorOptions

requirements-completed: [EXTR-02, EXTR-03, EXTR-05]

# Metrics
duration: 6 min
completed: 2026-03-02
---

# Phase 11 Plan 2: TJGO Search Implementation Summary

**Implemented Playwright Chromium-driven TJGO ConsultaPublicacao navigation with single-day date filter semantics**

## Performance

- **Duration:** 6 min
- **Started:** 2026-03-02T17:58:00Z
- **Completed:** 2026-03-02T18:04:02Z
- **Tasks:** 3
- **Files modified:** 12

## Accomplishments
- Created Playwright Chromium factory for centralized browser lifecycle management
- Implemented TJGO search service with locator-first form submission
- Worker orchestrates TJGO search job with scoped DI services per iteration
- Single-day queries enforce same-date DataInicial and DataFinal values

## Task Commits

Each task was committed atomically:

1. **Task 1: Build Playwright Chromium runtime boundary for extractor** - `0dab87f` (feat)
2. **Task 2: Implement TJGO ConsultaPublicacao navigation and single-day form submission** - `56f3ec2` (feat)
3. **Task 3: Wire worker orchestration to execute the TJGO search job** - `61325fa` (feat)

**Plan metadata:** (to be committed after summary)

## Files Created/Modified
- `src/OpenJustice.BrazilExtractor/Services/Browser/IPlaywrightBrowserFactory.cs` - Browser factory interface
- `src/OpenJustice.BrazilExtractor/Services/Browser/PlaywrightBrowserFactory.cs` - Playwright/Chromium lifecycle management
- `src/OpenJustice.BrazilExtractor/Models/TjgoSearchQuery.cs` - Search query with single-day date semantics
- `src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs` - Result contract
- `src/OpenJustice.BrazilExtractor/Services/Tjgo/ITjgoSearchService.cs` - TJGO service interface
- `src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs` - Navigation, form fill, submit implementation
- `src/OpenJustice.BrazilExtractor/Services/Jobs/ITjgoSearchJob.cs` - Job interface
- `src/OpenJustice.BrazilExtractor/Services/Jobs/TjgoSearchJob.cs` - Job orchestration
- `src/OpenJustice.BrazilExtractor/Worker.cs` - Worker now executes scoped job per iteration
- `src/OpenJustice.BrazilExtractor/Program.cs` - DI registration for all services
- `src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptions.cs` - Added ConsultaPublicacaoUrl, QueryDateWindowStartDate, HeadlessMode
- `src/OpenJustice.BrazilExtractor/appsettings.json` - Configuration for TJGO URL, date window, headless mode

## Decisions Made
- Singleton browser factory + scoped search services pattern for proper lifecycle management
- Single-day date semantics by setting both date fields to identical values
- Configurable headless mode for development vs production usage

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- Extractor can perform complete TJGO ConsultaPublicacao search flow via Playwright
- Single-day date range behavior enforced in submitted queries
- Ready for Phase 12 PDF capture and OCR artifact extraction

---
*Phase: 11-extractor-foundation-and-tjgo-search*
*Completed: 2026-03-02*
