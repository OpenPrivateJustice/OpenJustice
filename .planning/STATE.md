# Project State

## Project Reference

See: .planning/PROJECT.md (updated March 2, 2026)

**Core value:** Decentralized database of Brazilian historical crimes with full history tracking and confidence scores
**Current focus:** v2.0 BrazilExtractor - Defining requirements

## Current Position

Phase: Not started (defining requirements)
Plan: —
Status: Defining requirements
Last activity: March 2, 2026 — Milestone v2.0 BrazilExtractor started

Progress: [░░░░░░░░░░░░░░░░░░░░░] 0%

## Performance Metrics

**Velocity:**
- Total plans completed: 16
- Average duration: 7.4 min
- Total execution time: 1.32 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| 1. Database & Config | 2/2 | 15 min | 7.5 min |
| 6. VERIFICATION.md Gap Closure | 1/1 | 5 min | 5.0 min |
| 7. Configuration System | 2/2 | 5 min | 2.5 min |
| 8. Wire Torrent Import Pipeline | 1/1 | 5 min | 5.0 min |
| 9. Fix History Capture on Create | 1/1 | 1 min | 1.0 min |
| 10. Wire Reader to Generator History API | 2/2 | 6 min | 3.0 min |
| 2. Generator Core | 5/5 | 45 min | 9.0 min |
| 3. Generator History | 2/2 | 21 min | 10.5 min |
| 4. Reader Core | 3/4 | 17 min | 5.7 min |
| 5. Reader History UI | 3/3 | 12 min | 4.0 min |

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
- Plan 04-01: Reader core bootstrap - 4 min
- Plan 04-02: Reader sync pipeline - 5 min
- Plan 04-03: Reader search experience - 8 min
- Plan 05-02: Reader history UI - 3 min
- Plan 05-03: Cross-page UI polish - 5 min
- Plan 07-01: Generator configuration complete - 3 min
- Plan 07-02: Composite index alignment - 1 min
- Plan 08-01: Generator SQL export hardened for Reader compatibility - 5 min
- Plan 09-01: Initial field history capture on case creation - 1 min
- Plan 10-01: Reader-side API integration foundation with auth - 4 min
- Plan 10-02: Switch Reader history to live Generator API - 2 min

*Updated after each plan completion*
| Phase 04-reader-core P02 | 5 | 3 tasks | 8 files |
| Phase 5 P1 | 2 | 2 tasks | 6 files |
| Phase 5 P2 | 5 | 3 tasks | 5 files |
| Phase 5 P3 | 5 | 3 tasks | 5 files |

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

Key decisions from Phase 4 Plan 1:
- Created Reader WASM project as standalone SPA isolated from Generator.Web runtime
- Used IOptions pattern with IValidateOptions mirroring Generator's approach
- Created strongly-typed ReaderOptions with DataAnnotations validation
- Placed appsettings.json in wwwroot (standard for Blazor WASM)
- Added navigation entries for Sync, Search, Cases pages
- [Phase 04-reader-core]: Implemented sync pipeline with HTTP download fallback for browser WASM, in-memory SQL store
- [Phase 04-reader-core]: Implemented search with fuzzy name matching (Levenshtein distance), composable filters, sorting, and pagination
- [Phase 5]: Additive history parsing preserves existing case import stability
- [Phase 5]: Service resilience: returns empty collections instead of throwing on missing history
- [Phase 5]: Index-based diff selection ensures deterministic/stable A/B output
- [Phase 07]: Wired AddOpenJusticeConfiguration into Generator.Web Program.cs with ValidateOnStart()
- [Phase 07]: Created comprehensive docs/configuration.md mapping all runtime config keys
- [Phase 08]: Added --inserts pg_dump flag for INSERT-based SQL output compatible with Reader import
- [Phase 08]: Added contract tests to ensure export format remains Reader-compatible
- [Phase 10-01]: Added GeneratorHistoryApiOptions with fail-fast validation for BaseUrl, AccessToken, LoginUrl
- [Phase 10-01]: Created IGeneratorHistoryApiClient with Bearer token auth and explicit 401 handling
- [Phase 10-01]: Used IHttpClientFactory for proper connection pooling in Blazor WASM
- [Phase 10-02]: CaseHistoryService exclusively uses GeneratorHistoryApiClient - no more local SqliteCaseStore history queries
- [Phase 10-02]: 401 responses show explicit session-expired UI instead of empty history
- [Phase 10-02]: HasHistory check in CaseDetails shows history link if case exists locally, even if API auth fails

### Pending Todos

- v1.0 complete - Full MVP shipped
- v2.0 in progress - BrazilExtractor foundation

### Requirements Status (Configuration)

- [x] CFG-01: appsettings.json contains database connection config
- [x] CFG-02: appsettings.json contains file paths config
- [x] CFG-03: appsettings.json contains torrent settings
- [x] CFG-04: appsettings.Development.json exists and overrides local values
- [x] CFG-05: startup validation is fail-fast for missing required settings
- [x] CFG-06: configuration keys and usage documented in docs/configuration.md

### Blockers/Concerns

None yet.

## Session Continuity

Last session: March 2, 2026
Stopped at: Completed 10-02-PLAN.md execution (Switch Reader history to live Generator API)
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

## Requirements Status (Reader)

- [x] RDR-01: Reader opens as a client-side Blazor WebAssembly SPA
- [x] RDR-02: Local SQLite database for offline access
- [x] RDR-03: Torrent-based database sync
- [x] RDR-04: Search interface with filters
- [x] RDR-05: Reader reads required runtime settings from appsettings.json
- [x] RDR-06: Search by name with fuzzy matching
- [x] RDR-07: Filter by crime type
- [x] RDR-08: Filter by state
- [x] RDR-09: Filter by period (date range)
- [x] RDR-10: Filter by judicial status
- [x] RDR-11: Sort results
- [x] RDR-12: Paginate results
- [x] RDR-13: Combined filter AND conditions
- [x] RDR-21: Reader case history timeline display
- [x] RDR-22: Reader A/B diff comparison view
- [x] RDR-23: Reader confidence score visualization
- [x] RDR-24: Loading states for all async operations (sync, search, details, history)
- [x] RDR-25: Error handling with actionable messages (retry, navigation options)
- [x] RDR-26: Responsive layouts for mobile and desktop
- [x] RDR-27: Reader breadcrumbs and navigation polish

## Requirements Status (Extractor)

- [ ] EXTR-01: Create OpenJustice.BrazilExtractor project in solution
- [ ] EXTR-02: Playwright scraping of TJGO publicacao search
- [ ] EXTR-03: PDF download from search results
- [ ] EXTR-04: Local OCR text extraction from PDFs
- [ ] EXTR-05: Text analysis for criminal case identification
- [ ] EXTR-06: Filter for "trânsito em julgado" cases
- [ ] EXTR-07: Queue cases for Generator.Web admin evaluation
