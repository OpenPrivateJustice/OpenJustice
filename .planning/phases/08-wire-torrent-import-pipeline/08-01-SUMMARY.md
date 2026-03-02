---
phase: 08-wire-torrent-import-pipeline
plan: 01
subsystem: Generator Export
tags: [generator, export, reader, sql-compatibility, pgdump]
dependency_graph:
  requires:
    - GEN-15 (Snapshot export service)
  provides:
    - Reader-compatible SQL export contract
  affects:
    - Reader SQL import pipeline
tech_stack:
  added:
    - --inserts pgdump flag for INSERT-based SQL output
  patterns:
    - Contract testing for export SQL format
    - Reader contract documentation inline
key_files:
  created: []
  modified:
    - src/OpenJustice.Generator/Services/Export/SnapshotExportService.cs
    - tests/OpenJustice.Generator.Tests/Export/SnapshotExportServiceTests.cs
decisions:
  - Used --inserts flag instead of --column-inserts for simpler SQL format
  - Added inline Reader Contract documentation for maintainability
  - Created test helper method for contract verification without requiring pg_dump execution
metrics:
  duration: 5 min
  completed_date: 2026-03-02
---

# Phase 08 Plan 01: Wire Torrent Import Pipeline - Summary

## One-Liner

Added --inserts pgdump flag and contract tests to ensure Generator SQL exports are compatible with Reader import pipeline.

## Objective

Harden Generator snapshot export so SQL format and table naming are explicitly compatible with Reader SQL import. Eliminate Generator→Reader contract drift that blocks real torrent snapshot ingestion.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Enforce Reader-compatible pg_dump SQL output contract | 8505832 | SnapshotExportService.cs |
| 2 | Add export contract tests for SQL compatibility | d87de73 | SnapshotExportServiceTests.cs |

## Changes Made

### Task 1: Snapshot Export Service (8505832)

- Added `--inserts` flag to `BuildPgDumpArguments()` method
- Added inline documentation explaining Reader Contract for SQL parser compatibility
- Added `BuildPgDumpArgumentsForTest()` public method for test verification
- Ensures pg_dump produces INSERT statements instead of COPY for Reader SqliteCaseStore compatibility

### Task 2: Contract Tests (d87de73)

- Added 7 new contract tests for export SQL compatibility:
  - `BuildPgDumpArguments_ContainsInsertsFlag_ForReaderCompatibility`
  - `BuildPgDumpArguments_ContainsCleanAndIfExists_ForIdempotentImport`
  - `BuildPgDumpArguments_ExcludesOwnerAndPrivileges_ForCrossDatabaseImport`
  - `BuildPgDumpArguments_ContainsConnectionParameters`
  - `BuildPgDumpArguments_WithHostConfig_UsesHostParameters`
  - `BuildPgDumpArguments_ContainsFileOutputFlag`
- Tests are isolated (no real pg_dump execution required)

## Pre-existing Fixes (027fb58)

Fixed build errors that were blocking verification:
- Added missing `DiscoveredCases` DbSet to AppDbContext
- Added `System.ServiceModel.Syndication` package for RSS feed parsing
- Added `Microsoft.Extensions.Options.DataAnnotations` to Generator.Web
- Fixed `ValidateOnStart()` usage in Generator.Web Program.cs

## Verification

- Build: `dotnet build src/OpenJustice.Generator/OpenJustice.Generator.csproj` ✓
- Tests: `dotnet test tests/OpenJustice.Generator.Tests --filter "SnapshotExportServiceTests"` ✓ (11 tests passed)

## Requirements Status

- [x] GEN-15: Snapshot export service (pg_dump) - now with Reader-compatible SQL output

## Deviations from Plan

### Auto-fixed Issues

**1. [Rule 3 - Blocker] Fixed pre-existing build errors**
- **Found during:** Task verification
- **Issue:** Build failed due to missing DbSet, missing package references, and incorrect ValidateOnStart usage
- **Fix:** Added missing DbSet, packages, and fixed Program.cs
- **Files modified:** AppDbContext.cs, OpenJustice.Generator.csproj, OpenJustice.Generator.Web.csproj, Program.cs
- **Commit:** 027fb58

## Auth Gates

None - this was fully automated work.

## Self-Check: PASSED

- [x] SnapshotExportService.cs contains --inserts flag
- [x] SnapshotExportServiceTests.cs contains 7 new contract tests
- [x] All tests pass (11/11)
- [x] Build succeeds with 0 errors
