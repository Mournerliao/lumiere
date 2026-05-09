---
stepsCompleted:
  - step-01-init
  - step-02-discovery
  - step-02b-vision
  - step-02c-executive-summary
  - step-03-success
  - step-04-journeys
  - step-05-domain
  - step-06-innovation
  - step-07-project-type
  - step-08-scoping
  - step-09-functional
  - step-10-nonfunctional
  - step-11-polish
  - step-e-01-discovery
  - step-e-02-review
  - step-e-03-edit
inputDocuments:
  - harness/design/v0-mvp-reference/README.md
  - harness/planning/mvp-feature-list.md
  - _bmad-output/planning-artifacts/research/technical-lumiere-mvp-v0-design-winui-wgc-hdr-research-2026-05-09.md
  - docs/validation/lifecycle-validation.md
  - docs/validation/overlay-validation.md
  - _bmad-output/implementation-artifacts/1-1-scaffold-the-native-windows-app-foundation.md
  - _bmad-output/implementation-artifacts/1-2-centralize-hdr-constants-and-preview-readiness-status.md
  - _bmad-output/implementation-artifacts/1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md
  - _bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md
  - _bmad-output/implementation-artifacts/1-5-prove-minimal-wgc-fp16-capture-to-live-preview.md
  - _bmad-output/implementation-artifacts/2-1-start-capture-and-select-a-display-or-window-target.md
  - _bmad-output/implementation-artifacts/2-2-represent-capture-session-state-explicitly.md
  - _bmad-output/implementation-artifacts/2-3-stop-restart-and-recreate-capture-resources.md
  - _bmad-output/implementation-artifacts/2-4-validate-repeated-capture-lifecycle-stability.md
  - _bmad-output/implementation-artifacts/2-5-create-monitor-capture-targets-without-picker.md
  - _bmad-output/implementation-artifacts/3-1-show-a-fullscreen-overlay-above-the-hdr-preview.md
  - _bmad-output/implementation-artifacts/3-2-create-a-crop-selection-by-dragging.md
  - _bmad-output/implementation-artifacts/3-3-adjust-or-recreate-the-crop-selection.md
  - _bmad-output/implementation-artifacts/3-4-confirm-or-cancel-the-capture-overlay.md
  - _bmad-output/implementation-artifacts/3-5-manage-overlay-hit-testing-and-keyboard-escape.md
  - _bmad-output/implementation-artifacts/3-6-release-to-capture-and-copy.md
  - _bmad-output/implementation-artifacts/epic-1-retro-2026-05-04.md
  - _bmad-output/implementation-artifacts/epic-2-retro-2026-05-07.md
  - _bmad-output/implementation-artifacts/deferred-work.md
documentCounts:
  productBriefs: 0
  research: 1
  brainstorming: 0
  projectDocs: 23
  explicitReferences: 2
classification:
  projectType: desktop_app
  domain: general_native_graphics_utility
  complexity: medium-high
  projectContext: brownfield
planningConstraints:
  - Preserve existing Epic 1-3 implementation and validation documents as historical foundation work from the pre-MVP-rebaseline route.
  - When recreating epics for the updated MVP route, keep Epic 1-3 and begin rework or continued implementation from Epic 4.
releaseMode: single-release
workflowType: 'prd'
workflow: 'edit'
user_name: lumiere
project_name: lumiere
date: '2026-05-09'
lastEdited: '2026-05-09'
editHistory:
  - date: '2026-05-09'
    changes: 'Resolved critical PRD validation findings by making NFRs testable, moving Epic continuity out of numbered requirements, tightening traceability, and removing implementation leakage from requirements.'
---

# Product Requirements Document - lumiere

**Author:** lumiere
**Date:** 2026-05-09

## Executive Summary

Lumiere is a native Windows HDR screenshot utility for users who need fast, trustworthy capture of HDR desktop content without leaving their current workflow. The MVP centers on a low-interruption capture loop: trigger from a global shortcut, tray command, or compact main window; capture fullscreen or draw a region directly over the current display; release to copy and/or save; then return immediately to the original task.

The product exists because ordinary screenshot paths often fail HDR content by applying incorrect SDR conversion, washing out highlights, flattening contrast, or obscuring color relationships. Lumiere's core promise is to preserve capture fidelity through a Windows-native FP16/scRGB pipeline wherever the platform and hardware can support it, while giving honest, lightweight status feedback when HDR readiness or output fidelity cannot be proven.

This PRD rebaselines the MVP around the v0 MVP reference and `harness/planning/mvp-feature-list.md`. Existing Epic 1-3 implementation and validation artifacts remain historical foundation work from the pre-rebaseline route; future MVP planning begins from Epic 4.

### What Makes This Special

HDR fidelity is Lumiere's reason to exist, not a secondary export option. The differentiator is the combination of a Windows Graphics Capture FP16 capture path, scRGB/HDR preview architecture, and product language that refuses to claim HDR correctness unless the system, display, capture, preview, and output path provide enough evidence.

The interaction model is deliberately restrained. Lumiere should behave like a quiet native Windows instrument: fast shortcut and tray entry points, no picker-first interruption in the default MVP path, no library or gallery requirement, no annotation-heavy overlay, and no complex export workflow in the capture moment. The UI should only surface what the user needs to know: whether HDR is ready, where the output will go, and whether capture completed.

The core product insight is prioritized as follows: fidelity first, workflow speed second, trust feedback third. If HDR capture is visibly wrong, the product loses its purpose. If the workflow is slow, users will not adopt it. If the status language is vague or overconfident, users cannot trust the result.

## Project Classification

- **Project Type:** Native desktop app
- **Domain:** Native Windows graphics utility
- **Complexity:** Medium-high, driven by HDR fidelity, Windows Graphics Capture, Direct3D/DXGI, scRGB presentation, clipboard/file output semantics, multi-monitor behavior, and Windows manual validation requirements
- **Project Context:** Brownfield rebaseline
- **Implementation Baseline:** Existing Epic 1-3 work is retained as historical foundation; rebaselined MVP epic planning begins from Epic 4

## Success Criteria

### User Success

Users can trigger Lumiere from a shortcut, tray command, or main window, complete a fullscreen or region capture with minimal interruption, and return to their original workflow without managing a library, editor, or export wizard.

A successful MVP capture feels complete when the user can release a valid region and trust that the screenshot was copied, saved, or both according to their configured output target. The UI communicates only the necessary state: HDR readiness, active capture/output destination, failure or degraded status, and completion feedback.

The core user success test is whether HDR desktop content captured through Lumiere looks materially closer to what the user saw on the HDR display than a typical SDR screenshot path, without washed-out highlights, incorrect tone mapping, or misleading "HDR supported" language.

### Business Success

MVP success means Lumiere proves a distinct product reason to exist: a native Windows screenshot workflow where HDR fidelity is the primary value proposition and low-interruption capture is the adoption path.

The first release should be judged by product trust and workflow completion rather than broad feature count. Success is reached when early users can complete the primary HDR screenshot loop repeatedly, understand when HDR readiness is degraded or unavailable, and prefer Lumiere for HDR content over built-in screenshot tools.

A 3-month success target is a stable MVP that supports the core capture surfaces from the v0 reference: main window capture, global shortcut entry, tray entry, direct region selection, clipboard/file output settings, and credible HDR status. A 12-month success target is a refined Windows utility with validated HDR output semantics, reliable tray/hotkey behavior, configurable output workflows, and enough manual validation evidence to support public product claims.

### Technical Success

The primary preview and capture path preserves the HDR-first invariants: Windows Graphics Capture frame pool uses `R16G16B16A16Float`, preview presentation uses an FP16 DXGI swap chain, and scRGB/HDR readiness is represented through typed state rather than silent SDR fallback.

The default MVP path must not show a picker-first interruption before region capture. Direct monitor capture, overlay placement, crop interaction, and release-to-capture behavior must remain deterministic across repeated sessions, multi-monitor setups, common Windows scaling values, and HDR/SDR display combinations.

Output paths must be honest about fidelity. Clipboard output may provide a basic usable image in MVP, but HDR-preserving claims require explicit encoder, metadata, tone mapping, and Windows manual validation evidence. File output and color/export options must not expose unsupported claims as user-facing promises.

### Measurable Outcomes

- A user can complete the default region capture flow from trigger to output feedback without interacting with a system picker in the MVP happy path.
- A valid crop release produces configured output to clipboard, folder, or both, or reports a clear recoverable failure without leaving capture resources active.
- The app exposes distinct states for HDR ready, HDR available but not enabled, HDR unavailable, degraded preview, unsupported capture, failed capture, and completed output.
- Repeated capture start, cancel, restart, and release-to-output flows do not leak WGC, D3D11, DXGI, swap-chain, frame-pool, overlay, or tray resources in manual Windows validation.
- The MVP passes the repository's agreed automated quality gates and records Windows manual validation for direct monitor capture, overlay behavior, clipboard/file output, HDR readiness language, multi-monitor behavior, and lifecycle stability.

## Product Scope

### MVP - Minimum Viable Product

The MVP includes a compact native WinUI main window with Lumiere branding, fullscreen and region capture entry points, current shortcut display, HDR status summary, and a clear settings entry.

The MVP includes global shortcuts and tray commands for fullscreen and region capture. Tray behavior must include HDR status summary, capture commands, open main window, open settings, and quit. All entry points share one capture session state so users cannot start conflicting captures.

The MVP includes direct monitor capture for the default path, fullscreen overlay region selection, release-to-capture for valid regions, Escape/cancel behavior, invalid crop handling, and lightweight completion feedback.

The MVP includes settings for fullscreen/region shortcuts, HDR alerts, output target selection, save path when file output is enabled, capture-after behavior where applicable, timestamp naming preference, clipboard image copy option, export/color format presentation only where backed by real implementation semantics, and about/version information.

The MVP includes shared persisted settings state across main window, tray, hotkeys, and output pipeline. It also includes validation language and records that distinguish Mac edit, Windows CI-pass, and Windows manual-pass.

Before UI-heavy implementation begins, the MVP UX state inventory must define main window, settings, tray, overlay, HDR status, and output feedback states sufficiently to validate non-color-only status discrimination and prevent unsupported controls from appearing functional.

### Growth Features (Post-MVP)

Growth scope includes stronger HDR/SDR output semantics, validated file formats and metadata, explicit tone-mapping policy, richer error recovery, more robust diagnostics UI, broader shortcut conflict handling, startup/minimize behavior, installer/update flow, and refined multi-monitor targeting.

Growth features may also include more polished notification behavior, configurable filename templates, better output destination management, and targeted compatibility validation against common paste targets and image viewers.

### Vision (Future)

The long-term product is a trusted HDR capture instrument for Windows: fast enough to disappear into the user's workflow, accurate enough to be chosen specifically for HDR content, and honest enough to tell users when a capture cannot be trusted.

Future scope may include advanced HDR export profiles, deeper display capability diagnostics, richer validation tooling, professional workflows for creators and QA teams, and optional productivity features that do not compromise the quiet capture-first experience.

## User Journeys

### Journey 1: Maya Captures an HDR Reference Without Leaving Her Work

Maya is reviewing HDR video playback on a Windows HDR monitor. She sees a frame where highlight rolloff and color contrast matter, but she knows ordinary screenshot tools often make HDR content look gray, blown out, or unlike what she saw.

She presses the Lumiere region capture shortcut. The overlay appears directly over the current display without a picker-first interruption. Maya drags around the relevant frame area and releases. Lumiere confirms the valid region, copies and/or saves the output according to her settings, and shows brief completion feedback before disappearing.

The value moment is the absence of ceremony: Maya never opens a library, editor, onboarding flow, or export wizard. She trusts the result because Lumiere uses the HDR-first capture path where available and avoids overclaiming when HDR readiness cannot be proven.

This journey reveals requirements for global shortcuts, direct monitor capture, fullscreen overlay placement, region crop, release-to-capture, configured output targets, lightweight completion feedback, and trustworthy HDR status.

### Journey 2: Alex Uses the Tray While Staying in a Fullscreen Workflow

Alex is comparing HDR game or media output and does not want to bring the main app window forward. He needs a fast capture command while staying focused on the content.

He opens the tray menu, sees Lumiere status and the current HDR readiness summary, then chooses fullscreen or region capture. If he chooses region capture, the overlay opens on the intended monitor. If he chooses fullscreen capture, Lumiere captures the target display according to the configured output behavior.

The value moment is that tray access is not a secondary product shell; it is a compact command surface for the same capture engine. The tray menu mirrors shortcuts, output state, HDR readiness, and app commands without introducing a separate workflow.

This journey reveals requirements for system tray integration, shared capture state across tray and main window, capture command availability, current shortcut display, HDR status in tray, open settings, open main window, quit, and deterministic resource cleanup on exit.

### Journey 3: Priya Configures Output Once and Expects Capture to Obey It

Priya wants screenshots copied to the clipboard for quick chat sharing during the day, but saved to a folder when documenting issues. She opens Lumiere settings and chooses output target, save path, naming preference, shortcut bindings, and whether HDR-related warnings should appear.

Later, she triggers capture from a shortcut. Lumiere does not ask again where the screenshot should go; it follows the shared persisted settings. If the save path is invalid, the app reports a recoverable failure rather than silently losing output. If clipboard output is enabled, it provides a basic usable image without implying HDR preservation unless that path is validated.

The value moment is reliability: settings are not decorative UI. Main window, tray, hotkeys, and output pipeline all read the same source of truth.

This journey reveals requirements for persistent settings, shortcut configuration, shortcut conflict and registration failure handling, output target selection, save path selection and validation, timestamp naming, clipboard image option, HDR alert preference, about/version metadata, and settings state shared across all entry points.

### Journey 4: Daniel Encounters an HDR Readiness Problem and Still Knows What Happened

Daniel launches Lumiere on a Windows setup where the target display is not HDR-ready, Windows HDR is disabled, or the capture path cannot prove fidelity. He triggers capture expecting an HDR result.

Instead of pretending everything is fine, Lumiere shows a concise status: HDR ready, enable HDR, HDR unavailable, degraded preview, unsupported capture, or preview failed. The message is actionable and does not bury him in diagnostics during the capture moment. If capture cannot continue, the overlay closes or returns to an idle state without stranded topmost windows or active WGC resources.

The value moment is trust. Daniel may not get an HDR-verified result, but he understands why the capture is not trustworthy and does not mistake a degraded SDR-like output for a validated HDR capture.

This journey reveals requirements for evidence-based HDR state mapping, degraded/unsupported/failed status states, recoverable failure behavior, validation-language constraints, lifecycle teardown after failure, and optional diagnostics that do not dominate the MVP UI.

### Journey 5: The Developer Validates Repeated Capture Before Claiming Readiness

A Lumiere developer prepares a release candidate after implementing direct monitor capture, overlay crop, output, tray, and shortcuts. Automated tests pass, but the team cannot claim real HDR behavior from CI alone.

The developer runs the Windows validation checklist: direct monitor capture without picker, repeated start/stop/cancel/restart, release-to-output, invalid crop handling, Escape cancellation, multi-monitor placement, HDR/SDR displays, common DPI scales, clipboard/file output, and GPU memory or handle growth trends. Results are recorded as Windows manual validation where appropriate.

The value moment is disciplined evidence. Lumiere's PRD and implementation distinguish Mac edit, Windows CI-pass, and Windows manual-pass, preventing product claims from outrunning real platform behavior.

This journey reveals requirements for validation checklists, lifecycle evidence, deterministic teardown ordering, manual HDR hardware validation, multi-monitor testing, output verification, and explicit validation-level language in stories and release readiness.

### Journey Requirements Summary

The journeys require Lumiere to support five capability areas:

1. Low-interruption capture entry: global shortcuts, tray commands, compact main window actions, no picker-first default path, and shared capture state across entry points.
2. HDR-first capture and overlay workflow: direct monitor capture, FP16/scRGB preview, fullscreen overlay, valid region crop, release-to-capture, Escape/cancel, invalid crop handling, and stable overlay placement.
3. Honest output behavior: configured clipboard/folder/both output, save path handling, timestamp naming, basic clipboard image support, and no unsupported HDR-preservation claims.
4. Trustworthy state and recovery: evidence-based HDR readiness, degraded/unsupported/failed states, clear user-facing messages, failure recovery, and deterministic resource teardown.
5. Validation and maintainability: automated quality gates, Windows manual validation checklists, lifecycle stability evidence, and multi-monitor/HDR/DPI coverage.

## Domain-Specific Requirements

### Compliance & Regulatory

Lumiere has no domain-specific regulatory regime such as HIPAA, PCI-DSS, FDA, or financial compliance in MVP scope.

The relevant compliance-like requirement is product-claim discipline: the app must not claim HDR preservation, HDR readiness, or output fidelity unless the implementation has Windows manual validation evidence for the relevant path. PRD, story, UI, and release language must distinguish implementation intent from validated behavior.

### Technical Constraints

Lumiere is Windows-only for MVP and targets native Windows desktop APIs: WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, WinRT/COM interop, and x64 .NET.

The primary capture and preview path must preserve the HDR-first invariants: FP16 WGC frames, FP16 DXGI swap chain, scRGB color space, GPU-resident preview, typed readiness states, and no silent fallback to SDR bitmap preview.

All WGC, D3D11, DXGI, WinRT, COM, HWND, HMONITOR, tray, and global hotkey details must remain behind narrow module boundaries. UI code may orchestrate workflows but must not own native capture or graphics resource semantics.

Clipboard and file output require explicit fidelity semantics. A basic clipboard bitmap may be useful for MVP, but it must not be described as HDR-preserving unless supported by a concrete format, conversion policy, metadata strategy, target-app compatibility checks, and Windows manual validation.

### Integration Requirements

Lumiere must integrate with Windows system surfaces that are outside ordinary WinUI controls: system tray, global hotkeys, monitor-targeted capture, clipboard, file picker/folder picker, and display/HDR capability signals.

Tray and hotkey integration must share the same capture/session state as the main window and overlay. They must not create parallel capture paths, parallel settings state, or conflicting concurrent sessions.

Settings must be persisted locally and consumed by all entry points: main window, tray, hotkeys, output pipeline, and HDR alert behavior.

### Risk Mitigations

HDR fidelity risk is mitigated by preserving FP16/scRGB constants, blocking SDR fallback in the main preview path, and requiring Windows manual validation before product claims.

Interop risk is mitigated by isolating COM/WinRT/Win32 calls in infrastructure boundaries, using deterministic disposal, recording operation/stage/technical details for failures, and treating COM pointer ownership as a review-sensitive area.

Lifecycle risk is mitigated through a single capture session state model, generation-scoped callbacks, deterministic WGC and swap-chain teardown, detach-before-release semantics, and repeated lifecycle validation.

UX trust risk is mitigated through lightweight but accurate states: HDR ready, enable HDR, HDR unavailable, degraded preview, unsupported capture, preview failed, and output complete or failed. The UI must not use success language for degraded or unverified states.

Scope risk is mitigated by keeping MVP focused on capture, output, settings, tray, hotkeys, and trustworthy status. Gallery, annotation-heavy editing, onboarding, advanced export workflows, and history remain out of MVP unless explicitly pulled back into scope.

## Innovation & Novel Patterns

### Detected Innovation Areas

Lumiere's innovation is not a new interaction metaphor; it is a fidelity-first redefinition of a familiar screenshot workflow. The product keeps the user-facing behavior simple while changing the technical premise underneath: screenshots of HDR desktop content should not default to an SDR bitmap path that loses brightness, contrast, or color relationships.

The novel pattern is the combination of:

- HDR-first capture architecture: Windows Graphics Capture, FP16 frame handling, FP16 DXGI swap-chain preview, and scRGB readiness as first-class product constraints.
- Low-interruption workflow: shortcut, tray, or compact main window entry; no picker-first default path; direct overlay region selection; release-to-capture; configured output without a capture-time export wizard.
- Evidence-based state language: HDR readiness and degradation are product states backed by system/display/capture/preview/output evidence, not marketing labels.

### Market Context & Competitive Landscape

Built-in screenshot tools solve general capture but do not make HDR fidelity the primary product promise. Their typical value is convenience, annotation, sharing, or OS integration; Lumiere's value is trustworthy HDR capture with a minimal Windows-native workflow.

Lumiere should not compete by adding a gallery, editor, onboarding flow, or annotation suite in MVP. It competes by being the tool users reach for when ordinary screenshots make HDR content look wrong.

### Validation Approach

The innovative claim must be validated at three levels:

- Product workflow validation: users can trigger capture, select fullscreen or region, release, and receive configured output without leaving their current workflow.
- Technical fidelity validation: the capture and preview path preserves FP16/scRGB invariants and does not silently fall back to SDR bitmap presentation.
- Trust validation: UI state accurately distinguishes ready, degraded, unavailable, unsupported, failed, and completed states, with Windows manual validation before public HDR claims.

### Risk Mitigation

The main risk is overclaiming HDR correctness before output semantics are proven. Mitigation: keep HDR claims tied to validation level and separate basic clipboard usability from HDR-preserving output.

The second risk is workflow complexity. Mitigation: defer gallery, annotation, onboarding, advanced export flows, and history until after the MVP proves the quiet capture loop.

The third risk is platform fragility. Mitigation: keep WinRT/COM/Win32/DXGI interop behind narrow boundaries, require deterministic disposal, and treat Windows manual validation as a release-readiness gate.

## Desktop App Specific Requirements

### Project-Type Overview

Lumiere is a native Windows desktop application, not a web app, mobile app, Electron app, Tauri app, or cross-platform screenshot utility. The MVP targets Windows x64 with WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and WinRT/COM interop.

The product must behave like a lightweight Windows utility: launchable as a normal app, usable from global shortcuts and tray commands, capable of running quietly in the background, and able to return users to their previous workflow immediately after capture.

### Platform Support

MVP platform support is Windows-only. The project targets `.NET 10`, `net10.0-windows10.0.19041.0`, and x64. macOS may be used as an editing environment, but product behavior must be validated on Windows.

No cross-platform UI framework should be introduced for MVP. React, Tailwind, shadcn, Radix, Next.js, Electron, Tauri, WPF bitmap-first flows, WinForms, GDI screenshot paths, and web UI dependencies are out of scope for production implementation.

Windows manual validation is required for platform behavior that CI cannot prove: WGC capture, HDR display behavior, DXGI/scRGB presentation, tray interaction, global hotkeys, overlay topmost behavior, clipboard/file output, multi-monitor behavior, DPI scaling, and repeated lifecycle stability.

### System Integration

Lumiere must integrate with Windows system surfaces through narrow platform boundaries:

- Windows Graphics Capture for capture session creation and frame delivery.
- Direct3D 11 and DXGI for FP16/scRGB preview and presentation.
- WinUI 3 and Windows App SDK for app shell, settings, overlay, and windowing.
- Win32/COM interop for monitor-targeted capture, tray icon behavior, global hotkeys, HWND/AppWindow behavior, and native lifecycle details.
- Windows clipboard and file/folder picker APIs for output.
- Windows display/HDR capability signals for readiness and status mapping.

All system integration must share a single capture/session state model. Main window, overlay, tray, hotkeys, and output pipeline must not create conflicting capture state, duplicate settings state, or parallel platform abstractions.

### Update Strategy

MVP does not require a full auto-update system. Packaging, installer, update channel, signing, and release distribution can remain post-MVP unless needed for Windows manual validation or early tester distribution.

The PRD should treat update behavior as a growth requirement. MVP stories should focus on capture fidelity, workflow correctness, settings persistence, tray/hotkeys, output behavior, and validation evidence before introducing installer or auto-update complexity.

### Offline Capabilities

Lumiere must be fully local and offline for MVP. Captured content, settings, diagnostics, and output behavior must not depend on network services, cloud storage, telemetry, upload flows, remote processing, or account login.

Local-only operation is part of the trust model: screenshots may contain sensitive screen content, and the MVP should not introduce unnecessary data movement or remote dependencies.

### Technical Architecture Considerations

The architecture must preserve module boundaries:

- `Lumiere.App` owns startup, composition, and high-level flow orchestration.
- `Lumiere.Capture` owns WGC target selection, capture lifecycle, session state, and frame-pool behavior.
- `Lumiere.Graphics` owns D3D11/DXGI resources, HDR constants, swap-chain presentation, and preview readiness evidence.
- `Lumiere.Overlay` owns fullscreen overlay UI, crop interaction, pointer/keyboard input, and confirmation payloads.
- `Lumiere.Infrastructure` owns WinRT/COM/Win32 interop, diagnostics primitives, and native failure mapping.
- `Lumiere.Settings` owns local preference persistence and validation.

No UI layer should own WGC, D3D11, DXGI, COM pointer, frame-pool, swap-chain, or native monitor handle lifetimes directly.

### Implementation Considerations

Existing Epic 1-3 code and validation documents must be preserved as historical foundation from the pre-rebaseline route. The updated MVP route continues from Epic 4 by refactoring prior implementation into the new product shape or adding missing MVP capabilities on stable foundations.

Implementation should prioritize:

1. Retaining FP16/scRGB invariants and preventing accidental SDR fallback.
2. Aligning main window, tray, shortcuts, settings, overlay, and output behavior with the v0 MVP reference.
3. Turning output semantics into honest, validated behavior rather than UI-only options.
4. Keeping direct monitor capture, overlay placement, crop release, and resource teardown stable across repeated Windows sessions.
5. Recording validation level explicitly for each feature: Mac edit, Windows CI-pass, or Windows manual-pass.

## Project Scoping

### Strategy & Philosophy

**Approach:** Single-release MVP rebaseline

The MVP strategy is to ship a coherent native Windows HDR screenshot loop, not a broad screenshot suite. The release must prove that Lumiere can combine HDR-first capture fidelity, low-interruption workflow, trustworthy state feedback, and practical output behavior in one usable desktop utility.

**Resource Requirements:** The MVP requires Windows desktop, WinUI, WGC, D3D11/DXGI, WinRT/COM interop, shell integration, settings persistence, and QA validation expertise. Windows manual validation on HDR-capable hardware is required before claiming release readiness.

### Brownfield Planning Constraint

Existing Epic 1-3 implementation and validation artifacts remain preserved as historical foundation work from the pre-rebaseline route. Rebaselined MVP implementation planning begins from Epic 4, using later epics to refactor prior foundation work into the updated product shape or continue implementation where the foundation remains valid.

### Complete Feature Set

**Core User Journeys Supported:**

- Capture HDR content from the current workflow with shortcut, tray, or main window entry.
- Use tray commands without bringing the main window forward.
- Configure output and shortcuts once, then have all entry points obey the same settings.
- Understand degraded, unavailable, unsupported, failed, and completed states without misleading HDR claims.
- Validate repeated capture behavior before release.

**Must-Have Capabilities:**

- Native WinUI main window with Lumiere identity, fullscreen capture, region capture, shortcut display, HDR status summary, minimize/background intent, and settings entry.
- System tray menu with Lumiere status, fullscreen capture, region capture, shortcut labels, open main window, settings, and quit.
- Global shortcuts for fullscreen and region capture with conflict/registration failure handling.
- Direct monitor capture as the default MVP path, without picker-first interruption.
- Fullscreen overlay with FP16/scRGB preview, region crop, invalid crop handling, Escape/cancel, and release-to-capture.
- Shared capture/session state across main window, tray, hotkeys, overlay, and output pipeline.
- Settings for shortcuts, HDR alerts, output target, save path, capture-after behavior where applicable, timestamp naming, clipboard image option, export/color format presentation only where backed by implementation semantics, and about/version info.
- Output to clipboard, folder, or both according to settings, with clear recoverable failure behavior.
- Evidence-based HDR status mapping: HDR ready, enable HDR, HDR unavailable, degraded preview, unsupported capture, preview failed, and output completion/failure.
- Preservation of HDR-first invariants: FP16 WGC frame path, FP16 DXGI swap-chain preview, scRGB readiness, no silent SDR preview fallback.
- Local-only operation with no cloud upload, account requirement, telemetry dependency, or remote processing.
- Validation records distinguishing Mac edit, Windows CI-pass, and Windows manual-pass.

**Nice-to-Have Capabilities:**

- Installer, signing, release channel, and auto-update behavior.
- Advanced diagnostics UI beyond concise status and validation records.
- Rich filename templates beyond timestamp preference.
- Broader paste-target compatibility matrix.
- Refined HDR/SDR output profiles and advanced tone-mapping controls.
- Capture history, gallery, annotation tools, onboarding, and editor-like workflows.

### Risk Mitigation Strategy

**Technical Risks:** HDR fidelity, output semantics, COM/WinRT interop, tray/hotkey behavior, multi-monitor placement, and resource lifecycle are the highest-risk areas. Mitigation is to preserve module boundaries, keep native resources deterministically disposed, require Windows manual validation, and avoid unsupported HDR claims.

**Market Risks:** Users may not accept a tool that is technically correct but slower than built-in screenshot tools. Mitigation is to make the capture loop low-interruption: shortcut/tray/main entry, direct overlay, release-to-capture, configured output, and immediate return to workflow.

**Resource Risks:** If implementation capacity is constrained, do not weaken the HDR path. Reduce polish, installer scope, diagnostics depth, or advanced output options first. The minimum viable product must still preserve fidelity-first capture, direct region workflow, honest status, and configured output.

## Functional Requirements

### Capture Entry & Session Control

- FR1: Users can start a fullscreen capture from the main window.
- FR2: Users can start a region capture from the main window.
- FR3: Users can start fullscreen and region capture through global shortcuts.
- FR4: Users can start fullscreen and region capture from the system tray.
- FR5: Users can keep Lumiere available through a background or tray-oriented workflow after leaving the main window.
- FR6: Users can cancel an active capture flow and return to a recoverable idle state.
- FR7: The system prevents conflicting capture sessions from running at the same time.
- FR8: The system can recover from capture startup failure without leaving active capture resources or stranded overlay windows.

### HDR Readiness & Trust Feedback

- FR9: Users can see a concise HDR status summary from the main window.
- FR10: Users can see a concise HDR status summary from the tray menu.
- FR11: Users can distinguish HDR ready, HDR available but not enabled, HDR unavailable, degraded preview, unsupported capture, preview failed, and output completion or failure states.
- FR12: Users can receive actionable HDR-related alerts when HDR is unavailable, degraded, unsupported, or failed.
- FR13: Users can disable or enable HDR-related alerts in settings.
- FR14: The system can represent capture and preview trust as typed states instead of treating all successful starts as trustworthy HDR capture.

### Overlay & Region Selection

- FR15: Users can enter the default region capture flow without first choosing a target through a system picker.
- FR16: Users can select a region by dragging over a fullscreen overlay.
- FR17: Users can complete a valid region capture by releasing the pointer.
- FR18: Users can cancel region capture with Escape or an available cancel path.
- FR19: Users can attempt a new region selection after an invalid or too-small crop without producing output.
- FR20: Users can distinguish active, invalid-region, completed, canceled, degraded, unsupported, and failed region-capture states through overlay or status feedback.
- FR21: The overlay can remain interactive for crop input while displaying status and cancellation controls.

### Output Behavior

- FR22: Users can choose whether captures output to clipboard, folder, or both.
- FR23: Users can choose or change the save folder when file output is enabled.
- FR24: Users can receive completion feedback that identifies which configured output targets succeeded.
- FR25: Users can receive recoverable failure feedback that identifies which configured output target failed and whether retry or settings correction is needed.
- FR26: Users can enable or disable timestamp-based file naming.
- FR27: Users can enable or disable clipboard image output when clipboard output is part of the selected output target.
- FR28: The system can apply output settings consistently across main window, tray, shortcut, fullscreen, and region capture flows.
- FR29: The system can present export or color format options only where the product has defined implementation semantics for them.

### Settings & Preferences

- FR30: Users can open settings from the main window.
- FR31: Users can open settings from the tray menu.
- FR32: Users can configure fullscreen capture and region capture shortcuts.
- FR33: Users can restore or recover from invalid, conflicting, or unregistered shortcut choices.
- FR34: Users can configure output target preferences.
- FR35: Users can configure save path preferences.
- FR36: Users can configure supported after-capture behavior for opening or revealing an output artifact when the selected output target produces one.
- FR37: Users can view application name, version, and brief product description.
- FR38: The system persists settings locally and reuses them across app launches.

### Tray & Background Operation

- FR39: Users can open the tray menu while Lumiere is running in the background.
- FR40: Users can open the main Lumiere window from the tray.
- FR41: Users can start capture commands from the tray without duplicating capture state.
- FR42: Users can quit Lumiere from the tray.
- FR43: The system releases capture, overlay, tray, hotkey, and graphics resources when quitting.

### Validation & Diagnostics

- FR44: Developers can record validation level for each implemented capability as Mac edit, Windows CI-pass, or Windows manual-pass.
- FR45: Developers can validate repeated capture lifecycle behavior across start, cancel, restart, failure, and output flows.
- FR46: Developers can validate direct monitor capture without picker on Windows hardware.
- FR47: Developers can validate overlay behavior across HDR/SDR displays, multi-monitor placement, and common DPI scaling values.
- FR48: Developers can validate clipboard and file output behavior against configured settings.
- FR49: The system can retain structured diagnostic context for capture, preview, output, and interop failures, including operation, stage, mapped user-facing state, and technical detail needed for engineering triage.

## Non-Functional Requirements

### Performance

- NFR1: Capture entry responsiveness SHALL be validated on Windows reference hardware: elapsed time from user trigger through shortcut, tray, or main window to capture-active state SHALL be recorded at p50 and p95, and p95 SHALL NOT regress beyond the documented prior baseline without an explicit acceptance rationale.
- NFR2: Region selection pointer feedback SHALL remain visually continuous during drag, resize, invalid-crop, and release-to-capture interactions on supported Windows hardware; validation SHALL record pass/fail across the DPI scales listed in the manual test plan.
- NFR3: Overlay status, crop visuals, and completion feedback SHALL NOT resize, rescale, displace, or destabilize the HDR preview surface during a capture session; visual validation SHALL confirm stable preview framing while chrome updates.
- NFR4: Clipboard or file output, including slow or failing writes, SHALL NOT leave the overlay, WGC session, or graphics resources active indefinitely; validation SHALL confirm the session returns to a defined idle or disposed state within a bounded timeout documented by the test plan.
- NFR5: Repeated capture cycles across start, cancel, restart, release-to-output, and quit SHALL NOT produce monotonic growth beyond documented noise thresholds in selected resource indicators such as private bytes, handles, or GPU allocator trends; Windows validation SHALL compare baseline and post-cycle metrics across a defined cycle count.

### HDR Fidelity & Output Integrity

- NFR6: The primary capture and preview path SHALL preserve HDR-first invariants: FP16 WGC frames, FP16 DXGI swap-chain presentation, scRGB readiness evidence, and GPU-resident preview; review or automated checks SHALL verify configured formats and presentation path alignment.
- NFR7: The authoritative live HDR preview SHALL NOT be replaced by `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU bitmap readback, SDR texture fallback, or ordinary XAML bitmap `Image` presentation; exceptions SHALL be explicitly documented and SHALL NOT be marketed as HDR-equivalent preview.
- NFR8: Clipboard or file output SHALL NOT be described as HDR-preserving unless a written record exists for that path covering format choice, conversion or metadata policy, target-app assumptions where relevant, and Windows manual validation results.
- NFR9: Export or color-format options SHALL be hidden, disabled, or explicitly scoped when fidelity semantics are undefined; UI review SHALL confirm users cannot select options that imply validated HDR preservation without evidence.
- NFR10: HDR readiness and trust states SHALL be backed by capability, preview, and output evidence per the product state model; degraded, unvalidated, unsupported, or failed states SHALL NOT use success or completed language.

### Reliability & Resource Lifecycle

- NFR11: Capture cancellation, failure, restart, main-window close, and app quit SHALL deterministically dispose or hand off WGC session, frame pool, frames, swap chain, overlay, tray, hotkeys, and related native resources; Windows validation SHALL include teardown checks after each scenario class.
- NFR12: Preview teardown SHALL detach presentation from the UI surface before releasing DXGI swap-chain resources; ordering SHALL be enforced by review and covered by targeted lifecycle tests or inspections where feasible.
- NFR13: Capture callbacks, output completion handlers, diagnostics, and overlay updates SHALL be generation-scoped or equivalently session-token-scoped so stale async work cannot mutate UI or session state after a newer capture begins; automated tests SHALL cover stale completion rejection.
- NFR14: Failed capture startup, failed direct monitor resolution, failed overlay creation, failed clipboard write, and failed file write SHALL leave the application in a recoverable idle state with explicit user-facing failure feedback; validation SHALL include scripted failure injections for each class.
- NFR15: Ordinary stop or restart of capture SHALL NOT dispose the shared graphics device unless the application is shutting down or executing a documented device-loss recovery path; code review SHALL confirm capture recycling does not recreate the device per session by default.

### Privacy & Local Operation

- NFR16: MVP operation SHALL be fully local: capture, preview, settings, and output SHALL NOT require account login, cloud upload, remote processing, telemetry collection endpoints, or general network availability; validation SHALL demonstrate core flows with network disconnected.
- NFR17: Logs and diagnostics SHALL NOT include screenshot pixel data, raw frame dumps, or other screen content payloads; spot checks of generated logs during capture scenarios SHALL confirm absence of content payloads.
- NFR18: File output SHALL respect the configured save location and SHALL surface permission, missing path, or write failures without silent drop; tests SHALL include invalid paths and permission-denied cases where practical.
- NFR19: Clipboard output SHALL follow the user's configured output targets and SHALL accurately represent behavior under normal Windows clipboard semantics; settings UI SHALL NOT imply private vault storage beyond the OS clipboard model.

### Accessibility & Usability

- NFR20: Users SHALL have a reliable cancel path during capture, including keyboard Escape whenever the overlay can safely close; Windows manual validation SHALL verify cancel behavior for region capture and related flows.
- NFR21: HDR, degraded, unsupported, failed, and completed states SHALL be distinguishable without relying on color alone; UX review SHALL validate text and/or icon discrimination using a rendered state inventory.
- NFR22: Main window, tray, settings, and overlay controls SHALL use concise, native-feeling language during capture; primary capture surfaces SHALL NOT require reading long diagnostic paragraphs to understand next actions.
- NFR23: Tray and global shortcut workflows SHALL support completing the default capture flows without opening the main window; journey validation SHALL include tray-only and shortcut-only happy paths.
- NFR24: Settings SHALL NOT present options as fully supported capabilities when underlying semantics are absent; release QA SHALL cross-check controls against the implemented behavior matrix.

### Windows Integration Compatibility

- NFR25: The shipping product SHALL remain Windows-only and aligned to the approved desktop stack: `.NET 10`, `net10.0-windows10.0.19041.0` targeting minimum, x64, WinUI 3, Windows App SDK, WGC, D3D11, DXGI, and WinRT/COM interop; release packaging metadata SHALL match these constraints.
- NFR26: Tray, hotkeys, monitor-targeted capture, overlay windowing, clipboard, and picker integrations SHALL keep raw HWND, HMONITOR, COM, and DXGI ownership inside narrow platform boundary layers; UI orchestration SHALL depend on facades or interfaces rather than owning native lifetimes directly.
- NFR27: Release claims about multi-monitor placement, HDR/SDR mixed setups, common DPI scaling values, fullscreen or disruptive cases, and display topology changes SHALL be supported by recorded Windows manual validation against an explicit scenario list; gaps SHALL be documented as limitations rather than implied guarantees.
- NFR28: MVP SHALL NOT take architectural dependencies on web UI stacks, Electron/Tauri shells, cross-platform UI frameworks, cloud sync services, gallery or annotation suites, or SDR-first screenshot libraries called out as out of scope; dependency review SHALL be part of release readiness.

### Maintainability & Validation

- NFR29: The codebase SHALL preserve strict separation of concerns among application shell and workflow orchestration, capture session lifecycle, graphics and presentation, overlay interaction, native interop and diagnostics, and local settings persistence, such that UI layers do not directly own WGC, D3D, DXGI, COM resource lifetimes, or low-level monitor handles.
- NFR30: Platform interop failures SHALL be diagnosable with structured context including operation, stage, mapped user-facing status, and technical detail sufficient for engineering triage; sampling of failure logs SHALL confirm required fields are populated for representative failures.
- NFR31: HDR constants and readiness mapping SHALL have a single authoritative source of truth and SHALL be protected by automated tests; changes to constants or mapping SHALL update tests or fail the automated gate.
- NFR32: The Windows integration pipeline SHALL execute the repository's agreed automated quality gates end-to-end without unapproved waivers; mainline health SHALL be defined as passing those gates, with the exact gate set and runner configuration documented outside this PRD.
- NFR33: Behavior that cannot be proven in non-hardware automation, including real HDR displays, WGC timing, tray/global hotkeys, and multi-monitor geometry, SHALL carry an explicit validation level in implementation records; public-facing HDR and display fidelity claims SHALL only reference Windows hardware-level validation evidence.
