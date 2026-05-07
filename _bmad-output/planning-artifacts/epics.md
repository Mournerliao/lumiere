---
workflowType: epics-and-stories
project_name: lumiere
user_name: Asherliao
date: 2026-05-07
status: rebaselined
rebaseline:
  source:
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-mvp-direct-capture.md
    - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md
    - /Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png
  approved_by_user: 2026-05-07
---

# Lumiere - Rebaselined Epic Breakdown

## Overview

This document is the canonical epic and story breakdown after the 2026-05-07 MVP-to-1.0 rebaseline.

The previous plan mixed MVP, post-MVP settings, output, diagnostics, tray, hotkey, annotation, and installer work in a way that made BMad phase completion ambiguous. The current plan intentionally keeps only the revised MVP and installer-to-1.0 route in the active epic list.

## Completion Semantics

- **Epic 1-4 done** means MVP feature implementation is complete.
- **Epic 5 done** means MVP is complete and validated.
- **Epic 6 done** means the 1.0 installable release is complete.

## Required MVP Design Input

All MVP implementation stories must use this design asset as the visual and interaction reference:

- `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`

The design board defines the intended MVP interaction model: compact main capture entry, direct full-screen region selection, release-to-capture/copy, lightweight copied-to-clipboard feedback, and no multi-action toolbar in the capture overlay. Settings, tray, and broader output surfaces shown in the design remain product direction only unless a story explicitly includes them.

## Active Epic List

### Epic 1: HDR Preview Foundation

Users and developers can prove that Lumiere's core HDR capture and preview foundation works: a WGC FP16 frame reaches a Direct3D/DXGI scRGB swap-chain preview without SDR bitmap fallback, and the app can visibly state whether the preview is HDR-ready.

**Status:** Done.

**Stories:**

- 1.1 Scaffold the Native Windows App Foundation - done
- 1.2 Centralize HDR Constants and Preview Readiness Status - done
- 1.3 Create D3D11 Device and WinRT/DXGI Interop Bridge - done
- 1.4 Attach an FP16 scRGB Swap Chain to SwapChainPanel - done
- 1.5 Prove Minimal WGC FP16 Capture to Live Preview - done

### Epic 2: Direct Capture Session Lifecycle

Users can start a capture session from the app, avoid picker-first target selection on the default MVP path, and trust the app to start, stop, restart, resize, and tear down capture resources without stale frames or resource leaks.

**Status:** In progress.

**Stories:**

- 2.1 Start Capture and Select a Display or Window Target - done
- 2.2 Represent Capture Session State Explicitly - done
- 2.3 Stop, Restart, and Recreate Capture Resources - done
- 2.4 Validate Repeated Capture Lifecycle Stability - done
- 2.5 Create Monitor Capture Targets Without Picker - backlog

### Epic 3: Release-to-Copy Overlay Workflow

Users can interact with a full-screen capture overlay, create and adjust a crop selection over the HDR preview, release to complete the MVP capture flow, or press Escape to cancel and return safely to the desktop.

**Status:** In progress.

**Stories:**

- 3.1 Show a Fullscreen Overlay Above the HDR Preview - done
- 3.2 Create a Crop Selection by Dragging - done
- 3.3 Adjust or Recreate the Crop Selection - done
- 3.4 Confirm or Cancel the Capture Overlay - done
- 3.5 Manage Overlay Hit Testing and Keyboard Escape - done
- 3.6 Release to Capture and Copy - backlog

### Epic 4: MVP Output, Status, and Validation

Users can complete the revised MVP flow with a usable clipboard result, concise status/feedback, and documented Windows validation evidence without pulling in settings, tray, hotkey, annotation, or advanced export scope.

**Status:** Backlog.

**Stories:**

- 4.1 Show User-Facing Capture and Preview Status - backlog
- 4.2 Define and Implement MVP Clipboard Output - backlog
- 4.3 Document MVP Manual Validation Scenarios - backlog

### Epic 5: MVP Completion Gate

The project can explicitly determine when the MVP is complete, validated, and ready to move into installer and release work.

**Status:** Backlog.

**Stories:**

- 5.1 Define MVP Completion Gate - backlog
- 5.2 Run MVP Completion Validation and Triage - backlog
- 5.3 Complete MVP Retrospective and Go/No-Go - backlog

### Epic 6: Installer and 1.0 Release

Users can install Lumiere on Windows, launch the validated MVP reliably, and receive a clearly versioned 1.0 release package.

**Status:** Backlog until Epic 5 is done.

**Stories:**

- 6.1 Decide Packaging Strategy - backlog
- 6.2 Build Installer Package - backlog
- 6.3 Validate Install, Launch, and Uninstall - backlog
- 6.4 Prepare 1.0 Versioning and Release Notes - backlog
- 6.5 Cut 1.0 Release - backlog

## Story Details

## Epic 1: HDR Preview Foundation

### Story 1.1: Scaffold the Native Windows App Foundation

As a developer,
I want the Lumiere solution scaffolded with the approved WinUI 3 and .NET foundation,
So that all future HDR capture work starts from the correct native Windows runtime and project boundaries.

**Acceptance Criteria:**

- Given a clean repository workspace, when repository foundation work begins, then Git, `.gitignore`, `.editorconfig`, formatting configuration, README, and documented workflow conventions exist.
- Given the solution is created, when a developer inspects it, then it contains the approved source projects, `Lumiere.sln`, `Directory.Build.props`, `Directory.Packages.props`, `net10.0-windows10.0.19041.0`, and x64 configuration.
- Given package configuration is restored, when dependencies are inspected, then Windows App SDK and Vortice versions are pinned as architecture-approved versions.

### Story 1.2: Centralize HDR Constants and Preview Readiness Status

As a developer,
I want HDR formats and readiness states centralized,
So that implementation agents cannot accidentally weaken the capture or preview path.

**Acceptance Criteria:**

- Given `HdrConstants` exists, when tests inspect it, then WGC pixel format, DXGI swap-chain format, and DXGI color space match FP16/scRGB requirements.
- Given readiness cannot be established, when status is reported, then the app uses typed `Initializing`, `Ready`, `Degraded`, `Unsupported`, or `Failed` states.

### Story 1.3: Create D3D11 Device and WinRT/DXGI Interop Bridge

As a developer,
I want a narrow interop bridge for Direct3D, DXGI, WinRT, and COM objects,
So that capture and rendering code can share GPU resources without leaking native details into UI code.

**Acceptance Criteria:**

- Given graphics infrastructure initializes, when the D3D11 provider is created, then it creates device/context resources suitable for WGC and DXGI swap-chain rendering.
- Given WGC requires a WinRT Direct3D device, when the interop bridge wraps the DXGI device, then it returns a WinRT-compatible Direct3D device through a narrow infrastructure API.
- Given interop calls fail, when failures are mapped, then diagnostics include operation name, stage, and technical detail.

### Story 1.4: Attach an FP16 scRGB Swap Chain to SwapChainPanel

As an HDR screenshot user,
I want the preview surface to be hardware-rendered through an HDR-capable swap chain,
So that the app can preserve HDR appearance instead of showing a washed-out bitmap preview.

**Acceptance Criteria:**

- Given a `SwapChainPanel` is available on the UI thread, when the swap chain is attached, then it uses `DXGI_FORMAT_R16G16B16A16_FLOAT`.
- Given color space is configured, when configuration fails, then a visible degraded/failed readiness result is produced.
- Given graphics teardown begins, when resources are disposed, then `SetSwapChain(null)` runs on the UI thread before device-bound resources are released.

### Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview

As an HDR display user,
I want a minimal live preview that preserves the source display appearance,
So that Lumiere's core product promise is proven before broader workflow features are built.

**Acceptance Criteria:**

- Given capture starts, when WGC frame pool is created, then it uses `DirectXPixelFormat.R16G16B16A16Float`.
- Given a frame arrives, when rendered, then the routine live preview remains GPU-resident and avoids `BitmapImage`, `SoftwareBitmap`, GDI, WIC, or CPU readback.
- Given preview is running, when readiness is shown, then the user can see whether preview is HDR-ready, degraded, unsupported, or failed.

## Epic 2: Direct Capture Session Lifecycle

### Story 2.1: Start Capture and Select a Display or Window Target

As a screenshot user,
I want to start capture and choose a display or window when using picker fallback/debug workflows,
So that the app retains a Windows-supported explicit target-selection path.

**Acceptance Criteria:**

- Given explicit target selection is invoked, when the Windows picker returns a target, then the app creates a typed `CaptureTarget`.
- Given the user cancels the picker, when cancellation is received, then no capture session starts and the app returns to a recoverable idle state.
- Given the picker returns unsupported or failed state, when the app maps the result, then a typed readiness state is produced.

### Story 2.2: Represent Capture Session State Explicitly

As a user,
I want the app to distinguish normal, degraded, unsupported, and failed capture states,
So that I am not misled about whether the preview can be trusted.

**Acceptance Criteria:**

- Given capture is initialized, when capability checks pass, then session state moves to normal capturing state.
- Given capture is unavailable, when initialization fails, then session enters `Unsupported` or `Failed` with a concise reason.
- Given HDR correctness cannot be proven, when validation fails, then session enters `Degraded` rather than silently presenting SDR as valid.

### Story 2.3: Stop, Restart, and Recreate Capture Resources

As a user,
I want capture sessions to stop and restart without restarting the app,
So that I can recover from cancellation, target changes, or display size changes.

**Acceptance Criteria:**

- Given an active session, when stop/cancel occurs, then WGC session, frame pool, frames, and related resources are disposed deterministically.
- Given target size changes, when the frame size no longer matches preview resources, then capture and preview resources are recreated safely.
- Given capture restarts, when a new target is selected, then stale frames or invalid surfaces from the previous session are not reused.

### Story 2.4: Validate Repeated Capture Lifecycle Stability

As a developer,
I want repeatable lifecycle validation for capture start, stop, cancel, and restart,
So that graphics and capture resources do not leak across sessions.

**Acceptance Criteria:**

- Given lifecycle tests or manual diagnostics run, when capture starts/stops repeatedly, then session state returns to idle or recoverable failure without app restart.
- Given graphics teardown occurs, when resources are released, then preview presentation is detached before device-bound resources are disposed.
- Given repeated sessions are exercised, when diagnostics are inspected, then there is no evidence of unbounded frame pool, texture, render target, swap-chain, or device resource growth.

### Story 2.5: Create Monitor Capture Targets Without Picker

As a screenshot user,
I want Capture to enter region selection directly,
So that I can screenshot whatever is currently visible without first choosing a window or display.

**Acceptance Criteria:**

- Given the user clicks Capture, when the default MVP path starts, then no `GraphicsCapturePicker` UI appears.
- Given the pointer or capture-start context maps to a monitor, when direct capture starts, then Lumiere creates a `GraphicsCaptureItem` for that `HMONITOR` through a narrow infrastructure interop API.
- Given monitor target creation fails or is unsupported, when the default path cannot continue, then Lumiere reports a recoverable unsupported/failed status and may offer picker fallback outside the default MVP path.
- Given the target is created through monitor interop, when `CaptureTarget` is created, then its kind is `Display` and its size/display name are validated before WGC frame-pool startup.
- Given the implementation adds native monitor interop, when files are organized, then HMONITOR/COM/Win32 details remain inside `Lumiere.Infrastructure` and only narrow typed APIs are exposed.
- Given MVP design is consulted, when the story is implemented, then the flow aligns with `/Users/asherliao/Projects/lumiere/harness/design/mvp/lumiere-mvp-design.png`.

## Epic 3: Release-to-Copy Overlay Workflow

### Story 3.1: Show a Fullscreen Overlay Above the HDR Preview

As a screenshot user,
I want a fullscreen overlay that contains the live preview and capture controls,
So that I can select a region without leaving the capture flow.

**Acceptance Criteria:**

- Given capture preview is available, when overlay opens, then the `SwapChainPanel` fills the preview surface.
- Given overlay is visible, when UI controls render, then crop canvas and status/control layer appear above the hardware preview.
- Given overlay initialization fails, when failure is detected, then no unusable topmost window remains.

### Story 3.2: Create a Crop Selection by Dragging

As a screenshot user,
I want to drag over the preview to create a crop selection,
So that I can choose the exact region I care about.

**Acceptance Criteria:**

- Given the overlay is in selection mode, when the user presses and drags, then a crop rectangle is created from drag start and current pointer.
- Given the crop rectangle is active, when dragging continues, then active region and non-selected area remain visually clear.
- Given coordinates leave expected bounds, when crop geometry is computed, then crop is clamped to the preview area.

### Story 3.3: Adjust or Recreate the Crop Selection

As a screenshot user,
I want to adjust or recreate my crop selection,
So that small selection mistakes do not force me to restart capture.

**Acceptance Criteria:**

- Given a crop selection exists, when the user drags a handle or edge, then the crop rectangle updates without shifting preview layout.
- Given a crop selection exists, when the user starts a new selection gesture according to interaction rules, then the previous crop is replaced by the new crop.
- Given crop changes, when coordinates are mapped, then device-independent UI coordinates convert consistently to capture-pixel coordinates.

### Story 3.4: Confirm or Cancel the Capture Overlay

As a screenshot user,
I want confirm/cancel semantics available as an internal or fallback workflow,
So that the app has a typed confirmed selection contract and reliable cancellation.

**Acceptance Criteria:**

- Given a valid crop exists, when confirmation is invoked, then the selected crop region is captured as a typed confirmed selection state.
- Given the overlay is open, when cancel is invoked, then capture/preview resources begin teardown and desktop state is restored.
- Given confirm/cancel is invoked during degraded state, when operation completes, then the user receives appropriate status and overlay does not remain stuck.

### Story 3.5: Manage Overlay Hit Testing and Keyboard Escape

As a screenshot user,
I want overlay input to work reliably,
So that transparency or topmost behavior does not prevent crop interaction or cancellation.

**Acceptance Criteria:**

- Given overlay uses transparent/borderless behavior, when the user interacts with crop canvas, then hit testing routes input to crop controls.
- Given overlay is active, when user presses Escape, then capture flow exits safely.
- Given controls are visible, when keyboard navigation is used where practical, then the user is not trapped without a cancel path.

### Story 3.6: Release to Capture and Copy

As a screenshot user,
I want releasing the mouse after drawing a valid region to finish capture,
So that the screenshot flow is fast and familiar.

**Acceptance Criteria:**

- Given overlay is active, when the user drags a valid crop and releases the pointer, then overlay confirms the crop selection without requiring a Confirm button.
- Given overlay is active, when the user presses Escape before completion, then capture is canceled and resources are torn down safely.
- Given a valid crop completes, when output processing begins, then overlay shows lightweight progress/completion feedback without exposing a toolbar of extra actions.
- Given release-to-capture is enabled, when crop is too small or invalid, then overlay remains active or cancels according to a clearly defined MVP rule without producing output.
- Given MVP design is consulted, when overlay UI is updated, then the implementation preserves crop selection, optional size feedback, and lightweight `Copied to clipboard` feedback only.

## Epic 4: MVP Output, Status, and Validation

### Story 4.1: Show User-Facing Capture and Preview Status

As an HDR screenshot user,
I want concise status messages for capture, preview, and output health,
So that I know whether I can trust what happened.

**Acceptance Criteria:**

- Given capture or preview initialization succeeds, when overlay/main status is displayed, then the user can see a clear normal or HDR-ready status.
- Given capture, preview, or output is degraded, when status is shown, then the message explains that fidelity or output cannot be fully trusted.
- Given capture, preview, or clipboard output fails, when status is shown, then the message is actionable and does not require graphics API knowledge.
- Given status is shown during capture, when technical details exist, then concise user status remains separate from advanced diagnostics.

### Story 4.2: Define and Implement MVP Clipboard Output

As a screenshot user,
I want my selected region copied to the clipboard after release,
So that the MVP produces a usable screenshot result.

**Acceptance Criteria:**

- Given a confirmed crop selection, when clipboard output is produced, then the app copies a usable bitmap representation to the Windows clipboard.
- Given output is SDR or tone-mapped, when completion feedback is shown, then Lumiere does not claim the clipboard data is HDR-preserving.
- Given clipboard operation fails, when the user releases a valid crop, then Lumiere reports a concise recoverable failure and does not leave capture resources active.
- Given the live preview path is FP16/scRGB, when clipboard output code is added, then it is isolated from the main live preview path and does not introduce SDR fallback into routine preview presentation.

### Story 4.3: Document MVP Manual Validation Scenarios

As a developer,
I want a manual MVP validation matrix,
So that hardware-dependent fidelity and release-to-copy behavior can be checked consistently.

**Acceptance Criteria:**

- Given validation docs are generated, when a developer opens the MVP manual test matrix, then it covers HDR enabled, HDR disabled, SDR monitor, full-screen app, and multi-monitor start-monitor scenarios.
- Given lifecycle validation docs are updated, when repeated capture sessions are tested, then the checklist covers direct capture, release-to-copy, Escape cancel, target changes, resize, and teardown.
- Given output guidance is documented, when an MVP issue is investigated, then the guide maps user-visible status to capture, graphics, presentation, clipboard, overlay, interop, or lifecycle causes.

## Epic 5: MVP Completion Gate

### Story 5.1: Define MVP Completion Gate

As a project owner,
I want a concrete MVP completion checklist,
So that BMad and future agents can tell when MVP is actually done.

**Acceptance Criteria:**

- Given the revised direct-capture route, when the MVP completion gate is documented, then it lists exact required stories, validation commands, Windows manual scenarios, design-input checks, and deferred-work triage rules.
- Given a story is not required for MVP, when it is listed in the roadmap, then the gate explicitly marks it post-MVP and does not block MVP completion.
- Given sprint status is inspected, when Epic 5 is not done, then BMad must not treat MVP as complete.

### Story 5.2: Run MVP Completion Validation and Triage

As a developer,
I want to run MVP validation and triage known deferred work,
So that MVP completion is not claimed from macOS edits or unit tests alone.

**Acceptance Criteria:**

- Given required MVP stories are implemented, when validation runs on Windows hardware, then restore, build, tests, format, direct capture, release-to-copy, HDR, SDR, full-screen app, and multi-monitor scenarios are recorded.
- Given deferred work exists, when triage runs, then each item is classified as MVP blocker, release blocker, post-1.0 backlog, or closed-by-design.
- Given any MVP blocker is found, when the gate is evaluated, then MVP cannot be marked complete until the blocker is resolved or explicitly downgraded with rationale.

### Story 5.3: Complete MVP Retrospective and Go/No-Go

As a project owner,
I want an MVP retrospective and go/no-go decision,
So that the project intentionally moves into installer work.

**Acceptance Criteria:**

- Given MVP validation and deferred-work triage are complete, when retrospective is written, then it records what shipped, what is deferred, remaining risks, validation level, and go/no-go decision.
- Given decision is go, when sprint status is updated, then Epic 5 can be marked done and Epic 6 can begin.
- Given decision is no-go, when follow-up is planned, then blocker stories are created before installer work starts.

## Epic 6: Installer and 1.0 Release

### Story 6.1: Decide Packaging Strategy

As a developer,
I want a documented packaging strategy,
So that Lumiere can become an installable Windows application without disrupting the native WinUI/HDR architecture.

**Acceptance Criteria:**

- Given the app uses WinUI 3, Windows App SDK, .NET 10, and x64, when packaging options are evaluated, then the chosen strategy documents runtime dependency handling, architecture, install location expectations, and signing/not-signing status for MVP/1.0.
- Given packaging decisions are made, when project files are inspected, then they preserve `net10.0-windows10.0.19041.0`, x64/win-x64, Windows App SDK, and existing module boundaries.

### Story 6.2: Build Installer Package

As a user,
I want an installable Lumiere package,
So that I can install the app without running it from the development environment.

**Acceptance Criteria:**

- Given packaging strategy is approved, when the installer/package is built, then it installs the app with correct version, app identity, icon/name metadata, and x64 runtime assumptions.
- Given packaging introduces new artifacts, when they are committed, then generated build outputs remain ignored unless explicitly required for release documentation.

### Story 6.3: Validate Install, Launch, and Uninstall

As a release owner,
I want install/uninstall validation,
So that the 1.0 package does not fail before users reach the capture workflow.

**Acceptance Criteria:**

- Given package is built, when installed on a clean Windows target, then Lumiere launches and can run the MVP capture workflow.
- Given uninstall is invoked, when it completes, then app registration/files are removed according to the chosen packaging strategy.
- Given installer validation fails, when results are documented, then the failure is classified as release blocker or known limitation.

### Story 6.4: Prepare 1.0 Versioning and Release Notes

As a project owner,
I want clear versioning and release notes,
So that users know what 1.0 includes and what remains post-1.0.

**Acceptance Criteria:**

- Given MVP and installer validation are complete, when release notes are written, then they list MVP capabilities, validation level, known limitations, and deferred post-1.0 features.
- Given versioning is prepared, when metadata is inspected, then app/package/release version values are consistent.

### Story 6.5: Cut 1.0 Release

As a release owner,
I want to tag and publish the 1.0 release,
So that the project has a stable release milestone.

**Acceptance Criteria:**

- Given release validation passes, when 1.0 is cut, then the repo has a versioned release/tag, release notes, and the validated installer artifact.
- Given release is complete, when sprint status is inspected, then Epic 6 is done and BMad can report 1.0 complete.

## Deferred Post-1.0 Roadmap

The following work is intentionally outside the active MVP-to-1.0 epic list:

- Full HDR-preserving still-image export semantics and implementation.
- Advanced SDR tone-mapping controls.
- Configurable clipboard behavior beyond the MVP default output.
- Cursor inclusion/exclusion controls.
- Full HDR/SDR/multi-monitor capability diagnostics beyond MVP validation needs.
- Global hotkey and system tray workflows.
- Lightweight annotations.
- Capture history.
- Installer update flow and broader distribution polish.
