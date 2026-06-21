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

## Suggested Review Order

1. [Output capability resolution](../../src/Lumiere.Graphics/Output/OutputProfileContract.cs)
2. [Fidelity projection contract mapping](../../src/Lumiere.App.Core/PerfectHdrFidelityProjection.cs)
3. [App wiring](../../src/Lumiere.App/App.xaml.cs)
4. [Main window runtime capability resolution](../../src/Lumiere.App/MainWindow.xaml.cs)
5. [Output policy and projection tests](../../tests/Lumiere.Graphics.Tests/Output/OutputPolicyTests.cs)

## Validation

- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~OutputPolicyTests|FullyQualifiedName~Hdr10JxrViewerValidationEvidenceTests"`

## Remaining Work

Story `11-2` is still `in-progress`, not `done`.

Remaining blockers:

- The repo still does not contain real Windows manual HDR10 output validation artifacts.
- HDR10 is still not a public-release path until target-aware HDR, named viewers, metadata recognition, and Windows manual evidence are actually recorded.
- Story `11-3` still needs real target-app compatibility evidence, not just the runtime gate that can consume it.
