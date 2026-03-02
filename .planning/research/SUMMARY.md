# Project Research Summary

**Project:** OpenJustice v2.0 BrazilExtractor
**Domain:** Court-document extraction pipeline (TJGO scraping + OCR + legal triage + human curation)
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH

## Executive Summary

This project is a compliance-sensitive ingestion pipeline that turns TJGO court publications into reviewable case candidates inside the existing OpenJustice Generator workflow. The expert pattern is clear: keep scraping, download, OCR, and legal triage in a dedicated .NET worker; process in deterministic, throttled stages; and preserve evidence (artifacts, snippets, reasons, provenance) so humans can approve before publication.

The recommended approach is a single-runner `OpenJustice.BrazilExtractor` on `.NET 10` using `Microsoft.Playwright` for portal interaction, local OCR (`Tesseract` + `por`) for scanned documents, and rule-first legal classification with explainable outputs. Integrate at the existing `DiscoveredCase` boundary instead of creating a parallel moderation path. Build order should be dependency-first: contracts and idempotency guarantees, then scraper/download reliability, then OCR branching, then legal triage quality, then operational hardening.

Key risks are portal anti-automation flakiness, download lifecycle loss, OCR quality/performance collapse, false-positive final-judgment detection, and duplicate queue writes. Mitigation is equally explicit: locator/actionability-based Playwright flows, immediate persisted downloads with checksums, mixed PDF strategy (text-first then OCR fallback), negation-aware rule sets with regression corpus, and unique idempotency keys plus state-transition guards. Privacy controls (retention, redaction, sensitive-case handling) are required before broad rollout.

## Key Findings

### Recommended Stack

Research converges on staying inside the current OpenJustice runtime and operational model: `.NET Worker Service` (`net10.0`) plus existing DI/options/EF patterns. This minimizes integration risk and avoids introducing a second platform. The stack is intentionally local-first to align with project principles (cost control, resilience, privacy posture), with commercial OCR only as a controlled fallback.

**Core technologies:**
- `.NET Worker Service (net10.0)`: batch orchestration — matches current Generator conventions and deployment model.
- `Microsoft.Playwright 1.58.0`: TJGO browser automation/download control — robust auto-waiting and deterministic browser context handling.
- `Tesseract 5.2.0` (+ `por` tessdata): local OCR extraction — offline, auditable, and no per-document API cost.
- `Microsoft.ML 5.0.0` (optional later): model-assisted triage — only after rule baseline and labeled corpus exist.
- `Docnet.Core 2.6.0` + `Polly 8.6.5`: PDF rendering and resilience — needed for scanned PDFs and transient portal/download failures.

### Expected Features

The MVP is a reliability-first extraction pipeline, not an autonomy-first AI system. Table stakes center on stable TJGO query execution, anti-bot-aware pacing, evidence-preserving capture, Portuguese OCR quality controls, criminal/finality triage, and queue handoff into existing Generator review.

**Must have (table stakes):**
- Deterministic TJGO `ConsultaPublicacao` flow with token/captcha-aware interactions and strict cadence (`30s`, `max 15 PDFs/query`).
- PDF artifact persistence + provenance metadata + checksum before analysis.
- Portuguese OCR pipeline with confidence/empty-page quality gates and UTF-8 normalization.
- Rule-based criminal triage including context-aware `trânsito em julgado` detection.
- Integration into existing Generator admin evaluation queue (`EXTR-07`).

**Should have (competitive):**
- Explainable scoring payloads (matched rules/snippets/weights) to accelerate admin review.
- Idempotent reruns + dedup keys to prevent reviewer queue spam.
- Mixed extraction strategy (text layer first, OCR fallback) for speed/quality.

**Defer (v2+):**
- Reviewer feedback loop and taxonomy enrichment once sufficient reviewed corpus exists.
- Additional tribunal adapters (TJSP/TJRJ/TRFs) only after TJGO operational stability.
- ML/LLM-heavy classifier layers after deterministic baseline and evaluation harness mature.

### Architecture Approach

Architecture should follow a durable stage pipeline in a new `OpenJustice.BrazilExtractor` worker, with explicit boundaries: scraper -> downloader -> OCR -> analyzer -> publisher. Persist checkpoints and evidence after each stage, publish normalized candidates into existing `DiscoveredCase`, and keep Generator.Web as the authoritative human gate. Use a single-runner throttled queue first, then add bounded OCR parallelism only after correctness and curation UX stabilize.

**Major components:**
1. `OpenJustice.BrazilExtractor` worker — orchestrates batches with strict throttling and checkpointed execution.
2. Stage services (`IScraper`, `IPdfDownloader`, `IOcrProcessor`, `ITextAnalyzer`) — isolated responsibilities with auditable outputs.
3. Generator integration boundary (`DiscoveredCase`, `DiscoveryStatus`, `SourceType`) — preserves existing approval/reject/promote workflow.
4. Artifact + persistence layer (PostgreSQL + managed file paths) — stores provenance, OCR pointers, hashes, and immutable reasoning traces.

### Critical Pitfalls

1. **Playwright readiness mistakes** — avoid `networkidle`; gate on locator/actionability, explicit URL/element expectations, and keep traces.
2. **Download lifecycle loss** — always `WaitForDownloadAsync` + `SaveAsAsync`; do not close browser context before persisted checksum.
3. **OCR-everything strategy** — branch by document type (native text first, OCR for scanned/low-yield pages only).
4. **Naive `trânsito em julgado` matching** — implement positive/negative/uncertain patterns with snippet evidence and regression tests.
5. **Non-idempotent queue writes** — enforce deterministic keys, unique constraints/upsert, and explicit state transitions.

## Implications for Roadmap

Based on research, suggested phase structure:

### Phase 1: Contracts, Worker Skeleton, and Deterministic Ingestion
**Rationale:** Integration boundaries and lifecycle correctness are prerequisites for every downstream feature.
**Delivers:** `OpenJustice.BrazilExtractor` project, options validation, source-type/schema updates, idempotency key design, TJGO scrape+download baseline with strict cadence.
**Addresses:** EXTR-01, EXTR-02, EXTR-03 foundations; table-stakes query execution and artifact capture.
**Avoids:** Playwright readiness flakiness, context-bound download loss, hosted-service lifetime misuse.

### Phase 2: OCR Pipeline and Artifact Quality Gates
**Rationale:** Triage quality depends directly on extraction quality; OCR strategy must be correct before legal classification.
**Delivers:** mixed PDF extraction path (text-first, OCR fallback), Portuguese model setup, preprocessing, confidence flags, persisted OCR text artifacts.
**Uses:** `Tesseract`, `Docnet.Core`, resilient retries with `Polly`.
**Implements:** durable stage checkpoints and quality-gate metadata.

### Phase 3: Legal Triage and Explainability
**Rationale:** Business value comes from reducing admin review load without sacrificing trust.
**Delivers:** rule-based crime/finality scoring, negation-aware `trânsito em julgado` logic, explainable reason snippets, baseline precision/recall test set.
**Addresses:** EXTR-05 and EXTR-06; differentiator-level explainability.
**Avoids:** false-positive finality classification and opaque classifier outputs.

### Phase 4: Generator Queue Integration, Idempotency Hardening, and Compliance
**Rationale:** Output is only useful when safely consumable by existing curation workflow and legally defensible.
**Delivers:** `DiscoveredCase` publishing path, dedup/upsert constraints, admin UI evidence display, retention/redaction/sensitive-content controls.
**Addresses:** EXTR-07 and anti-feature constraints (no auto-publish).
**Avoids:** duplicate queue floods, workflow divergence, privacy/secrecy violations.

### Phase 5: Operational Hardening and Controlled Throughput Expansion
**Rationale:** Scale only after correctness and review UX are stable.
**Delivers:** resume/replay checkpoints, run metrics/alerts, bounded OCR parallelism, reviewer-priority filters.
**Addresses:** reliability and growth path toward higher PDF volumes.
**Avoids:** unbounded memory buffering, EF scope/thread misuse, premature scraper concurrency.

### Phase Ordering Rationale

- Dependencies are strict: ingestion reliability -> OCR quality -> triage validity -> queue integration/compliance -> throughput tuning.
- Architecture favors boundary-first integration (existing `DiscoveredCase` workflow) over new subsystems, reducing change surface.
- This order directly neutralizes the highest-risk pitfalls in the phase they most often appear.

### Research Flags

Phases likely needing deeper research during planning:
- **Phase 1:** TJGO anti-bot/token behavior can shift; validate current selectors/challenge paths with fresh portal runs.
- **Phase 3:** Legal language edge cases for final-judgment detection need curated jurisprudential examples.
- **Phase 4:** Data-retention/redaction policy needs legal/compliance sign-off criteria before production rollout.

Phases with standard patterns (skip research-phase):
- **Phase 2:** OCR pipeline implementation patterns are well documented (Tesseract quality and preprocessing guidance).
- **Phase 5:** Worker lifecycle, retry/backoff, and bounded concurrency follow established .NET operational patterns.

## Confidence Assessment

| Area | Confidence | Notes |
|------|------------|-------|
| Stack | HIGH | Version and compatibility claims are backed by official docs/NuGet feeds; only runtime OCR behavior on target Linux remains to validate. |
| Features | MEDIUM | Strong portal observations and clear milestone alignment, but anti-bot behavior is empirical and can drift. |
| Architecture | MEDIUM-HIGH | Pattern fit is strong with existing Generator boundaries and official hosted-service/EF guidance. |
| Pitfalls | MEDIUM-HIGH | Most failure modes are backed by official docs and domain constraints; legal/privacy interpretation still needs policy confirmation. |

**Overall confidence:** MEDIUM-HIGH

### Gaps to Address

- **Portal volatility baseline:** run repeatability probes (selectors/challenges/download paths) in planning and before each release candidate.
- **OCR acceptance thresholds:** define measurable quality SLOs (sample set, word-error tolerance, throughput targets) before go-live.
- **Legal-rule corpus:** create labeled PT-BR snippets for positive/negative/uncertain `trânsito em julgado` and homicide-related patterns.
- **Compliance policy formalization:** document retention windows, ACL model, redaction triggers, and incident response ownership.
- **Schema/index confirmation:** validate idempotency key uniqueness and query performance against expected queue volume.

## Sources

### Primary (HIGH confidence)
- `https://playwright.dev/dotnet/docs/intro` and related official Playwright .NET docs (library, navigations, actionability, downloads, browsers) — automation and download lifecycle patterns.
- `https://learn.microsoft.com/en-us/dotnet/core/extensions/workers` and scoped-service/options/EF docs — worker lifetime, DI, and persistence boundaries.
- NuGet feeds for `Microsoft.Playwright`, `Tesseract`, `Microsoft.ML`, `Polly` — package versions and compatibility checks.
- `https://tesseract-ocr.github.io/tessdoc/` and quality docs — OCR model/preprocessing guidance.
- `https://projudi.tjgo.jus.br/ConsultaPublicacao` — target portal workflow, operators, and form behavior.
- Brazilian legal/court references: LGPD, CPC, Lei 11.419, CNJ numbering/TPU pages — compliance and metadata normalization context.

### Secondary (MEDIUM confidence)
- Runtime Playwright observations on TJGO challenge behavior (Cloudflare/Turnstile) from 2026-03-02 exploratory runs.
- `charlesw/tesseract` wrapper repository notes on runtime dependencies and tessdata setup.
- CNJ public class/taxonomy lookup pages for later enrichment mapping strategy.

### Tertiary (LOW confidence)
- CNJ news/context article on `trânsito em julgado` automation language usage — useful context, not normative specification.

---
*Research completed: 2026-03-02*
*Ready for roadmap: yes*
