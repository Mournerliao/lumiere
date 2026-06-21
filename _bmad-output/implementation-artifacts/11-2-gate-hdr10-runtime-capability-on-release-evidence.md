---
title: 'Gate Hdr10 Runtime Capability On Release Evidence'
type: 'feature'
created: '2026-06-22'
status: 'in-progress'
route: 'vertical-slice'
story: '11-2'
---

# Gate HDR10 Runtime Capability On Release Evidence

## Intent

Story `11-2` already had most of the HDR10 JPEG XR building blocks in place: artifact encoder seams, FP16 readback, audit metadata write/read, typed format contracts, and viewer-evidence evaluation. What was still missing was the runtime closure.

The app was still deciding HDR10 executability from codec readiness alone. That left a gap against the `Public perfect-HDR-fidelity` direction: even if manual validation artifacts existed, runtime enablement did not actually depend on them.

This slice moves the app one step closer to a real supported output profile by requiring both:

1. implementation-level HDR10 JXR readiness, and
2. loaded Windows manual output validation evidence

before HDR10 becomes executable for the current session.

## Delivered In This Slice

1. Added `OutputProfileExecutionCapabilities.ResolveHdr10JxrReleaseCapabilities(...)` so HDR10 runtime capability can be resolved from codec implementation readiness plus loaded output validation artifacts.
2. Kept implementation-level prerequisites strict:
   - native WIC JPEG XR encoder
   - FP16 source acceptance
   - audit metadata write support
   - audit metadata round-trip evidence
   - complete HDR10 static metadata policy
3. Required `Hdr10JxrViewerValidationEvidence` to pass before HDR10 can become executable for the session.
4. Updated `MainWindow` to resolve output capabilities from the loaded validation snapshot instead of relying on a startup-time static capability snapshot.
5. Added tests that prove:
   - ready codec without manual artifacts still falls back to `sRGB`
   - incomplete viewer/manual evidence still falls back to `sRGB`
   - HDR10 becomes executable only when implementation readiness and manual evidence are both complete
6. Tightened the selected output-contract projection so a complete HDR10 format contract no longer keeps saying `pending implementation` after build prerequisites are satisfied. The contract surface now distinguishes:
   - `Build`: executable HDR10 output is still blocked by build/runtime prerequisites
   - `Validate`: the HDR10 contract is defined, but Windows manual viewer evidence is still incomplete
   - `Ready`: the validated HDR10-preserved contract is active for the current session
7. Corrected target execution semantics for mixed output:
   - clipboard remains a compatibility-only `sRGB` path
   - folder can independently resolve to `HDR10`
   - `Both` sessions no longer imply one synthetic runtime profile for every output target
8. Tightened HDR10 JXR release evidence so clipboard-only validation artifacts can no longer satisfy the folder-based HDR10 file-output gate. Runtime HDR10 enablement now only consumes manual artifacts that cover `Folder` output, while `Both` artifacts still count because they explicitly cover the file path as well.
9. Deepened the validation-artifact seam so a profile record can optionally declare `outputTargetsCovered`. This lets one manual session say "the session covered Both, but the HDR10 record only proves Folder" without splitting the whole artifact into multiple files.
10. Moved output-target evidence scoping into a shared `OutputProfileTargetScope` seam so runtime request policy and UI projection now resolve validation artifacts through the same target-aware rules instead of maintaining separate interpretations.
11. Tightened `OutputPolicy.FromSettings(...)` so the requested profile itself now respects the active `OutputTarget` when manual artifacts are applied. Folder-only HDR10 evidence no longer leaks into clipboard-session requested-profile semantics before runtime fallback is computed.
12. Corrected the folder execution path so `FolderOutputService` now encodes artifacts with `EffectiveProfileFor(OutputTarget.Folder)` instead of the aggregate mixed-target profile. In `Both` sessions, the file artifact can now actually use the validated HDR10 path when folder execution is ready, instead of being incorrectly forced back through aggregate `sRGB` fallback semantics.

## Suggested Review Order

1. [Output capability resolution](../../src/Lumiere.Graphics/Output/OutputProfileContract.cs)
2. [Fidelity projection contract mapping](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
3. [App wiring](../../src/Lumiere.App/App.xaml.cs)
4. [Main window runtime capability resolution](../../src/Lumiere.App/MainWindow.xaml.cs)
5. [Output target scope seam](../../src/Lumiere.Graphics/Output/OutputProfileTargetScope.cs)
6. [Folder output execution](../../src/Lumiere.Graphics/Output/FolderOutputService.cs)
7. [Output policy and projection tests](../../tests/Lumiere.Graphics.Tests/Output/OutputPolicyTests.cs)

## Validation

- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~OutputPolicyTests|FullyQualifiedName~Hdr10JxrViewerValidationEvidenceTests"`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~OutputPolicyTests|FullyQualifiedName~OutputResultTests|FullyQualifiedName~OutputResultProjectionTests|FullyQualifiedName~MainPanelProjectionTests|FullyQualifiedName~SettingsPanelProjectionTests|FullyQualifiedName~TrayMenuProjectionTests|FullyQualifiedName~PerfectHdrFidelityProjectionTests" --verbosity minimal /nr:false`

## Remaining Work

Story `11-2` is still `in-progress`, not `done`.

Remaining blockers:

- The repo still does not contain real Windows manual HDR10 output validation artifacts.
- HDR10 is still not a public-release path until target-aware HDR, named viewers, metadata recognition, and Windows manual evidence are actually recorded.
- Story `11-3` still needs real target-app compatibility evidence, not just the runtime gate that can consume it.
- Real Windows manual artifacts still need to start using `outputTargetsCovered` where one session mixes clipboard and folder semantics across profiles.
