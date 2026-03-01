# AtrocidadesRSS - Roadmap

## Project Overview
- **Core Value**: Decentralized database of Brazilian crime records with transparency and historical preservation
- **Components**: Generator (private curation) + Reader (public Blazor SPA)
- **Distribution**: PostgreSQL with SQL snapshots via torrent

## Phases

- [ ] **Phase 1: Database Foundation** - PostgreSQL schema, indices, migrations, and configuration
- [ ] **Phase 2: Generator Core** - API, web UI, curation system, and SQL export
- [ ] **Phase 3: Generator Data Collection** - RSS aggregation and Reddit scraping
- [ ] **Phase 4: Reader Application** - Blazor SPA with torrent download, search, and case viewing

## Phase Details

### Phase 1: Database Foundation
**Goal**: PostgreSQL database schema, indices, and configuration ready for both applications

**Depends on**: Nothing (first phase)

**Requirements**: DB-01, DB-02, DB-03, DB-04, DB-05, DB-06, DB-07, DB-08, DB-09, DB-10, DB-11, DB-12, DB-13, DB-14, DB-15, DB-16, DB-17, DB-18, DB-19, DB-20, CFG-01, CFG-02, CFG-03, CFG-04, CFG-05, CFG-06

**Success Criteria** (what must be TRUE):
  1. PostgreSQL database is running with all tables (Cases, CrimeType, CaseType, JudicialStatus, Sources, Evidence, Tags) properly defined
  2. All foreign key constraints and NOT NULL validations are enforced at database level
  3. Search queries by name return results in under 1 second (indices working)
  4. Filters by crime type, state, date range, and status return results efficiently (composite indices working)
  5. SQL snapshot can be exported and re-imported without data loss

**Plans**: TBD

---

### Phase 2: Generator Core
**Goal**: Private curation system with API, web UI, data validation, and SQL export capabilities

**Depends on**: Phase 1

**Requirements**: GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08, GEN-14, GEN-15, GEN-16, GEN-17

**Success Criteria** (what must be TRUE):
  1. Curator can insert new cases via web interface with all required fields
  2. Invalid data (missing required fields, invalid dates) is rejected with clear error messages
  3. Curator can edit existing cases and changes are tracked in audit log
  4. Curator can approve/reject pending cases and mark them as verified
  5. System generates SQL snapshots that can be downloaded (ATRO-YYYY-NNNN format)

**Plans**: TBD

---

### Phase 3: Generator Data Collection
**Goal**: Automated data collection via RSS feeds and Reddit scraping with approval workflow

**Depends on**: Phase 2

**Requirements**: GEN-09, GEN-10, GEN-11, GEN-12, GEN-13

**Success Criteria** (what must be TRUE):
  1. System automatically ingests RSS feeds and creates pending cases
  2. Reddit threads are scraped and converted to pending case records
  3. Curator can review automated imports and approve/reject each one
  4. Evidence links and tags can be associated with cases

**Plans**: TBD

---

### Phase 4: Reader Application
**Goal**: Public Blazor WASM application for searching and viewing crime records

**Depends on**: Phase 1 (database schema) and Phase 2 (SQL export capability)

**Requirements**: RDR-01, RDR-02, RDR-03, RDR-04, RDR-05, RDR-06, RDR-07, RDR-08, RDR-09, RDR-10, RDR-11, RDR-12, RDR-13, RDR-14, RDR-15, RDR-16, RDR-17, RDR-18, RDR-19, RDR-20, RDR-21, RDR-22, RDR-23, RDR-24

**Success Criteria** (what must be TRUE):
  1. User can download database via torrent and verify integrity
  2. User can search by name with fuzzy matching
  3. User can apply multiple filters simultaneously (crime type, state, date range, status)
  4. Sensitive content shows warning modal before display
  5. All case details, sources, evidence links, and judicial information are visible

**Plans**: TBD

---

## Coverage Map

| Phase | Requirements |
|-------|--------------|
| 1 - Database Foundation | DB-01 to DB-20, CFG-01 to CFG-06 |
| 2 - Generator Core | GEN-01, GEN-02, GEN-03, GEN-04, GEN-05, GEN-06, GEN-07, GEN-08, GEN-14, GEN-15, GEN-16, GEN-17 |
| 3 - Generator Data Collection | GEN-09, GEN-10, GEN-11, GEN-12, GEN-13 |
| 4 - Reader Application | RDR-01 to RDR-24 |

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 1. Database Foundation | 0/1 | Not started | - |
| 2. Generator Core | 0/1 | Not started | - |
| 3. Generator Data Collection | 0/1 | Not started | - |
| 4. Reader Application | 0/1 | Not started | - |

---

*Generated: March 1, 2026*
