# Project State

## Project Reference

See: .planning/PROJECT.md (updated March 1, 2026)

**Core value:** Decentralized database of Brazilian historical crimes with full history tracking and confidence scores
**Current focus:** Phase 1: Database & Configuration Foundation

## Current Position

Phase: 4 of 5 (Reader Core)
Plan: Not started
Status: Ready to begin
Last activity: March 1, 2026 — Phase 3 complete

Progress: [▓▓▓▓▓▓▓▓▓▓▓] 90%

## Performance Metrics

**Velocity:**
- Total plans completed: 8
- Average duration: 8.3 min
- Total execution time: 1.06 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1. Database & Config | 2/2 | 15 min | 7.5 min |
| 2. Generator Core | 5/5 | 45 min | 9.0 min |
| 3. Generator History | 2/2 | 21 min | 10.5 min |
| 4. Reader Core | - | - | - |
| 5. Reader History UI | - | - | - |

**Recent Trend:**
- Plan 01-01: EF Core domain schema - 5 min
- Plan 01-02: EF Core migration + backup/restore - 10 min
- Plan 02-01: Case management API - 11 min
- Plan 02-02: Curation workflow controls - 5 min
- Plan 02-03: Blazor web UI for case management - 15 min
- Plan 02-04: Discovery ingestion + curator review - 2 min
- Plan 02-05: Generator core finalization - 10 min
- Plan 03-01: Generator history system - 9 min
- Plan 03-02: Generator history UI - 12 min

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

Key decisions from Phase 2 Plan 1:
- FluentValidation para regras de validação declarativas
- Reference code gerado apenas na criação, nunca regenerado na edição
- Banco in-memory para testes unitários

Key decisions from Phase 2 Plan 2:
- CurationStatus enum com estados Pending/Approved/Rejected
- Auditoria append-only ( CaseAuditLog )
- Transições de estado atômicas com log de auditoria na mesma transação

Key decisions from Phase 2 Plan 3:
- Blazor Server rendering mode para integração completa com .NET
- CaseFormModel com DataAnnotations para validação declarativa
- Typed HttpClient para chamadas API com tratamento de erros
- Lookup data estático para dropdowns (simplificação)

Key decisions from Phase 2 Plan 4:
- Discovery items remain gated by curator review before promotion to official cases
- Hash-based deduplication prevents duplicate discovered items from same source URL
- Idempotent approve/reject operations for already-processed items

Key decisions from Phase 2 Plan 5:
- Evidence association uses EF Core relationship updates with duplicate-prevention
- Tag association supports both Tag ID lookup and TagName create-or-find
- Snapshot versioning uses sequential vN.sql naming with regex pattern matching
- GeneratorOptions consolidates all config into single options class with validation
- Export service requires pg_dump availability check before attempting export

Key decisions from Phase 3 Plan 1:
- Append-only pattern chosen for immutable audit trail
- Confidence scores preserved per field change without rollback coupling
- Field values serialized as JSON for flexibility
- History ordered by ChangedAt descending for timeline rendering

### Pending Todos

- Phase 1 complete - Database foundation laid
- Phase 2 complete - Generator core API implemented
- Phase 3 complete - Generator history system implemented
- Phase 4 in progress - Reader Core (Blazor WASM, torrent, local SQL)
- Phase 5 - Reader History UI & Polish

### Blockers/Concerns

None yet.

## Session Continuity

Last session: March 1, 2026
Stopped at: Completed 03-02-PLAN.md execution
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
- [x] DB-11: Index name search
- [x] DB-12: Index crime type
- [x] DB-13: Index location
- [x] DB-14: Index crime date
- [x] DB-15: Index judicial status
- [x] DB-16: Composite indexes (partial - FK only)
- [x] DB-17: FK constraints
- [x] DB-18: NOT NULL constraints
- [x] DB-19: DEFAULT values
- [x] DB-20: Migrations
- [x] DB-21: Backup/restore
- [x] DB-22: SQL snapshot export

## Requirements Status (Generator)

- [x] GEN-01: Case CRUD API endpoints
- [x] GEN-02: Reference code generation (ATRO-YYYY-NNNN)
- [x] GEN-03: FluentValidation for case requests
- [x] GEN-04: Curation workflow (approve/reject/verify)
- [x] GEN-05: Case audit log
- [x] GEN-06: Discovery RSS aggregator service
- [x] GEN-07: Discovery Reddit scraper service
- [x] GEN-08: Discovered case review workflow
- [x] GEN-09: Hash-based deduplication
- [x] GEN-10: Blazor Server UI components
- [x] GEN-11: Typed HTTP client for API calls
- [x] GEN-12: Evidence association API
- [x] GEN-13: Tag association API
- [x] GEN-15: Snapshot export service (pg_dump)
- [x] GEN-16: Snapshot versioning (v1, v2, ...)
- [x] GEN-17: Appsettings configuration validation
- [x] GEN-18: Case field history append-only tracking
- [x] GEN-19: Case history API endpoints
- [x] GEN-20: Change confidence per history entry
- [x] GEN-21: Generator UI exposes history timeline for each case
- [x] GEN-22: Diff view compares two selected versions with confidence scores
