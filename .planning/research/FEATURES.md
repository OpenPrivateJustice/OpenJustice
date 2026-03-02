# Feature Research

**Domain:** Court document scraping + OCR + criminal case triage for `OpenJustice.BrazilExtractor`
**Researched:** 2026-03-02
**Confidence:** MEDIUM

## Feature Landscape

### Table Stakes (Users Expect These)

Features users assume exist. Missing these = extractor is not reliable enough for Generator review workflow.

| Feature | Why Expected | Complexity | Notes |
|---------|--------------|------------|-------|
| TJGO `ConsultaPublicacao` query execution (free text + field filters) | Court-publication extractors must consistently retrieve relevant notices before any OCR/analysis can happen | MEDIUM | Page exposes two search modes (`Pesquisa Livre` and `Pesquisa por campo especifico`) and form-based submit to `ConsultaPublicacao`; implement deterministic Playwright form filling and submit handling. |
| Respectful portal access controls (captcha token + fixed cadence + bounded batch size) | Public court portals commonly enforce anti-bot and fair-use controls; ignoring this breaks scraping quickly | HIGH | `ConsultaPublicacao` includes Cloudflare Turnstile (`g-recaptcha-response`) and anti-automation challenge endpoints were observed during scripted submit. Keep strict pacing (project constraint: 30s interval, max 15 PDFs/query) and retry with backoff, not parallel burst scraping. |
| Evidence-preserving document capture | Admin reviewers need auditable source artifacts, not just extracted text | MEDIUM | Persist source metadata (query, URL/action, process number if available), PDF file, checksum, and fetch timestamp before OCR. |
| Portuguese OCR pipeline with quality gates | Court publications often contain scanned/low-quality pages; text extraction quality drives all downstream classification | HIGH | Use PT-BR OCR defaults (`por` language), preprocess image pages when needed, and store OCR confidence/empty-page flags to avoid silent false negatives. |
| Criminal-case triage rules (hediondos/homicidio/transito) | Extractor value is reducing manual review load to likely criminal, finalized cases | HIGH | Start with deterministic legal-term rules and reason snippets, including explicit `transito em julgado` detection and homicide/traffic-as-murder patterns from milestone scope. |
| Queue handoff into existing Generator curation | New extractor is only useful if candidates appear in current approval workflow | MEDIUM | Output must map to Generator evaluation queue (EXTR-07), with status, reasons, and artifact references for admin accept/reject. |

### Differentiators (Competitive Advantage)

Features that are not mandatory for first delivery, but materially improve reviewer trust and throughput.

| Feature | Value Proposition | Complexity | Notes |
|---------|-------------------|------------|-------|
| Explainable scoring per candidate | Reviewers can quickly see why a case was selected, reducing approval time and false positives | MEDIUM | Emit matched keywords/phrases, section snippets, and weighted confidence instead of one opaque score. |
| Idempotent re-runs with dedup across publications | Prevents queue spam from repeated publications and lets extractor run safely in batches | MEDIUM | Dedup key should combine process identifier + publication date + document hash where available. |
| OCR fallback path for mixed PDFs (text layer first, OCR second) | Improves speed and accuracy by avoiding unnecessary OCR on text-native documents | MEDIUM | Attempt direct text extraction first; only render+OCR pages with low/no text yield. |
| Analyst feedback loop into rules | Quality improves over time using admin accept/reject outcomes already present in Generator workflow | HIGH | Store false-positive/false-negative reasons from review and use them to tune rule weights and dictionaries. |

### Anti-Features (Commonly Requested, Often Problematic)

Features that sound attractive but are risky for this milestone.

| Feature | Why Requested | Why Problematic | Alternative |
|---------|---------------|-----------------|-------------|
| Auto-create public case records with no human approval | Feels faster and "fully automated" | High legal/reputational risk from OCR/classification errors | Keep mandatory Generator admin approval gate (existing curation flow). |
| Multi-court scraping in same milestone | Appears to increase coverage quickly | Different portals, schemas, and anti-bot behavior will delay TJGO reliability | Stabilize TJGO extractor first, then add court adapters in later phase. |
| High-concurrency scraping swarm | Looks like faster ingestion | Increases challenge/rate-limit risk and violates project pacing constraints | Single controlled worker with queue + backoff + strict 30s cadence. |
| LLM-only legal classification for MVP | Promises "smarter" triage | Harder to audit deterministically; higher false-positive risk on legal nuance | Rule-first classifier with explicit reasons; add ML model after labeled dataset matures. |

## Expected TJGO Publicacao Behaviors (for implementation)

1. `https://projudi.tjgo.jus.br/ConsultaPublicacao` is a form workflow, not a clean JSON API; extractor should model browser interactions, hidden fields, and post-submit navigation.
2. Search UX exposes boolean-like operators (`E`, `OU`, `ADJ`, `NAO`, `PROX`, `$`), so query composer should support operator templates, not plain keyword concatenation.
3. Download/open actions are token-coupled (`g-recaptcha-response` is appended in JS `abrirArquivo(action, id)`), so token lifecycle management is table-stakes.
4. Portal uses anti-automation checks (Cloudflare Turnstile + challenge endpoints observed in runtime); extractor must expect intermittent challenge/retry paths.
5. Page content/metadata indicates `ISO-8859-1` usage; normalize encoding to UTF-8 before OCR/text analysis storage.

## Feature Dependencies

```text
TJGO query execution
    -> requires -> captcha/token handling + cadence control
        -> requires -> document capture (PDF + metadata)
            -> requires -> OCR/text extraction
                -> requires -> criminal triage + transito em julgado detection
                    -> requires -> Generator evaluation queue handoff

Dedup/idempotency
    -> enhances -> Generator queue handoff

Explainable scoring
    -> enhances -> admin approval speed/accuracy

Auto-publish without review
    -> conflicts -> existing Generator curation workflow
```

### Dependency Notes

- **OCR depends on stable capture:** without reliable PDF retrieval and metadata, OCR quality controls and auditability collapse.
- **Triage depends on OCR/text quality:** weak extraction directly inflates false positives/negatives in crime detection.
- **Extractor output depends on Generator contracts:** queue payload must fit existing curation endpoints/entities; otherwise EXTR-07 cannot ship.
- **Dedup depends on Generator case history:** compare against existing case/process records before queue insertion to avoid reviewer noise.

## MVP Definition

### Launch With (milestone v2.0)

- [ ] TJGO publication scraping flow with controlled cadence and bounded downloads (15/query, 30s interval) - core ingestion contract.
- [ ] PDF artifact persistence + deterministic metadata trail - required for legal/audit review.
- [ ] Local OCR to UTF-8 text files with basic quality checks - required for downstream analysis.
- [ ] Rule-based criminal filtering + `transito em julgado` filtering - core triage value.
- [ ] Queue integration into Generator.Web admin evaluation - required to reuse current curation process.

### Add After Validation (v2.x)

- [ ] Reviewer feedback-driven rule tuning - add when at least ~200 reviewed candidates exist.
- [ ] Mixed-document extraction strategy (text-layer first, OCR fallback) - add when OCR cost/time becomes bottleneck.
- [ ] Broader legal taxonomy mapping (CNJ classes/assuntos enrichment) - add once baseline precision is stable.

### Future Consideration (v3+)

- [ ] Additional tribunal adapters (TJSP/TJRJ/TRFs) - defer until TJGO adapter is operationally stable.
- [ ] Statistical/ML classifier layer over rule baseline - defer until labeled corpus and evaluation harness exist.
- [ ] Near-real-time monitoring - defer; current scope is batch processing.

## Feature Prioritization Matrix

| Feature | User Value | Implementation Cost | Priority |
|---------|------------|---------------------|----------|
| TJGO scrape + token/cadence control | HIGH | HIGH | P1 |
| PDF capture + provenance metadata | HIGH | MEDIUM | P1 |
| OCR + text persistence | HIGH | HIGH | P1 |
| Rule-based crime/finality triage | HIGH | HIGH | P1 |
| Generator queue handoff | HIGH | MEDIUM | P1 |
| Explainable scoring payload | MEDIUM | MEDIUM | P2 |
| Dedup/idempotent reruns | MEDIUM | MEDIUM | P2 |
| Feedback-driven tuning loop | MEDIUM | HIGH | P3 |

## Dependencies on Existing Generator

| Generator Capability | BrazilExtractor Dependency | Why It Matters |
|----------------------|----------------------------|----------------|
| Existing case/curation workflow | Candidate queue ingestion target | Extractor should feed current admin review, not invent a second moderation path. |
| Existing PostgreSQL + EF Core models | Shared persistence for queue/artifact references | Avoids duplicate schemas and keeps reviewer context in one system. |
| Existing Blazor admin UI | Display OCR text, reasons, confidence, source links | Review speed depends on transparent evidence in current UI. |
| Existing history/audit patterns | Record extractor decision traces | Supports trust and later tuning of triage logic. |

## Sources

- https://projudi.tjgo.jus.br/ConsultaPublicacao - live TJGO publication form structure, operators, hidden fields, encoding markers, and Turnstile script (official portal, HIGH)
- Runtime observation via Playwright on `projudi.tjgo.jus.br` (2026-03-02): Cloudflare challenge/Turnstile flow during scripted submit and token-dependent interactions (direct empirical observation, MEDIUM)
- https://www.planalto.gov.br/ccivil_03/_ato2004-2006/2006/lei/l11419.htm - legal basis for electronic judicial publications/process documents (official federal law text, HIGH)
- https://tesseract-ocr.github.io/tessdoc/ImproveQuality - OCR quality constraints and preprocessing guidance (official docs, HIGH)
- https://tesseract-ocr.github.io/tessdoc/Command-Line-Usage.html - OCR output formats and segmentation behavior relevant to pipeline design (official docs, HIGH)
- https://www.cnj.jus.br/sgt/consulta_publica_classes.php - CNJ unified procedural classes (includes criminal class hierarchy, versioned table) for later taxonomy enrichment (official CNJ system, MEDIUM)

---
*Feature research for: OpenJustice.BrazilExtractor*
*Researched: 2026-03-02*
