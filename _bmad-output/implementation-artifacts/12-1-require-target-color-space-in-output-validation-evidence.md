# 12-1 Require Target Color Space In Output Validation Evidence

Date: 2026-06-29
Stories: 10-3, 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice tightens the output-validation evidence workflow so generated/sample artifacts cannot imply target-aware HDR evidence before a Windows validator records the observed target color space.

The runtime manual-evidence completeness check now treats missing or placeholder `targetHdrEvidence.colorSpace` as incomplete target-aware HDR evidence. Incomplete color-space evidence removes the loaded format contract and downgrades viewer statuses to limited evidence, so it cannot unlock HDR10 JXR readiness or public HDR-preserved claims.

## Code Changes

- `TargetAwareHdrValidationEvidence.GetMissingFields()` now includes `color space`.
- Both output-validation schema-v4 sample JSON files now keep manual-observation values as explicit placeholders:
  - top-level observed Windows HDR state
  - DPI scale
  - entry point
  - target-aware HDR state
  - target-aware color space
  - target-aware detail
- The durable harness template now uses workspace-local `evidence\REPLACE_WITH_SCENARIO_SESSION_RECORD.md` instead of a repo-relative path.

## Tests Written

- `OutputValidationSessionArtifactTests.ApplyTo_TreatsMissingTargetAwareColorSpaceAsIncompleteManualSession`
- `OutputValidationDocumentationTests.OutputValidationSessionTemplate_ParsesAsSchemaVersionFourAndDoesNotPassClaims` now asserts that the harness sample keeps workspace-local evidence paths and target-aware HDR placeholders.

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- WinUI app launch
- WGC / DXGI / WIC / JPEG XR output execution
- HDR display validation
- Target-app viewer validation

## Release-Gate Impact

This does not complete any public gate. It reduces the risk that a copied sample, generated draft, or partially edited artifact can be mistaken for target-aware display evidence.

Public perfect-HDR-fidelity remains blocked on real Windows manual evidence for target-aware HDR state, target color space, output profile contract, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility, and long-run resource trends.
