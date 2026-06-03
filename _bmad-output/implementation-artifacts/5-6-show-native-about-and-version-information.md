---
status: done
---

# Story 5.6: Show Native About and Version Information

Status: done

## Story

As a screenshot user,
I want to see Lumiere's name, version, and brief HDR-first description,
so that I can identify the app and understand its purpose.

## Acceptance Criteria

1. **Given** settings are open, **when** the About section is displayed, **then** it shows Lumiere name, version, and concise HDR-first product description.

2. **Given** version information is shown, **when** the app is built or packaged, **then** the displayed version comes from app/build metadata or a single authoritative source rather than fragile hardcoded copy.

3. **Given** product description mentions HDR, **when** validation is incomplete for a path, **then** the copy avoids unsupported claims about HDR-preserving output.

## Tasks / Subtasks

- [x] **Task 1: Audit existing About UI and metadata sources** (AC: 1,2,3)
  - [x] Read current About XAML, settings projection, App composition, and build/version metadata files.
  - [x] Confirm current About version/copy is hardcoded in XAML.
  - [x] Confirm copy must avoid claiming HDR-preserving output.

- [x] **Task 2: Add authoritative about metadata projection** (AC: 1,2,3)
  - [x] Add a small about metadata provider or projection that reads version from assembly/build metadata.
  - [x] Keep fallback app name, version, and description centralized and testable.
  - [x] Keep product description HDR-first but honest about output validation.

- [x] **Task 3: Bind native About UI to projection** (AC: 1,2,3)
  - [x] Replace hardcoded About version/description text with projection-applied values.
  - [x] Add accessible names/help text for app name, version, and description.
  - [x] Do not add settings persistence, output behavior, capture behavior, tray behavior, or hotkey behavior.

- [x] **Task 4: Add focused hardware-independent tests** (AC: 1,2,3)
  - [x] Verify about projection uses provider/build metadata values.
  - [x] Verify fallback values are safe when metadata is missing.
  - [x] Verify HDR description does not claim HDR-preserving output.

- [x] **Task 5: Validate and record limits** (AC: 1,2,3)
  - [x] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`.
  - [x] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
  - [x] Record that rendered WinUI About text, high contrast, text scaling, screen reader output, and packaged version display still need Windows manual validation.

### Review Findings

- No code defects found in the Story 5.6 review pass.

## Dev Notes

### Validation Level

**Windows CI-pass** — Automated gates pass on Windows. Assembly metadata projection tested; packaged version display not manually validated.

### Story Scope

Story 5.6 finishes Epic 5's native settings surface by making the About section real instead of hardcoded placeholder text. It does not implement packaging, signing, update checks, diagnostics UI, tray, hotkeys, output policy, or HDR output preservation.

### Current Implementation State

`MainWindow.xaml` already contains an `ABOUT` section with `Lumiere`, `v0.1.0`, and `Native screenshot reference for HDR-first capture.` hardcoded. This satisfies the visual shape but fails the single-source/version metadata requirement.

`SettingsPanelProjection` is the current pure projection for settings shell display data. Story 5.6 should keep the About data similarly testable and avoid moving version/copy policy into XAML literals.

Story 5.5 added `Lumiere.Settings` as the settings/source-of-truth owner. About metadata can live there as a narrow provider because the planning artifact calls out local settings/version/about metadata together.

### Architecture Compliance

- `Lumiere.Settings` may own a narrow about metadata provider.
- `Lumiere.App.Core` may project about metadata for native UI.
- `Lumiere.App` may compose the provider and apply projected text to XAML controls.
- Do not touch WGC, D3D11, DXGI, overlay, output execution, tray, or hotkeys.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 5.6`] - Story requirements and acceptance criteria.
- [Source: `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md#Story 5.6: About and Version`] - Single-source version/about and honest HDR copy guardrails.
- [Source: `_bmad-output/project-context.md`] - HDR/output claim honesty and boundary guidance.
- [Source: `_bmad-output/implementation-artifacts/5-5-persist-local-settings-across-launches.md`] - Previous story settings ownership and validation state.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-05-23: Story created from Epic 5 backlog and moved to in-progress for BMad dev-story execution.
- 2026-05-23: Existing About UI, settings projection, App composition, and build metadata files audited before implementation.
- 2026-05-23: RED tests added for about metadata provider, settings projection, fallback values, and HDR claim honesty; initial compile failed because about provider/projection did not exist.
- 2026-05-23: Added `IAboutInfoProvider`, `AssemblyAboutInfoProvider`, `AboutInfoProjection`, App composition, and native About UI projection application.
- 2026-05-23: Added App assembly metadata (`Product`, `Description`, `Version`) so About displays `Lumiere` and build-sourced `0.1.0` instead of `Lumiere.App` or hardcoded XAML version text.
- 2026-05-23: Narrow about/settings projection tests passed: 21 tests, 0 failed.
- 2026-05-23: Full validation passed after CRLF formatting: `dotnet build` passed with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 214/214; `dotnet format --verify-no-changes --no-restore` passed. Restore had already passed earlier in the Epic 5 run after NuGet network permission.
- 2026-05-23: Code-review pass found no defects. Final validation passed: `dotnet restore` passed with network permission; `dotnet build` passed with 0 warnings/errors; `Lumiere.Graphics.Tests` passed 214/214; `dotnet format --verify-no-changes --no-restore` passed.

### Completion Notes List

- Story context created; implementation in progress.
- Native About data now flows from assembly/build metadata through a narrow provider and pure settings projection.
- About description remains HDR-first without claiming HDR-preserving output.
- Validation level: Windows CI-pass for hardware-independent about metadata/projection tests. Windows manual validation remains needed for rendered About UI, accessibility, text scaling, and packaged version display.
- Story marked done after review and final validation.

### File List

- `_bmad-output/implementation-artifacts/5-6-show-native-about-and-version-information.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.App.Core/SettingsPanelProjection.cs`
- `src/Lumiere.App/App.xaml.cs`
- `src/Lumiere.App/Lumiere.App.csproj`
- `src/Lumiere.App/MainWindow.xaml`
- `src/Lumiere.App/MainWindow.xaml.cs`
- `src/Lumiere.Settings/AssemblyAboutInfoProvider.cs`
- `src/Lumiere.Settings/IAboutInfoProvider.cs`
- `tests/Lumiere.Graphics.Tests/App/SettingsPanelProjectionTests.cs`
- `tests/Lumiere.Graphics.Tests/Settings/AssemblyAboutInfoProviderTests.cs`

### Change Log

- 2026-05-23: Created ready-for-dev story context and started implementation.
- 2026-05-23: Implemented native About metadata projection and tests.
- 2026-05-23: Moved story to review after all tasks and validation gates completed.
- 2026-05-23: Review found no defects and story was marked done.
