# Roadmap: OpenJustice

## Milestones

- ✅ **v1.0 MVP** - Phases 1-10 (shipped 2026-03-02)
- 📋 **v2.0 BrazilExtractor** - Phases 11-13 (planned)

## Overview

v2.0 delivers a deterministic TJGO extraction pipeline that can run locally, capture publication PDFs with controlled cadence, and transform them into OCR text artifacts ready for downstream legal triage and Generator curation.

## Phases

**Phase Numbering:**
- Integer phases (11, 12, 13): Planned milestone work
- Decimal phases (11.1, 12.1): Urgent insertions (marked with INSERTED)

- [x] **Phase 11: Extractor Foundation and TJGO Search** - Stand up the worker, configuration, and criminal/date-filtered TJGO query execution. (3/3 plans complete)
- [ ] **Phase 12: PDF Acquisition Pipeline** - Persist result PDFs with deterministic limits and throttled request cadence.
- [ ] **Phase 13: OCR Text Extraction and Quality Signals** - Convert PDFs to Portuguese text artifacts and expose extraction failures for review.

## Phase Details

### Phase 11: Extractor Foundation and TJGO Search
**Goal**: The operator can run BrazilExtractor with validated settings and execute TJGO publicacao searches with the expected criminal/date filtering behavior.
**Depends on**: Nothing (first phase)
**Requirements**: EXTR-01, EXTR-02, EXTR-03, EXTR-04, EXTR-05, EXTR-13, EXTR-14
**Success Criteria** (what must be TRUE):
  1. Operator can start `OpenJustice.BrazilExtractor` and it loads required extractor settings from `appsettings.json` with fail-fast validation on missing/invalid values.
  2. Operator can run a search job that opens TJGO `ConsultaPublicacao` via Playwright Chromium and reaches the publication results workflow end-to-end.
  3. Operator can execute a single-day query where start date equals end date and receive results for that exact date window.
  4. Operator can apply criminal-oriented filtering in the TJGO query flow so civil-only result sets are excluded by default.
**Plans**: 3 plans
Plans:
- [x] 11-01-PLAN.md - Scaffold BrazilExtractor worker project with fail-fast configuration boundary. (Complete: 2026-03-02)
- [x] 11-02-PLAN.md - Implement Playwright Chromium TJGO navigation and same-day search execution. (Complete: 2026-03-02)
- [x] 11-03-PLAN.md - Apply criminal filter strategy and add TJGO smoke contract tests. (Complete: 2026-03-02)

### Phase 12: PDF Acquisition Pipeline
**Goal**: The extractor reliably captures and stores TJGO publication PDFs for each query in a reproducible, rate-limited way.
**Depends on**: Phase 11
**Requirements**: EXTR-06, EXTR-07, EXTR-08
**Success Criteria** (what must be TRUE):
  1. Operator can run a query and the extractor captures ALL available PDF links from the returned result set (up to 15 per page).
  2. Operator can observe a minimum 30-second interval between QUERY executions (new query or pagination), not between individual PDF downloads.
  3. Operator can find all downloaded PDFs persisted locally with unique file names and no accidental overwrite collisions.
**Plans**: TBD

### Phase 13: OCR Text Extraction and Quality Signals
**Goal**: The extractor produces reviewable Portuguese text artifacts from downloaded PDFs and surfaces OCR quality failures.
**Depends on**: Phase 12
**Requirements**: EXTR-09, EXTR-10, EXTR-11, EXTR-12
**Success Criteria** (what must be TRUE):
  1. Operator can process downloaded PDFs and obtain OCR text output using Tesseract in the extraction pipeline.
  2. Operator can find a `.txt` artifact for each successfully processed PDF using the same base file name for traceability.
  3. Operator can verify Portuguese-language court terms are extracted in text output without systematic encoding corruption.
  4. Operator can review a clear failure log listing PDFs that OCR could not process successfully.
**Plans**: TBD

## Progress

| Phase | Plans Complete | Status | Completed |
|-------|----------------|--------|-----------|
| 11. Extractor Foundation and TJGO Search | 3/3 | Complete    | 2026-03-02 |
| 12. PDF Acquisition Pipeline | 0/TBD | Not started | - |
| 13. OCR Text Extraction and Quality Signals | 0/TBD | Not started | - |
