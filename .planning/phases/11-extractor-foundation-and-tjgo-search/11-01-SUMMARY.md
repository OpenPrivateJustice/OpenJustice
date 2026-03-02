---
phase: 11-extractor-foundation-and-tjgo-search
plan: "01"
subsystem: extractor
tags: [.net, worker, configuration, tjgo]

# Dependency graph
requires:
  - phase: 10-generator-metadata
    provides: Solution infrastructure and existing project patterns
provides:
  - OpenJustice.BrazilExtractor worker project with typed options
  - Fail-fast configuration validation at startup
  - Settings contract for Phase 11-12 continuity
affects: [11-extractor-foundation-and-tjgo-search]

# Tech tracking
tech-stack:
  added: [Microsoft.Playwright, Microsoft.Extensions.Hosting, Microsoft.Extensions.Options]
  patterns: [IValidateOptions pattern, ValidateOnStart, BackgroundService]

key-files:
  created:
    - src/OpenJustice.BrazilExtractor/OpenJustice.BrazilExtractor.csproj
    - src/OpenJustice.BrazilExtractor/Program.cs
    - src/OpenJustice.BrazilExtractor/Worker.cs
    - src/OpenJustice.BrazilExtractor/appsettings.json
    - src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptions.cs
    - src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptionsValidator.cs
  modified:
    - OpenJustice.sln

key-decisions:
  - Used IValidateOptions<T> for custom validation rules (more flexible than DataAnnotations)
  - ValidateOnStart() ensures fail-fast before any service instantiation

requirements-completed: [EXTR-01, EXTR-13, EXTR-14]

# Metrics
duration: 15 min
completed: 2026-03-02
---

# Phase 11 Plan 1: BrazilExtractor Foundation Summary

**Scaffolded OpenJustice.BrazilExtractor worker project with validated configuration and fail-fast options validation**

## Performance

- **Duration:** 15 min
- **Started:** 2026-03-02T17:32:24Z
- **Completed:** 2026-03-02T17:48:02Z
- **Tasks:** 2
- **Files modified:** 7

## Accomplishments
- Created buildable .NET 10 worker project with Microsoft.Playwright dependency
- Implemented strongly-typed BrazilExtractorOptions with all required settings (TJGO URL, date window, criminal mode, download path, query cadence)
- Added fail-fast options validation that prevents startup when required config is missing or invalid
- Verified startup succeeds with valid config and fails immediately with missing required keys

## Task Commits

Each task was committed atomically:

1. **Task 1: Scaffold BrazilExtractor worker project and solution wiring** - `1517075` (feat)
2. **Task 2: Add typed extractor settings and fail-fast options validation** - `1517075` (feat)

**Plan metadata:** `1517075` (docs: complete plan)

## Files Created/Modified
- `src/OpenJustice.BrazilExtractor/OpenJustice.BrazilExtractor.csproj` - Worker project targeting net10.0 with Playwright
- `src/OpenJustice.BrazilExtractor/Program.cs` - DI registration with AddOptions + ValidateOnStart
- `src/OpenJustice.BrazilExtractor/Worker.cs` - Background service using IOptions<BrazilExtractorOptions>
- `src/OpenJustice.BrazilExtractor/appsettings.json` - BrazilExtractor config section with required keys
- `src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptions.cs` - Strongly typed settings contract
- `src/OpenJustice.BrazilExtractor/Configuration/BrazilExtractorOptionsValidator.cs` - Fail-fast validation rules
- `OpenJustice.sln` - Added BrazilExtractor project

## Decisions Made
- Used IValidateOptions<T> pattern for custom validation (more flexible than DataAnnotations)
- ValidateOnStart() ensures configuration errors fail immediately at host startup

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
None

## User Setup Required
None - no external service configuration required.

## Next Phase Readiness
- BrazilExtractor foundation complete with validated configuration
- Ready for TJGO portal automation and PDF extraction implementation in subsequent plans

---
*Phase: 11-extractor-foundation-and-tjgo-search*
*Completed: 2026-03-02*
