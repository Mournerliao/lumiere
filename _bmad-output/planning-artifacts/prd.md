---

## stepsCompleted: ['step-01-init', 'step-02-discovery', 'step-02b-vision', 'step-02c-executive-summary', 'step-03-success', 'step-04-journeys', 'step-05-domain', 'step-06-innovation', 'step-07-project-type', 'step-08-scoping', 'step-09-functional', 'step-10-nonfunctional', 'step-11-polish']
inputDocuments:
  - '/Users/asherliao/Projects/lumiere/harness/PROJECT_PLAN.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/research/technical-lumiere-hdr-capture-research-2026-04-20.md'
workflowType: 'prd'
documentCounts:
  productBriefs: 0
  research: 1
  brainstorming: 0
  projectDocs: 2
classification:
  projectType: desktop_app
  domain: desktop graphics / HDR capture
  complexity: high
  projectContext: greenfield
releaseMode: phased

# Product Requirements Document - Lumiere

**Author:** Asherliao
**Date:** 2026-04-20

## Executive Summary

Lumiere is a native Windows desktop screenshot tool designed to capture and preview HDR display content without the washed-out, tone-mapped artifacts common in SDR-oriented screenshot workflows. Its core product promise is visual fidelity: when a user captures an HDR screen, the capture preview should preserve the brightness, color, and contrast relationships visible on the source display rather than flattening them through ordinary bitmap or GDI paths.

The initial product scope focuses on proving and productizing an HDR-correct capture pipeline: Windows.Graphics.Capture produces FP16 frames, Direct3D 11/DXGI preserve those frames as GPU textures, and WinUI 3 presents the result through a scRGB-capable `SwapChainPanel` with an interactive crop overlay. The product will prioritize correctness, deterministic graphics resource management, and responsive crop interaction before expanding into export formats, annotation, history, or cloud features.

### What Makes This Special

Most screenshot tools treat display capture as a bitmap problem. Lumiere treats it as a color-accurate graphics pipeline problem. The differentiating moment is when an HDR monitor user sees the capture preview match the source screen instead of appearing gray, clipped, or overexposed.

The core insight is that HDR screenshot quality depends on preserving FP16/scRGB data through every stage of capture and preview. Lumiere therefore rejects SDR fallback paths for its primary workflow and builds around Windows.Graphics.Capture, Direct3D 11, DXGI, and WinUI swap-chain interop from the start.

## Project Classification

- **Project Type:** Native Windows desktop application
- **Domain:** Desktop graphics, HDR capture, screen capture tooling
- **Complexity:** High, due to Direct3D/DXGI/WinRT interop, HDR color-space handling, COM resource lifetime, and UI-thread constraints
- **Project Context:** Greenfield product with technical planning artifacts and no existing application codebase

## Success Criteria

### User Success

- HDR display users can capture a screen region and see a preview that visually matches the source HDR display without the washed-out SDR appearance that motivated the product.
- Users can start capture, select a region, adjust the crop, and confirm the screenshot without leaving the full-screen overlay flow.
- Users understand when capture is unavailable, degraded, or unsupported instead of receiving a silently incorrect SDR result.
- Users can trust the preview as the product's source of truth for what will be captured.

### Business Success

- MVP success means proving that HDR-native screenshot capture is feasible and valuable for a focused audience of HDR monitor users, creators, developers, gamers, and display-quality enthusiasts.
- Early validation succeeds when target users can distinguish Lumiere's HDR preview from ordinary SDR screenshot tools and describe the difference as meaningful.
- The product earns continued investment only if the Phase 0 HDR pipeline spike and MVP workflow both demonstrate a clear fidelity advantage over commodity screenshot tooling.

### Technical Success

- The primary preview pipeline preserves FP16/scRGB data from WGC capture through Direct3D/DXGI rendering and WinUI presentation.
- The app uses `.NET 10 LTS` with `TargetFramework` `net10.0-windows10.0.19041.0`, WinUI 3, Windows App SDK 1.8 stable, WGC, Direct3D 11, DXGI, and Vortice.
- The capture frame pool uses `DirectXPixelFormat.R16G16B16A16Float`.
- The swap chain uses `DXGI_FORMAT_R16G16B16A16_FLOAT` and `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`.
- Repeated capture start/stop cycles do not leak WGC frames, capture sessions, D3D textures, render targets, swap chains, or device-bound resources.
- UI-thread and capture-thread boundaries are explicit; frame callbacks do not mutate WinUI state directly.

### Measurable Outcomes

- Phase 0 spike demonstrates WGC -> FP16 D3D11 texture -> scRGB swap chain -> `SwapChainPanel` preview on HDR hardware.
- MVP preview path has no 8-bit SDR, GDI, `BitmapImage`, or `SoftwareBitmap` dependency in the main live preview path.
- Manual HDR validation covers HDR enabled, HDR disabled, SDR monitor, and at least one multi-monitor configuration.
- Crop interaction remains responsive over a full-screen preview during normal capture.
- Resource teardown tests or manual diagnostics show stable GPU memory across repeated capture sessions.

## Product Scope

### Approved MVP-to-1.0 Rebaseline (2026-05-08 v2)

The product roadmap is now organized into ten canonical epics that cover the expanded MVP (based on v0 design reference) and the path to a 1.0 installable release:

1. **Epic 1: HDR Preview Foundation** - already completed foundation for WinUI, WGC, D3D11/DXGI, FP16/scRGB preview, and readiness state.
2. **Epic 2: Direct Capture Session Lifecycle** - capture/session lifecycle plus pickerless monitor capture for the default screenshot path.
3. **Epic 3: Release-to-Copy Overlay Workflow** - full-screen overlay, crop interaction, keyboard cancel, and release-to-capture behavior.
4. **Epic 4: Main Panel UI Refactoring** - compact layout, dual capture buttons (Full Screen + Region), HDR status indicator, settings entry.
5. **Epic 5: Full Screen Capture Mode** - direct screen capture without crop interaction, automatic clipboard copy.
6. **Epic 6: Settings Panel** - shortcuts, HDR settings, output target, save path configuration.
7. **Epic 7: Tray Context Menu** - system tray integration with capture, open, settings, quit actions.
8. **Epic 8: MVP Output, Status, and Validation** - narrow MVP clipboard output, concise user-facing status, and manual validation.
9. **Epic 9: MVP Completion Gate** - explicit MVP completion checklist, blocker/deferred-work triage, Windows validation confirmation, and go/no-go.
10. **Epic 10: Installer and 1.0 Release** - packaging strategy, installer build, install/uninstall validation, release notes, versioning, and 1.0 release.

MVP is complete when Epic 9 is done. The 1.0 release is complete when Epic 10 is done.

### MVP - Minimum Viable Product

- Phase 0 technical spike proving the HDR capture/rendering chain on real HDR hardware.
- Native Windows desktop shell using `.NET 10 LTS`, WinUI 3, and Windows App SDK 1.8 stable.
- **Main panel UI** with compact layout (360px), dual capture buttons (Full Screen + Region), HDR status indicator, and settings entry (v0 design reference).
- Full-screen overlay window with hardware-rendered HDR preview via `SwapChainPanel`.
- WGC-based direct monitor capture using FP16 frame pool configuration; the default MVP flow must not require a picker-first target selection step.
- Direct3D 11/DXGI rendering path preserving scRGB/FP16 preview fidelity.
- **Full Screen capture mode**: single-click capture of entire screen without crop overlay, automatic clipboard copy.
- **Region capture mode**: click Region, enter a full-screen overlay, drag a region, release to capture/copy, and show lightweight completion feedback.
- Escape cancels the overlay. Explicit confirm controls are not part of the default MVP path.
- **Settings panel**: shortcuts configuration, HDR settings (warnings, export format), output target (clipboard/folder/both), save path configuration.
- **Tray context menu**: system tray integration with capture actions, open, settings, quit, and HDR status display.
- HDR status indicator showing Ready (green), Available (yellow), or Unavailable (red) states.
- Narrow MVP clipboard output with explicit semantics; it must not be described as HDR-preserving unless technically proven.
- Concise error/degraded-state reporting for unsupported capture, missing HDR capability, graphics initialization failure, or clipboard failure.
- Deterministic disposal for WGC, WinRT, COM, Direct3D, DXGI, and swap-chain resources.
- Windows manual validation covering HDR, SDR, full-screen app, multi-monitor start-monitor behavior, release-to-copy, and repeated lifecycle teardown.

### Installer and 1.0 Release

- Approved packaging strategy for WinUI 3 / Windows App SDK / .NET 10 / x64.
- Build an installable package or installer.
- Validate install, launch, MVP capture workflow, and uninstall on Windows.
- Prepare 1.0 versioning, release notes, known limitations, and release artifact/tag.

### Post-1.0 Roadmap

- HDR-aware export formats and SDR tone-mapping presets.
- Configurable clipboard behavior beyond the MVP default output.
- Cursor inclusion/exclusion controls.
- Multi-monitor target selection and diagnostics.
- Lightweight annotations that preserve preview correctness.
- Global hotkey beyond tray integration.
- Capture history and project organization.
- Update flow and broader installer polish.

### Vision (Future)

- Best-in-class HDR screenshot and visual capture workflow for Windows power users.
- HDR/SDR side-by-side comparison and tone-mapping controls.
- Capture history, annotations, and project organization.
- Video capture or short HDR clips.
- Hardware/display capability diagnostics for creators, gamers, and display reviewers.

## User Journeys

### Journey 1: HDR Creator Captures a Reference Image

Maya is a video colorist reviewing HDR footage on a Mini-LED HDR monitor. She wants to capture a region of the screen to share a visual issue with a collaborator. Her current screenshot tools flatten highlights and make the image look gray, so screenshots undermine the point she is trying to communicate.

She launches Lumiere, clicks Capture, immediately enters a full-screen overlay, and sees a preview that preserves the HDR appearance of the source display. She drags a crop rectangle over the relevant frame area and releases to capture/copy. The key moment is visual trust: the preview does not wash out the shot, so she believes the capture is worth using.

This journey reveals requirements for HDR-correct preview, direct full-screen overlay capture, release-to-copy crop interaction, keyboard cancel, and clear distinction between MVP clipboard output and later advanced export behavior.

### Journey 2: Gamer Captures an HDR Scene Without Washed-Out Highlights

Ryan is playing a game in HDR and wants to capture a dramatic high-contrast scene. Standard screenshot tools produce a dull image where neon highlights and shadow contrast are wrong. He needs a fast capture flow that does not require color-management expertise.

He triggers Lumiere, enters screenshot mode without first choosing a window, drags a crop region over the game scene, and sees a preview that keeps the intended HDR look. If the game or display cannot be captured correctly, Lumiere tells him the pipeline is degraded instead of pretending the screenshot is valid.

This journey reveals requirements for fast capture startup, display/window target selection, responsive overlay interaction, clear degraded-state messaging, and eventual global hotkey support.

### Journey 3: Power User Diagnoses HDR Capability

Alex is a Windows power user with multiple monitors: one HDR display and one SDR display. They want to know whether Lumiere is capturing the intended display with the expected FP16/scRGB path. They are comfortable reading technical diagnostics when something fails.

Alex starts capture on the HDR monitor. Lumiere validates the capture path and preview pipeline. If the swap chain color space cannot be set or the monitor/display path is not HDR-capable, Lumiere exposes a concise diagnostic message rather than silently falling back. Alex can change target display or disable HDR expectations and retry.

This journey reveals requirements for capability detection, target selection, explicit error states, technical diagnostics, multi-monitor awareness, and no silent SDR fallback.

### Journey 4: Developer Verifies Pipeline Stability During Repeated Captures

Nora is implementing or testing Lumiere. Her concern is not the crop UI; it is whether repeated start/stop cycles leak frames, swap chains, textures, or COM objects. She runs capture repeatedly, changes target size, cancels, restarts, and closes the overlay.

The app consistently disposes frame pools, sessions, frames, render targets, swap chains, and device-bound resources. `SetSwapChain(null)` detaches the preview before graphics teardown. Wrong-thread UI calls do not occur during frame arrival.

This journey reveals requirements for lifecycle tests, diagnostic logging, deterministic disposal, thread-boundary enforcement, resize handling, and device/resource teardown paths.

### Journey Requirements Summary

- Full-screen capture overlay with HDR preview and crop interaction.
- WGC direct monitor capture for the default MVP workflow, with picker-based selection retained only as fallback/debug or later explicit selection behavior.
- FP16 capture and scRGB presentation with explicit validation.
- Clear degraded/unsupported-state messages.
- Responsive crop selection, release-to-capture/copy, and cancel flows.
- Multi-monitor and HDR/SDR capability awareness.
- Deterministic graphics resource ownership and teardown.
- Diagnostics for developers and advanced users.

## Domain-Specific Requirements

### Compliance & Regulatory

- Use Windows.Graphics.Capture consent and capability mechanisms; do not bypass OS capture permission, picker, or border behavior.
- If borderless capture is pursued post-MVP, request the required borderless capture access and declare the appropriate package capability.
- Avoid misleading users about capture fidelity; if the app cannot preserve HDR preview correctness, show a degraded or unsupported state.

### Technical Constraints

- The primary capture and preview pipeline must preserve FP16/scRGB data and must not use SDR bitmap/GDI paths.
- WinUI objects and `SwapChainPanel` attachment must be manipulated on the UI thread.
- WGC frame callbacks and graphics rendering must be coordinated without retaining invalid frame or surface references.
- Direct3D/DXGI/WinRT/COM resources must have explicit owners and deterministic disposal.
- Multi-monitor behavior must account for HDR/SDR capability differences, target display changes, and scaling differences.
- The app must be architecture-specific rather than `Any CPU` because Windows App SDK and graphics dependencies include native components.

### Integration Requirements

- Integrate WGC `Direct3D11CaptureFramePool` with a D3D11 device exposed as a WinRT `IDirect3DDevice`.
- Convert or access captured `IDirect3DSurface` content as GPU resources usable by the rendering layer.
- Create a DXGI composition swap chain and attach it to WinUI through `ISwapChainPanelNative`.
- Use `IDXGISwapChain3.SetColorSpace1` to set the scRGB color space.
- Use a transparent/full-screen WinUI overlay with controlled hit testing for crop selection.

### Risk Mitigations

- Mitigate SDR fallback risk through constant validation, startup checks, and manual HDR test scenarios.
- Mitigate resource leaks through `IDisposable`, teardown tests, and mandatory `SetSwapChain(null)` during preview teardown.
- Mitigate wrong-thread crashes through `DispatcherQueue` and strict UI-thread attachment rules.
- Mitigate hardware variance through early HDR hardware spike, multi-monitor testing, and explicit diagnostics.
- Mitigate export ambiguity by excluding HDR still export from MVP until separate export-format research is complete.

## Innovation & Novel Patterns

### Detected Innovation Areas

- **HDR-first screenshot workflow:** Lumiere treats screenshot capture as a GPU/color pipeline problem rather than a simple bitmap extraction problem.
- **FP16/scRGB live preview as product differentiator:** The preview itself is the proof of value; it must preserve HDR appearance before export features are trusted.
- **No silent SDR fallback:** The product explicitly rejects the common pattern of falling back to SDR capture while presenting the output as valid.
- **Technical diagnostics as user trust:** For advanced users, visible capture-path diagnostics become part of product credibility.

### Market Context & Competitive Landscape

Common screenshot utilities optimize for convenience, annotation, sharing, or OS integration. Lumiere's differentiation is narrower and deeper: faithful HDR capture and preview for Windows users whose current screenshot tools fail on HDR content. This makes the initial product more specialized than general-purpose screenshot tools, but more compelling for HDR monitor owners, creators, gamers, display reviewers, and developers.

### Validation Approach

- Validate the Phase 0 HDR pipeline on real HDR hardware before treating the MVP as viable.
- Compare Lumiere preview against ordinary screenshot output on the same HDR scene.
- Ask target users whether the visual difference is meaningful enough to change their capture workflow.
- Track failure cases where HDR cannot be preserved and ensure the app reports them clearly.

### Risk Mitigation

- If the FP16/scRGB preview path fails on target hardware, pause product expansion and continue technical research rather than building around a compromised pipeline.
- If export cannot preserve HDR correctly in MVP, scope export out or clearly label SDR/tone-mapped output.
- If hardware variance is high, prioritize diagnostics and compatibility matrix before broad release.
- If the audience is narrower than expected, position Lumiere as a specialist HDR capture utility rather than a general screenshot replacement.

## Desktop App Specific Requirements

### Project-Type Overview

Lumiere is a Windows-native desktop application. It must integrate with Windows display capture, Direct3D/DXGI graphics presentation, WinUI 3 windowing, and local desktop input flows. It is not a web app, SaaS platform, mobile app, or cloud service.

### Platform Support

- Primary platform: Windows desktop.
- Runtime: `.NET 10 LTS`.
- Target framework: `net10.0-windows10.0.19041.0`.
- UI framework: WinUI 3 via Windows App SDK 1.8 stable.
- Graphics API: Direct3D 11 and DXGI through Vortice.
- Capture API: Windows.Graphics.Capture.
- Build architecture must be explicit (`x64` first); avoid `Any CPU`.
- Minimum Windows behavior must account for WGC desktop interop requirements, especially HWND/HMONITOR capture item creation requiring Windows 10 1903/build 18362 or later.

### System Integration

- Full-screen borderless overlay window.
- `SwapChainPanel` as the hardware preview surface.
- XAML `Canvas` overlay for crop selection and controls.
- Win32/WinUI interop for window handle access, transparent/layered styles, topmost behavior, and hit testing.
- Optional future global hotkey and tray integration.
- Explicit capture consent and capture-state visibility.

### Update Strategy

- MVP may defer full update infrastructure.
- Packaging strategy must be decided before broad release: MSIX, packaged with external location, or unpackaged distribution.
- If unpackaged, the app must include a clear Windows App Runtime dependency strategy.
- Package versions must be locked after the technical spike succeeds.

### Offline Capabilities

- MVP must function fully offline.
- No account, cloud service, telemetry dependency, or network requirement is needed for core capture.
- Local settings may store user preferences such as cursor capture, diagnostics visibility, and default capture behavior.

### Technical Architecture Considerations

- `GraphicsEngine` owns D3D11 device/context, DXGI swap chain, color space, render targets, shaders, resizing, and swap-chain attachment.
- `CaptureService` owns WGC frame pool/session lifecycle, target selection, frame arrival handling, and frame disposal.
- `OverlayUI` owns WinUI windowing, `SwapChainPanel`, overlay `Canvas`, crop interaction, toolbar, and UI-thread dispatch.
- Interop helpers must isolate WinRT/COM/DXGI bridge code from product UI logic.

### Implementation Considerations

- Implement the HDR pipeline spike before broad app work.
- Use stable package versions and record them in project files.
- Treat resource teardown as a first-class implementation path, not cleanup afterthought.
- Keep export out of the core MVP unless the HDR preview pipeline is proven.
- Prefer clear diagnostics and explicit unsupported states over hidden fallback behavior.

## Project Scoping & Phased Development

### MVP Strategy & Philosophy

**MVP Approach:** Technical proof and core experience MVP. Lumiere must first prove that HDR-native capture and preview are possible, then wrap that pipeline in the smallest useful screenshot workflow.

**Resource Requirements:** The MVP requires Windows desktop engineering, C#/.NET, WinUI 3, Direct3D 11/DXGI, WinRT/COM interop, HDR/color-space knowledge, and access to real HDR hardware for validation.

### MVP Feature Set (Phase 0 + Phase 1-4)

**Core User Journeys Supported:**

- HDR creator captures a reference image with faithful preview.
- Gamer captures an HDR scene without washed-out preview.
- Power user receives clear diagnostics when HDR capture is degraded or unsupported.
- Developer verifies repeated capture lifecycle stability.

**Must-Have Capabilities:**

- Phase 0 HDR pipeline spike proving WGC -> FP16 D3D11 texture -> scRGB swap chain -> WinUI `SwapChainPanel` preview.
- `.NET 10 LTS` + WinUI 3 + Windows App SDK 1.8 stable app scaffold.
- D3D11 device/context initialization and DXGI swap-chain creation.
- WGC capture service using FP16 frame pool.
- Graphics engine rendering captured textures into HDR-capable preview.
- Full-screen overlay with crop rectangle, confirm, and cancel.
- Explicit diagnostics for unsupported/degraded capture paths.
- Deterministic resource disposal and teardown.

### Post-MVP Features

**Phase 5 (Export & Practical Capture Output):**

- HDR-aware still-image export research and implementation.
- SDR tone-mapping export option with explicit labeling.
- Clipboard behavior with clear HDR/SDR semantics.

**Phase 6 (Workflow Expansion):**

- Cursor inclusion/exclusion controls.
- Global hotkey and tray integration.
- Multi-monitor target selection improvements.
- Lightweight annotation tools.
- Packaged installer and update flow.

**Phase 7 (Power User Platform):**

- HDR/SDR comparison tools.
- Hardware/display capability diagnostics.
- Capture history and organization.
- Video or short HDR clip capture.

### Risk Mitigation Strategy

**Technical Risks:** Phase 0 gates the project. If the FP16/scRGB preview path cannot be proven on HDR hardware, pause product implementation and return to technical research.

**Market Risks:** Validate with HDR monitor owners before broadening scope. The MVP should answer whether visual fidelity is compelling enough to justify a specialized screenshot tool.

**Resource Risks:** If resources are constrained, preserve the HDR pipeline and crop workflow; defer export formats, annotations, hotkeys, history, and packaging polish.

## Functional Requirements

### Capture Target Selection

- FR1: Users can initiate a new screen capture session from the desktop application.
- FR2: Users can choose a display or window as the capture target.
- FR3: Users can cancel capture target selection before a capture session begins.
- FR4: The system can report when screen capture is unsupported on the current device or Windows configuration.
- FR5: The system can distinguish between normal, degraded, and unsupported capture states.

### HDR Preview Fidelity

- FR6: Users can view a live preview of the selected capture target before confirming a crop.
- FR7: The system can preserve HDR-oriented capture data in the primary preview workflow.
- FR8: The system can validate that the primary preview path is using the required HDR-capable capture and presentation configuration.
- FR9: The system can notify users when the preview cannot be trusted as HDR-correct.
- FR10: Users can compare the app's preview state against a clear status indicator for HDR readiness.

### Crop Interaction

- FR11: Users can create a crop selection by dragging over the full-screen preview.
- FR12: Users can adjust or recreate the crop selection before confirmation.
- FR13: Users can confirm the selected capture region.
- FR14: Users can cancel the capture overlay and return to the prior desktop state.
- FR15: Users can see the active crop region and non-selected area clearly while selecting.
- FR16: Users can complete the MVP crop workflow without configuring advanced settings.

### Overlay and Desktop Window Behavior

- FR17: Users can interact with a full-screen overlay that displays the capture preview and crop controls.
- FR18: The system can keep preview rendering and interaction overlays visually layered in the correct order.
- FR19: The system can handle transparent or borderless overlay behavior required for screenshot selection.
- FR20: The system can manage overlay hit testing so crop selection remains possible.
- FR21: The system can close or dismiss the overlay reliably after confirm, cancel, or failure.

### Capability Detection and Diagnostics

- FR22: Users can see concise diagnostic information when HDR capture or preview setup fails.
- FR23: Advanced users can inspect whether the app is using the intended capture format, preview format, and color-space state.
- FR24: The system can detect and report target display or monitor capability differences relevant to HDR preview correctness.
- FR25: The system can report graphics initialization failures with enough context to support troubleshooting.
- FR26: The system can surface degraded output warnings instead of silently presenting SDR fallback as valid.

### Resource Lifecycle and Session Management

- FR27: The system can start, stop, and restart capture sessions without requiring app restart.
- FR28: The system can release capture, preview, and graphics resources when a session ends.
- FR29: The system can recreate capture and preview resources when target size or capture target changes.
- FR30: The system can detach preview presentation resources before graphics teardown.
- FR31: The system can prevent stale capture frames or invalid graphics surfaces from being reused after their valid lifetime.

### MVP Validation and Testing Support

- FR32: Developers can run a minimal HDR pipeline spike independent of later product features.
- FR33: Developers can verify the app's key HDR constants and capture/preview states.
- FR34: Developers can repeat capture start/stop flows to check resource stability.
- FR35: Developers can test capture behavior across HDR enabled, HDR disabled, SDR monitor, and multi-monitor scenarios.

### Settings and Preferences

- FR36: Users can access settings panel from main panel to configure capture behavior.
- FR37: Users can configure capture shortcuts (Full Screen and Region).
- FR38: Users can configure HDR settings (warnings toggle, export format: HDR10/P3/sRGB).
- FR39: Users can configure output target (clipboard/folder/both).
- FR40: Users can configure save path for captures.
- FR41: Users can see About information (version, description).
- FR42: Users can choose whether future capture sessions include cursor capture when that option is implemented.
- FR43: Users can enable or disable advanced diagnostics when diagnostic UI is available.

### Full Screen Capture

- FR44: Users can capture the entire current monitor with a single click (Full Screen mode).
- FR45: Full Screen capture skips the crop overlay and directly copies to clipboard.
- FR46: Users see lightweight "Copied to clipboard" feedback after Full Screen capture completes.

### Tray Integration

- FR47: Users can access Lumiere from the system tray.
- FR48: Users can perform capture actions (Full Screen, Region) from tray context menu.
- FR49: Users can open main window, access settings, or quit from tray context menu.
- FR50: Users can see HDR status in the tray context menu.

### Post-MVP Output and Workflow Capabilities

- FR51: Users can export or copy capture output after HDR/SDR output semantics are defined.
- FR52: Users can choose between HDR-preserving output and SDR tone-mapped output when export support exists.
- FR53: Users can use global hotkey beyond tray integration when post-MVP desktop integration is implemented.
- FR54: Users can add lightweight annotations when post-MVP annotation support is implemented.

## Non-Functional Requirements

### HDR Fidelity

- NFR1: The primary preview pipeline must preserve FP16/scRGB capture data and must not silently downgrade to SDR.
- NFR2: The system must expose a visible degraded or unsupported state when HDR preview correctness cannot be established.
- NFR3: MVP validation must include side-by-side comparison against ordinary SDR screenshot output on real HDR hardware.
- NFR4: HDR-related constants and configuration must be testable and centrally verifiable.

### Performance and Responsiveness

- NFR5: Crop interaction must remain responsive during live preview under normal capture conditions.
- NFR6: The live preview path must avoid CPU readback or bitmap conversion for routine frame presentation.
- NFR7: Frame processing must release WGC frame objects promptly enough to avoid frame pool starvation during normal use.
- NFR8: Overlay startup should feel immediate enough for screenshot use; any noticeable delay must be attributable to explicit target selection or graphics initialization.

### Reliability and Resource Lifecycle

- NFR9: Repeated capture start, cancel, confirm, and restart flows must not produce unbounded GPU memory growth.
- NFR10: All WGC, WinRT, COM, D3D11, DXGI, frame pool, session, texture, render target, and swap-chain resources must have deterministic teardown paths.
- NFR11: The preview swap chain must be detached before graphics device teardown.
- NFR12: Wrong-thread WinUI access must be prevented by design, not handled as a recoverable runtime error.
- NFR13: Device/resource initialization failures must leave the application in a recoverable state.

### Platform Compatibility

- NFR14: The MVP targets Windows desktop with `.NET 10 LTS` and `net10.0-windows10.0.19041.0`.
- NFR15: The MVP targets `x64` first and must not rely on `Any CPU`.
- NFR16: The application must run without network access for core capture workflows.
- NFR17: The application must handle HDR and SDR monitor configurations without presenting misleading output.

### Security and Privacy

- NFR18: The application must use Windows capture consent and capability mechanisms.
- NFR19: The MVP must not upload screenshots, telemetry, or display content to any remote service.
- NFR20: Any future diagnostics must avoid capturing or exposing screenshot content unless explicitly user-approved.
- NFR21: Borderless capture behavior must only be used with the required Windows capability and user consent.

### Accessibility and Usability

- NFR22: Core capture controls must be understandable without requiring graphics API knowledge.
- NFR23: Error and degraded-state messages must be actionable for non-developer users while allowing advanced diagnostics for power users.
- NFR24: Overlay controls should be keyboard-reachable where practical for MVP and must not trap users without a cancel path.

### Maintainability and Diagnostics

- NFR25: Native interop code must be isolated behind narrow APIs.
- NFR26: Diagnostics must identify capture stage, graphics initialization stage, and presentation stage failures separately.
- NFR27: Package versions and target framework decisions must be recorded in project files once scaffolding begins.
- NFR28: MVP code must preserve the module boundaries between capture, graphics rendering, and overlay UI.
