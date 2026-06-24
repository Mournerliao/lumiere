# 12-1 Carry Next Run Scope Into Validation Drafts

Date: 2026-06-24
Status: done

## Summary

Generated output-validation drafts now carry the same missing-run scope that Settings > Validation shows in the loaded-evidence summary. This keeps the native review surface and the durable JSON draft aligned while validators prepare real Windows manual sessions.

## Implementation Evidence

- `OutputValidationRunPlanner` centralizes the missing topology, entry-point, output-target, and HDR10 viewer-target planning rules.
- `PerfectHdrFidelityProjection` now consumes the shared planner instead of owning a private duplicate of those rules.
- `FileOutputValidationArtifactSource.CreateDraft(...)` computes the missing run scope from currently loaded local artifacts before creating a draft.
- `OutputValidationDraftFactory` adds the suggested next Windows run, suggested topology bucket, and suggested entry point into draft placeholder text without changing the JSON schema or counting the hints as evidence.

## Validation

- `OutputValidationDraftFactoryTests` covers seed-provided next-run hints.
- `OutputValidationArtifactSourceTests` covers loaded-artifact-derived next-run hints in generated drafts.
- Existing `PerfectHdrFidelityProjectionTests` continue to cover the Settings > Validation summary.

## Remaining Work

- This is still a planning accelerator. Public release remains blocked until validators replace draft placeholders with real current-build Windows observations and reload completed artifacts.
