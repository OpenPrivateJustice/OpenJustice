---
phase: 05-reader-history-ui-polish
plan: 02
subsystem: reader-ui
tags: [history, timeline, diff, breadcrumbs, confidence-scores]
dependency_graph:
  requires:
    - ICaseHistoryService
    - ICaseDetailsService
    - CaseFieldHistoryViewModel
    - FieldDiffSelection
  provides:
    - CaseHistory page route
    - Timeline filter UI
    - A/B diff controls
    - Breadcrumb navigation
  affects:
    - CaseDetails.razor
    - CaseSources.razor
    - CaseEvidence.razor
tech_stack:
  added:
    - Blazor component (CaseHistory.razor)
    - CSS styling (CaseHistory.razor.css)
  patterns:
    - Standard anchor-based breadcrumbs for browser history compatibility
    - Index-based diff selection for deterministic A/B output
    - Threshold-based confidence badge coloring (>=80: green, >=50: yellow, <50: red)
key_files:
  created:
    - src/AtrocidadesRSS.Reader/Pages/Cases/CaseHistory.razor
    - src/AtrocidadesRSS.Reader/Pages/Cases/CaseHistory.razor.css
  modified:
    - src/AtrocidadesRSS.Reader/Pages/Cases/CaseDetails.razor
    - src/AtrocidadesRSS.Reader/Components/Cases/CaseSources.razor
    - src/AtrocidadesRSS.Reader/Components/Cases/CaseEvidence.razor
decisions:
  - Used standard anchor navigation for breadcrumbs instead of JS history manipulation
  - Used index-based selection for diff to ensure deterministic ordering
  - Applied consistent confidence badge styling across all components
metrics:
  duration: 3 minutes
  completed: March 1, 2026
---

# Phase 5 Plan 2: Reader History UI Summary

## One-Liner
Case history page with timeline filter, A/B diff comparison, breadcrumbs navigation, and standardized confidence score badges.

## Objective
Deliver the reader history UI and case-level confidence visualization, including navigation polish for details ↔ history flows. Purpose: Expose the same auditability power available in generator to public reader users.

## Completed Tasks

### Task 1: Build CaseHistory page with timeline filter and visual A/B diff
- **Created:** `CaseHistory.razor` at `/cases/{caseId}/history`
- **Features:**
  - Loads timeline entries from ICaseHistoryService
  - Field filter dropdown to filter by specific fields
  - Chronological history cards with changed date, actor/source, old/new values, confidence badge
  - A/B selectors for two history versions of a selected field
  - Side-by-side diff cards showing both versions
  - Empty-state messaging when no history exists
- **Created:** `CaseHistory.razor.css` with timeline and diff styling
- **Commit:** `1c2f05c`

### Task 2: Wire details-to-history navigation with breadcrumbs and back compatibility
- **Modified:** `CaseDetails.razor`
- **Features:**
  - Added breadcrumb navigation (Busca > Caso)
  - Added "Histórico" button linking to case history page
  - Check if case has history before showing link
  - Browser back/forward works naturally with standard anchor navigation
- **Modified:** `CaseHistory.razor` (already created with breadcrumbs)
- **Features:**
  - Breadcrumbs: Busca > Caso > Histórico
  - Link back to case details page
- **Commit:** `1c2f05c`

### Task 3: Surface confidence scores consistently across case detail sections
- **Modified:** `CaseSources.razor`
- **Changes:**
  - Added `GetConfidenceBadgeClass` method
  - Confidence badges now use color coding (>=80: green, >=50: yellow, <50: red)
- **Modified:** `CaseEvidence.razor`
- **Changes:**
  - Added `GetConfidenceBadgeClass` method
  - Confidence badges now use color coding (>=80: green, >=50: yellow, <50: red)
- **Existing:** `CaseHeader.razor` already had confidence display with progress bar
- **Commit:** `1c2f05c`

## Verification Results

- [x] Build passes: `dotnet build src/AtrocidadesRSS.Reader/AtrocidadesRSS.Reader.csproj`
- [x] CaseHistory route at `/cases/{caseId}/history` renders timeline entries
- [x] Diff comparison controls functional with A/B selectors
- [x] Confidence badges display 0-100% values with consistent threshold coloring
- [x] Breadcrumb links work between Search, CaseDetails, and CaseHistory
- [x] Browser back/forward navigation preserves expected route flow

## Deviation Documentation

### Auto-fixed Issues
None - all tasks executed as planned.

### Auth Gates
None - no authentication required for reader UI.

## Requirements Coverage

| Requirement | Status |
|-------------|--------|
| RDR-21: User can open case history page | ✅ Implemented |
| RDR-22: User can compare two versions | ✅ Implemented |
| RDR-23: User sees confidence scores | ✅ Implemented |
| RDR-27: User can navigate with breadcrumbs | ✅ Implemented |

## Self-Check

- [x] Files exist: CaseHistory.razor, CaseHistory.razor.css, CaseDetails.razor, CaseSources.razor, CaseEvidence.razor
- [x] Commit exists: 1c2f05c
- [x] Build passes without errors

## Self-Check: PASSED
