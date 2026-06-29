# 12-1 Surface Manual Session Fields In Validation Summary

Date: 2026-06-29
Stories: 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice keeps Settings > Validation aligned with the stricter HDR10 JXR runtime gate for incomplete manual-session evidence. Loaded artifacts that are structurally readable but still miss manual fields such as `evidence paths` now surface those missing fields directly in the loaded-evidence summary.

The immediate release-risk case is a JSON artifact whose viewer rows and format record look promising, but whose `evidencePaths` list is empty. The summary now calls out `Manual validation session evidence is incomplete: evidence paths.` so validators do not need to infer that gap only from HDR10 gate fallback behavior or raw JSON inspection.

## Code Changes

- `PerfectHdrFidelityProjection.ProjectValidationEvidenceSummary(...)` now reuses `OutputValidationSessionArtifact.GetMissingManualEvidenceFields()` for loaded-evidence gap detail.
- The summary excludes target-aware HDR fields and target-app version fields from this new manual-session line because those already have dedicated summary rows/blockers.
- No runtime output execution path changed. HDR10 remains blocked by the existing gate until complete Windows manual evidence exists.

## Tests Written

- `PerfectHdrFidelityProjectionTests.ProjectValidationEvidenceSummary_CallsOutMissingManualSessionEvidencePaths`
- `SettingsPanelProjectionTests.Project_SnapshotValidationSummarySurfacesMissingManualSessionEvidencePaths`

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- WinUI Settings > Validation rendering
- Windows HDR display validation
- HDR10 JXR file output
- Workspace-local scenario evidence loading on Windows
- Keyboard, screen reader, high contrast, and DPI validation

## Release-Gate Impact

This does not complete any public gate. It makes incomplete manual-session evidence more visible in the validation review surface while keeping Public perfect-HDR-fidelity blocked on real Windows evidence.

Public perfect-HDR-fidelity remains blocked on target-aware HDR display evidence, observed target color space, workspace-local scenario notes, output profile contract, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility, and long-run resource trends.
