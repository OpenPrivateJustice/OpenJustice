# Stack Research

**Domain:** Court-document extraction pipeline (Playwright scraping + OCR + legal text triage)
**Researched:** 2026-03-02
**Confidence:** MEDIUM

## Recommended Stack

### Core Technologies

| Technology | Version | Purpose | Why Recommended |
|------------|---------|---------|-----------------|
| `.NET Worker Service` (`net10.0`) | 10.0 | Run BrazilExtractor as a long-running batch worker | Fits current Generator stack, reuses DI/IOptions/logging patterns already validated in OpenJustice. |
| `Microsoft.Playwright` | 1.58.0 | Browser automation for TJGO search, navigation, and file download | Official .NET library, resilient auto-waiting model, and deterministic browser control for JS-heavy court portals. |
| `Tesseract` (NuGet wrapper) | 5.2.0 | Local OCR extraction from image/PDF-rendered pages | Open-source, local/offline processing, no per-page API cost, aligns with censorship-resistant/offline project principles. |
| `Microsoft.ML` | 5.0.0 | Rule-plus-model text classification for criminal-case identification | Native .NET ML pipeline when regex rules plateau; keeps analysis in-process and versionable in repo. |

### Supporting Libraries

| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Docnet.Core` | 2.6.0 | Render PDF pages to images before OCR | Use when source PDFs are scanned/image-based and not directly text-selectable. |
| `Polly` | 8.6.5 | Retry/backoff/circuit-breaker for unstable fetch/download calls | Use around TJGO requests and PDF downloads to avoid transient failure storms. |
| `Npgsql.EntityFrameworkCore.PostgreSQL` | 10.0.0 (existing) | Persist extraction queue/results into existing Generator database | Reuse existing EF Core + Postgres integration instead of introducing a second persistence layer. |
| `IronOcr` (alternative) | 2026.3.3 | Commercial OCR engine with stronger PDF/filters out of the box | Use only if Tesseract accuracy is insufficient after preprocessing/tuning and licensing is approved. |

### Development Tools

| Tool | Purpose | Notes |
|------|---------|-------|
| `playwright.ps1 install` | Install browser binaries matched to Playwright package | Must be rerun when Playwright version changes. Pin in setup docs/CI image. |
| `tessdata` language files (`por`) | Portuguese OCR model data | Required for Brazilian documents; version and path should be configurable via `appsettings.json`. |
| Existing OpenJustice config (`IOptions`) | Centralized extractor tuning | Add `BrazilExtractorOptions` for interval (30s), max PDFs/query (15), OCR paths, and keyword thresholds. |

## Installation

```bash
# New BrazilExtractor project (if not yet created)
dotnet new worker -n OpenJustice.BrazilExtractor -f net10.0

# Core scraping + OCR + analysis
dotnet add package Microsoft.Playwright --version 1.58.0
dotnet add package Tesseract --version 5.2.0
dotnet add package Microsoft.ML --version 5.0.0

# Supporting
dotnet add package Docnet.Core --version 2.6.0
dotnet add package Polly --version 8.6.5

# If choosing commercial OCR route instead of Tesseract
dotnet add package IronOcr --version 2026.3.3

# Playwright browser install (after build)
dotnet build
pwsh bin/Debug/net10.0/playwright.ps1 install
```

## Alternatives Considered

| Recommended | Alternative | When to Use Alternative |
|-------------|-------------|-------------------------|
| `Microsoft.Playwright` | Selenium WebDriver | Only if team already has heavy Selenium tooling and does not need Playwright auto-wait/browser-context model. |
| `Tesseract 5.2.0` | `IronOcr 2026.3.3` | Use IronOcr when you need better OCR quality quickly and budget/legal approves commercial licensing. |
| Rule-first (`Regex` + keyword dictionaries) + optional `Microsoft.ML` | Immediate LLM/embedding pipeline | Use LLM pipeline later, after baseline extraction quality is measured and deterministic rules are saturated. |

## What NOT to Use

| Avoid | Why | Use Instead |
|-------|-----|-------------|
| New Python scraping/OCR microservice | Adds second runtime/deployment path and operational drift from current .NET-only Generator stack | Keep extractor in .NET 10 Worker project inside existing solution. |
| Cloud OCR APIs as default (Azure Vision, Google Vision) | Ongoing cost, external dependency, and potential legal/privacy concerns for sensitive court documents | Local OCR first (`Tesseract`), cloud OCR only as explicit fallback if approved. |
| Parallel browser swarm for MVP | Court portal + requirement already impose 30s cadence and 15 PDFs/query; parallelism raises blocking/rate-limit risk | Single-browser controlled queue with strict throttling. |
| Separate new database for extractor staging | Duplicates schema and complicates case-approval handoff | Reuse existing PostgreSQL and Generator data contracts/workflow. |

## Stack Patterns by Variant

**If cost-sensitive / fully local (recommended default):**
- Use `Playwright + Tesseract + Docnet.Core + rule-based text filters (+ Microsoft.ML later)`
- Because it stays fully local, open-source, and aligned with OpenJustice offline/censorship-resilient goals.

**If OCR quality is blocking delivery:**
- Use `Playwright + IronOcr` (drop `Tesseract` path)
- Because IronOcr can reduce engineering effort in preprocessing/PDF handling, at licensing cost.

## Version Compatibility

| Package A | Compatible With | Notes |
|-----------|-----------------|-------|
| `Microsoft.Playwright@1.58.0` | `.NET net10.0` | Existing repo test project is on `1.48.0`; update to avoid mixed Playwright versions. |
| `Tesseract@5.2.0` | `.NET Standard 2.0+` (`net10.0` compatible) | Wrapper is mature but older; validate runtime behavior on Linux target image before production. |
| `IronOcr@2026.3.3` | `.NET Standard 2.0+` (`net10.0` compatible) | Commercial license required for production use. |
| `Microsoft.ML@5.0.0` | `.NET net10.0` | Keep feature optional until enough labeled Brazilian legal text exists for useful training. |

## Integration Points with Generator

1. Add `OpenJustice.BrazilExtractor` as a new Worker project in the same solution, sharing `net10.0` and `appsettings.json`/`IOptions` conventions.
2. Reuse Generator domain contracts (DTOs/entities) via project reference or shared contracts package; do not redefine case payload schema.
3. Persist extracted candidates into existing Postgres (queue table/status fields) so Generator.Web admin approval flow can consume them directly.
4. Keep extractor output deterministic and auditable: raw PDF, OCR text, match reasons, and confidence score stored for admin review.

## Sources

- https://playwright.dev/dotnet/docs/intro - install flow and system requirements (official docs, HIGH)
- https://playwright.dev/dotnet/docs/library - .NET library usage pattern (official docs, HIGH)
- https://playwright.dev/dotnet/docs/browsers - browser binary/version coupling and install commands (official docs, HIGH)
- https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json - latest package version `1.58.0` (official package feed, HIGH)
- https://api.nuget.org/v3-flatcontainer/tesseract/index.json - latest package version `5.2.0` (official package feed, HIGH)
- https://github.com/charlesw/tesseract - wrapper dependencies and tessdata requirement (official repo, MEDIUM)
- https://tesseract-ocr.github.io/tessdoc/ - OCR engine guidance and traineddata model sets (official docs, HIGH)
- https://api.nuget.org/v3-flatcontainer/ironocr/index.json - latest package version `2026.3.3` (official package feed, HIGH)
- https://www.nuget.org/packages/IronOcr/ - commercial licensing statement and compatibility claims (official package page, HIGH)
- https://api.nuget.org/v3-flatcontainer/microsoft.ml/index.json - latest package version `5.0.0` (official package feed, HIGH)
- https://learn.microsoft.com/en-us/dotnet/machine-learning/ - ML.NET overview and supported workflows (official docs, HIGH)
- https://api.nuget.org/v3-flatcontainer/docnet.core/index.json - package versions for PDF rendering choice (official package feed, MEDIUM)
- https://api.nuget.org/v3-flatcontainer/polly/index.json - latest Polly version `8.6.5` (official package feed, HIGH)

---
*Stack research for: OpenJustice.BrazilExtractor*
*Researched: 2026-03-02*
