---
phase: 12-pdf-acquisition-pipeline
plan: "02"
subsystem: BrazilExtractor
tags:
  - pdf-extraction
  - tjgo
  - pdf-download
  - throttling
dependency_graph:
  requires:
    - EXTR-06
    - EXTR-07
    - EXTR-08
  provides:
    - PDF download persistence service
    - Query-level 30s cadence enforcement
    - Acquisition telemetry logging
  affects:
    - TjgoSearchService
    - TjgoSearchJob
    - Worker
    - PdfDownloadService
    - TjgoSearchResult
tech_stack:
  added:
    - PdfDownloadBatchResult model
    - IPdfDownloadService interface
    - PdfDownloadService implementation
    - Query cadence tracking
    - Download result in TjgoSearchResult
    - Worker telemetry logging
  patterns:
    - Deterministic unique filenames (query date + URL hash + sequence)
    - FileMode.CreateNew for collision prevention
    - Query-level throttling (not per-file)
    - Comprehensive acquisition telemetry
key_files:
  created:
    - src/OpenJustice.BrazilExtractor/Models/PdfDownloadBatchResult.cs
    - src/OpenJustice.BrazilExtractor/Services/Downloads/IPdfDownloadService.cs
    - src/OpenJustice.BrazilExtractor/Services/Downloads/PdfDownloadService.cs
  modified:
    - src/OpenJustice.BrazilExtractor/Models/TjgoSearchResult.cs
    - src/OpenJustice.BrazilExtractor/Services/Tjgo/TjgoSearchService.cs
    - src/OpenJustice.BrazilExtractor/Services/Jobs/TjgoSearchJob.cs
    - src/OpenJustice.BrazilExtractor/Worker.cs
    - src/OpenJustice.BrazilExtractor/Program.cs
decisions:
  - "Used singleton HttpClient for PDF downloads (proper disposal pattern)"
  - "Filename format: tjgo_{date}_{hash}_{sequence}.pdf for collision safety"
  - "Query cadence enforced at job level, not service level (simpler, more testable)"
  - "Telemetry logged at both job and worker levels for different visibility needs"
metrics:
  duration_minutes: 8
  completed_date: "2026-03-02"
  tasks_completed: 3
  files_created: 3
  files_modified: 5
---

# Phase 12 Plan 02: PDF Acquisition Pipeline Summary

## Objective

Implement end-to-end PDF acquisition: query-level throttling plus collision-safe local persistence for all harvested links. Deliver EXTR-07 and EXTR-08 while preserving the locked deterministic cadence (15 PDFs/query, 30s between query executions).

## What Was Built

### Task 1: PDF Download Persistence Service

Created dedicated download service with:
- **IPdfDownloadService interface** - Contract for batch PDF downloads
- **PdfDownloadService implementation** - Singleton service with collision-safe unique naming
- **Filename format:** `tjgo_{date}_{url-hash}_{sequence}.pdf` (e.g., `tjgo_2026-03-02_a1b2c3d4_001.pdf`)
- **FileMode.CreateNew** - Atomic creation prevents accidental overwrites
- **Batch result metadata** - Tracks attempted/succeeded/failed counts with file paths and failure reasons

### Task 2: Query-Level Cadence + Integration

Refactored acquisition flow with:
- **Query execution timestamps** - QueryExecutionStartUtc/EndUtc for cadence verification
- **Page index tracking** - For pagination support
- **Cadence enforcement** - Waits between query/pagination executions (not between PDF downloads)
- **Download integration** - PDFs downloaded immediately after link harvesting
- **Static lastQueryTime tracking** - Persists across job instances in worker process

### Task 3: Acquisition Telemetry

Enhanced logging at both job and worker levels:
- **Query-level telemetry:** Date window, filter profile, records found, pages traversed
- **Link harvest telemetry:** Total seen, unique retained, capped status
- **Download telemetry:** Attempted, succeeded, failed counts with file paths
- **Timing telemetry:** Query start/end times for cadence verification (EXTR-07)
- **Failure logging:** URLs, HTTP status codes, reason messages

## Verification

- ✅ `dotnet build src/OpenJustice.BrazilExtractor/OpenJustice.BrazilExtractor.csproj` succeeds
- ✅ DI resolves IPdfDownloadService
- ✅ Query cadence tracking implemented in TjgoSearchJob
- ✅ Download result attached to TjgoSearchResult
- ✅ Worker surfaces comprehensive telemetry

## EXTR-07/08 Status

**EXTR-07 (Query-Level Throttling):** ✅ Satisfied
- Query interval enforced between query/pagination executions
- Configurable via `BrazilExtractor:QueryIntervalSeconds` (default 30s)
- Timestamps logged for verification

**EXTR-08 (Collision-Safe Persistence):** ✅ Satisfied
- Unique filenames with query date, URL hash, and sequence
- FileMode.CreateNew prevents overwrites
- Batch results with success/failure tracking

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
| 1 | 28c85f0 | feat(12-02): add PDF download persistence service with collision-safe unique naming |
| 2 | d310ee2 | feat(12-02): enforce query-level 30s cadence and integrate PDF downloads |
| 3 | 588fc6a | feat(12-02): surface acquisition telemetry in worker-level logs |
