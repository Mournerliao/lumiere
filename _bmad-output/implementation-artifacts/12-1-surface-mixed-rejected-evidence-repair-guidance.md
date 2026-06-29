# 12-1 Surface Mixed Rejected Evidence Repair Guidance

Date: 2026-06-29
Story: 12-1 Establish Standard HDR/SDR Validation Content and Scenarios
Related stories: 11-3 Validate Target-App Compatibility for Supported Output, 13-2 Harden Native Settings and Accessibility Semantics
Status: implemented; pending Windows validation

## Summary

Settings > Validation already distinguished a fully rejected validation workspace from a truly empty workspace. This slice tightens the mixed case where at least one valid output-validation artifact loads while another artifact or companion evidence file is rejected.

The loaded artifact remains reviewable through `Open latest evidence`, but the rejected file's first concrete load issue now also appears in the evidence-summary gap text. This keeps repair guidance visible in the same action-oriented summary area that lists missing public-gate coverage, rather than only in the compact loaded-artifact sentence.

## Code Changes

- `src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs`
  - Reused the existing `OutputValidationArtifactLoadIssue` typed model.
  - Added a single `DescribeFirstLoadIssue(...)` projection helper.
  - Included first rejected-file detail in mixed loaded-evidence `GapDetail`.
  - Preserved `LatestArtifactPath` and `CanOpenLatestArtifact` for the latest valid artifact.
  - Kept rejected evidence from changing build alignment, target-app version evidence, or runtime gate state.

## Tests Added Or Updated

- `PerfectHdrFidelityProjectionTests.ProjectValidationEvidenceSummary_WithSnapshotIssuesCallsOutIgnoredFiles`
  - Now asserts that mixed valid+ignored evidence includes the rejected file and concrete issue detail in `GapDetail`.
  - Confirms latest valid artifact remains openable.
- `SettingsPanelProjectionTests.Project_SnapshotValidationSummaryKeepsRejectedEvidenceRepairVisibleWhenValidArtifactLoads`
  - Covers the native Settings projection path.
  - Confirms rejected scenario-session repair guidance remains visible while the latest valid artifact can still be opened.
  - Confirms HDR10 remains at the non-ready build/fallback projection and the main panel fidelity claim remains converted, not HDR-preserved.

## Validation Status

NOT RUN on macOS:

- `.NET restore/build/test/format`
- WinUI runtime validation
- WGC/DXGI/HDR/WIC/JXR validation
- Windows manual target-app/viewer validation

Pending Windows validation:

- Run the focused Graphics test filter that includes `PerfectHdrFidelityProjectionTests` and `SettingsPanelProjectionTests`.
- Load a real Windows validation workspace containing one valid artifact plus one rejected draft/markdown companion and confirm Settings > Validation keeps the valid artifact openable while the rejected file's repair guidance stays visible.

## Release Gate Impact

This does not pass any Public perfect-HDR-fidelity release gate by itself. It reduces release-claim risk by making mixed rejected evidence visible and actionable. Public HDR10/HDR-preserved readiness remains blocked until real Windows target-aware display evidence, output profile contract evidence, target-app version evidence, target-app compatibility evidence, and workspace-local scenario evidence all pass.
