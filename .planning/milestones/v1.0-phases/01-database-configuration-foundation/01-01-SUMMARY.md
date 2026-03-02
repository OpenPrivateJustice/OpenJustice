---
phase: 01-database-configuration-foundation
plan: 01
subsystem: database
tags: [efcore, postgresql, entity-framework, fluent-api, check-constraints]

# Dependency graph
requires: []
provides:
  - EF Core entity classes for Case, CrimeType, CaseType, JudicialStatus, Source, Evidence, Tag, CaseTag, CaseFieldHistory
  - AppDbContext with fluent API mapping for all tables
  - Foreign key relationships with cascade/restrict behaviors
  - Check constraints for confidence scores (0-100)
  - Default values for boolean fields
  - Indexes for common query patterns
affects: [generator-core, reader-core, generator-history, reader-history]

# Tech tracking
tech-stack:
  added:
    - Microsoft.EntityFrameworkCore 10.0.0
    - Npgsql.EntityFrameworkCore.PostgreSQL 10.0.0
  patterns:
    - Fluent API for EF Core configuration
    - Check constraints for domain validation
    - Composite primary key for many-to-many join tables

key-files:
  created:
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Case.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CrimeType.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseType.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/JudicialStatus.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Source.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Evidence.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Tag.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseTag.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseFieldHistory.cs
    - src/OpenJustice.Generator/Infrastructure/Persistence/AppDbContext.cs
  modified: []

key-decisions:
  - Used ToTable with lambda for check constraints to avoid EF Core 10 deprecation warnings
  - Cascade delete for one-to-many relationships (Sources, Evidences, CaseTags, FieldHistories)
  - Restrict delete for required lookups (CrimeType, CaseType, JudicialStatus) to prevent orphaned references

patterns-established:
  - "Confidence score validation: Check constraint ensures 0-100 range at database level"
  - "Append-only history: CaseFieldHistory table stores all field changes without updates"
  - "Many-to-many via composite key: CaseTag uses {CaseId, TagId} as primary key"

requirements-completed: [DB-01, DB-02, DB-03, DB-04, DB-05, DB-06, DB-07, DB-08, DB-09, DB-10, DB-17, DB-18, DB-19]

# Metrics
duration: 5min
completed: 2026-03-01
---

# Phase 01 Plan 01: Database Configuration Foundation Summary

**EF Core domain schema with all Phase 1 tables, foreign keys, and confidence score constraints**

## Performance

- **Duration:** 5 min
- **Started:** 2026-03-01T16:31:00Z
- **Completed:** 2026-03-01T16:36:20Z
- **Tasks:** 2
- **Files modified:** 10

## Accomplishments
- Created complete relational schema with 9 entity classes covering all DB-01..DB-10 requirements
- Configured AppDbContext with fluent API including table mappings, FKs, constraints, and indexes
- Fixed EF Core 10 deprecation warnings by using new ToTable(lambda) syntax for check constraints

## Task Commits

1. **Task 1: Implement EF Core entities for all Phase 1 database tables** - `130f7bd` (feat)
2. **Task 2: Configure DbContext mappings, keys, foreign keys, defaults, and constraints** - `130f7bd` (feat)

**Plan metadata:** `130f7bd` (docs: complete plan)

## Files Created/Modified
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Case.cs` - Main case aggregate with victim, accused, crime details, judicial info, and navigation properties
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CrimeType.cs` - Crime type lookup table
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseType.cs` - Modality (attempted/consummated) lookup
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/JudicialStatus.cs` - Judicial status lookup
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Source.cs` - Sources/reportages linked to cases
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Evidence.cs` - Evidence linked to cases
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/Tag.cs` - Tag categorization
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseTag.cs` - Many-to-many join table with composite key
- `src/OpenJustice.Generator/Infrastructure/Persistence/Entities/CaseFieldHistory.cs` - Append-only field change history
- `src/OpenJustice.Generator/Infrastructure/Persistence/AppDbContext.cs` - DbContext with full fluent API configuration

## Decisions Made
- Used modern EF Core 10 ToTable(lambda) syntax for check constraints instead of deprecated HasCheckConstraint
- Chose cascade delete for child collections, restrict for required lookups

## Deviations from Plan

None - plan executed exactly as written.

## Issues Encountered
- EF Core 10 deprecation warnings for HasCheckConstraint - fixed by using new ToTable(lambda) API syntax

## Next Phase Readiness
- Database schema foundation ready for migrations (DB-20)
- Entity models ready for Generator API development (Phase 2)
- Confidence score constraints ready for UI display (Phase 4-5)

---
*Phase: 01-database-configuration-foundation*
*Completed: 2026-03-01*
