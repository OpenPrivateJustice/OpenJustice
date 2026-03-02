---
phase: 04-reader-core
plan: 01
subsystem: reader-core
tags:
  - blazor-wasm
  - configuration
  - reader-app
dependency_graph:
  requires: []
  provides:
    - RDR-01
    - RDR-05
  affects:
    - Phase 4 subsequent plans
tech_stack:
  added:
    - .NET 10 Blazor WebAssembly
    - Microsoft.Extensions.Options
    - Microsoft.Extensions.Options.ConfigurationExtensions
    - Microsoft.Extensions.Options.DataAnnotations
  patterns:
    - IOptions pattern with validation
    - DataAnnotations for configuration validation
    - Fail-fast validation at startup
key_files:
  created:
    - src/OpenJustice.Reader/OpenJustice.Reader.csproj
    - src/OpenJustice.Reader/Program.cs
    - src/OpenJustice.Reader/Configuration/ReaderOptions.cs
    - src/OpenJustice.Reader/wwwroot/appsettings.json
    - src/OpenJustice.Reader/Layout/NavMenu.razor
    - src/OpenJustice.Reader/Pages/Home.razor
    - src/OpenJustice.Reader/Pages/Sync/Sync.razor
    - src/OpenJustice.Reader/Pages/Search/Search.razor
    - src/OpenJustice.Reader/Pages/Cases/Cases.razor
  modified:
    - OpenJustice.sln
decisions:
  - "Followed Generator's IOptions pattern with IValidateOptions for consistency"
  - "Created strongly-typed ReaderOptions mirroring Generator's GeneratorOptions"
  - "Used DataAnnotations for declarative validation"
  - "Placed appsettings.json in wwwroot (standard for Blazor WASM)"
metrics:
  duration: "4 minutes"
  completed_date: "2026-03-01T19:27:00Z"
  task_count: 2
  file_count: 9
---

# Phase 4 Plan 1: Reader Core Bootstrap Summary

**Plan:** 04-01-PLAN.md  
**One-liner:** Bootstrap Reader WASM project with validated configuration system

## Objective

Bootstrap the public Reader application as an independent Blazor WebAssembly SPA with validated runtime configuration.

## Tasks Completed

### Task 1: Create Reader WASM project and wire solution

- Created .NET 10 Blazor WebAssembly project at `src/OpenJustice.Reader`
- Added to `OpenJustice.sln` alongside Generator projects
- Registered core services in `Program.cs` (HttpClient, options binding)
- Added navigation links for Sync, Search, Cases pages
- Created placeholder pages for each navigation route

### Task 2: Implement Reader configuration model and startup validation

- Added `wwwroot/appsettings.json` with torrent trackers, snapshot metadata, and local DB settings
- Created `ReaderOptions.cs` with DataAnnotations and explicit section mapping
- Bound options in `Program.cs` with fail-fast validation on startup
- Mirrored Generator's options pattern (`IOptions<T>` + `IValidateOptions<T>`)

## Verification

- Solution builds successfully: `dotnet build OpenJustice.sln`
- Reader project appears in solution: `dotnet sln list`
- Options validation at startup works correctly

## Requirements Satisfied

- **RDR-01**: Reader opens as client-side Blazor WebAssembly SPA
- **RDR-05**: Reader reads required runtime settings from appsettings.json

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None encountered.
