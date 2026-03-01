# Project State

## Project Reference

See: .planning/PROJECT.md (updated March 1, 2026)

**Core value:** Decentralized database of Brazilian historical crimes with full history tracking and confidence scores
**Current focus:** Phase 1: Database & Configuration Foundation

## Current Position

Phase: 1 of 5 (Database & Configuration Foundation)
Plan: 01-01 complete, 01-02 pending
Status: Executing plan
Last activity: March 1, 2026 — Plan 01-01 completed

Progress: [▓▓▓▓▓▓▓▓░░] 10%

## Performance Metrics

**Velocity:**
- Total plans completed: 1
- Average duration: 5 min
- Total execution time: 0.08 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1. Database & Config | 1/2 | 5 min | 5 min |
| 2. Generator Core | - | - | - |
| 3. Generator History | - | - | - |
| 4. Reader Core | - | - | - |
| 5. Reader History UI | - | - | - |

**Recent Trend:**
- Plan 01-01: EF Core domain schema - 5 min

*Updated after each plan completion*

## Accumulated Context

### Decisions

Key architectural decisions from PROJECT.md:

- PostgreSQL para dados relacionais, Blazor WASM para leitor, .NET Core 10 para gerador
- Distribuição via torrent para descentralização e resistência à censura
- Histórico é append-only (sem rollbacks) para trilha de auditoria permanente
- Scores de confiança (0-100%) em cada campo de dado, independente do histórico
- Gerador permanece privado, leitor é open source

Key decisions from Phase 1 Plan 1:
- Usou sintaxe moderna EF Core 10 ToTable(lambda) para check constraints
- Cascade delete para coleções filhas, restrict para lookups obrigatórios

### Pending Todos

- Phase 1 Plan 2: Database configuration and migration setup

### Blockers/Concerns

None yet.

## Session Continuity

Last session: March 1, 2026
Stopped at: Completed 01-01-PLAN.md execution
Resume file: None

## Requirements Status (DB)

- [x] DB-01: Tabela Cases
- [x] DB-02: CrimeType
- [x] DB-03: CaseType
- [x] DB-04: JudicialStatus
- [x] DB-05: Sources
- [x] DB-06: Evidence
- [x] DB-07: Tags
- [x] DB-08: Many-to-many Cases-Tags
- [x] DB-09: CaseFieldHistory
- [x] DB-10: Confidence scores
- [ ] DB-11: Index name search
- [ ] DB-12: Index crime type
- [ ] DB-13: Index location
- [ ] DB-14: Index crime date
- [ ] DB-15: Index judicial status
- [ ] DB-16: Composite indexes
- [x] DB-17: FK constraints
- [x] DB-18: NOT NULL constraints
- [x] DB-19: DEFAULT values
- [ ] DB-20: Migrations
- [ ] DB-21: Backup/restore
- [ ] DB-22: SQL snapshot export
