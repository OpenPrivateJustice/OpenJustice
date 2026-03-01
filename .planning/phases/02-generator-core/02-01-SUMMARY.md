---
phase: 02-generator-core
plan: 01
subsystem: generator-api
tags: [api, cases, validation, reference-code]
dependency_graph:
  requires:
    - 01-01 database foundation
  provides:
    - REST endpoints for case CRUD
    - Validation services
    - Reference code generation
  affects:
    - Database persistence layer
tech_stack:
  added:
    - FluentValidation 11.3.1
    - ASP.NET Core 10 (via SDK)
    - xUnit for testing
    - Moq for mocking
    - FluentAssertions
  patterns:
    - FluentValidation for DTO validation
    - In-memory DbContext for tests
    - Repository pattern via CaseWorkflowService
key_files:
  created:
    - src/AtrocidadesRSS.Generator/Controllers/CasesController.cs
    - src/AtrocidadesRSS.Generator/Contracts/Cases/CreateCaseRequest.cs
    - src/AtrocidadesRSS.Generator/Contracts/Cases/UpdateCaseRequest.cs
    - src/AtrocidadesRSS.Generator/Validation/Cases/CreateCaseRequestValidator.cs
    - src/AtrocidadesRSS.Generator/Validation/Cases/UpdateCaseRequestValidator.cs
    - src/AtrocidadesRSS.Generator/Services/Cases/CaseReferenceCodeGenerator.cs
    - src/AtrocidadesRSS.Generator/Services/Cases/CaseWorkflowService.cs
    - tests/AtrocidadesRSS.Generator.Tests/Cases/CasesControllerTests.cs
    - tests/AtrocidadesRSS.Generator.Tests/Cases/CaseWorkflowServiceTests.cs
  modified:
    - src/AtrocidadesRSS.Generator/AtrocidadesRSS.Generator.csproj
    - src/AtrocidadesRSS.Generator/ServiceCollectionExtensions.cs
    - AtrocidadesRSS.sln
decisions:
  - Use FluentValidation for declarative validation rules
  - Reference code generated at create time only, never regenerated on edit
  - In-memory database for unit tests to avoid external dependencies
metrics:
  duration: 11 minutes
  completed: 2026-03-01T18:25:52Z
  files_created: 9
  tests_passed: 19
---

# Phase 02 Plan 01: Case Management API Summary

## Objective

Deliver the core case management API for the private generator with strong validation and deterministic reference-code generation.

## What Was Built

### REST Endpoints (CasesController.cs)
- `POST /api/cases` - Create new case with auto-generated ATRO reference code
- `PUT /api/cases/{id}` - Update existing case (reference code remains unchanged)
- `GET /api/cases/{id}` - Retrieve case by ID

### DTO Contracts
- `CreateCaseRequest` - Request payload for case creation
- `UpdateCaseRequest` - Request payload for case updates

### Validation (FluentValidation)
- Required field validation (CrimeTypeId, CaseTypeId, JudicialStatusId)
- Confidence scores must be 0-100
- Date consistency (CrimeDate <= ReportDate, no future dates)
- String length constraints
- Gender format validation (M/F/Other/Unknown)

### Services
- `CaseReferenceCodeGenerator` - Generates unique ATRO-YYYY-NNNN codes
- `CaseWorkflowService` - Orchestrates create/update with FK validation

### Tests (19 passing)
- 6 CasesController tests (create, update, get by id, error handling)
- 13 CaseWorkflowService tests (FK validation, reference code generation, sequential numbering)

## Key Truths Achieved

- ✅ Curator can create a case through REST API with required fields enforced
- ✅ Curator can edit an existing case with consistency validation
- ✅ Every newly created case receives a unique ATRO-YYYY-NNNN reference code

## Verification

```
dotnet test tests/AtrocidadesRSS.Generator.Tests
Passed! - Failed: 0, Passed: 19, Skipped: 0, Total: 19
```

## Deviations from Plan

None - plan executed exactly as written.

## Auth Gates

None - no authentication required for this API layer.

## Self-Check

- ✅ Files exist: All key files created
- ✅ Tests pass: 19/19
- ✅ Reference code format: ATRO-YYYY-NNNN (verified in tests)
- ✅ Reference code stability: Unchanged on edit (verified in tests)
- ✅ FK validation: Blocks invalid CrimeTypeId, CaseTypeId, JudicialStatusId
