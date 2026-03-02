---
phase: 06-create-missing-verification-files
plan: 06
type: execute
wave: 1
depends_on: []
files_modified:
  - .planning/phases/02-generator-core/02-VERIFICATION.md
  - .planning/phases/04-reader-core/04-VERIFICATION.md
  - .planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md
autonomous: true
requirements:
  - "Process requirement — VERIFICATION.md for phases 2, 4, 5"
must_haves:
  truths:
    - "Phase 2 has a VERIFICATION.md with scored requirement coverage and evidence references."
    - "Phase 4 has a VERIFICATION.md with scored requirement coverage and evidence references."
    - "Phase 5 has a VERIFICATION.md with scored requirement coverage and evidence references."
    - "Each verification artifact includes explicit gap analysis so missing/partial work is visible."
  artifacts:
    - path: ".planning/phases/02-generator-core/02-VERIFICATION.md"
      provides: "Verification report for Generator Core outcomes"
      contains: "Phase overview, requirements coverage, plans executed, confidence score, gap analysis, evidence"
    - path: ".planning/phases/04-reader-core/04-VERIFICATION.md"
      provides: "Verification report for Reader Core outcomes"
      contains: "Phase overview, requirements coverage, plans executed, confidence score, gap analysis, evidence"
    - path: ".planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md"
      provides: "Verification report for Reader History UI & Polish outcomes"
      contains: "Phase overview, requirements coverage, plans executed, confidence score, gap analysis, evidence"
  key_links:
    - from: ".planning/phases/02-generator-core/02-VERIFICATION.md"
      to: ".planning/phases/02-generator-core/*-SUMMARY.md"
      via: "Evidence section references completed plan summaries and concrete file artifacts"
      pattern: "SUMMARY|Evidence|Confidence|Gap"
    - from: ".planning/phases/04-reader-core/04-VERIFICATION.md"
      to: ".planning/phases/04-reader-core/*-SUMMARY.md"
      via: "Coverage table maps Phase 4 requirements to implemented plans"
      pattern: "Requirements Coverage|Plans Executed|Gap"
    - from: ".planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md"
      to: ".planning/phases/05-reader-history-ui-polish/*-SUMMARY.md"
      via: "Verification score justified by cited outputs and behavior evidence"
      pattern: "Confidence|Evidence|Success Criteria"
---

<objective>
Create missing process verification artifacts for phases 2, 4, and 5 so workflow compliance is restored.

Purpose: Close the v1.0 audit process gap by producing standardized VERIFICATION.md reports for all completed historical phases.
Output: Three phase-level VERIFICATION.md files using the GSD template with scoring, gap analysis, and concrete evidence.
</objective>

<execution_context>
@/home/eduardo/.config/opencode/get-shit-done/workflows/execute-plan.md
@/home/eduardo/.config/opencode/get-shit-done/templates/summary.md
</execution_context>

<context>
@.planning/ROADMAP.md
@.planning/STATE.md
@.planning/REQUIREMENTS.md
@.planning/phases/02-generator-core/*-SUMMARY.md
@.planning/phases/04-reader-core/*-SUMMARY.md
@.planning/phases/05-reader-history-ui-polish/*-SUMMARY.md
</context>

<tasks>

<task type="auto">
  <name>Task 1: Create Phase 2 verification artifact (Generator Core)</name>
  <files>.planning/phases/02-generator-core/02-VERIFICATION.md</files>
  <action>Create a complete Phase 2 verification report using the project VERIFICATION.md template. Include: phase overview, requirement coverage for GEN-01..GEN-17, plans executed (02-01 through 02-05), confidence score with rationale, explicit gaps/risks (if any), and evidence references to summary files and implementation artifacts. Do not leave template sections empty; mark non-applicable items as "N/A" with reason.</action>
  <verify>Confirm file exists and contains required sections: `test -f .planning/phases/02-generator-core/02-VERIFICATION.md && grep -E "Phase overview|Requirements|Plans|Confidence|Gap|Evidence" .planning/phases/02-generator-core/02-VERIFICATION.md`</verify>
  <done>Phase 2 verification file is present, template-complete, and traceable to Phase 2 executed plans and artifacts.</done>
</task>

<task type="auto">
  <name>Task 2: Create Phase 4 verification artifact (Reader Core)</name>
  <files>.planning/phases/04-reader-core/04-VERIFICATION.md</files>
  <action>Create a complete Phase 4 verification report using the same template. Reflect current reality: 04-01, 04-02, 04-03 completed and 04-04 pending. Requirements coverage must clearly distinguish complete vs partial/unmet outcomes for RDR-01..RDR-20. Include confidence score, explicit gap analysis tied to pending work, and evidence references from Phase 4 summaries and shipped files.</action>
  <verify>Confirm file exists and includes both achieved and pending status details: `test -f .planning/phases/04-reader-core/04-VERIFICATION.md && grep -E "04-01|04-02|04-03|04-04|Confidence|Gap|Evidence" .planning/phases/04-reader-core/04-VERIFICATION.md`</verify>
  <done>Phase 4 verification file documents actual completion state, does not overstate completion, and includes evidence-backed scoring.</done>
</task>

<task type="auto">
  <name>Task 3: Create Phase 5 verification artifact (Reader History UI & Polish)</name>
  <files>.planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md</files>
  <action>Create a complete Phase 5 verification report with coverage for RDR-21..RDR-27 using completed plans 05-01, 05-02, 05-03. Include confidence scoring, gap analysis (if any residual concerns), and evidence with references to summaries and relevant files/features delivered in this phase.</action>
  <verify>Confirm file exists and references all Phase 5 plans and key sections: `test -f .planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md && grep -E "05-01|05-02|05-03|Confidence|Gap|Evidence" .planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md`</verify>
  <done>Phase 5 verification file is complete, evidence-backed, and aligned with declared phase success criteria.</done>
</task>

</tasks>

<verification>
Run all checks below:
1. `test -f .planning/phases/02-generator-core/02-VERIFICATION.md`
2. `test -f .planning/phases/04-reader-core/04-VERIFICATION.md`
3. `test -f .planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md`
4. `grep -E "Confidence|Gap|Evidence" .planning/phases/02-generator-core/02-VERIFICATION.md .planning/phases/04-reader-core/04-VERIFICATION.md .planning/phases/05-reader-history-ui-polish/05-VERIFICATION.md`
</verification>

<success_criteria>
1. All three missing VERIFICATION.md files exist in phases 2, 4, and 5.
2. Each file contains: overview, requirements coverage, plans executed, confidence score, gap analysis, and evidence references.
3. Phase 4 report accurately captures partial plan completion (04-04 pending) instead of presenting false full completion.
4. Process compliance gap from v1.0 audit is closed for verification artifacts.
</success_criteria>

<output>
After completion, create `.planning/phases/06-create-missing-verification-files/06-06-SUMMARY.md`
</output>
