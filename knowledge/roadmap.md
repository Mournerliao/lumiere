# Product Roadmap

Status belongs in `state/CURRENT.md`; GitHub Issues own executable work. This file
defines the durable route, milestone order, deliverables, and exit gates.

## Route

Lumiere advances through four milestones. A later milestone may be researched early,
but it does not become the implementation frontier until the preceding exit gate passes.

| Milestone | Product outcome | Exit gate |
|---|---|---|
| 0. Cross-platform foundation | Final platform-owned repository with honest native-host seams | Layout, shell, protocol, and Windows-library gates pass |
| 1. HDR-aware MVP | Windows and macOS capture to sRGB Visual Match | Both platforms pass runtime, hardware, and distribution verification |
| 2. HDR-preserved export | One narrow, named HDR artifact path | Format/viewer contract and per-platform hardware verification pass |
| 3. Cross-platform HDR fidelity | Measured consistency across a named support matrix | Published tolerances and fixed-scene verification pass |

## Milestone 0: Cross-Platform Foundation

### Deliverables

- Electron/React/TypeScript shell that runs on Windows and macOS.
- Sandboxed renderer, context-isolated preload, validated IPC sender and payloads.
- A language-neutral JSON Lines platform-host protocol for capability queries and capture commands.
- Explicit `host-unavailable` behavior; no Electron desktop-capture fallback.
- Native ownership retained: a paused C# WGC/D3D11/DXGI engine on Windows and a
  documented Swift ScreenCaptureKit seam on macOS.
- Final `apps/`, `protocol/`, `hosts/`, and `knowledge/` ownership layout.
- WinUI, release-validation scaffolding, and experimental HDR10/JXR export removed.

### Exit Gate

- Frozen install, type checks, protocol tests, and production build pass.
- The production build launches from local `file://` content on macOS.
- Windows CI proves the same shared shell and the three preserved .NET engine modules build.
- No capture or HDR claim is inferred from shell-only verification.

## Milestone 1: Cross-Platform HDR-Aware MVP

The MVP has four ordered vertical slices. Each slice must leave the product runnable;
platform implementation may differ, but public output semantics remain shared.

### 1A. macOS Native Capture

Deliver a Swift host that owns Screen Recording permission, ScreenCaptureKit target
discovery, display capture, HDR capability reporting, native-resource disposal, and
one sRGB Visual Match file result through the platform-host interface.

Exit when one ordinary scene and the fixed bright/dark scenes can be captured on a
named Mac/display without passing raw HDR frames through Electron.

### 1B. Windows Native Host Adapter

Extract or wrap the existing C# WGC/D3D11/DXGI implementation behind the same
platform-host interface. Preserve the shared Windows tone mapper and keep clipboard
and folder delivery downstream from one conversion result.

Exit when the Electron shell can drive the existing Windows engine through repeat,
cancel, clean-exit, and HDR-state scenarios without regressing the native baseline.

### 1C. Shared Capture Product Surface

Deliver region and display capture, shared overlay interaction, main window,
tray/menu-bar, configured shortcut, settings, and clipboard/folder/both delivery.
Platform permission and native failure flows remain owned by their native hosts.

Exit when both platforms complete the same named user journeys and all user-facing
copy remains inside the approved HDR-aware/sRGB Visual Match claim boundary.

### 1D. Distribution And Release Verification

Deliver a traditional Windows setup executable and a signed/notarized macOS artifact.
Validate install, launch, upgrade, uninstall, and reinstall on non-development machines.

Release only after independent Windows and macOS repository, runtime, fixed-scene
Visual Match, receiving-app, repeat-loop, and clean-exit verification passes. One platform's
result never substitutes for the other.

## Milestone 2: One HDR-Preserved Export Path

Choose one format and named viewers before implementation or public claims. Define
source/destination pixel formats, transfer function, primaries, tone/gamut policy,
metadata, file extension, clipboard policy, and viewer assumptions.

A platform may first prove a narrower native path, but Lumiere only presents a shared
product mode after the contract and hardware verification cover every claimed platform.
Codec availability or high bit depth alone never satisfies the exit gate.

## Milestone 3: Cross-Platform HDR Fidelity

Measure fixed scenes across a named Windows/macOS display and viewer matrix. Define
observable tolerances before using fidelity language, then expand mixed HDR/SDR
topologies, DPI/Retina behavior, accessibility, viewers, and stability duration.

This milestone aims for documented, measured consistency. It does not promise
display-independent identity or universal fidelity.

## Non-Goals

Lumiere is not a general image editor, HDR mastering tool, cloud sharing product,
or promise of display-independent identity across all monitors and viewers.
