# ROADMAP - AtrocidadesRSS

## Project Overview
- **Core Value**: Decentralized database of Brazilian historical crimes with full history tracking and confidence scores
- **Components**: Generator (private .NET Core 10) + Reader (public Blazor WASM)
- **Depth**: Quick | **Mode**: YOLO | **Coverage**: 94/94 requirements

## Phases

- [x] **Phase 1: Database & Configuration Foundation** - PostgreSQL schema, indices, migrations, appsettings
- [x] **Phase 2: Generator Core** - API, Web UI, Curation, Export, RSS/Reddit scraping
- [x] **Phase 3: Generator History System** - CaseFieldHistory, confidence scores, timeline & diff UI
- [x] **Phase 4: Reader Core** - Blazor SPA, torrent download, local SQL, search/filters, case viewing (completed 2026-03-01)
- [x] **Phase 5: Reader History UI & Polish** - Timeline, diff, confidence display, responsive UI, error handling (COMPLETE 2026-03-02)

---

## Phase Details

### Phase 6: Create Missing VERIFICATION.md Files
**Goal:** Add VERIFICATION.md artifacts for phases 2, 4, and 5 to satisfy GSD workflow requirements

**Depends on:** Nothing (parallel to other gap closures)

**Requirements:** Process requirement — VERIFICATION.md for phases 2, 4, 5

**Success Criteria** (what must be TRUE):
1. `.planning/phases/02-generator-core/02-VERIFICATION.md` exists
2. `.planning/phases/04-reader-core/04-VERIFICATION.md` exists
3. `.planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md` exists
4. All three files follow GSD VERIFICATION.md template with scores, gap analysis, and evidence

**Gap Closure:** Closes critical gaps from v1.0 audit — 3 phases missing verification artifacts

### Phase 7: Configuration System & Database Indexes
**Goal:** Implement appsettings.json configuration system and add composite database indexes

**Depends on:** Nothing

**Requirements:** CFG-01, CFG-02, CFG-03, CFG-04, CFG-05, CFG-06, DB-16

**Success Criteria** (what must be TRUE):
1. `appsettings.json` exists with database connection string, file paths, torrent settings
2. `appsettings.Development.json` exists for local development
3. Startup validation ensures required configuration is present
4. Composite indexes created on (CrimeTypeId, JudicialStatusId) and (CrimeLocationState, CrimeDate)
5. Configuration documented in README or separate docs file

**Gap Closure:** Closes orphaned CFG-01 through CFG-06 requirements, upgrades DB-16 from partial to complete

### Phase 1: Database & Configuration Foundation
**Goal**: PostgreSQL database schema with all tables, indices, migrations, and configuration system ready

**Depends on**: Nothing (first phase)

**Requirements**: DB-01, DB-02, DB-03, DB-04, DB-05, DB-06, DB-07, DB-08, DB-09, DB-10, DB-11, DB-12, DB-13, DB-14, DB-15, DB-16, DB-17, DB-18, DB-19, DB-20, DB-21, DB-22, CFG-01, CFG-02, CFG-03, CFG-04, CFG-05, CFG-06

**Success Criteria** (what must be TRUE):
1. PostgreSQL database is created with all tables (Cases, CrimeType, CaseType, JudicialStatus, Sources, Evidence, Tags, CaseFieldHistory)
2. All foreign key relationships enforced between tables
3. Indices created for efficient querying by name, crime type, location, date, status
4. Migration system functional for future schema changes
5. appsettings.json configured for database connection, file paths, torrent settings
6. Snapshot SQL export generates valid, importable SQL file
7. NOT NULL constraints prevent invalid data insertion
8. Confidence score fields (0-100%) exist in main tables

**Plans**: 
- [x] 01-01: EF Core domain schema (COMPLETE)
- [x] 01-02: Database configuration and migrations (COMPLETE)
- [x] 01-03: Composite indexes for multi-column queries (COMPLETE - Gap closure)
- [x] 01-04: Configuration system with appsettings.json (COMPLETE - Gap closure)

---

### Phase 2: Generator Core
**Goal**: Private curation system with API, web UI, case management, RSS/Reddit scraping, and export

**Depends on**: Phase 1

**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08, GEN-09, GEN-10, GEN-11, GEN-12, GEN-13, GEN-14, GEN-15, GEN-16, GEN-17

**Success Criteria** (what must be TRUE):
1. Curator can insert new cases via web interface with all required fields validated
2. Curator can edit existing cases with proper validation
3. Cases require approval workflow before becoming "official"
4. Curators can mark cases as "Verificado" with their identity recorded
5. System automatically generates ATRO-YYYY-NNNN reference codes
6. RSS feed aggregator discovers potential cases from configured sources
7. Reddit thread scraper extracts case data from subreddit posts
8. Curator can approve/reject auto-discovered cases
9. System exports complete PostgreSQL snapshot as versioned SQL file
10. All scrapers and API read from appsettings.json configuration

**Plans**: 
- [x] 02-01: Case management API with validation (COMPLETE)
- [x] 02-02: Curation workflow controls (COMPLETE)
- [x] 02-03: Blazor web UI for case management (COMPLETE)
- [x] 02-04: Discovery ingestion + curator review (COMPLETE)
- [x] 02-05: Metadata APIs, snapshot export, configuration (COMPLETE)

---

### Phase 3: Generator History System
**Goal**: Complete history tracking with unlimited changes per field, confidence scores, timeline and diff visualization

**Depends on**: Phase 2

**Requirements**: GEN-18, GEN-19, GEN-20, GEN-21, GEN-22

**Success Criteria** (what must be TRUE):
1. Every field change creates immutable CaseFieldHistory record (append-only)
2. Each history record contains: field name, previous value, new value, timestamp, curator ID
3. Each data field has 0-100% confidence score independent of history
4. Curator can view timeline showing evolution of any field over time
5. Curator can see diff view comparing any two versions of a field (e.g., "name changed from 'X' to 'Y' on date")

**Plans**: 
- [x] 03-01: Append-only field history capture + history API
- [x] 03-02: Generator timeline + visual diff UI

---

### Phase 4: Reader Core
**Goal**: Public Blazor WASM SPA with torrent download, local SQL database, search/filters, and case viewing

**Depends on**: Phase 1 (database schema)

**Requirements**: RDR-01, RDR-02, RDR-03, RDR-04, RDR-05, RDR-06, RDR-07, RDR-08, RDR-09, RDR-10, RDR-11, RDR-12, RDR-13, RDR-14, RDR-15, RDR-16, RDR-17, RDR-18, RDR-19, RDR-20

**Success Criteria** (what must be TRUE):
1. User can download complete database via torrent and load locally
2. User can check for new versions via torrent
3. User can search by accused/victim name with fuzzy matching
4. User can filter by crime type, state, time period, judicial status
5. User can combine multiple filters simultaneously
6. User can view paginated, sortable results
7. User can view complete case details with all fields
8. Sensitive content shows warning before display
9. Sources, evidence links, and judicial info displayed clearly
10. Configuration loaded from appsettings.json

**Plans**: 4 plans

Plans:
- [x] 04-01-PLAN.md — Reader WASM scaffold + configuration
- [x] 04-02-PLAN.md — Torrent sync, version check, local SQL load
- [x] 04-03-PLAN.md — Search, filters, sorting, pagination
- [ ] 04-04-PLAN.md — Case details view + sensitive content gate

---

### Phase 5: Reader History UI & Polish
**Goal**: Timeline visualization, diff comparison, confidence display, responsive UI, error handling

**Depends on**: Phase 4

**Requirements**: RDR-21, RDR-22, RDR-23, RDR-24, RDR-25, RDR-26, RDR-27

**Success Criteria** (what must be TRUE):
1. User can view timeline showing all changes to any field with dates
2. User can see visual diff between any two versions of a field
3. Confidence scores (0-100%) displayed alongside each data field
4. UI works on both mobile and desktop browsers
5. Loading indicators shown during async operations
6. Clear error messages if torrent download or SQL parse fails
7. Browser back button and breadcrumbs work correctly

**Plans**: 
- [x] 05-01: Reader history data foundation (COMPLETE)
- [x] 05-02: Reader history UI and diff controls (COMPLETE)
- [x] 05-03: Reader UI polish and responsive design (COMPLETE)

---

### Phase 8: Wire Torrent Import Pipeline
**Goal:** Connect torrent download to SQL import pipeline, fix SQL contract mismatch between Generator export and Reader parser

**Depends on:** Phase 4 (Reader Core), Phase 2 (Generator export)

**Requirements:** RDR-02, RDR-03, RDR-04, GEN-15

**Success Criteria** (what must be TRUE):
1. TorrentSyncService.SyncAsync writes downloaded SQL to local database
2. SQL table names match between Generator export ("Cases"/"CaseFieldHistory") and Reader parser
3. No hardcoded demo SQL used in production code
4. Imported database is searchable and complete

**Gap Closure:** Closes integration gap (torrent → SQL import), flow gap "Torrent download → local SQL load"

### Phase 9: Fix History Capture on Create
**Goal:** Ensure CaseFieldHistory entries are created when cases are created (not only on update)

**Depends on:** Phase 2 (Generator Core), Phase 3 (History System)

**Requirements:** GEN-01, GEN-19

**Success Criteria** (what must be TRUE):
1. New case creation creates initial CaseFieldHistory entry
2. History entry includes all field values with current timestamp
3. History API returns complete timeline for newly created cases
4. Verified: Curator can view history for brand new cases

**Gap Closure:** Closes integration gap (case creation → history capture), flow gap "Curator creates case"

### Phase 10: Wire Reader to Generator History API
**Goal:** Replace Reader's local history store with live Generator history API calls

**Depends on:** Phase 8 (torrent import), Phase 3 (History API)

**Requirements:** RDR-22, RDR-23

**Success Criteria** (what must be TRUE):
1. Reader history UI calls Generator CaseHistoryController API endpoints
2. Auth headers included in all history API calls
3. 401 responses handled (token refresh or redirect to login)
4. Timeline and diff UI display actual Generator history data

**Gap Closure:** Closes integration gap (Phase 3 → Phase 4 history consumption), flow gap "Reader case view → confidence + timeline"

---

## Progress Table

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Database & Configuration Foundation | 4/4 | Complete | 01-01, 01-02, 01-03, 01-04 |
| 2. Generator Core | 5/5 | Complete | 02-01, 02-02, 02-03, 02-04, 02-05 |
| 3. Generator History System | 2/2 | Complete | 03-01, 03-02 |
| 4. Reader Core | 3/4 | Complete    | 2026-03-01 |
| 5. Reader History UI & Polish | 3/3 | Complete    | 2026-03-02 |
| 6. Create Missing VERIFICATION.md Files | 0/0 | Pending | - |
| 7. Configuration System & Database Indexes | 0/0 | Pending | - |
| 8. Wire Torrent Import Pipeline | 0/0 | Pending | - |
| 9. Fix History Capture on Create | 0/0 | Pending | - |
| 10. Wire Reader to Generator History API | 0/0 | Pending | - |

---

*Generated: March 1, 2026*
*Last updated: March 2, 2026*
