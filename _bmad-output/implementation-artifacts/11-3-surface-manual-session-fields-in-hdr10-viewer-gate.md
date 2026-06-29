# 11-3 Surface Manual Session Fields In HDR10 Viewer Gate

Date: 2026-06-29
Stories: 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice makes the HDR10 JXR viewer gate name incomplete manual-session fields when loaded folder-output artifacts cannot count as release evidence. The immediate release-risk case is missing scenario evidence links: an artifact can be structurally readable, but if `evidencePaths` is empty, the gate now reports `evidence paths` as a blocker instead of only surfacing broader format-contract or viewer-validation failures.

## Code Changes

- `OutputValidationSessionArtifact.GetMissingManualEvidenceFields()` is now reusable by the HDR10 viewer gate instead of remaining private to profile application.
- `Hdr10JxrViewerValidationEvidence.FromArtifacts(...)` now adds a specific manual-session blocker for missing fields such as `evidence paths`.
- Target-aware HDR fields and target-app version fields keep their dedicated blockers, so the viewer gate avoids duplicating those already-specific release blockers.

## Tests Written

- `Hdr10JxrViewerValidationEvidenceTests.FromArtifacts_BlocksWithSpecificManualSessionFieldWhenEvidencePathsAreMissing`

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- HDR10 JXR file output
- WIC/JPEG XR encoding
- Windows HDR display validation
- Target-app viewer validation
- Workspace-local scenario evidence loading on Windows

## Release-Gate Impact

This does not complete any public gate. It keeps HDR10 JXR blocked when manual session evidence is incomplete and makes the blocker actionable for the next Windows validation run.

Public perfect-HDR-fidelity remains blocked on real Windows manual evidence for target-aware HDR state, observed target color space, workspace-local scenario notes, output profile contract, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility, and long-run resource trends.
