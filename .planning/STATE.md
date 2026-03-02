# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-02)

**Core value:** Decentralized, censorship-resistant database with complete historical transparency.
**Current focus:** v2.0 BrazilExtractor roadmap execution (Phase 12 in progress)

## Current Position

Phase: 12 of 13 (PDF Acquisition Pipeline)
Plan: 1 of 2 in current phase
Status: In Progress
Last activity: 2026-03-02 - Completed 12-01 PDF link capture contract

Progress: [████████░░░░░░░░░░░░░░] 15% (Phase 12 started)

## Performance Metrics

**Velocity:**
- Total plans completed: 21
- Average duration: 7.4 min
- Total execution time: 2.6 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| v1.0 (1-10) | 21/21 | 155 min | 7.4 min |
| v2.0 (11-13) | 4/TBD | 21 min | 5.3 min |

**Recent Trend:**
- Last completed sequence: Phase 11 -> Phase 12
- Trend: Stable
| Phase 12-pdf-acquisition-pipeline P01 | 5 | 3 tasks | 4 files |

## Accumulated Context

### Decisions

- Keep v2.0 scoped to TJGO extractor foundation, PDF capture, and OCR artifacts only.
- Maintain deterministic ingestion cadence (15 PDFs/query, 30s interval) before advanced automation.
- Preserve existing Generator curation gate; no auto-publish behavior in extractor milestone.
- [11-01] Used IValidateOptions pattern for custom validation rules (more flexible than DataAnnotations)
- [11-01] ValidateOnStart() ensures fail-fast before any service instantiation
- [11-02] Singleton browser factory + scoped search services pattern for proper lifecycle management
- [11-02] Single-day queries enforce same-date DataInicial and DataFinal values
- [11-03] Portal has no native criminal toggle - implemented filter profile strategy with query params and post-result indicators
- [11-03] Smoke tests validate selector stability without actual form submission to avoid rate limits
- [12-01] Multiple CSS selector patterns for PDF detection (more resilient to portal changes)
- [12-01] Added fallback generic anchor scan when specific selectors fail
- [12-01] URL normalization preserves relative links from TJGO portal

### Pending Todos

- Complete Phase 12-02 for PDF download persistence
- Start Phase 13 planning for OCR text extraction

### Blockers/Concerns

- TJGO portal selectors/challenge behavior may drift and should be validated during Phase 11 planning.

## Session Continuity

Last session: 2026-03-02
Stopped at: Completed 12-01-PLAN.md (PDF link capture contract)
Resume file: None
