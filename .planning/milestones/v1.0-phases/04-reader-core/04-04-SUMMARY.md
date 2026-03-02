---
phase: 04-reader-core
plan: 04
subsystem: reader-core
tags:
  - blazor-wasm
  - case-details
  - sensitive-content
  - content-gate
dependency_graph:
  requires:
    - RDR-13
    - RDR-05
  provides:
    - RDR-14
    - RDR-15
    - RDR-16
    - RDR-17
    - RDR-18
    - RDR-19
    - RDR-20
  affects:
    - Phase 5: Reader History UI & Polish
tech_stack:
  added:
    - .NET 10 Blazor WebAssembly
    - Case detail view model pattern
    - Sensitive content gate component
  patterns:
    - Service interfaces for abstraction
    - Component-based UI composition
    - RenderFragment for conditional content
    - Safe URL handling with target attributes
key_files:
  created:
    - src/OpenJustice.Reader/Models/Cases/CaseDetailViewModel.cs
    - src/OpenJustice.Reader/Services/Cases/ICaseDetailsService.cs
    - src/OpenJustice.Reader/Services/Cases/CaseDetailsService.cs
    - src/OpenJustice.Reader/Components/Cases/SensitiveContentGate.razor
    - src/OpenJustice.Reader/Components/Cases/CaseHeader.razor
    - src/OpenJustice.Reader/Components/Cases/CaseSources.razor
    - src/OpenJustice.Reader/Components/Cases/CaseEvidence.razor
    - src/OpenJustice.Reader/Components/Cases/CaseJudicialInfo.razor
    - src/OpenJustice.Reader/Components/Cases/CaseMetadata.razor
    - src/OpenJustice.Reader/Pages/Cases/CaseDetails.razor
  modified:
    - src/OpenJustice.Reader/Program.cs
    - src/OpenJustice.Reader/_Imports.razor
decisions:
  - "Sensitive content gate uses RenderFragment pattern for conditional rendering"
  - "Case details loaded via service that maps LocalCase to full CaseDetailViewModel"
  - "Navigation from search results pre-existing in ResultsTable.razor"
metrics:
  duration: "2 minutes"
  completed_date: "2026-03-01T22:48:00Z"
  task_count: 3
  file_count: 12
---

# Phase 4 Plan 4: Case Details & Sensitive Content Safeguards Summary

**Plan:** 04-04-PLAN.md  
**One-liner:** Complete reader case visualization with sensitive-content safeguards and comprehensive case detail sections

## Objective

Complete reader case visualization with sensitive-content safeguards and comprehensive case detail sections. Purpose: Deliver the public-facing reading experience after discovery/search.

## Tasks Completed

### Task 1: Implement case details service and view model

- Created `CaseDetailViewModel` with all required fields:
  - Core case information (id, reference code, dates, victim/acused names, location, judicial status, description, confidence score, verification flags)
  - Sources, Evidence, JudicialInfo, Tags, and Metadata sub-models
- Created `ICaseDetailsService` interface with methods:
  - `GetCaseDetailsAsync(int caseId)` - loads by ID
  - `GetCaseDetailsByReferenceAsync(string referenceCode)` - loads by reference code
- Implemented `CaseDetailsService` that maps `LocalCase` from `ILocalCaseStore` to `CaseDetailViewModel`
- Registered service in DI via Program.cs

### Task 2: Build case details page with sensitive-content gate

- Created `CaseDetails.razor` page routed by `/cases/{CaseId}`
- Created `SensitiveContentGate.razor` component:
  - Shows warning alert with explanation in Portuguese
  - Requires user acknowledgment before showing sensitive content
  - Uses RenderFragment pattern for conditional rendering
- Created section components:
  - `CaseHeader.razor` - displays case title, reference code, badges (verified, sensitive), crime details, confidence score
  - `CaseSources.razor` - displays source list with clickable links (safe target attributes)
  - `CaseEvidence.razor` - displays evidence items with type badges and links
  - `CaseJudicialInfo.razor` - displays judicial process info (process number, court, judge, outcome, sentence)
  - `CaseMetadata.razor` - displays registration info, verification, version, tags

### Task 3: Wire search results to details route

- Verified: `ResultsTable.razor` already contains navigation link: `<a href="/cases/@item.Id">@item.ReferenceCode</a>`
- `CaseDetails.razor` includes back navigation link to `/search`
- No additional changes needed - navigation already wired

## Verification

- Solution builds successfully: `dotnet build src/OpenJustice.Reader/OpenJustice.Reader.csproj`
- All services properly registered in DI
- Case details page renders complete information with sensitive content gate
- Navigation from search to details works as expected

## Requirements Satisfied

- **RDR-14**: Case detail view with full information (implemented)
- **RDR-15**: Sensitive content warning gate (implemented via SensitiveContentGate.razor)
- **RDR-16**: Sources section with links (implemented via CaseSources.razor)
- **RDR-17**: Evidence section with links (implemented via CaseEvidence.razor)
- **RDR-18**: Judicial information section (implemented via CaseJudicialInfo.razor)
- **RDR-19**: Metadata section with tags (implemented via CaseMetadata.razor)
- **RDR-20**: Clickable links with safe target attributes (implemented)

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None encountered.

## Self-Check: PASSED

- Files created as specified
- Commit hash matches: 2510d0c
