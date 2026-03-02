---
phase: 05-reader-history-ui-polish
plan: "01"
subsystem: reader
tags: [history, timeline, diff, viewmodels, data-layer]
dependency_graph:
  requires:
    - src/OpenJustice.Reader/Services/Data/ILocalCaseStore
    - src/OpenJustice.Reader/Services/Data/SqliteCaseStore
    - src/OpenJustice.Generator.Web/Models/Cases/CaseFieldHistoryViewModel
  provides:
    - src/OpenJustice.Reader/Models/Cases/CaseHistoryViewModel
    - src/OpenJustice.Reader/Services/Cases/ICaseHistoryService
    - src/OpenJustice.Reader/Services/Cases/CaseHistoryService
  affects:
    - src/OpenJustice.Reader/Program.cs
tech_stack:
  added:
    - LocalCaseFieldHistory record (data entity)
    - CaseFieldHistoryViewModel (UI model with formatting)
    - FieldHistoryGroup (grouped timeline)
    - FieldDiffSelection (A/B diff model)
    - ICaseHistoryService interface
    - CaseHistoryService implementation
  patterns:
    - Mirrors generator's CaseFieldHistoryViewModel for consistency
    - Additive history parsing (keeps existing case import stable)
    - Resilient service (empty collections on no history)
    - Deterministic ordering (newest first)
    - Index-based stable A/B selection
key_files:
  created:
    - src/OpenJustice.Reader/Models/Cases/CaseHistoryViewModel.cs
    - src/OpenJustice.Reader/Services/Cases/ICaseHistoryService.cs
    - src/OpenJustice.Reader/Services/Cases/CaseHistoryService.cs
  modified:
    - src/OpenJustice.Reader/Services/Data/ILocalCaseStore.cs
    - src/OpenJustice.Reader/Services/Data/SqliteCaseStore.cs
    - src/OpenJustice.Reader/Program.cs
key_decisions:
  - Additive history parsing preserves existing case import behavior
  - Service resilience: returns empty collections instead of throwing
  - Index-based diff selection ensures stable/deterministic A/B output
  - Newest-first ordering for all timeline queries
---

# Phase 5 Plan 1: Reader History Data Foundation

**One-liner:** Local history parsing from SQL snapshots + history service with timeline/diff APIs

## Summary

Successfully created the reader-side history data foundation enabling Phase 5 UI features to run against locally imported snapshot data. The implementation provides:

1. **Local Store Extension**: Added `LocalCaseFieldHistory` entity and query methods to `ILocalCaseStore` interface with in-memory storage in `SqliteCaseStore`. SQL parsing now captures `CaseFieldHistory` INSERT statements alongside existing case imports.

2. **History Service**: Implemented `ICaseHistoryService` with full timeline, per-field timeline, and A/B diff selection methods. Service is read-only and resilient—returns empty collections when no history exists.

3. **View Models**: Created `CaseHistoryViewModel` mirroring the generator pattern with formatted field labels, confidence level badges/classes, empty-value formatting, and chronology metadata.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add local history records and query methods | 07b75b2 | ILocalCaseStore.cs, SqliteCaseStore.cs |
| 2 | Implement history service and timeline/diff view models | 193f7b2 | CaseHistoryViewModel.cs, ICaseHistoryService.cs, CaseHistoryService.cs, Program.cs |

## Verification

- ✅ `dotnet build src/OpenJustice.Reader/OpenJustice.Reader.csproj` succeeds
- ✅ Timeline/diff UI can consume real history data from local store APIs
- ✅ No regressions in existing search/details compile path
- ✅ Deterministic ordering (newest first) for all timeline queries
- ✅ Stable A/B selection output

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None.

## Deferred Issues

None.

---

**Duration:** ~2 min  
**Completed:** March 1, 2026
