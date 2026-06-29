# 11-3 Require All HDR10 Viewer Evidence Current-Build Alignment

Date: 2026-06-29
Stories touched: `11-3`, `11-2`, `12-1`

## Why this slice

The existing HDR10 JXR current-build gate checked loaded evidence freshness through the latest artifact. That was useful for the review summary, but it was too permissive for the runtime gate when HDR10 viewer evidence is aggregated from multiple artifacts.

For the Public perfect-HDR-fidelity target, one current-build artifact must not mask stale evidence for another named viewer. Every app-loaded artifact that participates in the HDR10 JXR folder-output runtime gate has to align with the current build before the first HDR-preserved output path can become executable.

## What changed

1. Added `ValidationArtifactBuildAlignment.EvaluateAll(...)` in `Lumiere.Graphics.Output`.
   - no artifacts: remains `NotChecked`
   - missing current build token: `Unknown`
   - any participating artifact without a comparable commit token: `Unknown`
   - any participating artifact with a different commit token: `StaleForCurrentBuild`
   - all participating artifacts matching the current build token: `MatchedCurrentBuild`
2. Updated `Hdr10JxrViewerValidationEvidence.FromArtifacts(...)` to use the all-artifact alignment path for the HDR10 JXR folder-output runtime gate.
3. Added targeted test coverage for a mixed evidence set where most named viewers match the current build but one viewer artifact is stale.

## Validation

Written but NOT RUN on macOS:

- `Hdr10JxrViewerValidationEvidenceTests.FromArtifacts_WithCurrentBuildVersion_BlocksWhenAnyParticipatingArtifactIsStale`

Reason:

- The current workspace is macOS. Project validation for `.NET 10`, WinUI 3, WGC, DXGI, WIC/JPEG XR, and HDR behavior is Windows-only per `AGENTS.md`.

Recommended Windows validation commands:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --filter "Hdr10JxrViewerValidationEvidenceTests|OutputPolicyTests|OutputValidationSessionArtifactTests" --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

Recommended manual evidence path for this story:

- `%LOCALAPPDATA%\Lumiere\validation\output\*.json`
- `%LOCALAPPDATA%\Lumiere\validation\output\evidence\*.md`

The manual artifact set should include at least one negative/stale-build rehearsal before release sign-off:

- one HDR10 JXR folder-output evidence set with complete viewer evidence and matching build commits for all participating artifacts
- one rehearsal set where one named viewer artifact carries an older build commit and proves the runtime gate stays blocked

## Release Gate Status

- `Supported output compatibility matrix`: NOT RUN, pending real Windows named-viewer evidence.
- `HDR-preserved output profile contract`: NOT RUN, pending real Windows HDR10 JXR output evidence and viewer-recognized metadata evidence.
- `Public perfect-HDR-fidelity`: blocked-on-Windows-evidence.

## Remaining Work

- Record real current-build Windows manual artifacts for Microsoft Paint, Windows Photos, and Microsoft Edge.
- Include concrete target-app versions, folder output target scope, target-aware HDR evidence, format contract evidence, viewer-recognized HDR10 metadata evidence, and workspace-local companion scenario notes.
- Keep stale, clipboard-only, incomplete, or missing-version artifacts reviewable but unable to unlock the runtime HDR10 JXR gate.
