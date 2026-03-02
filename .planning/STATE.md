# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-02)

**Core value:** Decentralized, censorship-resistant database with complete historical transparency.
**Current focus:** v2.0 BrazilExtractor roadmap execution (Phase 11 ready to plan)

## Current Position

Phase: 11 of 13 (Extractor Foundation and TJGO Search)
Plan: 3 of 3 in current phase
Status: Complete
Last activity: 2026-03-02 - Completed 11-03 criminal filter and smoke tests

Progress: [████████████████████░░░░░] 10% (Phase 11 complete)

## Performance Metrics

**Velocity:**
- Total plans completed: 21
- Average duration: 7.4 min
- Total execution time: 2.6 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| v1.0 (1-10) | 21/21 | 155 min | 7.4 min |
| v2.0 (11-13) | 3/TBD | 16 min | 5.3 min |

**Recent Trend:**
- Last completed sequence: Phase 8 -> Phase 9 -> Phase 10
- Trend: Stable
| Phase 11-extractor-foundation-and-tjgo-search P03 | 5 | 3 tasks | 8 files |

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

### Pending Todos

- Start Phase 12 planning for PDF acquisition pipeline

### Blockers/Concerns

- TJGO portal selectors/challenge behavior may drift and should be validated during Phase 11 planning.

## Session Continuity

Last session: 2026-03-02
Stopped at: Completed 11-03-PLAN.md (criminal filter and smoke tests)
Resume file: None
