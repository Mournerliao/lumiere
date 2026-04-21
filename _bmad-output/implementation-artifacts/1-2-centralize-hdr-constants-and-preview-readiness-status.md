# Story 1.2: Centralize HDR Constants and Preview Readiness Status

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

As a developer,
I want HDR formats and readiness states centralized,
so that implementation agents cannot accidentally weaken the capture or preview path.

## Acceptance Criteria

1. Given the graphics project exists, when `HdrConstants` is implemented, then it exposes `DirectXPixelFormat.R16G16B16A16Float`, `DXGI_FORMAT_R16G16B16A16_FLOAT`, and `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
2. Given automated tests are run, when HDR constants are inspected, then tests fail if any primary preview constant is changed to an 8-bit, SDR, bitmap, or GDI-oriented format.
3. Given the app initializes preview state, when HDR readiness cannot be established, then a typed status reports `Degraded` or `Unsupported` instead of silently falling back.

## Tasks / Subtasks

- [x] Confirm Story 1.1 scaffold prerequisites before implementation. (AC: 1, 2, 3)
  - [x] Verify `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and `src/Lumiere.Graphics/` exist from Story 1.1.
  - [x] Verify the app targets `net10.0-windows10.0.19041.0`, `x64`, Windows App SDK `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, and `Vortice.DXGI` `3.8.3`.
  - [x] If the scaffold is missing, stop and complete Story 1.1 first instead of inventing a different project layout.
- [x] Add centralized HDR constants in the graphics boundary. (AC: 1)
  - [x] Create `src/Lumiere.Graphics/Hdr/HdrConstants.cs` or the closest existing `Lumiere.Graphics` namespace/folder pattern.
  - [x] Expose the WGC frame-pool pixel format as `DirectXPixelFormat.R16G16B16A16Float`.
  - [x] Expose the DXGI swap-chain format as `Format.R16G16B16A16_Float` / `DXGI_FORMAT_R16G16B16A16_FLOAT` using the Vortice type already referenced by the project.
  - [x] Expose the DXGI color space as `ColorSpaceType.RgbFullG10NoneP709` / `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` using the Vortice type already referenced by the project.
  - [x] Keep the constants strongly typed; do not store these values as strings, magic integers, app settings, or loose duplicated literals.
- [x] Define typed preview readiness status for early pipeline state. (AC: 3)
  - [x] Add a compact status model under `src/Lumiere.Graphics/Hdr/` or a shared diagnostics/status namespace if Story 1.1 already established one.
  - [x] Include at least `Unknown` or `Initializing`, `Ready`, `Degraded`, `Unsupported`, and `Failed` states so future capture/graphics stories can distinguish "not yet validated" from "validated HDR-ready".
  - [x] Include stage/detail fields or a companion record for capture, graphics, presentation, overlay, interop, or lifecycle diagnostics when readiness cannot be established.
  - [x] Ensure degraded/unsupported/failed states can carry a user-facing message and technical detail without requiring UI code to inspect D3D/DXGI objects.
- [x] Add tests that lock the HDR constants and status behavior. (AC: 2, 3)
  - [x] Add or extend a test project that follows the scaffolded repository pattern, preferably `tests/Lumiere.Graphics.Tests/`.
  - [x] Assert the WGC pixel format remains `DirectXPixelFormat.R16G16B16A16Float`.
  - [x] Assert the swap-chain format remains the FP16 format and cannot drift to `B8G8R8A8`, `R8G8B8A8`, or other 8-bit SDR-oriented formats.
  - [x] Assert the color space remains the scRGB linear HDR color space and cannot drift to an SDR color space.
  - [x] Assert an unvalidated preview path reports a typed non-ready state such as `Degraded` or `Unsupported`, not `Ready` and not a silent fallback.
- [x] Integrate without implementing later graphics/capture behavior. (AC: 1, 3)
  - [x] Reference `HdrConstants` from any existing placeholder graphics status code if it exists, but do not implement D3D11 device creation, WGC frame pools, swap-chain attachment, or live preview in this story.
  - [x] Keep UI/app shell changes minimal; readiness may be represented as a typed model now and rendered in later stories.
  - [x] Do not add export, clipboard, hotkey, tray, annotation, history, or SDR screenshot fallback behavior.
- [x] Validate the story output. (AC: 1, 2, 3)
  - [x] Run formatting for touched files.
  - [x] Run `dotnet restore` on `Lumiere.sln`.
  - [x] Run `dotnet build Lumiere.sln -p:Platform=x64`.
  - [x] Run the HDR constants/readiness tests.
  - [x] Review the final diff and explicitly call out that HDR constants or readiness semantics were touched.

## Dev Notes

### Story Scope

This story creates the shared guardrails for later capture and rendering stories. It should centralize the exact HDR pixel/format/color-space choices and define typed readiness states that future components can reuse before any real D3D11 device, WGC frame pool, swap chain, or live preview exists.

Do not implement Story 1.3 interop, Story 1.4 swap-chain attachment, or Story 1.5 WGC capture/live preview here. This story exists to prevent those later stories from hardcoding or weakening the HDR path. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Story 1.2: Centralize HDR Constants and Preview Readiness Status`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Decision Impact Analysis`]

Story 1.2 depends on Story 1.1's scaffold. At story creation time, the repository contains a Story 1.1 file marked `ready-for-dev`, but the source scaffold is not present in the workspace. A dev agent must complete or verify Story 1.1 before applying this story's code changes. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-1-scaffold-the-native-windows-app-foundation.md`; `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/sprint-status.yaml`]

### Technical Requirements

- Preserve the non-negotiable HDR path: WGC frame pool pixel format `DirectXPixelFormat.R16G16B16A16Float`, DXGI swap-chain format `DXGI_FORMAT_R16G16B16A16_FLOAT`, and DXGI color space `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Technology Stack & Versions`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`]
- Centralize these values under a responsibility-specific type such as `HdrConstants`; do not duplicate literals in capture, graphics, overlay, or app-shell code. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Code Quality & Style Rules`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Code Naming Conventions`]
- Use typed readiness/status objects for expected platform states. Exceptions are for invariant violations or unrecoverable native failures, not for ordinary degraded/unsupported capability reporting. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#API & Communication Patterns`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Format Patterns`]
- Ensure readiness can represent "not yet validated" distinctly from `Ready`. Loading or initialization UI must not imply HDR readiness before validation completes. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Loading States`]

### Architecture Compliance

The architecture assigns HDR constants and validation to the graphics/HDR foundation. Place Direct3D/DXGI-facing constants in `Lumiere.Graphics`, and keep native interop details out of UI code. If a shared diagnostics/status model already exists after Story 1.1, use that established boundary; otherwise keep this story's readiness model small and close to the graphics/HDR namespace until the diagnostics epic expands it.

Boundary rules:

- `Lumiere.Graphics` owns HDR constants, Direct3D/DXGI rendering choices, future swap-chain format, and color-space validation.
- `Lumiere.Capture` will consume the WGC pixel format later but should not define its own competing HDR constants.
- `Lumiere.App` and `Lumiere.Overlay` may display readiness later but must not know D3D11/DXGI implementation details.
- `Lumiere.Infrastructure` should only be used if Story 1.1 established a shared diagnostics/result abstraction or an interop-facing type is genuinely needed.

[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Executive Architecture Summary`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Structure Patterns`]

### Library / Framework Requirements

Use the scaffolded package versions from Story 1.1:

- `Microsoft.WindowsAppSDK` `1.8.260317003`
- `Vortice.Direct3D11` `3.8.3`
- `Vortice.DXGI` `3.8.3`
- `Microsoft.Windows.CsWinRT` `2.2.0` only if concrete WinRT/native interop requires it

Latest technical check on 2026-04-21:

- Microsoft Learn lists Windows App SDK stable `1.8.6 (1.8.260317003)`, released 2026-03-18, with 2.0 preview/experimental releases available but not selected for MVP. [Source: `https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads`]
- NuGet lists `Vortice.Direct3D11` `3.8.3` and `Vortice.DXGI` `3.8.3`; both expose `net10.0` compatibility. [Source: `https://www.nuget.org/packages/Vortice.Direct3D11/`; `https://www.nuget.org/packages/Vortice.DXGI/`]
- NuGet lists `Microsoft.Windows.CsWinRT` stable `2.2.0`, with newer prerelease versions available; stay on stable unless a documented blocker requires otherwise. [Source: `https://www.nuget.org/packages/Microsoft.Windows.CsWinRT/`]

### File Structure Requirements

Expected files after implementation, adjusted only if Story 1.1 established a different but equivalent convention:

```text
src/
  Lumiere.Graphics/
    Hdr/
      HdrConstants.cs
      PreviewReadinessStatus.cs
      PreviewReadinessState.cs
tests/
  Lumiere.Graphics.Tests/
    Hdr/
      HdrConstantsTests.cs
      PreviewReadinessStatusTests.cs
```

Use one primary type per file. Avoid generic names like `Helpers.cs` or `Constants.cs`; use names that communicate HDR responsibility. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#File Structure Patterns`]

### UX Requirements Relevant to This Story

This story does not implement visible overlay UX, but the status model must support the future UX:

- The happy path can show a compact readiness badge.
- `Degraded` must never look equivalent to `Ready`.
- Basic user-facing status and advanced diagnostics must remain separate layers.
- Plain-language status text must identify readiness, degraded, unsupported, or failed states.
- Loading UI must not imply HDR readiness before validation completes.

[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Power User Diagnoses HDR Capability`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Loading States`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Core Accessibility Requirements`]

### Testing Requirements

Tests are mandatory for this story because NFR4 requires HDR-related constants and configuration to be testable and centrally verifiable. The tests should be narrow, deterministic, and not pretend to validate real HDR hardware.

Minimum test intent:

- Constants equal the approved WGC/DXGI values.
- Constants are strongly typed and not duplicated as loose strings or integers.
- A default/unvalidated readiness status is not `Ready`.
- Degraded and unsupported statuses can carry stage, user message, and technical detail needed by diagnostics.

Do not write fake D3D11 device, swap-chain, WGC frame, or monitor capability tests here. Real capture/render validation belongs to later stories and manual HDR validation. [Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#NFR4`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Testing Rules`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Test Organization`]

### Anti-Patterns to Avoid

- Do not hardcode HDR format values separately in capture, graphics, overlay, and app shell code.
- Do not represent core HDR constants as strings, magic numbers, JSON/app settings, or user preferences.
- Do not introduce an SDR fallback value beside the primary constants as if it were equivalent.
- Do not mark readiness `Ready` before actual capture, graphics, and presentation validation can prove it.
- Do not render preview through `BitmapImage`, `SoftwareBitmap`, GDI, or ordinary XAML image controls.
- Do not implement WGC frame pool creation, D3D11 device creation, DXGI swap-chain attachment, export, clipboard, hotkey, tray, annotation, or history in this story.

### Previous Story Intelligence

Previous story file: `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-1-scaffold-the-native-windows-app-foundation.md`.

Key learnings to carry forward:

- Story 1.1 must establish Git/repository workflow, solution files, central package management, source project boundaries, and x64/.NET target settings before this story edits production code.
- The approved module boundaries are `Lumiere.App`, `Lumiere.Overlay`, `Lumiere.Capture`, `Lumiere.Graphics`, `Lumiere.Infrastructure`, and `Lumiere.Settings`.
- Package pinning and target framework decisions should live in project files/central props, not only in documentation.
- No Git repository is detected at story creation time; do not rely on commit history for implementation patterns until Story 1.1 creates it.

[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-1-scaffold-the-native-windows-app-foundation.md`]

### Git Intelligence

No Git repository is detected at `/Users/asherliao/Projects/lumiere` as of story creation, so there are no recent commits to analyze. Treat planning artifacts, project context, and Story 1.1 as the source of truth until the repository workflow exists.

### Project Context Reference

Before implementing, read `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md`. Its highest-priority rules for this story are:

- Centralize HDR constants so format/color-space requirements are not duplicated or weakened.
- Preserve FP16/scRGB semantics: `DirectXPixelFormat.R16G16B16A16Float`, `DXGI_FORMAT_R16G16B16A16_FLOAT`, and `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
- Keep UI, capture, graphics, infrastructure, and diagnostics responsibilities separated.
- Prefer HDR correctness over convenience and fail visibly when correctness cannot be established.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-04-21: Loaded BMad configuration, sprint status, Story 1.2, and project context; selected Story 1.2 as the first `ready-for-dev` story in sprint order.
- 2026-04-21: Updated Story 1.2 and sprint status from `ready-for-dev` to `in-progress`.
- 2026-04-21: Verified Story 1.1 scaffold prerequisites: `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, and `src/Lumiere.Graphics/` exist; target framework is `net10.0-windows10.0.19041.0`; platform/runtime are `x64`/`win-x64`; package pins include Windows App SDK `1.8.260317003`, `Vortice.Direct3D11` `3.8.3`, and `Vortice.DXGI` `3.8.3`.
- 2026-04-21: Added HDR constants and preview readiness model under `src/Lumiere.Graphics/Hdr/`.
- 2026-04-21: Added `tests/Lumiere.Graphics.Tests` with xUnit tests for HDR constants and readiness state behavior.
- 2026-04-21: XML parsing for `Directory.Packages.props` and `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj` passed.
- 2026-04-21: Attempted `dotnet format Lumiere.sln`, `dotnet restore Lumiere.sln`, `dotnet build Lumiere.sln -p:Platform=x64`, and `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64`; all failed because this machine has no .NET SDK installed, only runtimes.
- 2026-04-21: Installed .NET SDK `10.0.202` with `winget` and verified `dotnet --list-sdks`.
- 2026-04-21: Ran `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`; restore passed.
- 2026-04-21: Ran `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`; build passed with 0 warnings and 0 errors.
- 2026-04-21: Ran `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`; 7 tests passed.
- 2026-04-21: Ran `dotnet format Lumiere.sln`, then `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`; format check passed.

### Completion Notes List

- Scaffold prerequisite verification is complete.
- HDR constants, typed readiness states, readiness stages, and readiness status factories have been implemented in the graphics boundary.
- Guardrail tests have been added and pass.
- HDR constants/readiness semantics were intentionally touched: `HdrConstants` now centralizes WGC FP16 format, DXGI FP16 swap-chain format, and DXGI scRGB color space; `PreviewReadinessStatus` now models initializing, ready, degraded, unsupported, and failed states with stage and detail fields.
- Story is ready for code review.

### File List

- `Directory.Packages.props`
- `Lumiere.sln`
- `_bmad-output/implementation-artifacts/1-2-centralize-hdr-constants-and-preview-readiness-status.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `src/Lumiere.Graphics/Hdr/HdrConstants.cs`
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStage.cs`
- `src/Lumiere.Graphics/Hdr/PreviewReadinessState.cs`
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs`
- `tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj`
- `tests/Lumiere.Graphics.Tests/Hdr/HdrConstantsTests.cs`
- `tests/Lumiere.Graphics.Tests/Hdr/PreviewReadinessStatusTests.cs`

### Change Log

- 2026-04-21: Started Story 1.2, verified scaffold prerequisites, added HDR constants/readiness implementation and tests; validation was initially blocked by missing .NET SDK.
- 2026-04-21: Installed .NET SDK `10.0.202`, completed restore/build/format/test validation, and moved Story 1.2 to review.

### Review Findings

- [x] [Review][Patch] `PreviewReadinessStatus` can still be constructed as `Ready` without validation [src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs:3]
