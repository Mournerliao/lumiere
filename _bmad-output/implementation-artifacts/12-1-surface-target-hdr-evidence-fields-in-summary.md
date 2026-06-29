# 12-1 Surface Target HDR Evidence Fields In Summary

Date: 2026-06-29
Stories: 10-3, 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice keeps the Settings > Validation loaded-evidence summary aligned with the stricter target-aware HDR evidence gate. When loaded artifacts exist but none of them has complete `targetHdrEvidence`, the summary now names the missing target-aware HDR fields directly instead of only showing broader topology, viewer, or checklist gaps.

The immediate release-risk fix is for missing or placeholder target color-space evidence: a validator can now see `target-aware HDR evidence color space` in the loaded-evidence gap text before trying to treat the artifact as current release evidence.

## Code Changes

- `PerfectHdrFidelityProjection.CreateGapDetail(...)` now collects incomplete target-aware HDR evidence fields when no loaded artifact has complete target-aware evidence.
- The summary suppresses this gap once at least one loaded artifact has complete target-aware HDR evidence, matching the runtime gate's "any complete participating artifact" semantics.
- No WinUI control shape changed; this is a projection/copy improvement for the existing compact native evidence summary.

## Tests Written

- `PerfectHdrFidelityProjectionTests.ProjectValidationEvidenceSummary_CallsOutIncompleteTargetHdrColorSpace`
- `SettingsPanelProjectionTests.Project_SnapshotValidationSummarySurfacesIncompleteTargetHdrColorSpace`

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- WinUI settings panel rendering
- Keyboard/screen-reader validation
- Windows manual HDR/display evidence loading
- HDR10 JXR output/viewer validation

## Release-Gate Impact

This does not complete any public gate. It makes incomplete target-aware HDR evidence more visible in the validation workflow so placeholder or partial artifacts are less likely to be mistaken for public-release proof.

Public perfect-HDR-fidelity remains blocked on real Windows manual evidence for target-aware HDR state, observed target color space, output profile contract, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility, and long-run resource trends.
