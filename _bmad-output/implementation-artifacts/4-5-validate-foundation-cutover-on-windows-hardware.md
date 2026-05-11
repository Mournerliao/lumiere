# Story 4.5: Validate Foundation Cutover on Windows Hardware

Status: ready-for-dev

## Story

As a Lumiere developer,
I want the retained foundation validated under the rebaselined MVP path,
so that UI and output work does not build on unverified direct capture, overlay, or lifecycle assumptions.

## Acceptance Criteria

1. **Given** direct monitor capture, overlay crop, release-to-capture, and basic clipboard output are retained, **when** Windows manual validation runs, **then** results are recorded for no-picker capture, overlay placement, valid crop release, invalid crop recovery, Escape cancel, clipboard attempt, repeated lifecycle, multi-monitor, HDR/SDR displays, and common DPI scales.

2. **Given** validation cannot be completed for a scenario, **when** the story is closed, **then** the gap is recorded with validation level and carried into Epic 8 rather than hidden.

3. **Given** automated gates are run, **when** they complete, **then** restore, build, relevant tests, and format verification outcomes are recorded separately from Windows manual validation.

## Tasks / Subtasks

- [ ] **Task 1: Run automated quality gates on Windows** (AC: 3)
  - [ ] Run `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`
  - [ ] Run `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
  - [ ] Run `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
  - [ ] Run `dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
  - [ ] Run `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`
  - [ ] Record all outcomes in the validation report section below

- [ ] **Task 2: Validate direct monitor capture without picker** (AC: 1)
  - [ ] Launch Lumiere on Windows x64 with Windows App SDK runtime
  - [ ] Trigger fullscreen capture from main window — confirm no picker appears
  - [ ] Trigger region capture from main window — confirm no picker appears before overlay
  - [ ] Confirm direct monitor target is resolved automatically
  - [ ] Record pass/fail/validation level

- [ ] **Task 3: Validate overlay placement and preview** (AC: 1)
  - [ ] Confirm overlay opens as borderless topmost window on the intended monitor
  - [ ] Confirm hardware preview is attached through `SwapChainPanel` and fills the overlay surface
  - [ ] Confirm status/control layer appears above preview without resizing or shifting preview
  - [ ] Repeat on HDR display, SDR display, and multi-monitor placement
  - [ ] Record pass/fail/validation level per display configuration

- [ ] **Task 4: Valid crop release to capture** (AC: 1)
  - [ ] Drag a valid crop region over the preview
  - [ ] Release pointer — confirm crop is confirmed without clicking a Confirm button
  - [ ] Verify lightweight "Copied to clipboard" feedback appears in closing state
  - [ ] Confirm overlay closes and capture resources are torn down
  - [ ] Record pass/fail/validation level

- [ ] **Task 5: Invalid crop recovery** (AC: 1)
  - [ ] Drag a tiny or near-zero crop region
  - [ ] Confirm no output is produced
  - [ ] Confirm overlay remains active and user can retry selection
  - [ ] Confirm no capture resources are leaked
  - [ ] Record pass/fail/validation level

- [ ] **Task 6: Escape cancel** (AC: 1)
  - [ ] Open overlay and press Escape before creating a crop
  - [ ] Confirm overlay closes and capture/preview resources are torn down
  - [ ] Open overlay, create a crop, then press Escape
  - [ ] Confirm overlay closes without producing output
  - [ ] Confirm no stranded overlay or active WGC resources remain
  - [ ] Record pass/fail/validation level

- [ ] **Task 7: Basic clipboard output attempt** (AC: 1)
  - [ ] Complete a valid region capture
  - [ ] Confirm clipboard write is attempted (image available in clipboard)
  - [ ] If clipboard write fails, confirm structured diagnostic is logged and overlay still closes
  - [ ] Record pass/fail/validation level
  - [ ] Note: clipboard output is basic bitmap usability only — NOT HDR-preserving

- [ ] **Task 8: Repeated lifecycle validation** (AC: 1)
  - [ ] Follow the lifecycle validation checklist in `docs/validation/lifecycle-validation.md`
  - [ ] Run repeated start, stop, cancel, restart, release-to-output loop (at least 5 cycles)
  - [ ] Confirm each teardown completes fully (frame handler unsubscribe, session stop/dispose, frame pool dispose, preview detach, swap-chain release)
  - [ ] Confirm stale callbacks from previous generations do not update UI
  - [ ] Confirm shared graphics device is NOT disposed during ordinary stop/restart
  - [ ] Monitor for resource growth (private bytes, handles, GPU allocator)
  - [ ] Record pass/fail/validation level

- [ ] **Task 9: Multi-monitor behavior** (AC: 1)
  - [ ] Test with 2+ monitors if available
  - [ ] Confirm overlay appears on the correct/target monitor
  - [ ] Confirm crop coordinates map correctly per monitor
  - [ ] Record pass/fail/validation level or "gap: single monitor only"

- [ ] **Task 10: DPI scaling** (AC: 1)
  - [ ] Test with Windows display scaling at 100%, 125%, 150%, and 200%
  - [ ] Confirm overlay boundaries, crop handles, and status text remain stable
  - [ ] Confirm crop coordinate mapping is correct at each scale
  - [ ] Record pass/fail/validation level per scale

- [ ] **Task 11: HDR/SDR display behavior** (AC: 1)
  - [ ] Test on HDR-capable display if available
  - [ ] Confirm FP16/scRGB preview path is preserved (WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_Float`, scRGB color space)
  - [ ] Test on SDR display
  - [ ] Confirm app does not crash or show misleading HDR-ready state on SDR
  - [ ] Record pass/fail/validation level or "gap: HDR display not available"

- [ ] **Task 12: Record validation gaps for Epic 8** (AC: 2)
  - [ ] Review all validation results
  - [ ] Identify any scenarios that could not be completed or failed
  - [ ] Document each gap with: scenario, validation level, reason, and Epic 8 dependency
  - [ ] Ensure no gap is hidden or silently marked complete

- [ ] **Task 13: Produce final validation report** (AC: 1, 2, 3)
  - [ ] Compile all results into the Validation Report section below
  - [ ] Separate automated gate results from Windows manual validation results
  - [ ] Mark story as done only when all recordable scenarios have explicit pass/fail/gap status

## Dev Notes

### Story Scope

This is a **validation story**, not an implementation story. The primary output is a **validation report** documenting Windows hardware behavior for the foundation cutover completed in Stories 4.1–4.4.

This story does NOT:
- Implement new features or change application code
- Add new UI components or modify existing ones
- Introduce new dependencies or architectural changes

This story DOES:
- Run automated quality gates on Windows and record outcomes
- Perform manual Windows hardware validation for direct capture, overlay, crop, clipboard, lifecycle, multi-monitor, DPI, and HDR/SDR scenarios
- Record validation levels (Mac edit, Windows CI-pass, Windows manual-pass) for each scenario
- Document gaps that cannot be validated and carry them into Epic 8
- Produce evidence that the MVP foundation is verified before UI and output work build on it

### Why This Story Exists

Epic 4 Stories 4.1–4.4 made code changes: cutover classification, MVP session contract, legacy picker demotion, and app-facing seams. Those stories were validated through Mac edit and Windows CI-pass (automated tests). However, several critical behaviors **cannot be verified from code alone**:

- Real WGC frame pool and DXGI swap-chain behavior
- Direct monitor capture without picker on actual hardware
- Overlay topmost placement across HDR/SDR and multi-monitor
- Crop coordinate mapping at different DPI scales
- Clipboard output through Windows clipboard API
- Repeated capture lifecycle resource stability
- GPU memory and handle trends across sessions

This story exists because `NFR27` and `NFR33` require Windows manual validation for these behaviors. Without this validation, later stories (Epic 5: main window UI, Epic 6: output, Epic 7: tray/hotkeys) would build on unverified assumptions.

### Previous Story Intelligence

From Story 4.4 (establish app-facing seams):
- **Key interfaces created**: `ICaptureCommandCoordinator`, `IOutputService`, `ISettingsProvider`
- **Refactored `MainWindow.xaml.cs`**: now accepts services via constructor injection
- **Wired in `App.xaml.cs`**: manual composition, no DI container
- **17 unit tests added**: 8 for `CaptureCommandCoordinator`, 9 for `DefaultSettingsProvider`
- **Task 12 blocked**: `dotnet` SDK not available on macOS — Windows CI validation required
- **Review findings**: all resolved — TOCTOU guard preserved, COM/DXGI disposal in App noted, cancellation token checks added, duplicate `OutputTarget` enum removed
- **Learnings**:
  - `sessionState` ownership stays in `CaptureService`, not `MainWindow`
  - `ApplySessionState` must be called on UI thread (DispatcherQueue)
  - `Disposed` status should reject commands
  - Place capture tests in `tests/Lumiere.Graphics.Tests/Capture/`
  - Follow existing naming patterns (`CaptureStartResult`, `CaptureTargetSelectionResult`)

From Story 4.3 (demote legacy picker and dashboard):
- Dashboard-era resource keys renamed to neutral names
- `MainWindow.xaml` restructured to single-column compact layout
- Direct monitor capture verified as default path
- All 147 Graphics tests + 79 Overlay tests passing
- `CaptureService` remains concrete sealed class; `CaptureCommandCoordinator` wraps it

From Story 4.2 (MVP session contract):
- `CaptureCommand` and `CaptureCommandResult` types established
- TOCTOU race addressed via `TryReserveCommand`
- Session guard prevents conflicting capture sessions

### Architecture Compliance

**Module Boundaries (do NOT modify):**
- `Lumiere.Capture`: capture session lifecycle, command coordination
- `Lumiere.Graphics`: D3D11/DXGI resources, HDR constants, swap-chain, clipboard output
- `Lumiere.Infrastructure`: COM/WinRT/Win32 interop, diagnostics
- `Lumiere.Overlay`: fullscreen overlay, crop UI, pointer/keyboard interaction
- `Lumiere.Settings`: local preferences (currently stub with `DefaultSettingsProvider`)
- `Lumiere.App`: startup, composition, window orchestration

**Key Architecture Rules from [Source: architecture.md]:**
- "Preserve FP16/scRGB constants and never introduce SDR/bitmap preview fallback into the main preview path"
- "Keep platform/native APIs inside their boundary module"
- "Reuse existing typed state/result models before adding new ones"
- "Label validation accurately as Mac edit, Windows CI-pass, or Windows manual-pass"
- "Ordinary stop or restart of capture SHALL NOT dispose the shared graphics device unless app shutdown or documented device-loss recovery is in progress"
- "Preview teardown SHALL detach presentation from the UI surface before releasing DXGI swap-chain resources"

**HDR Invariants (must not be violated during validation):**
- WGC frame pool: `R16G16B16A16Float`
- DXGI swap-chain: `R16G16B16A16_FLOAT`
- Color space: scRGB `RgbFullG10NoneP709`
- Preview: GPU-resident, no `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU readback, SDR fallback

### Validation Infrastructure

**Existing validation docs (read and follow):**
- `docs/validation/lifecycle-validation.md` — Lifecycle validation checklist with required scenarios, inspection points, and repeated sequence loop
- `docs/validation/overlay-validation.md` — Overlay validation with automated and manual checks for Stories 3.1–3.6

**Existing test counts (should pass before manual validation):**
- `Lumiere.Graphics.Tests`: ~155 tests (capture, HDR constants, readiness, lifecycle, coordinator, settings)
- `Lumiere.Overlay.Tests`: ~79 tests (crop, overlay state, release-to-capture)

**Key types to verify during validation:**
- `CaptureSessionState` — lifecycle states: Idle, SelectingTarget, Initializing, Capturing, Degraded, Unsupported, Failed, Disposed
- `CaptureCommand` / `CaptureCommandResult` — command routing from Story 4.2
- `ICaptureCommandCoordinator` — shared entry point from Story 4.4
- `IOutputService` / `OutputResult` — output abstraction from Story 4.4
- `ISettingsProvider` — settings surface from Story 4.4 (currently stub with defaults)
- `CropPixelRect` — crop region type
- `CapturedFrameTexture` — frame texture type
- `PreviewReadinessStatus` — HDR readiness evidence

### Validation Level Definitions

- **Mac edit**: Code was written/edited on macOS. No build or runtime verification.
- **Windows CI-pass**: `dotnet restore`, `dotnet build`, `dotnet test`, `dotnet format` all pass on Windows.
- **Windows manual-pass**: The scenario was executed on Windows hardware with real WGC, DXGI, D3D11, and display behavior observed. Results recorded.

### Anti-Patterns

- **DO NOT** claim Windows manual-pass for scenarios that were not actually run on hardware
- **DO NOT** mark a scenario as "pass" if only automated tests ran — that's Windows CI-pass at best
- **DO NOT** hide validation gaps — document them and carry to Epic 8
- **DO NOT** modify application code during validation — this is a validation-only story
- **DO NOT** introduce `BitmapImage`, `SoftwareBitmap`, GDI, WIC, or CPU readback during testing
- **DO NOT** dispose shared `GraphicsDeviceResources` during ordinary stop/restart testing
- **DO NOT** skip the repeated lifecycle loop — it's the primary resource-leak detection mechanism
- **DO NOT** record pixel data, frame dumps, or captured screen content in validation logs

### Testing Requirements

**Automated Gates (Windows CI-pass):**
```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Overlay.Tests/Lumiere.Overlay.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

**Manual Validation (Windows manual-pass):**
- Follow `docs/validation/lifecycle-validation.md` for lifecycle scenarios
- Follow `docs/validation/overlay-validation.md` for overlay scenarios
- Record each scenario with: pass/fail, validation level, display configuration, DPI scale, notes

### File Structure Notes

**Files to validate (read-only — do NOT modify):**
- `src/Lumiere.App/App.xaml.cs` — Service wiring and MainWindow constructor injection
- `src/Lumiere.App/MainWindow.xaml.cs` — Capture orchestration, UI-thread marshalling
- `src/Lumiere.Capture/CaptureService.cs` — Core capture service with `TryReserveCommand()`
- `src/Lumiere.Capture/CaptureCommandCoordinator.cs` — Command coordinator from Story 4.4
- `src/Lumiere.Graphics/Clipboard/ClipboardOutputService.cs` — Clipboard output implementing `IOutputService`
- `src/Lumiere.Graphics/Presentation/` — Swap-chain preview and frame presentation
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` — Overlay lifecycle
- `src/Lumiere.Overlay/Crop/` — Crop geometry and coordinate mapping
- `src/Lumiere.Settings/DefaultSettingsProvider.cs` — Stub settings with MVP defaults

**Validation docs to update:**
- `docs/validation/lifecycle-validation.md` — Add any new findings
- `docs/validation/overlay-validation.md` — Add any new findings

**Story output file:**
- This file (`4-5-validate-foundation-cutover-on-windows-hardware.md`) — append validation results

### Git Intelligence

Recent commits:
- `0d9e498` feat: Epic 4 foundation cutover and capture command refactoring
- `2f404f1` fix: reset capture session state to Idle after overlay dismissal
- `abdcecf` docs: complete Epic 3 retrospective and add Stories 4.6-4.7 to Epic 4
- `a07bba3` docs: rebaseline BMad MVP planning artifacts
- `3892d0b` feat: introduce structured logging system with Microsoft.Extensions.Logging

The most recent commit (`0d9e498`) includes the Epic 4 foundation cutover work from Stories 4.1–4.4. This validation story verifies that commit's changes work correctly on Windows hardware.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story 4.5] — Story definition and acceptance criteria
- [Source: _bmad-output/planning-artifacts/architecture.md#Core Architectural Decisions] — Architecture patterns
- [Source: _bmad-output/planning-artifacts/architecture.md#Enforcement Guidelines] — Validation requirements
- [Source: _bmad-output/project-context.md#Development Workflow Rules] — Validation level definitions
- [Source: _bmad-output/project-context.md#Critical Don't-Miss Rules] — HDR invariant and teardown rules
- [Source: docs/validation/lifecycle-validation.md] — Lifecycle validation checklist
- [Source: docs/validation/overlay-validation.md] — Overlay validation checklist
- [Source: _bmad-output/implementation-artifacts/4-4-establish-app-facing-seams-for-settings-output-tray-and-hotkeys.md] — Previous story with blocked Task 12
- [Source: _bmad-output/implementation-artifacts/4-3-demote-legacy-picker-and-dashboard-behavior-from-the-default-path.md] — Previous story with UI restructuring
- [Source: _bmad-output/implementation-artifacts/4-2-cut-over-capture-commands-to-the-mvp-session-contract.md] — Previous story with session contract

## Validation Report

_This section is populated after validation tasks are completed._

### Automated Gate Results (Windows CI-pass)

| Gate | Result | Notes |
|------|--------|-------|
| `dotnet restore` | _pending_ | |
| `dotnet build` | _pending_ | |
| `dotnet test Lumiere.Graphics.Tests` | _pending_ | |
| `dotnet test Lumiere.Overlay.Tests` | _pending_ | |
| `dotnet format --verify-no-changes` | _pending_ | |

### Windows Manual Validation Results

| Scenario | Result | Validation Level | Display Config | DPI | Notes |
|----------|--------|-----------------|----------------|-----|-------|
| No-picker direct capture | _pending_ | | | | |
| Overlay placement | _pending_ | | | | |
| Valid crop release | _pending_ | | | | |
| Invalid crop recovery | _pending_ | | | | |
| Escape cancel (no crop) | _pending_ | | | | |
| Escape cancel (with crop) | _pending_ | | | | |
| Clipboard output attempt | _pending_ | | | | |
| Repeated lifecycle (5+ cycles) | _pending_ | | | | |
| Multi-monitor | _pending_ | | | | |
| DPI 100% | _pending_ | | | | |
| DPI 125% | _pending_ | | | | |
| DPI 150% | _pending_ | | | | |
| DPI 200% | _pending_ | | | | |
| HDR display | _pending_ | | | | |
| SDR display | _pending_ | | | | |

### Validation Gaps Carried to Epic 8

_Document any scenarios that could not be completed, with reason and Epic 8 dependency._

| Gap | Reason | Epic 8 Story |
|-----|--------|--------------|
| (none yet) | | |

## Dev Agent Record

### Agent Model Used

_This section is populated by the dev agent during implementation._

### Debug Log References

### Completion Notes List

### File List
