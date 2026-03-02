---
phase: 12-pdf-acquisition-pipeline
plan: "01"
subsystem: BrazilExtractor
tags:
  - pdf-extraction
  - tjgo
  - link-harvesting
dependency_graph:
  requires:
    - EXTR-06
  provides:
    - PDF link typed model
    - Search result with PdfLinks collection
    - PDF selector smoke tests
  affects:
    - TjgoSearchService
    - TjgoPublicationPdfLink
    - TjgoSearchResult
    - TjgoConsultaPublicacaoSmokeTests
tech_stack:
  added:
    - TjgoPublicationPdfLink model
    - PdfLinks collection in TjgoSearchResult
    - PDF link extraction in TjgoSearchService
    - PDF link smoke test
  patterns:
    - URL normalization (relative to absolute)
    - De-duplication by normalized URL
    - MaxResultsPerQuery cap enforcement
    - DOM order preservation
key_files:
  created:
    - src/OpenJustice.BrazilExtractor/Models/TjgoPublicationPdfLink.cs
  modified:
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs
    - tests/OpenJustice.Playwright/TjgoConsultaPublicacaoSmokeTests.cs
decisions:
  - "Used multiple CSS selector patterns to detect PDF links (more resilient to portal changes)"
  - "Added fallback generic anchor scan when specific selectors fail"
  - "Included no-results marker detection in smoke test for complete contract coverage"
metrics:
  duration_minutes: 5
  completed_date: "2026-03-02"
  tasks_completed: 3
  files_created: 1
  files_modified: 3
---

# Phase 12 Plan 01: PDF Link Capture Contract Summary

## Objective

Establish a reliable PDF-link capture contract from TJGO query results before implementing file download persistence. Purpose is to satisfy EXTR-06 foundation by ensuring link harvesting is deterministic, capped, and observable.

## What Was Built

### Task 1: Typed PDF-Link Capture Contract

Created `TjgoPublicationPdfLink` model with:
- NormalizedUrl (absolute URL after resolution)
- OriginalHref (raw DOM value)
- DomOrderIndex (for deterministic ordering)
- SourcePageIndex (pagination support)
- DisplayText (optional context)
- CapturedAt timestamp

Extended `TjgoSearchResult` with:
- `PdfLinks` collection (IReadOnlyList, non-null)
- `TotalLinksSeen` (before de-dup)
- `UniqueLinksRetained` (after de-dup)
- `WasCapped` (boolean flag)
- `MaxResultsPerQuery` (config value)
- New `SuccessfulWithPdfLinks` factory method

### Task 2: Result-Page PDF Link Harvesting

Implemented in `TjgoSearchService`:
- `ExtractPdfLinkAsync` method with multiple selector patterns
- URL normalization (relative → absolute)
- De-duplication via HashSet<StringComparer.OrdinalIgnoreCase>
- Cap enforcement at MaxResultsPerQuery (default 15)
- Pattern matching for PDF detection (.pdf, /download, downloadPdf, etc.)
- Display text extraction (text content, title, aria-label)
- PdfLinkHarvestResult internal class for statistics

### Task 3: Playwright Smoke Contract

Added `TjgoConsultaPublicacao_ShouldDetectPdfLinkSelectors`:
- Fills form and submits (7 days ago for likely results)
- Tests all PDF link selector patterns
- Falls back to generic anchor scan
- Checks for no-results marker as valid state
- Validates extractor contract: can detect result OR empty state

## Verification

- ✅ `dotnet build src/OpenJustice.BrazilExtractor/OpenJustice.BrazilExtractor.csproj` succeeds
- ✅ `dotnet build tests/OpenJustice.Playwright/OpenJustice.Playwright.csproj` succeeds

## EXTR-06 Foundation

**Status:** ✅ Satisfied

- Deterministic harvesting: DOM order preserved via DomOrderIndex
- Capped results: MaxResultsPerQuery enforcement (default 15)
- Observable metadata: TotalLinksSeen, UniqueLinksRetained, WasCapped exposed in result

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None - no authentication requirements in this plan.

## Deferred Issues

None - all tasks completed.

---

## Commits

| Task | Commit | Message |
|------|--------|---------|
| 1 | cb06bbc | feat(12-01): add typed PDF-link capture contract to TJGO search results |
| 2 | f98b35a | feat(12-01): implement result-page PDF link harvesting in TjgoSearchService |
| 3 | 4856ca9 | test(12-01): extend Playwright smoke contract to guard PDF-link selectors |
