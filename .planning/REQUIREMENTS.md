# Requirements: OpenJustice

**Defined:** March 2, 2026
**Core Value:** Decentralized, censorship-resistant database with complete historical transparency.

## v2.0 Requirements

Requirements for BrazilExtractor milestone. Each maps to roadmap phases.

### Extractor Core

- [ ] **EXTR-01**: Create OpenJustice.BrazilExtractor project in solution (.NET 10 Worker)
- [x] **EXTR-02**: Configure Playwright with Chromium for TJGO scraping
- [x] **EXTR-03**: Implement TJGO publicacao page navigation and form handling
- [ ] **EXTR-04**: Support criminal filter to exclude civil cases
- [x] **EXTR-05**: Support date range filter (single day: start = end)

### PDF Download

- [ ] **EXTR-06**: Extract PDF links from search results (15 per query)
- [ ] **EXTR-07**: Download PDFs with 30-second interval between requests
- [ ] **EXTR-08**: Save PDFs to local storage with unique naming

### OCR Processing

- [ ] **EXTR-09**: Integrate Tesseract OCR for PDF text extraction
- [ ] **EXTR-10**: Convert PDF to .txt file with same naming
- [ ] **EXTR-11**: Handle Portuguese language in OCR
- [ ] **EXTR-12**: Log OCR failures for review

### Configuration

- [ ] **EXTR-13**:appsettings.json configuration for extractor (download path, interval, etc.)
- [ ] **EXTR-14**: IOptions pattern for configuration management

## v2.1+ Requirements (Future)

### Legal Analysis

- **EXTR-15**: Text analysis to identify crimes hediondos
- **EXTR-16**: Detect trânsito em julgado (final judgment)
- **EXTR-17**: Classify case type (crime/processo/autos)

### Generator Integration

- **EXTR-18**: Queue cases to Generator.Web DiscoveredCase workflow
- **EXTR-19**: Preserve metadata (court, date, process number)

## Out of Scope

| Feature | Reason |
|---------|--------|
| Real-time monitoring | Batch processing only |
| Auto-publish to database | Manual admin approval required |
| Multi-court extraction | TJGO first, expand later |
| Concurrent scraping | Single-threaded for reliability |

## Traceability

Which phases cover which requirements. Updated during roadmap creation.

| Requirement | Phase | Status |
|-------------|-------|--------|
| EXTR-01 | Phase 11 | Pending |
| EXTR-02 | Phase 11 | Complete |
| EXTR-03 | Phase 11 | Complete |
| EXTR-04 | Phase 11 | Pending |
| EXTR-05 | Phase 11 | Complete |
| EXTR-06 | Phase 12 | Pending |
| EXTR-07 | Phase 12 | Pending |
| EXTR-08 | Phase 12 | Pending |
| EXTR-09 | Phase 13 | Pending |
| EXTR-10 | Phase 13 | Pending |
| EXTR-11 | Phase 13 | Pending |
| EXTR-12 | Phase 13 | Pending |
| EXTR-13 | Phase 11 | Pending |
| EXTR-14 | Phase 11 | Pending |

**Coverage:**
- v2.0 requirements: 14 total
- Mapped to phases: 14
- Unmapped: 0 ✓

---
*Requirements defined: March 2, 2026*
*Last updated: March 2, 2026 after v2.0 milestone started*
