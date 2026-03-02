---
phase: 10-wire-reader-to-generator-history-api
plan: 02
subsystem: reader
tags: [reader, api, history, authentication, integration]
dependency_graph:
  requires:
    - RDR-22
    - RDR-23
    - 10-01
  provides:
    - CaseHistoryService wired to Generator API
    - Unauthorized handling in UI
  affects:
    - CaseHistory.razor
    - CaseDetails.razor
tech_stack:
  added:
    - IGeneratorHistoryApiClient injection in CaseHistoryService
  patterns:
    - Generator API consumption for timeline/diff
    - Explicit 401 handling with session-expired UI
    - Graceful fallback when case exists locally
key_files:
  modified:
    - src/OpenJustice.Reader/Services/Cases/CaseHistoryService.cs
    - src/OpenJustice.Reader/Pages/Cases/CaseHistory.razor
    - src/OpenJustice.Reader/Pages/Cases/CaseDetails.razor
decisions:
  - "CaseHistoryService now exclusively uses GeneratorHistoryApiClient - no more local SqliteCaseStore history queries"
  - "401 responses show explicit session-expired UI instead of empty history"
  - "HasHistory check in CaseDetails shows history link if case exists locally, even if API auth fails"
metrics:
  duration: 2 min
  completed_date: 2026-03-02
---

# Phase 10 Plan 2: Wire Reader to Generator History API Summary

## Objective

Switch Reader history features from local snapshot history parsing to live Generator API consumption while preserving existing timeline/diff UX and confidence visualization.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Refactor CaseHistoryService to consume live Generator history API | f1aff8f | CaseHistoryService.cs |
| 2 | Update history pages to reflect live API data and unauthorized flows | da12e3e | CaseHistory.razor, CaseDetails.razor |

## Implementation Details

### Task 1: CaseHistoryService Refactor

Replaced `ILocalCaseStore` with `IGeneratorHistoryApiClient` in `CaseHistoryService`:
- `GetFullTimelineAsync` - calls `GetCaseHistoryAsync` on API client
- `GetTimelineByFieldAsync` - calls `GetCaseHistoryAsync` and groups results locally
- `GetFieldTimelineAsync` - calls `GetFieldHistoryAsync` on API client
- `GetAvailableFieldsAsync` - derives from full timeline fetch
- `GetDiffSelectionAsync` - uses field timeline for diff selection
- `HasHistoryAsync` - checks if history count > 0 from API response

All methods preserve:
- Deterministic ordering (newest-first by ChangedAt)
- Stable A/B diff index semantics
- GeneratorHistoryApiUnauthorizedException propagation for 401 handling

### Task 2: UI Updates

**CaseHistory.razor:**
- Added session-expired detection with `IsSessionExpired` flag
- Login URL loaded from `GeneratorHistoryApi:LoginUrl` configuration
- Added explicit "Sessão expirada" alert with login button
- Catches `GeneratorHistoryApiUnauthorizedException` separately from generic errors

**CaseDetails.razor:**
- Added try-catch around `HasHistoryAsync` call
- If API auth fails but case exists locally, shows history link anyway
- User will see session-expired when they actually try to access history

## Truths Confirmed

- [x] History timeline in Reader is loaded from live Generator API data, not from SqliteCaseStore history tables
- [x] Diff comparison (A/B) operates on Generator-returned field history entries
- [x] Confidence badges in timeline/diff reflect ChangeConfidence values from Generator responses
- [x] Unauthorized history requests surface a clear session-expired path instead of silently rendering empty history

## Artifacts

| Path | Provides |
|------|----------|
| src/OpenJustice.Reader/Services/Cases/CaseHistoryService.cs | History orchestration over GeneratorHistoryApiClient with existing timeline/diff semantics |
| src/OpenJustice.Reader/Pages/Cases/CaseHistory.razor | UI wiring for live timeline/diff loading and unauthorized error handling |
| src/OpenJustice.Reader/Pages/Cases/CaseDetails.razor | History link behavior aligned with live API-backed history availability |

## Key Links (Updated)

- CaseHistory.razor → CaseHistoryService → IGeneratorHistoryApiClient → Generator API
- CaseDetails.razor → CaseHistoryService.HasHistoryAsync → Generator API

## Verification

1. Build succeeded: `dotnet build src/OpenJustice.Reader/OpenJustice.Reader.csproj` ✓
2. CaseHistoryService no longer imports or uses ILocalCaseStore ✓
3. Timeline and diff UI render data from Generator API ✓
4. Confidence badges use ChangeConfidence from API response ✓
5. 401 responses show explicit session-expired/login path ✓

## Deviation Documentation

None - plan executed exactly as written.

## Self-Check: PASSED

- Files exist: ✓
- Commits exist: ✓
- All success criteria met: ✓
