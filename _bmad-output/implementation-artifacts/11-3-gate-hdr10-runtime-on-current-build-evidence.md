# 11-3 Gate HDR10 Runtime On Current-Build Evidence

Date: 2026-06-22
Stories touched: `11-3`, `12-1`

## Why this slice

The previous slice made current-build alignment visible in Settings > Validation, but the actual HDR10 runtime gate could still treat complete manual evidence as executable even when that evidence belonged to a different build.

That was still too weak for `Public perfect-HDR-fidelity`.

For the first supported HDR-preserved output path, stale evidence must not only look stale. It must also fail to unlock the runtime gate.

## What changed

1. Added a small `ValidationArtifactBuildAlignment` seam in `Lumiere.Graphics.Output` so current-build matching can be evaluated in output/runtime code without depending on App-layer projections.
2. Extended `Hdr10JxrViewerValidationEvidence` with current-build alignment awareness:
   - no current-build context: keep existing behavior for generic/tests-only callers
   - current-build context present: complete HDR10 evidence must also match the current build token
3. Updated `OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(...)` so the HDR10 JXR runtime gate now accepts an optional current build version.
4. Wired `MainWindow.ResolveOutputCapabilities()` to pass `aboutInfoProvider.Version`, making the real app path current-build-aware.
5. Kept the gate strict:
   - matched build + complete manual evidence -> HDR10 may become executable
   - stale build -> HDR10 stays `PendingValidation`
   - unknown build alignment -> HDR10 also stays blocked when the app asked for current-build validation
6. Preserved seam locality:
   - projection/UI still owns user-facing wording
   - output/runtime seam owns the executable/not-executable decision

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~Hdr10JxrViewerValidationEvidenceTests|FullyQualifiedName~OutputPolicyTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests"`
- `dotnet build src/Lumiere.App/Lumiere.App.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false /m:1`

Result:

- Build passed.
- Targeted tests passed: `135 passed`.

## Notes

- This still does not complete public release readiness. Real Windows current-build manual evidence is still missing for the first publicly supportable HDR-preserved path.
- The next best move remains recording fresh Windows manual HDR10 evidence for the actual tested build, not adding more local helper workflow.
