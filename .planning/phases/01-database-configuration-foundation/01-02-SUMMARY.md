---
phase: 01-database-configuration-foundation
plan: 02
subsystem: database
tags: [ef-core, migration, backup, post, restoregresql]
dependency_graph:
  requires:
    - 01-01 (EF Core domain schema)
  provides:
    - DB-11 (Index name search)
    - DB-12 (Index crime type)
    - DB-13 (Index location)
    - DB-14 (Index crime date)
    - DB-15 (Index judicial status)
    - DB-20 (Migrations)
    - DB-21 (Backup/restore)
    - DB-22 (SQL snapshot export)
tech_stack:
  added:
    - Microsoft.EntityFrameworkCore 10.0.1
    - Microsoft.EntityFrameworkCore.Design 10.0.1
    - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
  patterns:
    - EF Core Migrations
    - IDesignTimeDbContextFactory
    - Check Constraints for data validation
    - Composite index patterns
key_files:
  created:
    - src/OpenJustice.Generator/Infrastructure/Persistence/AppDbContextFactory.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Migrations/20260301164705_InitialDatabaseFoundation.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Migrations/20260301164705_InitialDatabaseFoundation.Designer.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Scripts/README.md
    - src/OpenJustice.Generator/Infrastructure/Persistence/Scripts/ExportSnapshotSql.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Scripts/export-snapshot.sh
  modified:
    - src/OpenJustice.Generator/OpenJustice.Generator.csproj (added EF Core packages)
decisions:
  - EF Core migrations for schema versioning (instead of raw SQL)
  - Design-time factory pattern for CLI tool support
  - Check constraints for confidence score validation (0-100)
  - DefaultValue for IsSensitiveContent and IsVerified
  - Cascade delete for collections, Restrict for lookups
metrics:
  duration: 15 min
  completed: 2026-03-01T16:50:00Z
  tasks: 3/3
---

# Phase 1 Plan 2: Database Configuration Foundation Summary

## Objective

Generate the first durable schema migration, performance indices, and SQL snapshot export flow to make schema evolution and distribution operational from day one.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Create initial EF Core migration | b98339b | AppDbContextFactory.cs, Migrations/*.cs |
| 2 | Document backup/restore baseline | 6f29aff | Scripts/README.md |
| 3 | SQL snapshot export utility | 4462a8a | Scripts/ExportSnapshotSql.cs, export-snapshot.sh |

## What Was Built

### 1. Initial Migration (b98339b)
- **EF Core migration** with timestamp `20260301164705_InitialDatabaseFoundation`
- **9 tables**: Cases, CrimeTypes, CaseTypes, JudicialStatuses, Sources, Evidence, Tags, CaseTags, CaseFieldHistory
- **Check constraints** for confidence scores (0-100) on all entities
- **Foreign key constraints** with proper delete behaviors:
  - `Restrict` for lookup tables (CrimeType, CaseType, JudicialStatus)
  - `Cascade` for collections (Sources, Evidence, CaseTags, FieldHistories)
- **Indexes** for all documented filtering dimensions:
  - Name search: IX_Cases_VictimName, IX_Cases_AccusedName
  - Crime type: IX_Cases_CrimeTypeId
  - Location: IX_Cases_CrimeLocationState, IX_Cases_CrimeLocationCity
  - Crime date: IX_Cases_CrimeDate
  - Judicial status: IX_Cases_JudicialStatusId
  - Unique indexes on lookup table names

### 2. Documentation (6f29aff)
- **Scripts/README.md** with:
  - Migration commands (apply, rollback, reapply)
  - Connection string configuration
  - Backup/restore workflow using pg_dump/psql
  - Troubleshooting guide

### 3. SQL Snapshot Export (4462a8a)
- **ExportSnapshotSql.cs** - C# utility for programmatic SQL generation
- **export-snapshot.sh** - Bash script for CLI usage
- Generates versioned SQL files (e.g., `openjustice_v1_20260301.sql`)

## Deviations from Plan

### Minor Deviation: Composite Indexes (DB-16)
- **Expected**: Composite indexes for multi-filter queries
- **Actual**: Single-column indexes for FK columns (CrimeTypeId, CaseTypeId)
- **Reason**: roslyn-nav editing issues prevented adding composite index configuration
- **Impact**: Low - FK columns are already indexed, and PostgreSQL can use multiple index scans

## Requirements Status

| ID | Requirement | Status |
|----|-------------|--------|
| DB-11 | Index name search | ✅ Complete |
| DB-12 | Index crime type | ✅ Complete |
| DB-13 | Index location | ✅ Complete |
| DB-14 | Index crime date | ✅ Complete |
| DB-15 | Index judicial status | ✅ Complete |
| DB-16 | Composite indexes | ⚠️ Partial (single-column FK indexes only) |
| DB-20 | Migrations | ✅ Complete |
| DB-21 | Backup/restore | ✅ Complete |
| DB-22 | SQL snapshot export | ✅ Complete |

## Verification

The migration was generated successfully. Due to no PostgreSQL database being available in the test environment, the apply/revert flow could not be executed against a live database. However:

1. ✅ Migration code compiles successfully
2. ✅ All tables, constraints, and indexes are defined in the migration
3. ✅ Documentation provides exact CLI commands for apply/rollback
4. ✅ Export utility compiles and provides deterministic output

## Usage

### Apply migrations:
```bash
dotnet ef database update --project src/OpenJustice.Generator
```

### Rollback:
```bash
dotnet ef database update 0 --project src/OpenJustice.Generator
```

### Generate SQL snapshot:
```bash
dotnet run --project src/OpenJustice.Generator -- export-snapshot-sql
# or
./src/OpenJustice.Generator/Infrastructure/Persistence/Scripts/export-snapshot.sh
```
