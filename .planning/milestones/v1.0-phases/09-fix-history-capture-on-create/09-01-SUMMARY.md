---
phase: 09-fix-history-capture-on-create
plan: 01
subsystem: Generator / Case Workflow / History
tags: [history, case-creation, regression-tests, timeline]
dependency_graph:
  requires: []
  provides:
    - CaseWorkflowService.CreateCaseAsync now captures initial field history
    - Case history API works for brand-new cases
  affects:
    - CaseFieldHistoryService (used on create path)
    - CaseWorkflowServiceTests (new regression tests)
    - CaseHistoryControllerTests (new API tests)
tech_stack:
  added:
    - Initial history capture on case creation
    - Regression tests for create-path history
  patterns:
    - Append-only field history for both create and update paths
    - null -> value transitions for new cases
key_files:
  created: []
  modified:
    - src/OpenJustice.Generator/Services/Cases/CaseWorkflowService.cs
    - tests/OpenJustice.Generator.Tests/Cases/CaseWorkflowServiceTests.cs
    - tests/OpenJustice.Generator.Tests/History/CaseHistoryControllerTests.cs
decisions:
  - AppendChangesAsync called with oldCase=null for new cases to capture initial state
  - All tracked fields get null->value history entries on creation
  - Timestamps (ChangedAt, CreatedAt) and CuratorId are set from the request
metrics:
  duration: "~1 min"
  completed: "March 2, 2026"
  tasks: 2
---

# Phase 09 Plan 01: Fix History Capture on Create Summary

## One-Liner
Initial field history capture on case creation - timeline and diff features now work from the first saved version.

## Objective
Ensure new case creation writes initial immutable field history so timeline and diff features work from the first saved version. Close the create→history integration gap in Generator.

## What Was Built

### Task 1: Capture Initial Field History During Case Creation
Modified `CaseWorkflowService.CreateCaseAsync` to call `_fieldHistoryService.AppendChangesAsync()` after the case is persisted and has an ID:
- Pass `null` as oldCase to indicate this is a new case (no previous state)
- Captures all tracked fields as null → current value transitions
- Sets timestamp and curator metadata from the request
- Does not change update-path semantics (append-only remains intact)

### Task 2: Regression Coverage for Create-Path History and API Timeline
Added tests proving the new behavior works:

**CaseWorkflowServiceTests:**
- `CreateCaseAsync_CreatesInitialFieldHistory` - Verifies history entries are created with null → value transitions
- `CreateCaseAsync_HistoryHasNullToValueTransitions` - Verifies all history entries have null old values

**CaseHistoryControllerTests:**
- `GetCaseHistory_NewlyCreatedCase_ReturnsHistoryEntries` - API-level proof that history endpoint returns entries immediately after creation
- `GetCaseHistory_NewCaseTimeline_ContainsExpectedFields` - Verifies key fields (city, state, victims) are tracked in initial history
- Fixed `GetFieldHistory_ValidCaseAndField_ReturnsFieldHistory` to account for automatic initial history

## Verification
All 106 tests pass, including:
- 13 CaseWorkflowServiceTests (including 2 new)
- 9 CaseHistoryControllerTests (including 2 new, 1 fixed)

## Success Criteria

- [x] POST case creation path persists initial CaseFieldHistory entries
- [x] Initial entries represent null -> value transitions with timestamp/curator metadata
- [x] Case history endpoint returns timeline entries for brand-new cases
- [x] Regression tests enforce the behavior

## Deviations from Plan

### None - Plan Executed Exactly as Written

The plan specified exactly what was implemented:
1. Added `AppendChangesAsync` call in CreateCaseAsync after SaveChangesAsync with `oldCase = null`
2. Added regression tests that verify history is created without requiring UpdateCaseAsync
3. Added controller tests proving the API returns entries for newly created cases

## Test Results
```
Passed! - Failed: 0, Passed: 106, Skipped: 0, Total: 106
```

## Commits
- 1834e4c: feat(09-fix-history-capture-on-create): capture initial field history on case creation

## Self-Check
- [x] Files created: None (all existing files modified)
- [x] Commit exists: 1834e4c found
- [x] All tests pass: 106/106
