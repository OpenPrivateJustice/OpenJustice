---
phase: 11-extractor-foundation-and-tjgo-search
plan: "03"
subsystem: extractor
tags: [tjgo, criminal-filter, playwright, smoke-tests, scraping]

# Dependency graph
requires:
  - phase: 11-extractor-foundation-and-tjgo-search
    provides: TJGO ConsultaPublicacao navigation with single-day date semantics
provides:
  - CriminalFilterProfile for deterministic criminal filtering strategy
  - Filter metadata propagation through search results and job output
  - Playwright smoke tests for TJGO selector drift detection
affects: [11-extractor-foundation-and-tjgo-search, 12-pdf-acquisition-pipeline]

# Tech tracking
tech-stack:
  added: []
  patterns: [criminal filter profile strategy, smoke test contract validation]

key-files:
  created:
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/CriminalFilterProfile.cs
    - tests/OpenJustice.Playwright/TjgoConsultaPublicacaoSmokeTests.cs
  modified:
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchQuery.cs
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs
    - src/OpenJustice.BrazilExtractor/Services/Jobs/TjgoSearchJob.cs
    - src/OpenJustice.BrazilExtractor/Program.cs

key-decisions:
  - Portal does not expose native criminal toggle - implemented deterministic filter profile strategy
  - Criminal indicators defined for post-result filtering (crimes hediondos, homicídio, tráfico, etc.)
  - Smoke tests validate selector stability without requiring actual form submission

requirements-completed: [EXTR-04]

# Metrics
duration: 5 min
completed: 2026-03-02
---

# Phase 11 Plan 3: Criminal Filter and Smoke Tests Summary

**Implemented deterministic criminal filter profile with TJGO smoke tests for selector drift detection**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-02T18:13:36Z
- **Completed:** 2026-03-02T18:18:48Z
- **Tasks:** 3
- **Files modified:** 8

## Accomplishments
- Criminal filter profile with deterministic strategy and Brazilian legal indicators
- Metadata propagation through search results for auditability
- 7 Playwright smoke tests validating TJGO portal selector stability
- All smoke tests pass against live TJGO ConsultaPublicacao

## Task Commits

Each task was committed atomically:

1. **Task 1: Implement deterministic criminal filter profile** - `079ff9e` (feat)
2. **Task 2: Propagate criminal filter metadata** - `0add8d5` (feat)
3. **Task 3: Add Playwright smoke tests** - `28a9845` (test)

**Plan metadata:** `77a1d29` (chore: DI registration)

## Files Created/Modified

- `src/OpenJustice.BrazilExtractor/Services/Tjgo/CriminalFilterProfile.cs` - Criminal filter strategy definition with legal indicators
- `src/OpenJustice.BrazilExtractor/Models/TjgoSearchQuery.cs` - Extended with CriminalFilterProfile
- `src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs` - Extended with filter metadata (profile, excluded counts, date window)
- `src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs` - Applies filter profile and logs active strategy
- `src/OpenJustice.BrazilExtractor/Services/Jobs/TjgoSearchJob.cs` - Detailed logging of filter application
- `src/OpenJustice.BrazilExtractor/Program.cs` - DI registration for services
- `tests/OpenJustice.Playwright/TjgoConsultaPublicacaoSmokeTests.cs` - 7 smoke tests for portal contract

## Decisions Made

- Portal has no native criminal toggle - implemented profile-based filtering with query parameters and post-result indicators
- Smoke tests validate selectors without submission to avoid rate limits/bot detection
- Criminal indicators include: "crime hediondo", "homicídio", "tráfico", "estupro", "roubo", etc.

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered

None

## User Setup Required

None - no external service configuration required.

## Next Phase Readiness

- Criminal filter is implemented, configurable, and traceable via logs
- Smoke tests detect selector drift before Phase 12 execution
- Ready for Phase 12 PDF capture pipeline

---
*Phase: 11-extractor-foundation-and-tjgo-search*
*Completed: 2026-03-02*
