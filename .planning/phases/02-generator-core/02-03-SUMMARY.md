---
phase: 02-generator-core
plan: 03
subsystem: generator-web-ui
tags: [blazor, spa, case-management, api-client]
dependency_graph:
  requires:
    - 02-01 case-management-api
  provides:
    - Blazor web UI for case CRUD
    - Typed API client
    - Form validation
  affects:
    - OpenJustice.Generator API
    - Database via API
tech_stack:
  added:
    - Blazor Web App (.NET 10)
    - Moq 4.20.72
    - FluentAssertions 6.12.2
  patterns:
    - EditForm with DataAnnotations validation
    - Typed HttpClient for API calls
    - Navigation between list/edit/create pages
key_files:
  created:
    - src/OpenJustice.Generator.Web/OpenJustice.Generator.Web.csproj
    - src/OpenJustice.Generator.Web/Program.cs
    - src/OpenJustice.Generator.Web/Pages/Cases/CasesList.razor
    - src/OpenJustice.Generator.Web/Pages/Cases/CaseCreate.razor
    - src/OpenJustice.Generator.Web/Pages/Cases/CaseEdit.razor
    - src/OpenJustice.Generator.Web/Models/Cases/CaseFormModel.cs
    - src/OpenJustice.Generator.Web/Services/GeneratorApiClient.cs
    - tests/OpenJustice.Generator.Web.Tests/Cases/CaseFormTests.cs
  modified:
    - src/OpenJustice.Generator/Controllers/CasesController.cs
    - src/OpenJustice.Generator/Services/Cases/CaseWorkflowService.cs
    - OpenJustice.sln
decisions:
  - Used static lookup data for dropdowns instead of API calls for simplicity
  - Blazor Server rendering mode for full .NET integration
  - CaseFormModel with validation attributes mirrors API contracts
  - API client returns tuple (Result, Error) for error handling
metrics:
  duration: 15 minutes
  completed: 2026-03-01T18:52:45Z
  files_created: 7
  tests_passed: 5
---

# Phase 02 Plan 03: Blazor Web UI for Case Management Summary

## Objective

Deliver the private Blazor web UI for manual case insertion and editing on top of generator API, giving curators an operational interface to manage cases without direct DB interaction.

## What Was Built

### Blazor Web Project (OpenJustice.Generator.Web)
- New Blazor Web App project using .NET 10
- Integrated with existing Generator API via HttpClient
- Bootstrap 5 styling for responsive UI
- Navigation menu with "Casos" link

### Pages
- **CasesList.razor**: Table view of all cases with reference code, crime type, judicial status, location, last update, edit button
- **CaseCreate.razor**: Full form with EditForm for creating new cases with sections for crime details, victim info, accused info, judicial info, classification
- **CaseEdit.razor**: Same form structure for editing existing cases, pre-populated from API

### Form Model
- **CaseFormModel.cs**: Shared form model with DataAnnotations validation (Required, Range, StringLength)
- Converts to CreateCaseRequest and UpdateCaseRequest for API calls
- Maps from Case entity for edit scenarios

### API Client
- **GeneratorApiClient.cs**: Typed HTTP client with methods:
  - GetAllCasesAsync()
  - GetCaseByIdAsync(id)
  - CreateCaseAsync(request)
  - UpdateCaseAsync(id, request)
- Handles error responses and returns tuples for error handling

### API Extension
- Added GET /api/cases endpoint to CasesController
- Added GetAllCasesAsync to CaseWorkflowService

### Tests
- **CaseFormTests.cs**: 5 tests covering:
  - ToCreateRequest maps all fields correctly
  - ToUpdateRequest maps all fields correctly
  - FromCase populates form model from entity
  - FromCase handles null fields
  - Default values are set correctly

## Key Truths Achieved

- ✅ Curator can create new cases from the web SPA and submit to API
- ✅ Curator can edit existing cases from the web SPA and persist updates
- ✅ Form validation feedback is visible before invalid data is submitted

## Verification

```
dotnet test tests/OpenJustice.Generator.Web.Tests --filter "FullyQualifiedName~CaseFormTests"
Passed! - Failed: 0, Passed: 5, Skipped: 0, Total: 5
```

All tests pass, build succeeds.

## Deviations from Plan

**1. [Rule 2 - Feature] Added GetAll endpoint to API**
- **Found during:** Task 1 - needed case list
- **Issue:** API didn't have list endpoint
- **Fix:** Added GetAllCasesAsync to CaseWorkflowService and GET /api/cases to CasesController
- **Files modified:** CasesController.cs, CaseWorkflowService.cs

**2. [Naming Convention] Project naming difference**
- **Plan used:** Atrocidades.Generator.Web
- **Actual:** OpenJustice.Generator.Web (consistent with solution naming)
- **No impact on functionality**

## Auth Gates

None - no authentication required for this internal tool.

## Self-Check

- ✅ Files exist: All key files created
- ✅ Tests pass: 5/5
- ✅ Build succeeds: 0 errors
- ✅ Navigation works: /cases, /cases/create, /cases/edit/{id}
- ✅ API client integrated with OnValidSubmit handlers
