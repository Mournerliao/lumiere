---
stepsCompleted:
  - 1
  - 2
  - 3
  - 4
  - 5
  - 6
inputDocuments:
  - harness/planning/project-plan.md
  - harness/design/index.md
  - harness/design/v0-mvp-reference/README.md
workflowType: research
lastStep: 6
research_type: technical
research_topic: >-
  Lumiere MVP native implementation aligned with harness/design/v0-mvp-reference
  (WinUI 3 shell, system tray, global hotkeys, WGC FP16 capture, D3D11/scRGB preview,
  fullscreen overlay region selection, clipboard/file output, HDR status UX)
research_goals: >-
  Provide PRD- and architecture-ready technical grounding: map v0 MVP surfaces to Windows APIs,
  interop boundaries, HDR invariants, output encoding realities, and validation risks—without
  treating the React prototype as a behavioral specification.
user_name: lumiere
date: '2026-05-09'
web_research_enabled: true
source_verification: true
---

# Research Report: technical

**Date:** 2026-05-09  
**Author:** lumiere  
**Research Type:** technical  

---

## Research Overview

This report updates technical research for **Lumiere**: a **native WinUI 3** HDR-first screenshot tool whose **MVP UX scope** is defined by [`harness/design/v0-mvp-reference/`](../../../harness/design/v0-mvp-reference/) and [`harness/design/index.md`](../../../harness/design/index.md). The prototype covers **main panel**, **settings**, **tray menu**, and **HDR status simulation**; production behavior must come from **Windows.Graphics.Capture (WGC)**, **D3D11/DXGI**, and **WinUI**—not from Next.js/React.

Key findings (see **Executive Summary** in the synthesis section): **WinUI 3 has no first-class system-tray control** (expect **Win32 `Shell_NotifyIcon`** or a vetted helper library); **global hotkeys** typically require **`RegisterHotKey` + `WM_HOTKEY`** via a **message window or subclass/hook**; **picker-free monitor capture** is supported via **`IGraphicsCaptureItemInterop::CreateForMonitor`**; **FP16 frame pools** are a documented **`DirectXPixelFormat`** path but **device/format support and color-space correctness must be validated on real HDR hardware**; **clipboard** paths are **not a native HDR container**—product claims for “HDR10 / P3 / sRGB” export options must be mapped to **real encoders + metadata + validation**.

---

<!-- Sequential workflow content below -->

## Technical Research Scope Confirmation

**Research Topic:** Lumiere MVP native implementation aligned with `harness/design/v0-mvp-reference` (WinUI 3 shell, system tray, global hotkeys, WGC FP16 capture, D3D11/scRGB preview, fullscreen overlay region selection, clipboard/file output, HDR status UX).

**Research Goals:** Provide PRD- and architecture-ready technical grounding: map v0 MVP surfaces to Windows APIs, interop boundaries, HDR invariants, output encoding realities, and validation risks—without treating the React prototype as a behavioral specification.

**Technical Research Scope:**

- Architecture Analysis — layering, lifecycle, threading, composition
- Implementation Approaches — WinUI + Win32 interop patterns, capture/output pipelines
- Technology Stack — .NET, WinUI 3, WinRT, D3D11/DXGI, WGC
- Integration Patterns — WinRT/COM interop, clipboard/file formats, shell integration
- Performance Considerations — frame latency, GPU memory churn, multi-monitor

**Research Methodology:**

- Public documentation and issue trackers (Microsoft Learn, WinUI repos) with URLs cited
- Cross-check against Lumiere harness constraints ([`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md), [`AGENTS.md`](../../../AGENTS.md))
- Confidence notes where ecosystem guidance is “patterns + samples” rather than a single normative doc

**Scope Confirmed:** 2026-05-09 (executed in a single session per explicit user request to rerun technical research)

---

## Technology Stack Analysis

> Note: This MVP is a **desktop GPU capture utility**, not a cloud or web service. Sections below emphasize **native Windows** stack items relevant to v0 MVP surfaces.

### Programming Languages

- **C# / .NET**: Primary language for Lumiere per project harness (`net10.0-windows10.0.19041.0`). Modern interop is commonly done via **CsWin32** source-generated P/Invoke (community write-ups and tooling references exist; evaluate against repo standards).
- **C++/WinRT**: Alternative for lowest-level samples (Microsoft capture samples), but not required if C# interop boundaries are kept narrow.

_Source (examples / ecosystem): [Chapter 5: Add global hot key in WinUI 3 – Whid](https://whid.eu/2022/05/13/chapter-5-add-global-hot-key-in-winui-3/) (mentions CsWin32); [RegisterHotKey function (Win32)](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)_

### Development Frameworks and Libraries

- **WinUI 3 + Windows App SDK**: Application UI framework for main window and settings UX ([`SwapChainPanel` class docs](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.swapchainpanel?view=windows-app-sdk-1.7)).
- **Windows WinRT APIs**: `Windows.Graphics.Capture` for capture; WinRT interop for `GraphicsCaptureItem` creation from monitor handles.
- **Direct3D 11 / DXGI**: Swap chains and HDR/scRGB presentation paths (project invariants in [`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md)).
- **Vortice.Windows** (optional): D3D11/DXGI convenience wrapper—already aligned with repo direction in [`AGENTS.md`](../../../AGENTS.md).

_Source: [Direct3D11CaptureFramePool class](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-22000); [Direct3D11CaptureFramePool.Create](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.create?view=winrt-22000)_

### Database and Storage Technologies

- **Not applicable** as a primary architecture component for MVP.
- **Local persistence** for settings is expected to use **WinUI/Windows application data** patterns (e.g., local settings store / small file)—schema design should remain versioned and testable.

### Development Tools and Platforms

- **Visual Studio 2022** + Windows App SDK workload for WinUI debugging (see [`harness/workflows/cross-platform-development.md`](../../../harness/workflows/cross-platform-development.md)).
- **CI on Windows**: `dotnet restore/build/test/format` as recorded in [`AGENTS.md`](../../../AGENTS.md).
- **Hardware validation**: HDR correctness requires **Windows manual validation**; CI cannot substitute.

### Cloud Infrastructure and Deployment

- **Not applicable** for MVP product runtime. Distribution may use **MSIX** or packaged desktop conventions later; out of scope for this research pass except to note: enterprise policies can affect capture permissions and auto-start behaviors (needs story-time validation).

### Technology Adoption Trends

- **WinUI 3 ecosystem gap: system tray**: multiple community workarounds because **first-party tray support is still effectively a feature gap** tracked as proposals/issues.
- **HDR UI / composition**: issues and proposals indicate **HDR UI via scRGB/FP16 is not “solved by default”** in WinUI—teams should expect **edge cases when resizing/recreating swap chains** and validate on HDR displays.

_Source: [Proposal: System tray icon for WinUI 3 Desktop · Issue #2020](https://github.com/microsoft/microsoft-ui-xaml/issues/2020); [Using NotifyIcon in WinUI 3 | Albert Akhmetov (2025)](https://albertakhmetov.com/posts/2025/using-notifyicon-in-winui-3/); WinUI issue discussion on swap chain format changes ([example thread context in search synthesis](https://github.com/microsoft/microsoft-ui-xaml/issues/2761))_

---

## Integration Patterns Analysis

> Focus: how Lumiere integrates **OS shell**, **capture**, **composition**, and **output**—not microservices.

### WinRT interop: `GraphicsCaptureItem` without a picker

- For **monitor-targeted capture**, desktop apps use **`IGraphicsCaptureItemInterop::CreateForMonitor`** with an `HMONITOR`.
- Minimum supported client is documented as **Windows 10, version 1903 (build 18362)** per Microsoft Docs.

_Source: [IGraphicsCaptureItemInterop::CreateForMonitor](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor)_

### WGC frame delivery and D3D interop

- `Direct3D11CaptureFramePool` is the standard bridge between **WinRT capture** and **D3D11 textures**.
- Pixel format is selected via **`DirectXPixelFormat`**; FP16 formats are part of the broader DirectX pixel format catalog (verify on target GPUs).

_Source: [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-22000); Win2D pixel format reference lists `R16G16B16A16Float` as a float surface format ([WinUI3 pixel formats](https://microsoft.github.io/Win2D/WinUI3/html/PixelFormats.htm))_

### Clipboard integration (MVP settings include “Copy as Image”)

- **Classic clipboard bitmap formats** (`CF_BITMAP`, many `CF_DIB` paths) are **not HDR containers** and have **long-standing alpha/color-management inconsistencies** across apps.
- Practical implication: “copy as image” may require a **defined conversion policy** (tone mapping / clamp / format choice) and explicit QA targets (target apps: Snipping Tool paste targets, Paint, browsers, etc.).

_Source: [Standard Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats); practitioner notes on alpha/DIBv5 inconsistencies ([StackOverflow: Win32 clipboard and alpha channel images](https://stackoverflow.com/questions/15689541/win32-clipboard-and-alpha-channel-images))_

### Shell integration: system tray and context menus

- WinUI 3 apps commonly integrate tray icons via **`Shell_NotifyIcon`** and a **hidden message window** to receive tray notifications; this matches MVP’s **tray context menu** surface in the v0 reference.
- Third-party helpers exist (e.g., community libraries listed in discussions); adopt only if license and long-term maintenance fit the project bar.

_Source: [microsoft-ui-xaml Issue #4782 discussion context](https://github.com/microsoft/microsoft-ui-xaml/issues/4782); [StackOverflow: Shell_NotifyIcon in WinUI 3](https://stackoverflow.com/questions/69946433/shell-notifyicon-in-winui-3)_

### Global hotkeys (`WM_HOTKEY`)

- `RegisterHotKey` is the Win32 entry point; WinUI requires a strategy to receive **`WM_HOTKEY`** (message-only window, subclassing, or hooks). This maps to MVP settings for **fullscreen vs region** shortcuts.

_Source: [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey); WinUI discussion on `WM_HOTKEY` handling ([Issue #5815](https://github.com/microsoft/microsoft-ui-xaml/issues/5815))_

---

## Architectural Patterns and Design

### System architecture pattern (recommended)

- **Layered desktop architecture** with strict boundaries:
  - **Shell**: WinUI pages, navigation, tray UI orchestration
  - **Session controller**: single capture session state machine (idle → capturing → preview/commit → output)
  - **Capture service**: WGC lifecycle + device + frame pool
  - **Render/preview**: D3D11 presentation to `SwapChainPanel`
  - **Output**: encode + clipboard + file IO behind narrow interfaces
  - **Settings**: persistence + validation only (no platform truth)

This aligns with [`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md) “职责分层” guidance.

### Threading and composition

- **Rule**: WinRT capture callbacks can arrive off UI thread; **UI/composition updates must be marshalled** (WinUI `DispatcherQueue` patterns are standard for this class of app).

_Source: Lumiere harness constraints and WinUI threading practices (see WinUI hotkey articles referencing message dispatch)_

### HDR preview architecture risks

- Expect **swap chain recreation** rather than only resize when changing **pixel format / color space**, based on WinUI issue discussions around composition not tracking some swap chain format transitions cleanly.
- Treat “HDR UI” as **test-gated**: validate on HDR hardware with representative workloads.

_Source: [microsoft-ui-xaml Issue #2761 (swap chain format changes)](https://github.com/microsoft/microsoft-ui-xaml/issues/2761); related HDR UI proposal discussion ([Issue #777](https://github.com/microsoft/microsoft-ui-xaml/issues/777))_

### Security and privacy (desktop capture)

- Screen capture is **high sensitivity**. Architecture should centralize:
  - **exclusion of the app’s own overlay/window** where supported (project already mentions capture exclusion interop in codebase)
  - **logging redaction** (no raw frame payloads in logs)
  - **least privilege** for file paths and clipboard operations

---

## Implementation Approaches and Technology Adoption

### Technology adoption strategy for MVP

- **Adopt in this order** (matches [`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md) execution route):
  1. **End-to-end FP16 capture → D3D texture** on a chosen monitor path (`CreateForMonitor`) with clean disposal.
  2. **Preview** on FP16/scRGB swap chain attached to WinUI (`SwapChainPanel`).
  3. **Region overlay** interaction + commit semantics (“release valid crop completes”).
  4. **Output**: file save + clipboard behind explicit conversion policies.
  5. **Shell**: tray + global hotkeys + settings persistence.

### Development workflows

- Maintain **Mac-edit / Windows-validate** loop ([`harness/workflows/cross-platform-development.md`](../../../harness/workflows/cross-platform-development.md)): CI proves compile/test; HDR proves correctness.

### Testing and quality assurance

- **Automated**: unit tests for geometry, settings validation, pure C# logic (repo already has graphics/overlay test projects).
- **Manual**: HDR monitors, multi-monitor, mixed DPI, fullscreen exclusive apps (games), and clipboard paste into major consumers.

### Risk assessment and mitigation (MVP-critical)

| Risk | Mitigation |
| --- | --- |
| Tray + message loop complexity | Isolate in `Infrastructure`-style boundary; single owner for `WndProc`/`HWND_MESSAGE` window |
| Hotkey conflicts / OS reservations | User-configurable keys + conflict detection + graceful fallback UX |
| HDR preview mismatch vs export | Treat as one pipeline with explicit transforms; document “preview truth” vs “export truth” |
| Clipboard HDR expectations | Product language must not promise HDR clipboard unless a format/decoder contract is implemented and tested |
| WinUI composition edge cases | Prefer recreate swap chain on format changes; maintain golden manual tests |

### Technical Research Recommendations (MVP)

- **Implementation roadmap**: implement **monitor capture** first (matches “no picker-first interruption”), then **region overlay**, then **output**, then polish **tray/hotkeys**—or parallelize tray only after capture session ownership is stable.
- **Technology stack recommendations**: stay on **WinUI 3 + WGC + D3D11**; keep Win32 interop **thin and centralized**.
- **Skill development**: WinRT interop (`CreateForMonitor`), DXGI color spaces, HDR tone mapping for export, Win32 message handling.

---

# Lumiere MVP (v0 Design Reference) → Native Windows: Technical Research Synthesis

## Executive Summary

Lumiere’s MVP UX reference (`harness/design/v0-mvp-reference`) implies four user-visible surfaces—**main panel**, **settings**, **tray menu**, and **HDR status**—plus two capture modes (**fullscreen** and **region**) and flexible **output targets** (clipboard/folder/both). Translating this into a shipping WinUI 3 app depends on a small set of **high-leverage platform integrations**: **WGC** for capture, **D3D11/DXGI** for HDR-capable preview, **Win32** for **tray** and **global hotkeys**, and pragmatic **output encoding** policies for clipboard and files.

Verified public sources reinforce three engineering realities for planning:

1. **Tray is not a built-in WinUI control**; production apps typically use **`Shell_NotifyIcon`** and a **message-processing HWND** pattern (plus mature community helpers). ([GitHub Issue #2020](https://github.com/microsoft/microsoft-ui-xaml/issues/2020), [NotifyIcon write-up](https://albertakhmetov.com/posts/2025/using-notifyicon-in-winui-3/))
2. **Picker-free monitor capture is supported** via **`IGraphicsCaptureItemInterop::CreateForMonitor`** (1903+). ([Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor))
3. **HDR “truth” is not guaranteed by clipboard formats**; HDR pipelines must be validated on **Windows + HDR hardware**, and WinUI/HDR composition has **known edge cases** around swap chain format transitions. ([Issue #2761](https://github.com/microsoft/microsoft-ui-xaml/issues/2761))

**Top recommendations**

1. **Lock a session state machine early**: one owner for capture start/stop, preview mount/unmount, and output—prevents resource leaks and double-starts (aligns with harness disposal rules).
2. **Treat prototype export toggles (HDR10/P3/sRGB) as UX placeholders** until each maps to a concrete encoder, metadata, and QA matrix on Windows.
3. **Build tray/hotkeys behind a Win32 interop façade** with automated smoke tests on Windows and explicit HWND lifetime rules.
4. **Define clipboard behavior as an explicit policy** (e.g., sRGB 8-bit PNG vs tonemapped bitmap) rather than implying HDR fidelity.

## Table of Contents

1. Technical Research Introduction and Methodology  
2. MVP Surface → Platform Mapping  
3. Capture and Preview Pipeline (HDR Invariants)  
4. Shell: Tray + Global Hotkeys  
5. Output: Files + Clipboard Realities  
6. HDR Status UX → OS Signals (engineering checklist)  
7. Risks, Validation Gates, and Next Steps  
8. Sources  

## 1. Technical Research Introduction and Methodology

**Scope:** Map v0 MVP reference screens to **native implementation constraints** while preserving Lumiere’s HDR-first goals ([`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md)).

**Method:** Microsoft Learn docs for APIs; WinUI GitHub issues for ecosystem gaps; cross-check against harness validation language ([`harness/design/design-principles.md`](../../../harness/design/design-principles.md)).

**Limitations:** Public docs rarely specify “best HDR screenshot” semantics; empirical validation remains mandatory for color science claims.

## 2. MVP Surface → Platform Mapping

| v0 surface | Native implementation anchor |
| --- | --- |
| Main panel actions | WinUI window + view models; triggers session controller |
| Settings | WinUI settings UI + persistent settings store |
| Tray menu | Win32 `Shell_NotifyIcon` + context menu loop ([Issue #4782](https://github.com/microsoft/microsoft-ui-xaml/issues/4782)) |
| HDR status | Derived from display/HDR readiness APIs + validation labeling in UI copy |
| Fullscreen capture | `CreateForMonitor` + WGC session ([Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor)) |
| Region capture | Fullscreen transparent overlay + crop geometry + compose/blit policy |
| Hotkeys | `RegisterHotKey` + `WM_HOTKEY` dispatch ([Microsoft Learn](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)) |

## 3. Capture and Preview Pipeline (HDR Invariants)

**Invariant alignment:** Lumiere harness requires FP16 capture and FP16/scRGB preview path for HDR fidelity ([`harness/planning/project-plan.md`](../../../harness/planning/project-plan.md), [`AGENTS.md`](../../../AGENTS.md)).

**API anchors**

- Frame pool creation and pixel format parameter: [Direct3D11CaptureFramePool.Create](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.create?view=winrt-22000)
- `SwapChainPanel` as composition host: [SwapChainPanel](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.swapchainpanel?view=windows-app-sdk-1.7)

**Engineering note:** Ecosystem reports (e.g., capture sample release notes) warn that **moving between SDR and FP16 capture** can surface **clipping** if color space configuration is inconsistent—treat as a mandatory regression test when offering multiple export modes.

## 4. Shell: Tray + Global Hotkeys

**Tray:** plan for Win32 notify icon integration; WinUI does not provide a complete first-party tray control as of commonly cited issues/proposals. ([Issue #2020](https://github.com/microsoft/microsoft-ui-xaml/issues/2020))

**Hotkeys:** expect a dedicated message path; multiple WinUI windows cannot share the same hotkey id per reported limitations ([Issue #10073](https://github.com/microsoft/microsoft-ui-xaml/issues/10073)).

## 5. Output: Files + Clipboard Realities

**Files:** choose formats per story (PNG/JPEG XL/JXR/etc.) with explicit metadata strategy; prototype labels must not be treated as implemented features.

**Clipboard:** assume **consumer apps are not HDR-aware** unless proven otherwise; document conversion and test against paste targets.

_Source primer: [Standard Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats)_

## 6. HDR Status UX → OS Signals (engineering checklist)

Build a small internal model with **evidence-based states**, for example:

- HDR display present?
- Windows “HDR” enabled for that display?
- WGC capture session can start in FP16?
- Preview swap chain configured to expected color space?

UI strings must follow harness “validation language” so “HDR Ready” is not claimed from Mac-only work.

## 7. Risks, Validation Gates, and Next Steps

**Gates**

- **Windows CI-pass**: build/test/format ([`AGENTS.md`](../../../AGENTS.md))
- **Windows manual-pass**: HDR hardware matrix for capture/preview/output

**Next steps**

1. Write PRD acceptance criteria per MVP surface with explicit **platform limitations** (tray/hotkeys/clipboard).  
2. Architecture doc: one diagram for session state + resource ownership.  
3. Create a Windows-only QA checklist for HDR + multi-monitor + clipboard paste.

## 8. Sources

- [IGraphicsCaptureItemInterop::CreateForMonitor](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor)  
- [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-22000)  
- [Direct3D11CaptureFramePool.Create](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.create?view=winrt-22000)  
- [SwapChainPanel class](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.ui.xaml.controls.swapchainpanel?view=windows-app-sdk-1.7)  
- [RegisterHotKey](https://learn.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-registerhotkey)  
- [Standard Clipboard Formats](https://learn.microsoft.com/en-us/windows/win32/dataxchg/standard-clipboard-formats)  
- [WinUI: System tray proposal discussion (Issue #2020)](https://github.com/microsoft/microsoft-ui-xaml/issues/2020)  
- [WinUI: System tray workaround discussion (Issue #4782)](https://github.com/microsoft/microsoft-ui-xaml/issues/4782)  
- [WinUI: WM_HOTKEY discussion (Issue #5815)](https://github.com/microsoft/microsoft-ui-xaml/issues/5815)  
- [WinUI: Hotkey ID collision across windows (Issue #10073)](https://github.com/microsoft/microsoft-ui-xaml/issues/10073)  
- [WinUI: Swap chain pixel format transitions (Issue #2761)](https://github.com/microsoft/microsoft-ui-xaml/issues/2761)  
- [WinUI: HDR UI / scRGB proposal context (Issue #777)](https://github.com/microsoft/microsoft-ui-xaml/issues/777)  
- [Win2D WinUI3 pixel formats (includes R16G16B16A16Float listing)](https://microsoft.github.io/Win2D/WinUI3/html/PixelFormats.htm)  

---

## Technical Research Conclusion

This document re-grounds Lumiere’s **v0 MVP reference** in **native Windows engineering**: WinUI for product UI, WGC+D3D11 for HDR capture/preview, Win32 for tray/hotkeys, and conservative claims for clipboard/HDR export until validated.

**Technical Research Completion Date:** 2026-05-09  
**Source Verification:** Web-assisted verification of cited Microsoft Learn + WinUI tracker references; hardware-dependent claims remain **manual-test gated**.
