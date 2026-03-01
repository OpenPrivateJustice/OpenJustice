---
phase: 03-generator-history-system
plan: 01
subsystem: generator
tags: [history, audit, append-only, field-tracking]
dependency_graph:
  requires: []
  provides:
    - ICaseFieldHistoryService
    - CaseHistoryController
  affects:
    - CaseWorkflowService
    - AppDbContext
tech_stack:
  added:
    - System.Text.Json for field value serialization
  patterns:
    - Append-only history persistence
    - Field-diff detection
    - Confidence score per change
    - RFC7807 problem details for 404 responses
key_files:
  created:
    - src/AtrocidadesRSS.Generator/Services/History/ICaseFieldHistoryService.cs
    - src/AtrocidadesRSS.Generator/Services/History/CaseFieldHistoryService.cs
    - src/AtrocidadesRSS.Generator/Contracts/Cases/CaseFieldHistoryDto.cs
    - src/AtrocidadesRSS.Generator/Controllers/CaseHistoryController.cs
    - tests/AtrocidadesRSS.Generator.Tests/History/CaseFieldHistoryServiceTests.cs
    - tests/AtrocidadesRSS.Generator.Tests/History/CaseHistoryControllerTests.cs
  modified:
    - src/AtrocidadesRSS.Generator/Services/Cases/CaseWorkflowService.cs
    - src/AtrocidadesRSS.Generator/ServiceCollectionExtensions.cs
    - tests/AtrocidadesRSS.Generator.Tests/Cases/CaseWorkflowServiceTests.cs
    - tests/AtrocidadesRSS.Generator.Tests/Curation/CurationServiceTests.cs
decisions:
  - "Append-only pattern chosen for immutable audit trail"
  - "Confidence scores preserved per field change without rollback coupling"
  - "Field values serialized as JSON for flexibility"
  - "History ordered by ChangedAt descending for timeline rendering"
metrics:
  duration_minutes: 9
  completed_date: 2026-03-01
  files_created: 6
  files_modified: 4
  tests_added: 9
  tests_passed: 24
---

# Phase 03 Plan 01: Generator History System Summary

## Overview

Implemented the generator-side history engine that persists unlimited per-field changes in append-only mode and exposes query endpoints for timeline consumption.

## What Was Built

### Task 1: Append-Only CaseFieldHistory Capture Service

- **ICaseFieldHistoryService**: Interface defining history tracking operations
- **CaseFieldHistoryService**: Implementation with:
  - Field-diff detection comparing old vs new case values
  - Append-only persistence (only inserts, never updates/deletes)
  - Tracks 40+ mutable fields including victim, accused, crime, judicial info
  - Stores field name, previous/new serialized values, ChangedAt, CuratorId, ChangeConfidence
- **Integration**: CaseWorkflowService.UpdateCaseAsync now captures history after each update
- **DI Registration**: Service registered in ServiceCollectionExtensions

### Task 2: Case History Timeline API Endpoints

- **GET /api/cases/{caseId}/history**: Returns all field history ordered by ChangedAt descending
- **GET /api/cases/{caseId}/history/{fieldName}**: Returns history for specific field
- **RFC7807 404 responses** when case not found
- **CaseFieldHistoryDto**: API response with all required metadata

## Key Features

1. **Append-Only Behavior**: Every update creates new history rows without modifying prior entries
2. **Immutable Audit Trail**: Complete chain of changes preserved with curator identity and timestamps
3. **Confidence Tracking**: ChangeConfidence (0-100) stored per history entry
4. **Timeline-Ready**: History ordered descending for UI timeline rendering

## Test Coverage

- **CaseFieldHistoryServiceTests**: 8 tests covering:
  - Creating history from scratch
  - Tracking only changed fields
  - Append-only behavior verification
  - Query by case and by field
  - Confidence score preservation

- **CaseHistoryControllerTests**: 5 tests covering:
  - Valid case history retrieval
  - 404 for non-existent cases
  - Field-specific history
  - Required field presence

- **CaseWorkflowServiceTests**: Updated to use new service dependency

## Deviation Documentation

**None - plan executed exactly as written.**

## Self-Check: PASSED

- [x] CaseFieldHistoryService.cs exists
- [x] ICaseFieldHistoryService.cs exists
- [x] CaseHistoryController.cs exists
- [x] CaseFieldHistoryDto.cs exists
- [x] Commit 59c9e1e exists
- [x] All tests pass (24 total)

## Requirements Satisfied

- GEN-18: History capture automatic on case updates
- GEN-19: History API returns change metadata for timeline
- GEN-20: Confidence data preserved (0-100) without rollback coupling
