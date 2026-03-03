# Phase 11: Extractor Foundation and TJGO Search - Research

**Researched:** 2026-03-02
**Domain:** .NET Worker foundation + Playwright-driven TJGO `ConsultaPublicacao` query workflow
**Confidence:** HIGH

<phase_requirements>
## Phase Requirements

| ID | Description | Research Support |
|----|-------------|-----------------|
| EXTR-01 | Create OpenJustice.BrazilExtractor project in solution (.NET 10 Worker) | Worker template + existing solution conventions define project shape, host model, and `net10.0` alignment. |
| EXTR-02 | Configure Playwright with Chromium for TJGO scraping | Playwright install/runtime model, browser binary coupling, and stable locator/wait patterns are documented and mapped to repo usage. |
| EXTR-03 | Implement TJGO publicacao page navigation and form handling | Live page HTML confirms form action, field IDs, hidden pagination fields, and submit/token behavior needed for deterministic automation. |
| EXTR-04 | Support criminal filter to exclude civil cases | Portal does not expose a direct "criminal-only" toggle in `ConsultaPublicacao`; plan must implement an explicit filter strategy (query/operator and/or typed field restriction) and verify with fixtures. |
| EXTR-05 | Support date range filter (single day: start = end) | Live form includes `DataInicial` and `DataFinal` fields; single-day contract is implemented by setting both to same value and validating request/results behavior. |
| EXTR-13 | `appsettings.json` configuration for extractor (download path, interval, etc.) | Existing repo uses sectioned `appsettings.json` + typed options; same pattern should be applied to extractor settings (portal URL, dates, filters, paths, cadence). |
| EXTR-14 | IOptions pattern for configuration management | Existing codebase already uses `AddOptions<T>().Bind(...).ValidateOnStart()` and custom `IValidateOptions<T>`; this is the standard for fail-fast config. |

</phase_requirements>

## Summary

Phase 11 is mostly a reliability and contract phase: stand up `OpenJustice.BrazilExtractor` as a .NET 10 worker, wire validated configuration, and prove deterministic navigation/submission on TJGO `ConsultaPublicacao`. The planning focus should be on execution correctness, not throughput. The major unknown is not Playwright mechanics; it is portal behavior drift (selectors, Turnstile token timing, and possible challenge pages).

The TJGO page is currently a server-rendered form (`action="ConsultaPublicacao"`) with explicit fields such as `textoDigitado`, `Texto`, `ProcessoNumero`, `DataInicial`, and `DataFinal`, plus hidden `PaginaAtual`/`PosicaoPaginaAtual`. The page also injects Cloudflare Turnstile and writes token value into hidden `g-recaptcha-response` before submit. This means Phase 11 planning must include robust request/response verification checkpoints and failure handling paths, not only happy-path UI clicks.

**Primary recommendation:** Plan Phase 11 around a minimal vertical slice: validated options -> Playwright Chromium session -> fill + submit form (including same-day date window) -> assert result-state contract with repeatable smoke runs.

## Standard Stack

### Core
| Library | Version | Purpose | Why Standard |
|---------|---------|---------|--------------|
| .NET Worker SDK | `net10.0` | Run extractor as hosted background process | Matches current solution target framework and hosting model. |
| `Microsoft.Extensions.Hosting` | `10.x` | Generic host + worker lifecycle | Official worker foundation and consistent with existing projects. |
| `Microsoft.Playwright` | `1.58.0` (latest) | Chromium automation for TJGO workflow | Official .NET browser automation with strong locator/actionability model. |
| `Microsoft.Extensions.Options.DataAnnotations` | `10.x` | Startup validation for extractor config | Enforces fail-fast settings; aligns with existing options pattern in repo. |

### Supporting
| Library | Version | Purpose | When to Use |
|---------|---------|---------|-------------|
| `Microsoft.Extensions.Options.ConfigurationExtensions` | `10.x` | Bind options from `appsettings` sections | Use for all extractor setting groups. |
| `xunit` + existing Playwright test project style | existing repo baseline | Smoke checks for portal flow regressions | Use to lock selectors/form contract before adding Phase 12 downloads. |

### Alternatives Considered
| Instead of | Could Use | Tradeoff |
|------------|-----------|----------|
| Playwright | Selenium WebDriver | Selenium is viable but less aligned with existing repo and modern auto-wait patterns. |
| Worker service | API-triggered scraping endpoint | Simpler trigger, but wrong lifetime model for long-running browser tasks and cancellation handling. |

**Installation:**
```bash
dotnet new worker -n OpenJustice.BrazilExtractor -f net10.0
dotnet add src/OpenJustice.BrazilExtractor package Microsoft.Playwright --version 1.58.0
dotnet add src/OpenJustice.BrazilExtractor package Microsoft.Extensions.Options.DataAnnotations --version 10.0.1
dotnet build src/OpenJustice.BrazilExtractor
pwsh src/OpenJustice.BrazilExtractor/bin/Debug/net10.0/playwright.ps1 install chromium
```

## Architecture Patterns

### Recommended Project Structure
```text
src/
├── OpenJustice.BrazilExtractor/
│   ├── Configuration/          # BrazilExtractorOptions + validators
│   ├── Services/
│   │   ├── Browser/            # Playwright factory/lifecycle
│   │   ├── Tjgo/               # Form navigation, fill, submit, result-state parsing
│   │   └── Jobs/               # Single-run orchestration service
│   ├── Models/                 # Query request/result contracts
│   └── Program.cs              # Host + options + DI registration
```

### Pattern 1: Fail-Fast Options Boundary
**What:** Bind extractor settings to typed options and validate on startup.
**When to use:** Always; this phase requires validated settings as explicit success criteria.
**Example:**
```csharp
builder.Services.AddOptions<BrazilExtractorOptions>()
    .Bind(builder.Configuration.GetSection("BrazilExtractor"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

### Pattern 2: Locator-First Submission Contract
**What:** Use stable locators/IDs and explicit post-submit assertions instead of arbitrary delays.
**When to use:** TJGO form submission, date filtering, and criminal filter paths.
**Example:**
```csharp
await page.GotoAsync(options.ConsultaPublicacaoUrl);
await page.Locator("#DataInicial").FillAsync(query.StartDate);
await page.Locator("#DataFinal").FillAsync(query.EndDate);
await page.Locator("input[name='tipoConsulta'][value='campo']").CheckAsync();
await page.Locator("#formLocalizarBotao").ClickAsync();
await page.WaitForURLAsync("**/ConsultaPublicacao**");
```

### Pattern 3: Scoped Work Inside Singleton Worker
**What:** Worker remains singleton, but DB/network-heavy services run in per-iteration scopes.
**When to use:** Any `BackgroundService` loop or single-run hosted job.
**Example:**
```csharp
using var scope = serviceScopeFactory.CreateScope();
var job = scope.ServiceProvider.GetRequiredService<ISearchJob>();
await job.RunAsync(stoppingToken);
```

### Anti-Patterns to Avoid
- **Fixed `Task.Delay` navigation timing:** use locator/actionability and URL assertions.
- **Unvalidated config:** do not allow worker start with missing URL/paths/filter settings.
- **Inline portal logic in `Program.cs`:** keep navigation and query composition in dedicated services.

## Don't Hand-Roll

| Problem | Don't Build | Use Instead | Why |
|---------|-------------|-------------|-----|
| Browser automation reliability | Custom HTTP+HTML scraping stack for JS workflow | Playwright .NET locators/events | TJGO flow includes JS handlers, hidden fields, and challenge token behavior. |
| Config lifecycle/validation | Manual JSON parsing | `IOptions<T>` + validation APIs | Existing repo pattern is mature and fail-fast friendly. |
| Worker lifecycle management | Custom host loop and DI container wiring | Worker template + hosted services | Official lifecycle, cancellation, and logging behavior is already solved. |

**Key insight:** Phase 11 risk is integration drift, not algorithm complexity; rely on framework primitives and reserve custom code for TJGO-specific mapping only.

## Common Pitfalls

### Pitfall 1: Missing Turnstile Token Timing
**What goes wrong:** Submit occurs before `g-recaptcha-response` is populated; query silently fails or returns challenge.
**Why it happens:** Token is filled asynchronously in JS callback.
**How to avoid:** Before submit, assert hidden token has non-empty value or detect challenge and retry path.
**Warning signs:** Frequent empty results for known-good queries; intermittent anti-bot pages.

### Pitfall 2: Assuming a Native Criminal Toggle Exists
**What goes wrong:** Planning expects checkbox/select that does not exist in `ConsultaPublicacao` form.
**Why it happens:** Requirement intent (criminal-only) is higher-level than current UI controls.
**How to avoid:** Define criminal filter strategy explicitly (query operators and/or `ArquivoTipo` restriction and/or post-result exclusion) and validate with known civil/criminal samples.
**Warning signs:** Civil-heavy result sets despite "criminal" mode enabled.

### Pitfall 3: Date Window Off-By-One or Locale Formatting
**What goes wrong:** Single-day queries include wrong date or no data.
**Why it happens:** Incorrect date format or mismatched start/end assignment.
**How to avoid:** Standardize `dd/MM/yyyy`, set both fields identically for single day, assert outbound form values and result metadata.
**Warning signs:** Same-day query yields broader range than expected.

### Pitfall 4: Encoding Corruption in Parsed Text
**What goes wrong:** Filter text/metadata has mojibake (`Publica��es`) and breaks matching.
**Why it happens:** Portal declares `ISO-8859-1`; parser assumes UTF-8 everywhere.
**How to avoid:** Preserve raw page behavior in browser automation; normalize extracted strings to UTF-8 at boundaries.
**Warning signs:** Broken accents in logs and rule checks.

## Code Examples

Verified patterns from official sources:

### Playwright Download-Safe Pattern
```csharp
var waitForDownloadTask = page.WaitForDownloadAsync();
await page.GetByText("Download file").ClickAsync();
var download = await waitForDownloadTask;
await download.SaveAsAsync(path);
```
Source: https://playwright.dev/dotnet/docs/downloads

### Options Validation on Startup
```csharp
builder.Services
    .AddOptions<BrazilExtractorOptions>()
    .Bind(builder.Configuration.GetSection("BrazilExtractor"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```
Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/options

### Scoped Service Inside BackgroundService
```csharp
while (!stoppingToken.IsCancellationRequested)
{
    using IServiceScope scope = serviceScopeFactory.CreateScope();
    var processor = scope.ServiceProvider.GetRequiredService<IScopedProcessingService>();
    await processor.DoWorkAsync(stoppingToken);
}
```
Source: https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service

## State of the Art

| Old Approach | Current Approach | When Changed | Impact |
|--------------|------------------|--------------|--------|
| Sleep-heavy Selenium scripts | Locator/actionability-first Playwright automation | Playwright mainstream adoption (ongoing) | Lower flake rate and clearer failure modes for dynamic forms. |
| Runtime-only config errors | Startup options validation (`ValidateOnStart`) | .NET options validation maturation | Fail fast before expensive browser run starts. |
| Ad-hoc worker loops with mixed lifetimes | Worker + scoped dependencies per unit of work | Standardized in hosted-service guidance | Avoids `DbContext`/service lifetime bugs in long runs. |

**Deprecated/outdated:**
- Treating `networkidle`/fixed waits as authoritative page-ready signal for portal automation.

## Open Questions

1. **Criminal filter implementation contract (EXTR-04)**
   - What we know: `ConsultaPublicacao` form exposes text/operator, process number, serventia, magistrado, `Tipo de Arquivo`, and date fields; no explicit "criminal" toggle is visible.
   - What's unclear: Which exact field/value combination reliably excludes civil publications.
   - Recommendation: Lock a deterministic strategy in planning (preferred order: typed field restriction if authoritative, else operator-based query profile + result validation fixture).

2. **Challenge handling policy for automated runs**
   - What we know: Page includes Cloudflare Turnstile and hidden token wiring.
   - What's unclear: Frequency and shape of challenge interruption in unattended runs.
   - Recommendation: Add a Phase 11 smoke harness with retry/backoff and artifact capture (screenshot + HTML on failure) before Phase 12 dependency work.

## Sources

### Primary (HIGH confidence)
- https://projudi.tjgo.jus.br/ConsultaPublicacao - live form structure, field IDs/names, hidden pagination fields, `ISO-8859-1`, and Turnstile token wiring.
- https://playwright.dev/dotnet/docs/intro - .NET Playwright install/runtime model.
- https://playwright.dev/dotnet/docs/navigations - navigation and explicit URL wait guidance.
- https://playwright.dev/dotnet/docs/actionability - locator/actionability behavior for stable interactions.
- https://playwright.dev/dotnet/docs/downloads - download lifecycle and `SaveAsAsync` requirement.
- https://learn.microsoft.com/en-us/dotnet/core/extensions/workers - worker template and hosted-service lifecycle.
- https://learn.microsoft.com/en-us/dotnet/core/extensions/options - options binding/validation patterns.
- https://learn.microsoft.com/en-us/dotnet/core/extensions/scoped-service - scoped dependency pattern within `BackgroundService`.
- https://api.nuget.org/v3-flatcontainer/microsoft.playwright/index.json - current package versions.

### Secondary (MEDIUM confidence)
- `.planning/research/STACK.md` - prior stack recommendation baseline for extractor milestone.
- `.planning/research/FEATURES.md` - milestone-level portal behavior and feature constraints.
- `.planning/research/PITFALLS.md` - known failure modes from earlier investigation.

## Metadata

**Confidence breakdown:**
- Standard stack: HIGH - official docs + current repository conventions agree.
- Architecture: HIGH - worker/options/scoped patterns are mature and directly applicable.
- Pitfalls: MEDIUM-HIGH - portal drift/challenge behavior is real but can vary over time.

**Research date:** 2026-03-02
**Valid until:** 2026-04-01 (revalidate TJGO selectors/challenge behavior before implementation start)
