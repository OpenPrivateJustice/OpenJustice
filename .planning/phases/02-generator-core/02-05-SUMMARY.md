---
phase: 02-generator-core
plan: 05
subsystem: generator-core
tags: [api, evidence, tags, export, configuration]
dependency_graph:
  requires:
    - 02-02-PLAN (curation workflow)
    - 02-04-PLAN (discovery ingestion)
  provides:
    - GEN-12 (evidence association API)
    - GEN-13 (tag association API)
    - GEN-15 (snapshot export service)
    - GEN-16 (snapshot versioning)
    - GEN-17 (appsettings configuration)
  affects:
    - API endpoints
    - Export services
    - Configuration system
tech_stack:
  added:
    - Microsoft.Extensions.Options
    - System.Diagnostics.Process (for pg_dump)
  patterns:
    - IOptions pattern for configuration
    - IValidateOptions for startup validation
    - RESTful API controller conventions
    - EF Core Include() for navigation properties
key_files:
  created:
    - src/AtrocidadesRSS.Generator/Controllers/CasesEvidenceController.cs
    - src/AtrocidadesRSS.Generator/Controllers/CasesTagsController.cs
    - src/AtrocidadesRSS.Generator/Services/Export/SnapshotExportService.cs
    - src/AtrocidadesRSS.Generator/Services/Export/SnapshotVersionService.cs
    - src/AtrocidadesRSS.Generator/Configuration/GeneratorOptions.cs
    - tests/AtrocidadesRSS.Generator.Tests/Cases/CasesMetadataTests.cs
    - tests/AtrocidadesRSS.Generator.Tests/Export/SnapshotExportServiceTests.cs
  modified:
    - src/AtrocidadesRSS.Generator/ServiceCollectionExtensions.cs
    - src/AtrocidadesRSS.Generator/appsettings.json
decisions:
  - "Evidence association uses EF Core relationship updates with duplicate-prevention"
  - "Tag association supports both Tag ID lookup and TagName create-or-find"
  - "Snapshot versioning uses sequential vN.sql naming with regex pattern matching"
  - "GeneratorOptions consolidates all config into single options class with validation"
  - "Export service requires pg_dump availability check before attempting export"
metrics:
  duration: ~10 min
  completed: March 1, 2026
  tasks_completed: 3/3
  files_created: 7
  tests_added: 23
  tests_total_passed: 81
---

# Phase 2 Plan 5: Generator Core Finalization Summary

## Objective

Finalize generator core with metadata association endpoints (evidence/tags), snapshot export/versioning, and appsettings-driven runtime configuration. Enable full curator data enrichment and produce distributable database artifacts for downstream reader consumption.

## Key Truths Achieved

- ✅ Curator can associate evidence and tags with cases through API endpoints
- ✅ Generator exports complete PostgreSQL snapshot SQL files  
- ✅ Snapshot files are versioned sequentially (v1, v2, ...) and configurable through appsettings
- ✅ Generator runtime features (scraping/export/paths/db) read from validated appsettings options

## Implementation Details

### Task 1: Evidence/Tag Association APIs

**Created:**
- `CasesEvidenceController.cs` - REST endpoints for managing evidence:
  - `GET /api/cases/{caseId}/evidence` - Get all evidence for case
  - `POST /api/cases/{caseId}/evidence` - Add evidence to case
  - `DELETE /api/cases/{caseId}/evidence/{evidenceId}` - Remove evidence
  
- `CasesTagsController.cs` - REST endpoints for managing tags:
  - `GET /api/cases/{caseId}/tags` - Get all tags for case
  - `POST /api/cases/{caseId}/tags` - Add tag (by TagId or TagName)
  - `DELETE /api/cases/{caseId}/tags/{tagId}` - Remove tag

**Features:**
- Duplicate prevention (same link/filename for evidence, same tag for case)
- Entity existence validation
- 409 Conflict on duplicates
- 404 Not Found on invalid case/tag IDs
- Supports both Tag ID lookup and Tag Name create-or-find pattern

### Task 2: SQL Snapshot Export & Versioning

**Created:**
- `SnapshotVersionService.cs` - Manages sequential versioning:
  - `GetNextVersionAsync()` - Determines next version number
  - `GenerateFileName()` - Creates versioned filenames (e.g., "snapshot-v1.sql")
  - `GetExistingVersionsAsync()` - Scans directory for existing versions
  
- `SnapshotExportService.cs` - Exports PostgreSQL database:
  - Uses `pg_dump` for SQL export
  - Configurable via `SnapshotExportOptions`
  - Validates pg_dump availability before export
  - Returns `SnapshotExportResult` with file path, version, size, and duration

**Configuration:**
- Supports both connection string and host/port/user/password
- Custom pg_dump path support
- Export options: clean, if-exists, no-owner, no-privileges

### Task 3: Appsettings Options & Validation

**Created:**
- `GeneratorOptions.cs` - Consolidated options class:
  - `DatabaseOptions` - Connection string, host, port, credentials
  - `FilePathsOptions` - Snapshot, backup, export, temp directories
  - `ExportOptions` - File prefix, pg_dump path, export behavior
  - `GeneratorOptionsValidator` - Startup validation

**Updated:**
- `ServiceCollectionExtensions.cs`:
  - Added `AddExportServices()` method
  - Registered `ISnapshotVersionService` and `ISnapshotExportService`
  - Wired `SnapshotExportOptions` from configuration
- `appsettings.json` - Added Database and Export sections

## Test Coverage

**New Tests Added:**
- 18 tests for CasesMetadataTests (evidence + tags)
- 13 tests for SnapshotVersionService  
- 5 tests for SnapshotExportService

**All Tests Pass:**
- 81 total tests (76 Generator + 5 Web)

## Deviations from Plan

None - plan executed exactly as written.

## Artifacts Delivered

| Path | Purpose |
|------|---------|
| Controllers/CasesEvidenceController.cs | Evidence association API |
| Controllers/CasesTagsController.cs | Tag association API |
| Services/Export/SnapshotExportService.cs | PostgreSQL snapshot export |
| Services/Export/SnapshotVersionService.cs | Sequential version naming |
| Configuration/GeneratorOptions.cs | Consolidated config options |

---

## Self-Check: PASSED

- ✅ All files created exist
- ✅ Commit 9aac9d5 exists
- ✅ Tests pass (81 total)
- ✅ Build succeeds
