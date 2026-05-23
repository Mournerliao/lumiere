---
title: 'Fix App Projection Testhost Hang'
type: 'bugfix'
created: '2026-05-23'
status: 'done'
baseline_commit: '9349743e0166b6cec329af8aba0ec6711de4e358'
context:
  - '{project-root}/_bmad-output/project-context.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** Hardware-independent App projection tests hang when executed because the test project references and executes types from the WinUI `Lumiere.App` executable assembly. Even pure projection methods become coupled to Windows App SDK/XAML test-host behavior, so `SettingsPanelProjectionTests` and older `MainPanelProjectionTests` cannot reliably run.

**Approach:** Move pure app-facing projection types into a non-WinUI class library that can be referenced by both `Lumiere.App` and `Lumiere.Graphics.Tests`. Keep the WinUI executable responsible only for XAML/code-behind application of the projection.

## Boundaries & Constraints

**Always:** Preserve existing projection public behavior and current settings UI behavior. Keep the new project Windows x64/.NET 10 compatible, nullable-enabled through shared props, and free of `UseWinUI`, Windows App SDK, XAML pages, D3D/DXGI/WGC ownership, or platform interop.

**Ask First:** If moving projection types requires changing user-facing copy, changing story 5.4 output semantics, adding package dependencies, or changing the repository validation command set.

**Never:** Do not make tests instantiate `MainWindow`, `Application`, XAML controls, D3D devices, capture services, overlays, or clipboard output. Do not suppress or skip projection tests to hide the hang.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Pure app projection test | `SettingsPanelProjectionTests` or `MainPanelProjectionTests` runs under VSTest | Tests execute without loading the WinUI executable assembly and complete normally | Test failures report assertions rather than hang dumps |
| App runtime use | `MainWindow` applies settings and shell projections | Existing names, values, pending/read-only metadata, and main panel state remain available to code-behind | Build fails if `Lumiere.App` cannot reference the projection library |
| Out-of-scope UI behavior | Settings UI is opened manually | No new capture, overlay, output, or persistence behavior is introduced | Manual rendered WinUI validation remains a separate Windows validation activity |

</frozen-after-approval>

## Code Map

- `src/Lumiere.App/MainPanelProjection.cs` -- pure main panel trust/action projection currently inside WinUI executable.
- `src/Lumiere.App/SettingsPanelProjection.cs` -- pure settings/output projection currently inside WinUI executable.
- `src/Lumiere.App/AppShellProjection.cs` -- pure shell visibility projection currently inside WinUI executable.
- `src/Lumiere.App/Lumiere.App.csproj` -- WinUI executable project; should reference the new projection library.
- `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj` -- should reference the new projection library instead of `Lumiere.App`.
- `tests/Lumiere.Graphics.Tests/App/*ProjectionTests.cs` -- pure tests that should import the new namespace/project and run without WinUI test-host hangs.
- `Lumiere.sln` -- solution must include the new projection project for restore/build/test/format.

## Tasks & Acceptance

**Execution:**
- [x] `src/Lumiere.App.Core/Lumiere.App.Core.csproj` -- add a plain class library project -- provides app-facing projections without WinUI/XAML executable loading.
- [x] `src/Lumiere.App/{MainPanelProjection.cs,SettingsPanelProjection.cs,AppShellProjection.cs}` -- move to `src/Lumiere.App.Core/` with an appropriate namespace or preserved namespace -- keeps behavior stable while removing WinUI test-host coupling.
- [x] `src/Lumiere.App/Lumiere.App.csproj` -- reference `Lumiere.App.Core` -- lets WinUI code-behind keep using the projections.
- [x] `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj` -- reference `Lumiere.App.Core` and remove direct `Lumiere.App` reference if no remaining test needs it -- prevents projection tests from loading WinUI App.
- [x] `tests/Lumiere.Graphics.Tests/App/*ProjectionTests.cs` -- update usings only as needed -- preserves test intent while targeting the plain library.
- [x] `Lumiere.sln` -- add the new project and x64 configurations -- keeps repository validation commands whole.

**Acceptance Criteria:**
- Given App projection tests run under VSTest, when `SettingsPanelProjectionTests`, `MainPanelProjectionTests`, and `AppShellProjectionTests` execute, then they complete without hang dumps.
- Given the full graphics test project runs, when the previous hang condition is reached, then the test host continues and reports pass/fail normally.
- Given `Lumiere.App` builds, when `MainWindow.xaml.cs` applies projection types, then no WinUI behavior or projection output changes are required.

## Spec Change Log

## Design Notes

The projection library should remain a pure application-model layer: it can depend on `Lumiere.Capture`, `Lumiere.Graphics`, and `Lumiere.Settings` for typed state, but not on `Microsoft.UI.Xaml`, `Microsoft.WindowsAppSDK`, generated XAML, or UI controls. This matches the existing tests' intent: they validate deterministic state mapping, not rendered WinUI.

## Verification

**Commands:**
- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false` -- expected: restore succeeds.
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: build succeeds with no errors.
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter FullyQualifiedName~SettingsPanelProjectionTests` -- expected: completes without hang and passes.
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false --filter "FullyQualifiedName~MainPanelProjectionTests|FullyQualifiedName~AppShellProjectionTests"` -- expected: completes without hang and passes.
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` -- expected: completes without hang; failures, if any, are ordinary assertions to fix.
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal` -- expected: no formatting changes required.

## Suggested Review Order

**Projection Boundary**

- New pure projection home
  [`Lumiere.App.Core.csproj:1`](../../src/Lumiere.App.Core/Lumiere.App.Core.csproj#L1)

- Main trust model extracted
  [`MainPanelProjection.cs:6`](../../src/Lumiere.App.Core/MainPanelProjection.cs#L6)

- Settings projection extracted
  [`SettingsPanelProjection.cs:6`](../../src/Lumiere.App.Core/SettingsPanelProjection.cs#L6)

- Shell projection extracted
  [`AppShellProjection.cs:5`](../../src/Lumiere.App.Core/AppShellProjection.cs#L5)

**Consumers**

- WinUI consumes pure core
  [`Lumiere.App.csproj:18`](../../src/Lumiere.App/Lumiere.App.csproj#L18)

- UI maps core icon
  [`MainWindow.xaml.cs:997`](../../src/Lumiere.App/MainWindow.xaml.cs#L997)

- Solution builds core
  [`Lumiere.sln:7`](../../Lumiere.sln#L7)

**Tests**

- Tests avoid WinUI app
  [`Lumiere.Graphics.Tests.csproj:19`](../../tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj#L19)

- Icon assertions updated
  [`MainPanelProjectionTests.cs:42`](../../tests/Lumiere.Graphics.Tests/App/MainPanelProjectionTests.cs#L42)

- Settings tests now run
  [`SettingsPanelProjectionTests.cs:105`](../../tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs#L105)
