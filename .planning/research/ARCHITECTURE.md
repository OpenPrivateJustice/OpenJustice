# Architecture Research

**Domain:** BrazilExtractor integration into existing OpenJustice Generator
**Researched:** 2026-03-02
**Confidence:** MEDIUM-HIGH

## Standard Architecture

### System Overview

```
┌──────────────────────────────────────────────────────────────────────────────┐
│                    Generator Control + Curation Layer (existing)            │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌──────────────────────────────┐  ┌──────────────────────────────────────┐ │
│  │ OpenJustice.Generator API    │  │ OpenJustice.Generator.Web            │ │
│  │ DiscoveryController (mod)    │  │ Discovery review screens (mod)       │ │
│  └───────────────┬──────────────┘  └──────────────────┬───────────────────┘ │
│                  │                                    │                     │
├──────────────────┴────────────────────────────────────┴─────────────────────┤
│                  BrazilExtractor Processing Layer (new)                     │
├──────────────────────────────────────────────────────────────────────────────┤
│  ┌────────────────────────────────────────────────────────────────────────┐  │
│  │ OpenJustice.BrazilExtractor (Worker, new)                             │  │
│  │                                                                        │  │
│  │  [1] PlaywrightScraper -> [2] PdfDownloader -> [3] OcrProcessor       │  │
│  │       -> [4] LegalTextAnalyzer -> [5] CandidatePublisher              │  │
│  │                                                                        │  │
│  │  Execution model: single-runner queue, strict throttling (30s cadence)│  │
│  └────────────────────────────────────────────────────────────────────────┘  │
├──────────────────────────────────────────────────────────────────────────────┤
│                     Persistence + Filesystem Layer                          │
│  ┌────────────────────┐  ┌────────────────────┐  ┌───────────────────────┐ │
│  │ PostgreSQL (mod)    │  │ /downloads (new)    │  │ /temp OCR txt (new)   │ │
│  │ DiscoveredCase flow │  │ raw PDFs            │  │ extracted text         │ │
│  └────────────────────┘  └────────────────────┘  └───────────────────────┘ │
└──────────────────────────────────────────────────────────────────────────────┘
```

### Component Responsibilities

| Component | Responsibility | Typical Implementation |
|-----------|----------------|------------------------|
| `OpenJustice.BrazilExtractor` (new) | Execute batch extraction pipeline and push candidates into Generator review queue | .NET 10 Worker Service with `BackgroundService`, DI, `IOptions`, `PeriodicTimer` |
| `Scraping/OCR/Analysis stages` (new) | Isolate each step (portal navigation, download, OCR, legal triage) with clear stage outputs | Stage services (`IScraper`, `IPdfDownloader`, `IOcrProcessor`, `ITextAnalyzer`) + stage result records |
| `Generator discovery review workflow` (modified) | Keep human approval gate before promoting to official case | Reuse `DiscoveredCase` + `DiscoveryStatus` + existing approve/reject/promote endpoints |
| `Generator persistence contracts` (modified) | Store extractor-specific traceability (OCR text path, match reasons, judgment markers) | Extend `DiscoveredCase.Metadata` schema and `SourceType` enum for `CourtPortal` |

## New vs Modified Components

| Area | New | Modified |
|------|-----|----------|
| Solution/projects | `src/OpenJustice.BrazilExtractor/` Worker | `OpenJustice.sln` project references |
| Configuration | `BrazilExtractorOptions` section | Generator `appsettings*.json` and options binding |
| Discovery source model | N/A | `DiscoverySourceType` add court source value |
| Data storage | Local PDF/TXT artifact paths and hash metadata | `DiscoveredCase.Metadata` conventions, optional indexed fields for queryability |
| API/web review | Optional trigger endpoint for extractor run status | `DiscoveryController` filters/display for court-source candidates |
| Operations | Browser and tessdata runtime dependencies | Startup docs/scripts (`playwright.ps1 install`, `tessdata/por`) |

## Recommended Project Structure

```
src/
├── OpenJustice.BrazilExtractor/                     # New worker project (batch extraction)
│   ├── Configuration/                               # BrazilExtractorOptions + validation
│   ├── Pipeline/
│   │   ├── Scraping/                                # Playwright navigation and query submission
│   │   ├── Download/                                # PDF capture, naming, and checksum
│   │   ├── Ocr/                                     # PDF->image render and OCR extraction
│   │   ├── Analysis/                                # legal keyword/rule scoring
│   │   └── Orchestration/                           # run coordinator + stage checkpoints
│   ├── Persistence/                                 # EF writes to Generator database
│   └── Program.cs                                   # Host builder, DI, hosted service
├── OpenJustice.Generator/                           # Existing API/domain/persistence
│   ├── Domain/Enums/                                # modify DiscoverySourceType
│   ├── Infrastructure/Persistence/Entities/         # optional DiscoveredCase metadata fields
│   ├── Services/Discovery/                          # integrate source-specific mapping rules
│   └── Controllers/DiscoveryController.cs           # optional extractor run/status endpoints
└── OpenJustice.Generator.Web/                       # Existing admin UI
    └── Pages/ (or Components/)                      # show court-source extraction evidence
```

### Structure Rationale

- **`OpenJustice.BrazilExtractor/`:** keep runtime-heavy browser/OCR dependencies out of API process; failures do not take down Generator API.
- **Pipeline folders:** each stage can be tested independently and retried safely.
- **Generator modifications only at boundaries:** queue persistence and review UI stay in current curation workflow.

## Architectural Patterns

### Pattern 1: Durable Stage Pipeline (recommended default)

**What:** Persist stage outputs (artifact paths, hashes, analysis summary) after each step.
**When to use:** Required for court documents because OCR/analysis quality must be auditable by human curators.
**Trade-offs:** More storage and schema discipline, but avoids full rerun after partial failures.

**Example:**
```csharp
public sealed record ExtractionCheckpoint(
    string DiscoveryHash,
    string SourceUrl,
    string? PdfPath,
    string? OcrTextPath,
    string Stage,
    DateTime UpdatedAtUtc);
```

### Pattern 2: Single-Runner Throttled Queue

**What:** Process one query/download chain at a time with explicit delay windows.
**When to use:** TJGO extraction with hard requirements (15 PDFs/query, 30s interval) and anti-blocking risk.
**Trade-offs:** Lower throughput; much lower portal lockout risk and easier incident debugging.

**Example:**
```csharp
while (!stoppingToken.IsCancellationRequested)
{
    await _orchestrator.RunNextBatchAsync(stoppingToken);
    await Task.Delay(TimeSpan.FromSeconds(options.IntervalSeconds), stoppingToken);
}
```

### Pattern 3: Existing Discovery Queue as Integration Boundary

**What:** Publish extractor output as `DiscoveredCase` records rather than introducing a second queue.
**When to use:** When the admin decision path already exists and must remain authoritative.
**Trade-offs:** Discovery model grows in metadata complexity, but avoids duplicate moderation workflow.

## Data Flow

### Request Flow

```
[Scheduled run or manual run trigger]
    ↓
[BrazilExtractor Worker]
    ↓ Playwright search on TJGO
[PDF download (max 15/query, delay 30s)]
    ↓
[OCR extraction -> .txt artifact]
    ↓
[Legal text analysis]
    ↓ (contains crime markers + "transito em julgado" hit)
[CandidatePublisher]
    ↓
[Generator DB: DiscoveredCase(Status=Pending, SourceType=CourtPortal)]
    ↓
[Generator.Web admin review -> approve/reject/promote]
```

### State Management

```
[Extractor run state]
    ↓ (in-memory run context)
[Stage services] -> [Checkpoint persistence] -> [PostgreSQL + files]
    ↓
[Admin curation state remains DiscoveryStatus workflow]
```

### Key Data Flows

1. **Ingestion flow:** court query -> document list -> local PDF artifacts + dedupe hash.
2. **Qualification flow:** PDF -> OCR text -> rule scoring (crime + final judgment) -> pending candidate.
3. **Curation flow:** pending candidate -> human decision -> optional promotion into formal case records.

## Scaling Considerations

| Scale | Architecture Adjustments |
|-------|--------------------------|
| Pilot (up to ~500 PDFs/day) | Single worker instance, sequential pipeline, local disk artifacts, one DB writer scope per item |
| Growth (~5k PDFs/day) | Separate scrape queue from OCR queue, bounded channels, parallel OCR workers with independent `DbContext` scopes |
| High volume (50k+/day) | Move artifacts to object storage, split extractor into dedicated services, add explicit run ledger and reprocessing jobs |

### Scaling Priorities

1. **First bottleneck:** OCR throughput and disk IO; fix with staged queue + controlled OCR parallelism before any scraper parallelism.
2. **Second bottleneck:** DB writes and candidate review volume; fix with batch inserts and improved review filters (source, score, judgment marker).

## Anti-Patterns

### Anti-Pattern 1: Running Playwright inside Generator API request handlers

**What people do:** Trigger full scraping/OCR from an HTTP endpoint synchronously.
**Why it's wrong:** Long-running browser/OCR work blocks API threads and increases API instability.
**Do this instead:** Trigger worker-run jobs and persist status for polling.

### Anti-Pattern 2: Creating a second candidate schema separate from `DiscoveredCase`

**What people do:** Add a Brazil-only queue table and separate approval UI.
**Why it's wrong:** Duplicates moderation logic and creates divergent curation behavior.
**Do this instead:** Reuse current discovery workflow, extend metadata and source typing.

### Anti-Pattern 3: Sharing one `DbContext` across parallel OCR tasks

**What people do:** Reuse scoped context instance across concurrent stage tasks.
**Why it's wrong:** EF Core `DbContext` is not thread-safe and causes intermittent corruption/errors.
**Do this instead:** create scope/factory per work item or worker lane.

## Integration Points

### External Services

| Service | Integration Pattern | Notes |
|---------|---------------------|-------|
| TJGO portal (`projudi.tjgo.jus.br`) | Playwright browser automation (`WaitForDownloadAsync`, controlled navigation) | Download files are tied to browser context lifecycle; persist immediately to managed path |
| Local OCR engine (`tesseract` + `por` data) | In-process OCR stage with configured tessdata path | Keep OCR local-first; fallback OCR engine only if quality gates fail |

### Internal Boundaries

| Boundary | Communication | Notes |
|----------|---------------|-------|
| `BrazilExtractor -> Generator DB` | EF Core writes using shared persistence model | Publish only normalized candidate + evidence pointers, not full raw binary in DB |
| `Generator API -> Generator.Web` | existing discovery endpoints | Keep approval/rejection/promotion unchanged; add court-source filters/details |
| `Generator config -> Extractor runtime` | `IOptions`/`IOptionsMonitor` binding from appsettings | Validate on startup (`ValidateOnStart`) to fail fast for missing paths/dependencies |

## Build Order (Dependency-First)

1. **Contracts and persistence boundary first**
   - Extend `DiscoverySourceType`, define metadata schema for OCR/analyzer evidence, add migration/indexes.
2. **Extractor skeleton and configuration**
   - Create worker project, bind `BrazilExtractorOptions`, add options validation, wire logging/DI.
3. **Stage implementations in strict sequence**
   - Playwright scraper -> PDF downloader -> OCR processor -> legal text analyzer.
4. **Publisher integration to existing discovery queue**
   - Map analyzer outputs into `DiscoveredCase` and verify Generator.Web can review items.
5. **Operational hardening**
   - Add retry/backoff, checkpoint resume, run metrics, and failure alerts.
6. **Optional throughput phase**
   - Introduce bounded parallel OCR only after baseline correctness and curator UX are stable.

## Sources

- Internal architecture baselines (HIGH):
  - `/home/eduardo/Projects/OpenJustice/.planning/PROJECT.md`
  - `/home/eduardo/Projects/OpenJustice/.planning/research/STACK.md`
  - `/home/eduardo/Projects/OpenJustice/src/OpenJustice.Generator/ServiceCollectionExtensions.cs`
  - `/home/eduardo/Projects/OpenJustice/src/OpenJustice.Generator/Infrastructure/Persistence/Entities/DiscoveredCase.cs`
  - `/home/eduardo/Projects/OpenJustice/src/OpenJustice.Generator/Controllers/DiscoveryController.cs`
  - `/home/eduardo/Projects/OpenJustice/src/OpenJustice.Generator/Services/Discovery/RssAggregatorService.cs`
- Official docs (HIGH):
  - https://learn.microsoft.com/en-us/aspnet/core/fundamentals/host/hosted-services?view=aspnetcore-10.0
  - https://playwright.dev/dotnet/docs/downloads
  - https://learn.microsoft.com/en-us/dotnet/core/extensions/options
  - https://learn.microsoft.com/en-us/ef/core/dbcontext-configuration/

---
*Architecture research for: OpenJustice.BrazilExtractor integration*
*Researched: 2026-03-02*
