# Project State

## Project Reference

See: .planning/PROJECT.md (updated 2026-03-02)

**Core value:** Decentralized, censorship-resistant database with complete historical transparency.
**Current focus:** v2.0 BrazilExtractor roadmap execution (Phase 13 in progress)

## Current Position

Phase: 13 of 13 (OCR Text Extraction and Quality Signals)
Plan: 2 of 2 in current phase
Status: Completed
Last activity: 2026-03-02 - Completed 13-02 OCR pipeline integration

Progress: [████████████████████] 100% (Phase 13 complete)

## Performance Metrics

**Velocity:**
- Total plans completed: 22
- Average duration: 7.4 min
- Total execution time: 2.7 hours

**By Phase:**

| Phase | Plans | Total | Avg/Plan |
|-------|-------|-------|----------|
| v1.0 (1-10) | 21/21 | 155 min | 7.4 min |
| v2.0 (11-13) | 5/TBD | 29 min | 5.8 min |

**Recent Trend:**
- Last completed sequence: Phase 13
- Trend: Stable
| Phase 13-ocr-text-extraction P01 | 8 | 2 tasks | 7 files |
| Phase 13-ocr-text-extraction P02 | 4 | 2 tasks | 5 files |

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
- [12-02] Used singleton HttpClient for PDF downloads (proper disposal pattern)
- [12-02] Filename format: tjgo_{date}_{hash}_{sequence}.pdf for collision safety
- [12-02] Query cadence enforced at job level, not service level (simpler, more testable)
- [13-01] Default OCR language set to Portuguese ('por') for legal document processing
- [13-01] Quality signals: character count, encoding replacement char count, empty output flag
- [13-02] OCR invoked after PDF downloads complete, only for successfully downloaded files
- [13-02] Same-base .txt files saved using Path.ChangeExtension alongside PDFs
- [13-02] Failure log appends with timestamp, PDF path, language, reason for traceability

### Pending Todos

- None - Phase 13 complete

### Blockers/Concerns

- TJGO portal selectors/challenge behavior may drift and should be validated during Phase 11 planning.

## Session Continuity

Last session: 2026-03-02
Stopped at: Completed 13-02-PLAN.md (OCR pipeline integration)
Resume file: None
