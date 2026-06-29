# 11-3 Surface Target HDR Fields In HDR10 Viewer Gate

Date: 2026-06-29
Stories: 10-3, 11-3, 12-1, 13-2
Status: implemented; NOT RUN on macOS; pending Windows validation

## Summary

This slice keeps the HDR10 JXR viewer gate blocker text aligned with the stricter target-aware HDR evidence contract. When folder-output HDR10 validation artifacts are present but none has complete target-aware HDR evidence, the viewer gate now names the missing target-aware fields instead of only reporting a generic target-aware evidence blocker.

The immediate release-risk case is placeholder target color-space evidence. If the loaded artifact still contains `REPLACE_WITH_OBSERVED_TARGET_COLOR_SPACE`, the HDR10 JXR gate blocker now mentions `color space` directly.

## Code Changes

- `Hdr10JxrViewerValidationEvidence.FromArtifacts(...)` now collects missing target-aware HDR evidence fields when all participating folder-output artifacts are incomplete.
- Existing complete-evidence semantics are preserved: any participating artifact with complete target-aware HDR evidence satisfies that part of the viewer gate.
- No output execution path changed. This only improves blocker specificity for the already-blocked HDR10 JXR validation gate.

## Tests Written

- `Hdr10JxrViewerValidationEvidenceTests.FromArtifacts_BlocksWithSpecificTargetAwareHdrFieldWhenColorSpaceIsMissing`

## Validation Status

NOT RUN in this macOS environment:

- .NET restore/build/test/format
- HDR10 JXR file output
- WIC/JPEG XR encoding
- Windows HDR display validation
- Target-app viewer validation

## Release-Gate Impact

This does not complete any public gate. It makes the HDR10 JXR gate explain exactly which target-aware evidence field still blocks release proof.

Public perfect-HDR-fidelity remains blocked on real Windows manual evidence for target-aware HDR state, observed target color space, output profile contract, target-app versions, viewer-recognized HDR10 metadata, mixed HDR/SDR topology, DPI/accessibility, and long-run resource trends.
