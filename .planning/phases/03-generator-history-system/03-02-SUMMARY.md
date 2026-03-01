---
phase: 03-generator-history-system
plan: 02
subsystem: generator
tags: [history, timeline, diff, blazor-ui, curator-tools]
dependency_graph:
  requires:
    - 03-01 (CaseHistoryController, ICaseFieldHistoryService)
  provides:
    - CaseHistory.razor page
    - CaseFieldHistoryViewModel
    - History navigation from CaseEdit
  affects:
    - GeneratorApiClient
    - CaseEdit.razor
tech_stack:
  added:
    - Blazor Server page routing
    - Timeline UI components
  patterns:
    - Visual diff comparison (A/B selector)
    - Confidence score display (Alta/Média/Baixa)
    - Field filtering for timeline
key_files:
  created:
    - src/AtrocidadesRSS.Generator.Web/Models/Cases/CaseFieldHistoryViewModel.cs
    - src/AtrocidadesRSS.Generator.Web/Pages/Cases/CaseHistory.razor
    - src/AtrocidadesRSS.Generator.Web/Pages/Cases/CaseHistory.razor.css
    - tests/AtrocidadesRSS.Generator.Tests/History/CaseHistoryApiContractTests.cs
  modified:
    - src/AtrocidadesRSS.Generator.Web/Services/GeneratorApiClient.cs
    - src/AtrocidadesRSS.Generator.Web/Pages/Cases/CaseEdit.razor
    - tests/AtrocidadesRSS.Generator.Tests/AtrocidadesRSS.Generator.Tests.csproj
decisions:
  - "Append-only history captured in plan 03-01 provides foundation for timeline"
  - "Confidence scores (0-100) displayed as labels for curator trust assessment"
  - "Field filtering allows curators to narrow timeline to specific attributes"
  - "Visual diff uses side-by-side cards with before/after values"
metrics:
  duration_minutes: 12
  completed_date: 2026-03-01
  files_created: 4
  files_modified: 3
  tests_added: 9
  tests_passed: 96
---

# Phase 03 Plan 02: Generator History UI Summary

## Overview

Delivered the generator web UI for timeline visualization and visual diff comparison, enabling curators to inspect how each field evolved across all edits.

## What Was Built

### Task 1: API Client and View Models (Committed: 2b6ebb6)

- **CaseFieldHistoryViewModel**: UI-facing model with:
  - Field name formatting (PascalCase → Title Case)
  - Confidence level labels (Alta ≥80%, Média ≥50%, Baixa)
  - Badge CSS classes for visual styling
  - Curator display fallback to "Sistema"
  - Value formatting for null/empty states
- **GeneratorApiClient extensions**:
  - `GetCaseHistoryAsync(caseId)` - full timeline
  - `GetCaseFieldHistoryAsync(caseId, fieldName)` - filtered
  - Resilient error handling (returns empty list on 404/errors)
- **CaseHistoryApiContractTests**: 9 tests covering client behavior

### Task 2: CaseHistory Timeline Page (Committed: a5a5828)

- **Route**: `/cases/{caseId}/history`
- **Components**:
  - Breadcrumb navigation (Casos → Editar Caso → Histórico)
  - Field filter dropdown with change counts per field
  - A/B version selector for diff comparison
  - Side-by-side before/after cards showing:
    - Changed timestamp
    - Curator identity
    - Confidence score with badge
    - Old value / New value in code blocks
  - Chronological timeline with visual connectors
- **Features**:
  - Read-only inspection (no mutation controls)
  - Confidence scores visible throughout
  - Filter resets diff selections

### Task 3: History Navigation Wiring (Committed: a5a5828)

- Added "Histórico" button to CaseEdit.razor
- Links to `/cases/{id}/history` with case context
- Return navigation via breadcrumb back to case edit

## Key Features

1. **Timeline Visualization**: Chronological display of all field changes with visual timeline connectors
2. **Field Filtering**: Narrow timeline to specific fields (e.g., only CrimeDescription changes)
3. **Visual Diff (A/B)**: Side-by-side comparison cards showing before/after values
4. **Confidence Display**: Color-coded badges (green/yellow/red) showing trust level per change
5. **Curator Attribution**: Each change shows who made it and when

## Test Coverage

- **CaseHistoryApiContractTests**: 9 tests covering:
  - Empty history returns empty list
  - History with entries returns view models
  - 404 handling returns empty list
  - Field-filtered queries work correctly
  - Confidence level formatting (Alta/Média/Baixa)
  - Field name formatting
  - Null curator handling ("Sistema")
  - Empty value handling ("(vazio)")

- All 96 tests in the project pass

## Deviation Documentation

**None - plan executed exactly as written.**

## Requirements Satisfied

- **GEN-21**: Generator UI exposes history timeline for each case
- **GEN-22**: Diff view compares two selected versions with before/after clarity, confidence scores displayed

## Self-Check: PASSED

- [x] CaseHistory.razor exists
- [x] CaseHistory.razor.css exists  
- [x] CaseFieldHistoryViewModel.cs exists
- [x] GeneratorApiClient has history methods
- [x] CaseEdit.razor has history navigation link
- [x] Commit 2b6ebb6 exists (Task 1)
- [x] Commit a5a5828 exists (Task 2-3)
- [x] All tests pass (96 total)
