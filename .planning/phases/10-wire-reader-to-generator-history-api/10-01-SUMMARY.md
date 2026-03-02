---
phase: 10-wire-reader-to-generator-history-api
plan: 01
subsystem: reader
tags: [reader, api, history, authentication, integration]
dependency_graph:
  requires:
    - RDR-22
  provides:
    - GeneratorHistoryApiClient
    - IGeneratorHistoryApiClient
    - GeneratorHistoryApiOptions
  affects:
    - ReaderOptions
    - Reader history UI
tech_stack:
  added:
    - Microsoft.Extensions.Http (for IHttpClientFactory)
  patterns:
    - IOptions pattern for configuration
    - IValidateOptions for fail-fast validation
    - HttpClient factory for typed HTTP clients
    - Bearer token authentication
    - Explicit 401 handling with redirect to login URL
key_files:
  created:
    - src/AtrocidadesRSS.Reader/Services/Cases/IGeneratorHistoryApiClient.cs
    - src/AtrocidadesRSS.Reader/Services/Cases/GeneratorHistoryApiClient.cs
  modified:
    - src/AtrocidadesRSS.Reader/Configuration/ReaderOptions.cs
    - src/AtrocidadesRSS.Reader/wwwroot/appsettings.json
    - src/AtrocidadesRSS.Reader/Program.cs
    - src/AtrocidadesRSS.Reader/AtrocidadesRSS.Reader.csproj
decisions:
  - "Used IHttpClientFactory for proper connection pooling and lifetime management"
  - "Created dedicated exception type (GeneratorHistoryApiUnauthorizedException) for explicit 401 handling"
  - "Preserved ChangeConfidence and timestamps when mapping Generator DTOs to Reader view models"
metrics:
  duration: 4 min
  completed_date: 2026-03-02
---

# Phase 10 Plan 1: Wire Reader to Generator History API Summary

## Objective

Create the Reader-side API integration foundation so history data can be fetched live from Generator with authenticated requests and deterministic unauthorized handling.

## Completed Tasks

| Task | Name | Commit | Files |
|------|------|--------|-------|
| 1 | Add Generator history API/auth configuration with fail-fast validation | 0f4cd29 | ReaderOptions.cs, appsettings.json |
| 2 | Implement authenticated Generator history API client and register it in DI | 0f4cd29 | IGeneratorHistoryApiClient.cs, GeneratorHistoryApiClient.cs, Program.cs |

## Implementation Details

### Task 1: Configuration

Added `GeneratorHistoryApiOptions` class to `ReaderOptions.cs` with:
- `BaseUrl` - Generator API base URL (required)
- `AccessToken` - Bearer token for authentication (required)
- `LoginUrl` - Redirect URL for 401 Unauthorized (required)
- `RequestTimeoutSeconds` - Configurable timeout (1-300 seconds, default: 30)

Updated `ReaderOptionsValidator` to validate:
- GeneratorHistoryApi is not null
- BaseUrl is not empty
- AccessToken is not empty
- LoginUrl is not empty

Added placeholder configuration in `appsettings.json`.

### Task 2: API Client

Created `IGeneratorHistoryApiClient` interface with methods:
- `GetCaseHistoryAsync(caseId)` - Fetches all field history for a case
- `GetFieldHistoryAsync(caseId, fieldName)` - Fetches history for specific field

Implemented `GeneratorHistoryApiClient`:
- Uses `IHttpClientFactory` for proper connection pooling
- Always adds `Authorization: Bearer {AccessToken}` header
- Handles 404 as empty list (not found)
- Handles 401 explicitly by throwing `GeneratorHistoryApiUnauthorizedException` with login URL
- Maps Generator DTOs to `CaseFieldHistoryViewModel` preserving `ChangedAt`, field values, and `ChangeConfidence`

Registered in DI:
- Added `AddHttpClient(nameof(GeneratorHistoryApiClient))`
- Added `AddSingleton<IGeneratorHistoryApiClient, GeneratorHistoryApiClient>()`

## Truths Confirmed

- [x] Reader sends authenticated requests to Generator history endpoints
- [x] If Generator responds 401, Reader executes explicit auth recovery path (redirect to configured LoginUrl)
- [x] History endpoint JSON is mapped into Reader history view models without losing ChangedAt, field values, or ChangeConfidence

## Artifacts

| Path | Provides |
|------|----------|
| src/AtrocidadesRSS.Reader/Configuration/ReaderOptions.cs | Generator history API + auth settings with startup validation |
| src/AtrocidadesRSS.Reader/Services/Cases/IGeneratorHistoryApiClient.cs | Contract for authenticated history API calls |
| src/AtrocidadesRSS.Reader/Services/Cases/GeneratorHistoryApiClient.cs | HTTP implementation with Authorization header and 401 handling |
| src/AtrocidadesRSS.Reader/Program.cs | DI wiring for GeneratorHistoryApiClient |

## Verification

1. Build succeeded: `dotnet build src/AtrocidadesRSS.Reader/AtrocidadesRSS.Reader.csproj` ✓
2. Options validation includes GeneratorHistoryApi required fields ✓
3. GeneratorHistoryApiClient contains Authorization header and explicit 401 redirect ✓

## Self-Check: PASSED

- Files exist: ✓
- Commit exists: ✓
- All success criteria met: ✓
