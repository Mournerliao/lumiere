# 11-3 Require Target-App Version Evidence For HDR10 Runtime

## Context

The public perfect-HDR-fidelity direction already required target-app versions in planning and validation docs, and recent app work could now record and summarize those versions. The remaining gap was that HDR10 JXR runtime readiness still treated target-app versions as review-only metadata instead of executable release evidence.

## What Changed

1. `Hdr10JxrViewerValidationEvidence` now requires complete target-app version evidence before the HDR10 JXR folder-output path can become executable for the current session.
2. `OutputValidationSessionArtifact` now treats missing target-app versions as incomplete manual evidence when `targetAppsTested` names a viewer/app without a corresponding real version record.
3. Test fixtures that represent complete manual evidence now include `TargetAppVersions`, while targeted tests also cover the missing-version blocker path.

## Why This Matters

- Public HDR-ready runtime gates now align more closely with the release checklist instead of accepting viewer pass rows that cannot be tied to concrete app builds.
- The review summary, artifact-completeness semantics, and runtime execution gate now use the same evidence standard.
- Drafts and templates remain honest: placeholders still help the validator start quickly, but they no longer risk being interpreted as complete manual evidence.

## Validation

- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~Hdr10JxrViewerValidationEvidenceTests|FullyQualifiedName~OutputPolicyTests|FullyQualifiedName~OutputValidationSessionArtifactTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests" --verbosity minimal /nr:false`

## Status

Story `11-3` remains `in-progress`: runtime evidence standards are tighter, but real Windows manual target-app validation artifacts are still required before public release can call the HDR10 path supported.
