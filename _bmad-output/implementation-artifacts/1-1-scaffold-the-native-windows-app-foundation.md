# Story 1.1: Scaffold the Native Windows App Foundation

Status: in-progress

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a developer,
I want the Lumiere solution scaffolded with the approved WinUI 3 and .NET foundation,
so that all future HDR capture work starts from the correct native Windows runtime and project boundaries.

## Acceptance Criteria

1. Given a clean repository workspace, when repository foundation work begins, then Git is initialized before WinUI scaffolding proceeds; and the repository contains `.gitignore`, `.editorconfig`, formatting configuration, README, and documented developer workflow conventions.
2. Given a clean repository workspace with repository foundation files in place, when the solution is created, then it contains `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and the source projects defined by the architecture; and the app project targets `net10.0-windows10.0.19041.0` and `x64`.
3. Given the package configuration, when dependencies are restored, then Windows App SDK, `Vortice.Direct3D11`, `Vortice.DXGI`, and any required CsWinRT package versions are pinned as architecture-approved versions.
4. Given a developer prepares a local change, when they read the repository workflow documentation, then the expected formatting command, build/restore validation commands, and commit message convention are clear enough to follow before code review.
5. Given the solution is opened by a developer, when they inspect project references, then UI, overlay, capture, graphics, infrastructure, and settings boundaries are represented as separate projects or modules.

## Tasks / Subtasks

- [x] Establish repository foundation before WinUI scaffolding. (AC: 1, 4)
  - [x] Initialize Git at the repository root before creating the WinUI solution.
  - [x] Add a Windows/.NET/Visual Studio oriented `.gitignore` that excludes build outputs, IDE state, package caches, logs, and local machine artifacts without excluding planning documents.
  - [x] Add `.editorconfig` for C#/.NET, XML/XAML, Markdown, YAML, and shell/script basics used by this repository.
  - [x] Add formatting configuration compatible with the selected .NET tooling, such as `.config/dotnet-tools.json` plus documented `dotnet format` usage if a local tool manifest is needed.
  - [x] Add `README.md` with project purpose, native Windows/HDR constraints, prerequisites, restore/build commands, and the first developer workflow.
  - [x] Document commit message convention in the README or a dedicated developer workflow section; prefer concise Conventional Commit style such as `feat:`, `fix:`, `docs:`, `chore:`, and `test:`.
  - [x] Record the expected pre-review validation sequence: format, `dotnet restore`, `dotnet build Lumiere.sln -p:Platform=x64`, and any available tests.
- [x] Create the native WinUI 3 solution foundation. (AC: 2)
  - [x] Create `Lumiere.sln` at the repository root.
  - [x] Create `src/Lumiere.App/Lumiere.App.csproj` as the WinUI 3 application project.
  - [x] Set `TargetFramework` to `net10.0-windows10.0.19041.0`.
  - [x] Set the platform/runtime defaults to explicit `x64` / `win-x64`; do not leave the solution as `Any CPU`.
  - [x] Include minimal WinUI app shell files: `App.xaml`, `App.xaml.cs`, `MainWindow.xaml`, `MainWindow.xaml.cs`, and `app.manifest`.
- [x] Add central build and package configuration. (AC: 2, 3)
  - [x] Add `Directory.Build.props` for shared target framework, platform, nullable/implicit usings, and deterministic build defaults that fit the repo.
  - [x] Add `Directory.Packages.props` and enable central package management.
  - [x] Pin `Microsoft.WindowsAppSDK` to `1.8.260317003`.
  - [x] Pin `Vortice.Direct3D11` to `3.8.3`.
  - [x] Pin `Vortice.DXGI` to `3.8.3`.
  - [x] Add `Microsoft.Windows.CsWinRT` `2.2.0` only if the concrete scaffold or initial interop code requires it; otherwise leave it documented for Story 1.3.
- [x] Create architecture boundary projects. (AC: 2, 5)
  - [x] Create `src/Lumiere.Overlay/Lumiere.Overlay.csproj` for WinUI overlay and crop UI behavior.
  - [x] Create `src/Lumiere.Capture/Lumiere.Capture.csproj` for Windows.Graphics.Capture lifecycle.
  - [x] Create `src/Lumiere.Graphics/Lumiere.Graphics.csproj` for D3D11/DXGI rendering and presentation.
  - [x] Create `src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj` for interop, diagnostics, result types, and UI-thread helpers.
  - [x] Create `src/Lumiere.Settings/Lumiere.Settings.csproj` for local preferences only.
  - [x] Wire project references so `Lumiere.App` composes the boundary projects without moving capture or graphics ownership into UI code.
- [x] Add starter tests or validation scaffolding appropriate for a new repository. (AC: 2, 5)
  - [x] Create test project placeholders only if the selected local test framework can be restored cleanly.
  - [x] Prefer first validation coverage for project configuration and future HDR constants/lifecycle scaffolding.
  - [x] Do not fake HDR graphics tests before the relevant production code exists.
- [ ] Verify repository foundation and scaffold. (AC: 1, 2, 3, 4, 5)
  - [ ] Run the documented formatting check or formatting command once files exist.
  - [ ] Run `dotnet restore` on `Lumiere.sln`.
  - [ ] Run `dotnet build Lumiere.sln -p:Platform=x64` or the repo-equivalent x64 build.
  - [x] Confirm no web, Electron, Tauri, WPF bitmap-first, GDI, or SDR screenshot library scaffold was introduced.

## Dev Notes

### Story Scope

This story is scaffolding only. It must first establish the repository foundation, then establish the solution, project boundaries, target framework, platform architecture, and package pinning that later HDR capture/render stories depend on.

Do not implement WGC capture, D3D11 device creation, DXGI swap chain presentation, overlay crop interaction, export, clipboard, hotkeys, tray behavior, annotations, or history in this story. Those capabilities are assigned to later stories. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Story 1.1: Scaffold the Native Windows App Foundation`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Workflow Completion Summary`]

Repository foundation is now explicitly in scope for this story. Initialize Git before WinUI scaffolding, add `.gitignore`, `.editorconfig`, formatting configuration, commit convention, README, and a basic developer workflow so project/package decisions are tracked from the first scaffold commit. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Development Workflow Rules`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/research/technical-lumiere-hdr-capture-research-2026-04-20.md#Development Workflows and Tooling`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

### Technical Requirements

- Build a native Windows desktop application using C#/.NET, WinUI 3, Windows App SDK, WGC, Direct3D 11, DXGI, and Vortice as the approved technology direction. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Primary Technology Domain`]
- Target `.NET 10 LTS` with `TargetFramework` `net10.0-windows10.0.19041.0`. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#NFR14`]
- Target `x64` first and avoid `Any CPU`, because Windows App SDK and graphics dependencies include native components. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#NFR15`]
- Record package version and target framework decisions in project files during scaffolding. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#NFR27`]
- Preserve module boundaries between capture, graphics rendering, overlay UI, infrastructure, and settings from the beginning. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#NFR28`]
- Keep MVP workflows local/offline. Do not introduce network, cloud, telemetry, screenshot upload, or remote diagnostics dependencies. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Critical Don't-Miss Rules`]

### Architecture Compliance

Use the official WinUI 3 / Windows App SDK application foundation, then add explicit modules for capture, graphics, overlay, infrastructure, diagnostics, and settings. The architecture rejects Electron, Tauri, WPF bitmap-first templates, web UI starters, cross-platform screenshot libraries, and generic SDR screenshot boilerplate because they conflict with GPU-resident HDR preview fidelity. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Starter Template Evaluation`]

The approved starter command is:

```bash
dotnet new winui --name Lumiere --framework net10.0-windows10.0.19041.0
```

If the installed WinUI template does not support `--framework`, create the WinUI 3 blank app and edit the project file so the app still uses:

```xml
<TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
<Platforms>x64</Platforms>
<RuntimeIdentifier>win-x64</RuntimeIdentifier>
```

[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Selected Starter: WinUI 3 Blank App with Custom Graphics/Capture Infrastructure`]

### Library / Framework Requirements

Use central package management in `Directory.Packages.props`. Required approved versions:

- `Microsoft.WindowsAppSDK` `1.8.260317003`
- `Vortice.Direct3D11` `3.8.3`
- `Vortice.DXGI` `3.8.3`
- `Microsoft.Windows.CsWinRT` `2.2.0` only when concrete WinRT/native interop requires it

Do not use `Microsoft.Windows.CsWinRT` `3.0.0-preview.*` by default; the architecture explicitly keeps MVP on stable package choices unless a documented blocker requires otherwise. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Package References`]

Latest technical check on 2026-04-21:

- Microsoft Learn still lists Windows App SDK stable `1.8.6 (1.8.260317003)`, released 2026-03-18. [Source: `https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads`]
- NuGet lists `Vortice.Direct3D11` `3.8.3` and `Vortice.DXGI` `3.8.3`; both are compatible with `net10.0`. [Source: `https://www.nuget.org/packages/Vortice.Direct3D11`; `https://www.nuget.org/packages/Vortice.DXGI`]
- NuGet lists `Microsoft.Windows.CsWinRT` stable `2.2.0`, with newer prerelease versions available; stay on stable unless justified. [Source: `https://www.nuget.org/packages/Microsoft.Windows.CsWinRT`]

### File Structure Requirements

Create or align to this root structure in this story:

```text
lumiere/
├── README.md
├── .editorconfig
├── .gitignore
├── Lumiere.sln
├── Directory.Build.props
├── Directory.Packages.props
├── src/
│   ├── Lumiere.App/
│   ├── Lumiere.Overlay/
│   ├── Lumiere.Capture/
│   ├── Lumiere.Graphics/
│   ├── Lumiere.Infrastructure/
│   └── Lumiere.Settings/
└── tests/
```

Boundary rules:

- `Lumiere.App`: application composition and startup only.
- `Lumiere.Overlay`: UI, crop, and window behavior only.
- `Lumiere.Capture`: WGC capture lifecycle only.
- `Lumiere.Graphics`: D3D11/DXGI rendering and presentation only.
- `Lumiere.Infrastructure`: interop, diagnostics, result types, and UI-thread helpers.
- `Lumiere.Settings`: local preferences only.

Do not place Direct3D/DXGI creation in `Lumiere.App` or `Lumiere.Overlay`. Do not place WinUI overlay state in `Lumiere.Capture` or `Lumiere.Graphics`. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Project Structure & Boundaries`]

### UX Requirements Relevant to Scaffolding

There is no full overlay UX implementation in this story, but the scaffold must keep the future UX possible:

- The app shell must support a future full-screen WinUI 3 overlay containing a DirectX-backed `SwapChainPanel`.
- The default app foundation must not force preview through `BitmapImage`, `SoftwareBitmap`, GDI, or ordinary XAML image controls.
- The app must preserve room for visible HDR readiness, degraded, unsupported, and failed states.
- The structure must support safe cancel/escape behavior and recovery paths in future overlay stories.

[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Core User Experience`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Platform Strategy`]

### Testing Requirements

No test framework is scaffolded yet. If this story adds tests, choose a standard .NET test project only after confirming restore/build works locally with the approved target framework and package set. Keep tests honest: scaffold/configuration tests are acceptable; fake HDR rendering tests are not.

Future test layout should mirror source boundaries:

- `tests/Lumiere.Capture.Tests/`
- `tests/Lumiere.Graphics.Tests/`
- `tests/Lumiere.Overlay.Tests/`
- `tests/Lumiere.Infrastructure.Tests/`

Early validation should focus on project configuration, package versions, platform architecture, and boundary references. Story 1.2 will add explicit HDR constants and tests. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Test Organization`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Testing Rules`]

### Anti-Patterns to Avoid

- Do not scaffold a web, Electron, Tauri, WPF bitmap-first, WinForms, GDI, or generic screenshot-tool foundation.
- Do not add CPU bitmap readback, SDR preview, `BitmapImage`, `SoftwareBitmap`, or GDI as a main preview path.
- Do not introduce network, cloud, upload, telemetry, or remote diagnostics dependencies.
- Do not collapse all code into the app project for speed.
- Do not introduce export, clipboard, hotkey, tray, annotation, or history modules before later stories define their semantics.
- Do not depend on `Any CPU`.

### Previous Story Intelligence

This is the first story in Epic 1. There is no previous story file in `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts` to learn from.

### Git Intelligence

No Git repository is detected at `/Users/asherliao/Projects/lumiere` as of story creation. Story 1.1 must initialize Git and establish repository workflow files before WinUI scaffolding proceeds. Treat the planning artifacts as the source of truth until a repository workflow exists.

### Developer Workflow Requirements

- Establish a first local workflow in `README.md` before code scaffolding depends on implicit conventions.
- Document prerequisites: Visual Studio 2022 with WinUI/Windows App SDK workloads, .NET 10 SDK, and Windows SDK `10.0.26100.x` unless local tooling requires a documented alternative.
- Document restore/build validation using `dotnet restore` and `dotnet build Lumiere.sln -p:Platform=x64`.
- Document formatting expectations and the command developers should run before review.
- Document commit convention using concise Conventional Commit prefixes (`feat:`, `fix:`, `docs:`, `chore:`, `test:`), with the first scaffold work expected to use `chore:` or `feat:` depending on commit grouping.

### Project Context Reference

Before implementing, read `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md`. Its highest-priority rules for this story are:

- Use `.NET 10 LTS`, WinUI 3, Windows App SDK, WGC, Direct3D 11, DXGI, and Vortice.
- Keep native resource ownership explicit.
- Keep native interop behind narrow infrastructure APIs.
- Preserve module boundaries.
- Prefer HDR correctness over convenience.

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-04-21: Ran `git init` before solution/project scaffolding; repository initialized at `/Users/asherliao/Projects/lumiere/.git`.
- 2026-04-21: Ran `dotnet --info` and `dotnet new list winui`; both failed with `zsh:1: command not found: dotnet`, so the WinUI scaffold was created manually from the approved architecture requirements.
- 2026-04-21: Ran `xmllint --noout` for props, project, XAML, and manifest files; XML validation passed.
- 2026-04-21: Ran static checks for target framework, x64/runtime, central package pins, project references, `Any CPU`, and rejected scaffold technologies.
- 2026-04-21: Attempted `dotnet format Lumiere.sln`, `dotnet restore Lumiere.sln`, and `dotnet build Lumiere.sln -p:Platform=x64`; all failed because `dotnet` is not installed in this environment.

### Completion Notes List

- Repository foundation files were added before source scaffolding: `.gitignore`, `.editorconfig`, `.config/dotnet-tools.json`, and `README.md`.
- `README.md` documents native Windows/HDR constraints, Windows prerequisites, the format/restore/build validation sequence, and Conventional Commit prefixes.
- `Lumiere.sln`, `Directory.Build.props`, and `Directory.Packages.props` were created with `net10.0-windows10.0.19041.0`, `x64`, `win-x64`, central package management, Windows App SDK `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, and `Vortice.DXGI` `3.8.3`.
- `Microsoft.Windows.CsWinRT` was intentionally not referenced because this scaffold does not yet include concrete WinRT/native interop code; Story 1.3 remains the expected point for that dependency.
- Source boundaries were created for App, Overlay, Capture, Graphics, Infrastructure, and Settings. The App project composes the boundary projects via project references.
- No test project was created because the local test framework cannot be restored without an installed .NET SDK. A `tests/.gitkeep` placeholder preserves the intended test root without faking HDR tests.
- Story is blocked from review completion until `dotnet format`, `dotnet restore`, and `dotnet build Lumiere.sln -p:Platform=x64` can run successfully on a machine with the required .NET/WinUI toolchain.

### File List

- `.config/dotnet-tools.json`
- `.editorconfig`
- `.gitignore`
- `Directory.Build.props`
- `Directory.Packages.props`
- `Lumiere.sln`
- `README.md`
- `src/Lumiere.App/App.xaml`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/Lumiere.App.csproj`
- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.App/app.manifest`
- `src/Lumiere.Capture/CaptureBoundary.cs`
- `src/Lumiere.Capture/Lumiere.Capture.csproj`
- `src/Lumiere.Graphics/GraphicsBoundary.cs`
- `src/Lumiere.Graphics/Lumiere.Graphics.csproj`
- `src/Lumiere.Infrastructure/InfrastructureBoundary.cs`
- `src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj`
- `src/Lumiere.Overlay/Lumiere.Overlay.csproj`
- `src/Lumiere.Overlay/OverlayBoundary.cs`
- `src/Lumiere.Settings/Lumiere.Settings.csproj`
- `src/Lumiere.Settings/SettingsBoundary.cs`
- `tests/.gitkeep`

### Change Log

- 2026-04-21: Initialized Git and added the native Windows repository foundation, WinUI app scaffold, boundary projects, central build/package configuration, and developer workflow documentation.
