# Pitfalls Research

**Domain:** Adding `OpenJustice.BrazilExtractor` (Playwright scraping + OCR + legal text triage) to existing OpenJustice `.NET 10` system
**Researched:** 2026-03-02
**Confidence:** MEDIUM

## Critical Pitfalls

### Pitfall 1: Treating Playwright navigation as "done" too early (or waiting on wrong signal)

**What goes wrong:**
Extractor reads partial TJGO result pages, misses links/download buttons, or becomes flaky with intermittent `TimeoutError`.

**Why it happens:**
The implementation waits on generic page load behavior instead of explicit element/action readiness. Playwright itself warns that `networkidle` is discouraged and that actionability-driven waits are preferred.

**How to avoid:**
- Use locator-first workflow (`GetByRole`, `Locator`, explicit `WaitForURL` when navigation is expected).
- Do not rely on `networkidle` as completion criteria; gate each step on concrete selectors/data presence.
- Persist Playwright traces/screenshots for failed runs.

**Warning signs:**
- Same query returns different number of result rows across runs.
- Increased timeout failures after portal UI changes.
- Empty downloads despite visible results in manual browsing.

**Phase to address:**
Phase 1 - Scraping foundation and deterministic navigation contract.

---

### Pitfall 2: Losing downloads by closing browser context too soon

**What goes wrong:**
PDF files disappear after "successful" click/download flow; downstream OCR receives missing-file errors.

**Why it happens:**
Playwright download artifacts are tied to browser context lifecycle; files are deleted when the producing context closes unless explicitly persisted.

**How to avoid:**
- Always `WaitForDownloadAsync` before click that triggers download.
- Immediately `SaveAsAsync` to extractor-owned storage path.
- Keep context alive until save completes and checksum is recorded.

**Warning signs:**
- Download event observed, but file not found on disk minutes later.
- OCR queue contains many "file not found" entries.

**Phase to address:**
Phase 1 - Download pipeline and storage guarantees.

---

### Pitfall 3: OCR-everything strategy without document-type branching

**What goes wrong:**
Very slow pipeline and low text quality; searchable PDFs are degraded by unnecessary image rendering + OCR.

**Why it happens:**
Scanned and digital PDFs are handled identically. Tesseract quality guidance emphasizes image preprocessing quality, segmentation mode, and language pack setup.

**How to avoid:**
- Add extractor step: detect text-selectable PDF first; use native text extraction path when possible.
- Run OCR only for image/scanned pages.
- Configure Portuguese model (`por`) and pre-processing (deskew/binarization) for OCR path.

**Warning signs:**
- OCR throughput collapses while CPU remains high.
- Frequent garbage tokens (broken accents, split legal terms).
- High mismatch between manual read and extracted text.

**Phase to address:**
Phase 2 - OCR pipeline with adaptive extraction strategy.

---

### Pitfall 4: Misclassifying "trânsito em julgado" due to naive keyword matching

**What goes wrong:**
Cases are falsely marked final (or excluded) because rule engine matches negated/contextual phrases (`"não transitou em julgado"`, procedural references, appeals context).

**Why it happens:**
`Contains("transito em julgado")` style checks ignore negation, section context, and procedural stage language.

**How to avoid:**
- Introduce context-aware rule set: positive, negative, and uncertain patterns.
- Normalize accents but keep original text for audit.
- Store match explanation (rule id + snippet) for admin review.
- Add gold-set regression tests with true/false examples.

**Warning signs:**
- Admin rejection rate spikes for "final judgment" candidates.
- Frequent contradictions in extracted snippets (contains both positive and negative cues).

**Phase to address:**
Phase 3 - Legal text triage and rule validation.

---

### Pitfall 5: Ignoring Brazilian process metadata standards (CNJ numbering + TPU)

**What goes wrong:**
Duplicate or unjoinable records because process IDs/classes are stored inconsistently (format drift, punctuation variants, class naming mismatch).

**Why it happens:**
Extractor keeps free-text identifiers instead of normalizing to CNJ 20-digit process number and CNJ TPU taxonomy where available.

**How to avoid:**
- Normalize and validate CNJ process number format early in ingestion.
- Persist both raw value and canonical normalized value.
- Map detected class/subject terms to TPU identifiers when possible; keep mapping versioned.

**Warning signs:**
- Same process appears multiple times with small formatting differences.
- Difficulty linking extracted records to existing Generator entities.

**Phase to address:**
Phase 1-2 - Ingestion schema and metadata normalization.

---

### Pitfall 6: Breaking existing Generator workflow with non-idempotent queue writes

**What goes wrong:**
Admin queue floods with duplicates, status races, and inconsistent candidate history.

**Why it happens:**
New extractor writes directly to existing DB without idempotency keys, unique constraints, or state-transition rules.

**How to avoid:**
- Define deterministic idempotency key (`processNumber + documentHash + publicationDate`).
- Add unique index/constraint and upsert semantics.
- Explicit queue states (`Extracted -> OCRed -> Classified -> Queued -> Reviewed`) and transition guards.
- Keep raw artifact + OCR text + classifier reasons immutable for audit.

**Warning signs:**
- Sudden jump in queue volume without source increase.
- Same document appears repeatedly in Generator.Web review list.

**Phase to address:**
Phase 4 - Generator integration and data-contract hardening.

---

### Pitfall 7: Hosted-service lifetime misuse in .NET worker

**What goes wrong:**
Memory leaks, stale DbContext usage, or unclean shutdown during long batches.

**Why it happens:**
`BackgroundService` is singleton by registration; scoped dependencies are used incorrectly, and cancellation/stop flow is not respected.

**How to avoid:**
- Resolve scoped services via `IServiceScopeFactory` per batch unit.
- Honor `CancellationToken` in every I/O step.
- Implement graceful stop semantics (finish current item, checkpoint progress).

**Warning signs:**
- Increasing memory over long runs.
- Random EF context disposal errors.
- Restart resumes from wrong place.

**Phase to address:**
Phase 1 - Worker skeleton and lifecycle correctness.

---

### Pitfall 8: Privacy and secrecy boundary violations with "public" court documents

**What goes wrong:**
Pipeline stores/distributes sensitive personal data beyond intended purpose, including data from proceedings that should be restricted.

**Why it happens:**
Assumption that all court publications are safe for unrestricted downstream handling. Brazilian law still imposes data-protection principles (necessity, purpose, security), and procedural law defines secrecy cases.

**How to avoid:**
- Implement minimization and retention policy for raw artifacts.
- Mark and isolate sensitive/possibly secret-of-justice documents for manual gatekeeping.
- Add field-level redaction for exported/public views.
- Log legal basis and processing purpose for extractor dataset.

**Warning signs:**
- Documents include intimate/family/minor-sensitive details without extra controls.
- Team uncertainty about whether a document may be republished.

**Phase to address:**
Phase 4 - Compliance controls before broad operational rollout.

## Technical Debt Patterns

Shortcuts that seem reasonable but create long-term problems.

| Shortcut | Immediate Benefit | Long-term Cost | When Acceptable |
|----------|-------------------|----------------|-----------------|
| Hardcode CSS selectors with no abstraction | Faster first scrape | Breaks on minor portal HTML changes | Only for spike/prototype branch |
| Single regex list for crime detection (no test corpus) | Quick initial triage | Silent drift and false positives over time | MVP only if every candidate is manually reviewed |
| OCR all PDFs with one config | Simple code path | High cost + low quality for mixed document types | Never (use branching early) |
| Write queue rows without uniqueness constraints | Rapid integration | Duplicate backlog and admin fatigue | Never |

## Integration Gotchas

Common mistakes when connecting to external services.

| Integration | Common Mistake | Correct Approach |
|-------------|----------------|------------------|
| TJGO web portal via Playwright | Assume static timing and fixed DOM | Use actionability-based locators + resilient wait and retry policy |
| Playwright downloads | Read temp path and close context immediately | Persist with `SaveAsAsync` before context close |
| Generator PostgreSQL | Add extractor table without state model/idempotency | Use queue-state machine + unique key + upsert |
| Worker DI | Inject scoped repository directly into singleton worker | Create explicit scope per unit of work |

## Performance Traps

Patterns that work at small scale but fail as usage grows.

| Trap | Symptoms | Prevention | When It Breaks |
|------|----------|------------|----------------|
| Unbounded in-memory candidate buffering | OOM/restarts during large result sets | Bounded queues and checkpointed batches | ~hundreds of large PDFs per run |
| Excessive browser/page churn | Slow throughput, high CPU/startup overhead | Reuse browser/context safely; rotate on controlled cadence | Multi-hour batch runs |
| Per-request new `HttpClient` for PDF fetches | Socket exhaustion/time-wait buildup | Long-lived client or `IHttpClientFactory` with policy | Frequent download loops |
| OCR without preprocess tuning | High CPU and bad text simultaneously | Deskew/binarize + language model tuning + OCR fallback rules | Scanned/low-quality court PDFs |

## Security Mistakes

Domain-specific security issues beyond general web security.

| Mistake | Risk | Prevention |
|---------|------|------------|
| Storing raw court PDFs/OCR text in unrestricted shared paths | Sensitive data leakage | Encrypted storage path, restricted ACL, explicit retention rules |
| Logging full extracted text in application logs | Permanent leak in log pipeline | Log metadata/snippets only; redact personal identifiers |
| Treating all extracted records as publication-ready | Legal/privacy exposure | Mandatory admin gate + sensitivity flags + redaction workflow |

## UX Pitfalls

Common user experience mistakes in this domain.

| Pitfall | User Impact | Better Approach |
|---------|-------------|-----------------|
| Showing classifier score without rationale | Admin cannot trust/triage quickly | Show matched rules + highlighted snippet + source PDF link |
| No distinction between uncertain and confident matches | Review time wasted on weak candidates | Add confidence bands and review priority queues |
| Missing provenance chain (query/date/portal page) | Hard to audit disputed entries | Persist and display full extraction provenance metadata |

## "Looks Done But Isn't" Checklist

Things that appear complete but are missing critical pieces.

- [ ] **Scraper:** Handles DOM variations and retries - verify with snapshots from at least 3 different day/runs.
- [ ] **Download flow:** Files survive process restarts - verify checksum and persisted path after context close.
- [ ] **OCR:** Portuguese model and preprocessing configured - verify on scanned + digital PDF samples.
- [ ] **Classifier:** Negation-aware `trânsito em julgado` rules - verify against labeled false-positive set.
- [ ] **Integration:** Idempotent queue writes - verify no duplicates after rerunning same source batch.
- [ ] **Compliance:** Sensitive-content handling policy implemented - verify redaction/isolation path in admin UI.

## Recovery Strategies

When pitfalls occur despite prevention, how to recover.

| Pitfall | Recovery Cost | Recovery Steps |
|---------|---------------|----------------|
| Duplicate queue flood | MEDIUM | Backfill dedupe script by idempotency key, add unique index, replay once |
| OCR quality collapse after deployment | MEDIUM | Roll back OCR config, run sampled calibration suite, reprocess failed batch |
| Portal selector breakage | LOW/MEDIUM | Update selector map, run smoke extraction on known fixtures, resume from checkpoint |
| Sensitive data over-exposure | HIGH | Revoke access, rotate storage/log credentials, purge exposed artifacts, incident review |

## Pitfall-to-Phase Mapping

How roadmap phases should address these pitfalls.

| Pitfall | Prevention Phase | Verification |
|---------|------------------|--------------|
| Navigation flakiness / wrong readiness checks | Phase 1 (Scraping foundation) | Stable extraction count across repeated runs |
| Download lifecycle loss | Phase 1 (Download pipeline) | Downloaded files exist after context closure and restart |
| OCR misuse and low quality | Phase 2 (OCR pipeline) | Word-error rate and throughput within defined acceptance band |
| `trânsito em julgado` false positives | Phase 3 (Legal triage rules) | Precision/recall on labeled legal-text set |
| Queue duplication / workflow races | Phase 4 (Generator integration) | Re-run idempotency test yields zero duplicate inserts |
| Privacy/secrecy handling gaps | Phase 4 (Compliance hardening) | Sensitive documents flagged and restricted in review flow |

## Sources

- https://playwright.dev/dotnet/docs/actionability (official docs, HIGH)
- https://playwright.dev/dotnet/docs/navigations (official docs, HIGH)
- https://playwright.dev/dotnet/docs/api/class-page (official API docs, HIGH)
- https://playwright.dev/dotnet/docs/downloads (official docs, HIGH)
- https://playwright.dev/dotnet/docs/browsers (official docs, HIGH)
- https://learn.microsoft.com/en-us/dotnet/core/extensions/workers (official docs, HIGH)
- https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service (official docs, HIGH)
- https://learn.microsoft.com/en-us/dotnet/fundamentals/networking/http/httpclient-guidelines (official docs, HIGH)
- https://tesseract-ocr.github.io/tessdoc/ImproveQuality.html (official docs, HIGH)
- https://tesseract-ocr.github.io/tessdoc/Data-Files-in-different-versions.html (official docs, HIGH)
- https://www.cnj.jus.br/programas-e-acoes/numeracao-unica/ (official CNJ page, MEDIUM)
- https://www.cnj.jus.br/programas-e-acoes/tabela-processuais-unificadas/ (official CNJ page, MEDIUM)
- https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2018/lei/l13709.htm (official law text, HIGH)
- https://www.planalto.gov.br/ccivil_03/_ato2015-2018/2015/lei/l13105.htm (official law text, HIGH)
- https://www.cnj.jus.br/pje-vai-automatizar-certificacao-de-transito-em-julgado/ (CNJ news article used for term context, LOW/MEDIUM)

---
*Pitfalls research for: OpenJustice.BrazilExtractor*
*Researched: 2026-03-02*
