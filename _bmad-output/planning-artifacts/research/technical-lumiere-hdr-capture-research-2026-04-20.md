---
stepsCompleted: [1, 2, 3, 4, 5, 6]
inputDocuments:
  - '/Users/asherliao/Projects/lumiere/harness/PROJECT_PLAN.md'
  - '/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md'
workflowType: 'research'
lastStep: 6
research_type: 'technical'
research_topic: 'Lumiere HDR-native Windows screenshot pipeline'
research_goals: 'Verify the technical feasibility and architecture for HDR-preserving Windows screen capture using .NET, WinUI 3, Windows.Graphics.Capture, Direct3D 11, DXGI, Vortice.Windows, and CsWinRT.'
user_name: 'Asherliao'
date: '2026-04-20'
web_research_enabled: true
source_verification: true
status: 'complete'
---

# Research Report: technical

**Date:** 2026-04-20
**Author:** Asherliao
**Research Type:** technical

---

## Executive Summary

Lumiere's core premise is technically feasible, but only if the product treats HDR preservation as a first-class graphics pipeline requirement rather than a screenshot feature layered on top of ordinary SDR capture. Current Microsoft documentation supports the central design choice: `Windows.Graphics.Capture` can participate in an HDR-safe pipeline, and Microsoft explicitly recommends `DXGI_FORMAT_R16G16B16A16_FLOAT` across the capture pipeline on Windows HD Color systems to avoid overclipping and washed-out HDR output.

The recommended architecture is a native Windows desktop application using C#/.NET, WinUI 3, Windows App SDK 1.8 stable, WGC, Direct3D 11, DXGI, and Vortice.Windows. The highest-risk areas are not ordinary UI or app scaffolding; they are WinRT/COM/DXGI interop, swap-chain color-space correctness, frame lifetime management, Direct3D threading, and overlay hit-testing.

**Key Technical Findings:**

- WGC + Direct3D 11 + FP16 textures is the right capture/rendering foundation for HDR preservation.
- `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` is the correct scRGB color-space target for a 16-bit float preview path.
- `SwapChainPanel` is appropriate because it supports a DirectX swap chain while allowing XAML elements above it.
- C# is suitable for orchestration, but the project needs narrow, well-tested interop helpers for DXGI/WinRT/COM access.
- A minimal HDR proof-of-concept should precede PRD/architecture hardening because the main unknowns are platform behavior and hardware/display variance.

**Top Recommendations:**

- Scaffold with `.NET 10 LTS` using `net10.0-windows10.0.19041.0`, Windows App SDK `1.8.x` stable, and current Vortice packages unless package compatibility proves otherwise.
- Build a Phase 0 technical spike before product implementation: capture one HDR display, keep FP16, render through scRGB swap chain, and verify visual output on HDR hardware.
- Keep `GraphicsEngine`, `CaptureService`, and `OverlayUI` as separate ownership boundaries.
- Treat every WGC frame and Direct3D/COM object as a deterministic lifetime object.
- Do not introduce SDR/GDI/bitmap screenshot fallbacks into the primary preview pipeline.

## Table of Contents

1. Technical Research Scope Confirmation
2. Technology Stack Analysis
3. Integration Patterns Analysis
4. Architectural Patterns and Design
5. Implementation Approaches and Technology Adoption
6. Strategic Technical Recommendations
7. Source Verification Notes

## Research Overview

This research evaluates whether Lumiere can deliver HDR-native Windows screenshots without relying on DWM tone-mapped SDR output. The research used current Microsoft Learn documentation, NuGet metadata, Windows SDK documentation, and the project's local planning/context artifacts. Claims about current versions, Windows API behavior, capture formats, swap-chain integration, and threading/lifetime constraints were verified against public sources where possible.

---

## Technical Research Scope Confirmation

**Research Topic:** Lumiere HDR-native Windows screenshot pipeline
**Research Goals:** Verify the technical feasibility and architecture for HDR-preserving Windows screen capture using .NET, WinUI 3, Windows.Graphics.Capture, Direct3D 11, DXGI, Vortice.Windows, and CsWinRT.

**Technical Research Scope:**

- Architecture Analysis - design patterns, frameworks, system architecture
- Implementation Approaches - development methodologies, coding patterns
- Technology Stack - languages, frameworks, tools, platforms
- Integration Patterns - APIs, protocols, interoperability
- Performance Considerations - scalability, optimization, patterns

**Research Methodology:**

- Current web data with rigorous source verification
- Multi-source validation for critical technical claims
- Confidence level framework for uncertain information
- Comprehensive technical coverage with architecture-specific insights

**Scope Confirmed:** 2026-04-20

---

## Technology Stack Analysis

### Programming Languages

Lumiere's planned language choice should be C# on modern .NET for the WinUI application layer, with narrowly isolated native/COM interop where WinUI, WGC, DXGI, and Direct3D require it. WinUI 3 supports C#/.NET desktop apps, and Microsoft describes WinUI 3 as part of the Windows App SDK rather than an OS-bundled UWP-only UI stack. The project should target a Windows-specific TFM once scaffolded, and it should avoid `Any CPU` because Windows App SDK/WinUI projects involve native runtime assets and architecture-specific packaging/runtime behavior.

_Popular Languages:_ C# for WinUI 3 app/UI/service orchestration; C++/WinRT remains a viable fallback for extremely sharp interop edges, but should not be the default unless C# interop blocks the HDR path.

_Emerging Languages:_ No emerging language should displace C# here. Rust or C++ could be useful for future native helpers, but neither is required for Phase 1-4.

_Language Evolution:_ .NET 10 LTS is the preferred target for a new 2026 Windows desktop project. Microsoft's support policy lists .NET 10 as an active LTS release supported until November 14, 2028, while .NET 8 and .NET 9 both end support on November 10, 2026. Lumiere should use the Windows-specific `TargetFramework` `net10.0-windows10.0.19041.0`. Vortice's current packages include compatibility with modern .NET targets, including `.NET 10.0`.

_Performance Characteristics:_ C# is suitable for app orchestration and UI; performance-critical pixel movement should stay on GPU textures/shaders rather than CPU byte processing.

_Sources:_ [.NET support policy](https://dotnet.microsoft.com/platform/support-policy), [WinUI overview](https://learn.microsoft.com/mt-mt/windows/apps/WinUI/), [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [Vortice.Direct3D11 NuGet](https://www.nuget.org/packages/Vortice.Direct3D11/)

### Development Frameworks and Libraries

The core framework choice is technically coherent: WinUI 3/Windows App SDK for native Windows desktop UI, Windows.Graphics.Capture for secure OS-supported capture, Direct3D 11/DXGI for GPU texture handling and swap chain presentation, and Vortice.Windows packages for C# bindings over Direct3D/DXGI. Microsoft documents Windows.Graphics.Capture as the Windows API for capturing displays/windows via secure picker UI, and its screen-capture guidance explicitly notes that on systems with Windows HD Color enabled, apps should consider `DXGI_FORMAT_R16G16B16A16_FLOAT` for every component in the capture pipeline to avoid washed-out HDR capture.

_Major Frameworks:_ WinUI 3, Windows App SDK, WGC, Direct3D 11, DXGI.

_Specialized Libraries:_ `Vortice.Direct3D11` and `Vortice.DXGI` are appropriate low-level C# bindings. NuGet lists `Vortice.Direct3D11` and `Vortice.DXGI` version `3.8.3` as current as of March 2026, with package descriptions covering Direct3D11/DXGI bindings.

_Interop Libraries:_ `Microsoft.Windows.CsWinRT` remains relevant when authoring/consuming WinRT components or bridging generated WinRT projections. NuGet lists stable `2.2.0` and a newer `3.0.0-preview.260319.2`; use stable unless a specific Windows App SDK / .NET / WinRT requirement forces preview.

_Ecosystem Maturity:_ WGC and Direct3D 11 are stable Windows platform APIs. WinUI 3 is the current native desktop UI framework under Windows App SDK, but advanced swap chain/native interop work should be treated as specialized rather than mainstream sample-path development.

_Sources:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [Windows.Graphics.Capture namespace](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-28000), [Vortice.DXGI NuGet](https://www.nuget.org/packages/Vortice.DXGI), [Microsoft.Windows.CsWinRT NuGet](https://www.nuget.org/packages/microsoft.windows.cswinrt/)

### Database and Storage Technologies

No database is required for the planned MVP. Lumiere's critical data path is GPU texture capture/rendering, not persistent business data. Storage choices should be limited to configuration and screenshot export formats until product requirements add history, libraries, sync, OCR, or annotation databases.

_Relational Databases:_ Not needed for Phase 1-4.

_NoSQL Databases:_ Not needed for Phase 1-4.

_In-Memory Storage:_ Runtime capture state should be in memory and GPU resources; avoid CPU-side frame caches unless needed for export.

_File Storage:_ Future export implementation must distinguish HDR-preserving formats from tone-mapped SDR formats. The WGC docs warn that additional processing may be required for saving HDR content or HDR-to-SDR tone mapping.

_Sources:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

### Development Tools and Platforms

The development platform should be Windows-first with Visual Studio 2022, a current Windows SDK, and Windows App SDK 1.8 stable unless a specific feature requires preview. Microsoft lists Windows App SDK `1.8.6` as the current stable runtime as of March 18, 2026, and `2.0` only as preview/experimental, so the stable default should be 1.8.x. The Windows SDK page says SDK `10.0.28000.1` is early/OEM-oriented and recommends `10.0.26100.x` for most apps.

_IDE and Editors:_ Visual Studio 2022 is the practical default for WinUI 3 templates, packaging, XAML tooling, and Windows SDK integration.

_Version Control:_ No Git repository exists in the project root yet; establish Git before implementation work so package/project decisions are tracked.

_Build Systems:_ Use SDK-style `.csproj` with Windows-specific TFM and Windows App SDK package references. Avoid hand-built native project complexity until needed.

_Testing Frameworks:_ No test framework is scaffolded. For C#, use the project's chosen .NET test framework after scaffolding; keep hardware/HDR verification as integration/manual tests where automation cannot observe monitor behavior.

_Sources:_ [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [Microsoft.WindowsAppSDK NuGet](https://www.nuget.org/packages/Microsoft.WindowsAppSdk/), [Windows SDK downloads](https://developer.microsoft.com/en-us/windows/downloads/windows-sdk/)

### Cloud Infrastructure and Deployment

Lumiere is a local native desktop tool; cloud infrastructure is not part of the technical core. Deployment research should focus on Windows packaging choices, Windows App SDK runtime deployment, hardware/OS requirements, and installer flow rather than cloud hosting.

_Major Cloud Providers:_ Not applicable to Phase 1-4.

_Container Technologies:_ Not applicable for the desktop runtime.

_Serverless Platforms:_ Not applicable.

_Desktop Deployment:_ Windows App SDK supports packaged and unpackaged desktop app deployment paths. The downloads page notes standalone runtime installers and redistributables for packaged-with-external-location and unpackaged apps.

_Sources:_ [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [Windows App SDK overview](https://learn.microsoft.com/windows/apps/windows-app-sdk/)

### Technology Adoption Trends

The project is aligned with Microsoft's native Windows app direction: Windows App SDK provides modern Windows APIs and WinUI for desktop apps, with independent SDK releases. For Lumiere, the more important trend is not UI fashion but Windows Advanced Color/HDR support: Microsoft documents scRGB and FP16 formats as the correct path for high-bit-depth/HDR DirectX work, and WGC documentation now explicitly flags FP16 capture to avoid HDR overclipping/washed-out output.

_Migration Patterns:_ Prefer WinUI 3/Windows App SDK over UWP-only WinUI 2 or WPF/GDI for this app because the core preview surface needs native DirectX swap chain integration.

_Emerging Technologies:_ Windows App SDK 2.0 preview exists, but stable 1.8.x is the better default until a concrete 2.0 feature is needed.

_Legacy Technology:_ GDI, CPU bitmap pipelines, and 8-bit SDR preview/export paths are legacy or secondary paths for this product's main value proposition.

_Community Trends:_ Vortice.Windows is the current maintained C# DirectX binding family after SharpDX-era development. Current Vortice packages target modern .NET versions, reinforcing the need to validate exact target framework compatibility during scaffolding.

_Sources:_ [Use DirectX with Advanced Color](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range), [DXGI_COLOR_SPACE_TYPE](https://learn.microsoft.com/en-us/windows/win32/api/dxgicommon/ne-dxgicommon-dxgi_color_space_type), [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [Vortice.Direct3D11 NuGet](https://www.nuget.org/packages/Vortice.Direct3D11/)

### Technology Stack Confidence

- **High confidence:** WGC + Direct3D 11 + FP16 texture path is the right research direction for HDR-preserving capture because Microsoft documentation explicitly recommends `DXGI_FORMAT_R16G16B16A16_FLOAT` across the capture pipeline for HDR scenarios.
- **High confidence:** `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` is the correct scRGB color-space constant for 16-bit/float channel work.
- **Medium confidence:** C# + Vortice can cover the required D3D11/DXGI work; this should be proven early with a minimal swap chain + WGC prototype.
- **High confidence:** .NET 10 LTS with `net10.0-windows10.0.19041.0` is the best initial target because .NET 10 has active LTS support until November 2028 and current Vortice packages are compatible with `.NET 10.0`; confirm during scaffolding that the selected Windows App SDK tooling accepts this TFM.
- **Low relevance:** databases/cloud infrastructure for the current MVP.

---

## Integration Patterns Analysis

### API Design Patterns

Lumiere is not a networked system, so REST/GraphQL/gRPC patterns are irrelevant to the core architecture. The meaningful API design is internal: define narrow service interfaces between `OverlayUI`, `CaptureService`, and `GraphicsEngine`, and keep native handles/COM pointers behind implementation classes. The internal contracts should pass domain-specific values such as capture target, crop rectangle, frame metadata, and graphics texture references, not raw window styles or UI controls.

_RESTful APIs:_ Not applicable to MVP.

_GraphQL APIs:_ Not applicable.

_RPC and gRPC:_ Not applicable unless future automation/remote-control features are added.

_Webhook Patterns:_ Not applicable.

_Relevant Internal APIs:_ `CaptureService` should expose start/stop/target-selection/frame events; `GraphicsEngine` should expose initialize/resize/render/dispose; `OverlayUI` should expose crop interaction state and commands.

_Sources:_ [Windows.Graphics.Capture namespace](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-28000), [GraphicsCaptureSession](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession?view=winrt-26100)

### Communication Protocols

The dominant "protocol" is GPU resource exchange across WinRT, DXGI, Direct3D 11, and XAML interop. WGC produces `Direct3D11CaptureFrame` instances whose `Surface` is an `IDirect3DSurface`; Microsoft documents `IDirect3DSurface` as representing an `IDXGISurface` and supporting interop between WinRT components. The interop header exposes functions to create an `IDirect3DDevice` from `IDXGIDevice`, create `IDirect3DSurface` from `IDXGISurface`, and retrieve DXGI interfaces from WinRT Direct3D objects.

_HTTP/HTTPS Protocols:_ Not applicable.

_WebSocket Protocols:_ Not applicable.

_Message Queue Protocols:_ Not applicable.

_GPU/COM Interop Protocol:_ Use `IDirect3DDevice`/`IDirect3DSurface` for WinRT-facing WGC calls and `IDXGIDevice`/`ID3D11Texture2D`/`IDXGISwapChain` for rendering work. Keep conversions centralized.

_Sources:_ [Windows.Graphics.DirectX.Direct3D11 interop header](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/), [Direct3D11CaptureFrame.Surface](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframe.surface?view=winrt-26100), [IDirect3DSurface](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.directx.direct3d11.idirect3dsurface?view=winrt-28000)

### Data Formats and Standards

The critical data format is the pixel format. Lumiere's capture frame pool, intermediate texture path, render target, and swap chain should use `R16G16B16A16Float`/`DXGI_FORMAT_R16G16B16A16_FLOAT` for the primary HDR path. Microsoft documents `DirectXPixelFormat.R16G16B16A16Float` as corresponding to `DXGI_FORMAT_R16G16B16A16_FLOAT`. DXGI documents `DXGI_FORMAT` float variants as using half-precision for 16-bit floating-point channels.

_JSON and XML:_ Useful only for app settings.

_Protobuf and MessagePack:_ Not needed.

_CSV and Flat Files:_ Not needed.

_Custom Data Formats:_ If HDR export is added, evaluate HDR-capable image formats separately from this preview/capture research.

_Sources:_ [DirectXPixelFormat](https://learn.microsoft.com/en-us/windows/windows-app-sdk/api/winrt/microsoft.graphics.directx.directxpixelformat?view=windows-app-sdk-1.7), [DXGI_FORMAT](https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format), [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

### System Interoperability Approaches

The key interoperability path is: create a D3D11 device, expose it to WGC as a WinRT `IDirect3DDevice`, create a `Direct3D11CaptureFramePool`, receive `Direct3D11CaptureFrame.Surface`, recover/handle the underlying D3D11 texture, render into a composition swap chain, attach the swap chain to WinUI `SwapChainPanel`, and draw XAML overlay elements above it.

_Point-to-Point Integration:_ Yes, but inside-process: WGC -> CaptureService -> GraphicsEngine -> SwapChainPanel.

_API Gateway Patterns:_ Not applicable.

_Service Mesh:_ Not applicable.

_Enterprise Service Bus:_ Not applicable.

_Desktop Capture Targeting:_ For direct HWND/HMONITOR targeting, Windows provides `IGraphicsCaptureItemInterop` with `CreateForWindow` and `CreateForMonitor`; this requires Windows 10 version 1903/build 18362 or later. Picker-based capture remains the user-consent path.

_Sources:_ [IGraphicsCaptureItemInterop](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nn-windows-graphics-capture-interop-igraphicscaptureiteminterop), [CreateForWindow](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow), [Display WinRT UI objects](https://learn.microsoft.com/en-us/windows/apps/develop/ui-input/display-ui-objects)

### WinUI Swap Chain Integration

`SwapChainPanel` is the appropriate XAML integration surface because it supports application-managed DirectX swap chains and allows XAML elements to be overlaid in front. For WinUI/XAML composition swap chains, `IDXGIFactory2::CreateSwapChainForComposition` is the relevant creation path. Microsoft states this supports WinUI XAML composition and requires flip presentation model via `DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL` and `DXGI_SCALING_STRETCH`.

The `ISwapChainPanelNative.SetSwapChain` call must happen on the UI thread for the owning panel; Microsoft documents `RPC_E_WRONG_THREAD` if called from another thread. It also increments references to the swap-chain object graph, so teardown should detach by calling `SetSwapChain(null)` before releasing related device resources.

_Sources:_ [CreateSwapChainForComposition](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgifactory2-createswapchainforcomposition), [ISwapChainPanelNative.SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain), [DirectX and XAML interop](https://learn.microsoft.com/en-us/windows/uwp/gaming/directx-and-xaml-interop)

### Event-Driven Integration

WGC frame delivery is event-driven through `Direct3D11CaptureFramePool.FrameArrived`. The normal `Create` method depends on a `DispatcherQueue`; `CreateFreeThreaded` removes that dependency and raises `FrameArrived` on the frame pool's internal worker thread. Either way, Lumiere must treat frame delivery as asynchronous and must never mutate WinUI objects from the wrong thread.

_Publish-Subscribe Patterns:_ Internal frame event from CaptureService to GraphicsEngine.

_Event Sourcing:_ Not applicable.

_Message Broker Patterns:_ Not applicable.

_CQRS Patterns:_ Not applicable.

_Sources:_ [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-26100), [CreateFreeThreaded](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded?view=winrt-26100)

### Integration Security and Consent Patterns

Screen capture is a consent-sensitive Windows capability. WGC uses secure system UI for users to select windows/displays, and the capture border behavior is controlled by OS policy and permissions. If Lumiere attempts borderless capture, `GraphicsCaptureAccess.RequestAccessAsync` with `GraphicsCaptureAccessKind.Borderless` and the `graphicsCaptureWithoutBorder` capability are required; denial means the border can still appear.

_OAuth/JWT/API Keys/mTLS:_ Not applicable.

_Desktop Security Pattern:_ Respect WGC capability declarations, picker/consent flows, border requirements, and explicit user action for capture.

_Sources:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [GraphicsCaptureSession.IsBorderRequired](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.isborderrequired?view=winrt-28000)

---

## Architectural Patterns and Design

### System Architecture Patterns

The recommended architecture is a modular native desktop architecture, not microservices or cloud-native decomposition. The project plan's three modules are sound and should be preserved:

- `GraphicsEngine`: owns D3D11 device/context, DXGI swap chain, render targets, shaders, resizing, color-space setup, and swap-chain attachment/detachment.
- `CaptureService`: owns WGC target selection, frame pool, capture session, frame lifetime, and conversion/access to GPU textures.
- `OverlayUI`: owns WinUI windowing, `SwapChainPanel`, transparent/borderless overlay, crop interaction, toolbar, and UI-thread dispatch.

This separation maps well to the underlying platform boundaries: WGC is a capture API, Direct3D/DXGI are graphics APIs, and WinUI/XAML is the interaction/presentation shell.

_Source:_ [Windows.Graphics.Capture namespace](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-28000), [ISwapChainPanelNative](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nn-windows-ui-xaml-media-dxinterop-iswapchainpanelnative)

### Design Principles and Best Practices

The dominant design principle should be explicit ownership. Every object that represents an OS, WinRT, DXGI, Direct3D, or COM resource needs a clear owner and deterministic disposal. The WGC docs state that captured frames are checked out with `TryGetNextFrame` and checked back in according to the lifetime of the frame object; managed applications should call `Dispose`, and apps should not retain frame objects or underlying surfaces after the frame has been checked back in.

_Design Principles:_ deterministic lifetime, narrow interop boundary, GPU-first frame path, UI-thread isolation, fail-loud HDR validation.

_Best Practice Patterns:_ `IDisposable` per owner, single module for HDR constants, explicit teardown ordering, no silent SDR fallback.

_Source:_ [Screen capture frame processing](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

### Scalability and Performance Patterns

For a screenshot utility, "scalability" means low-latency GPU movement, stable memory use, and robust behavior across high-resolution/HDR/multi-monitor setups. The app should avoid CPU readback in the live preview path. When WGC frames arrive, process/copy/render on the GPU, release frame objects promptly, and recreate frame pools when capture size changes.

Use `ID3D11Multithread` only where needed. Microsoft documents that D3D11 does not have the same default multithreaded layer behavior as D3D10, and that `SetMultithreadProtected`, `Enter`, and `Leave` add protection but also overhead. If frame processing and rendering share the immediate context across threads, lock the same device/context around ordered graphics commands.

_Performance Patterns:_ GPU-resident frame path, bounded frame buffering, prompt frame disposal, resize-aware frame pool recreation, minimal UI-thread work.

_Source:_ [ID3D11Multithread](https://learn.microsoft.com/en-us/windows/win32/api/d3d11_4/nn-d3d11_4-id3d11multithread), [Screen capture frame processing](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

### Integration and Communication Patterns

Use event-driven capture internally, but keep rendering scheduling explicit. `FrameArrived` should notify a controlled render path rather than directly touching XAML. `ISwapChainPanelNative.SetSwapChain` must be UI-thread-only, while frame processing may occur on the capture thread or a controlled worker path depending on `Create` vs `CreateFreeThreaded`.

_Source:_ [CreateFreeThreaded](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded?view=winrt-26100), [ISwapChainPanelNative.SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain)

### Security Architecture Patterns

Security architecture is mostly OS capability/consent architecture. Do not try to bypass WGC consent and border policies. For future sensitive features such as background capture, history, OCR, cloud sync, or global hotkeys, add explicit user consent, local-only defaults, and visible state indicators.

_Source:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [GraphicsCaptureSession.IsBorderRequired](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.isborderrequired?view=winrt-28000)

### Data Architecture Patterns

There is no database data architecture in the MVP. The live data architecture is a GPU pipeline. If app settings are needed, they can be simple local settings. Screenshot export requires separate research into HDR image encoding and SDR tone mapping.

_Source:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)

### Deployment and Operations Architecture

Prefer Windows App SDK stable runtime deployment. Microsoft documents stable Windows App SDK 1.8.x and provides runtime installers/redistributables for packaged-with-external-location and unpackaged apps. Decide packaged vs unpackaged early because capture capabilities, app identity, distribution, and runtime installation experience can be affected.

_Source:_ [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [Windows App SDK overview](https://learn.microsoft.com/windows/apps/windows-app-sdk/)

---

## Implementation Approaches and Technology Adoption

### Technology Adoption Strategies

Adopt the stack through a proof-first path. The central risk is not whether ordinary WinUI/C# apps work; it is whether Lumiere can maintain FP16/scRGB correctness through WGC, D3D11, DXGI swap chain, WinUI presentation, and real HDR display output. Start with a small technical spike before formal product scope expands.

_Recommended Spike:_ create D3D11 device -> wrap as WinRT `IDirect3DDevice` -> create WGC frame pool with `R16G16B16A16Float` -> capture display/window -> render to `DXGI_FORMAT_R16G16B16A16_FLOAT` composition swap chain -> set `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` -> attach to `SwapChainPanel`.

_Source:_ [CreateDirect3D11DeviceFromDXGIDevice](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/nf-windows-graphics-directx-direct3d11-interop-createdirect3d11devicefromdxgidevice), [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [IDXGISwapChain3.SetColorSpace1](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/nf-dxgi1_4-idxgiswapchain3-setcolorspace1)

### Development Workflows and Tooling

Use Visual Studio 2022 and current Windows SDK tooling for WinUI 3. Establish Git before scaffolding. Use project files to lock exact package versions: `Microsoft.WindowsAppSDK`, `Vortice.Direct3D11`, `Vortice.DXGI`, and any CsWinRT dependency. Do not rely on "latest" floating references after the first successful spike.

_Source:_ [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [Vortice.Direct3D11 NuGet](https://www.nuget.org/packages/Vortice.Direct3D11/), [Microsoft.WindowsAppSDK NuGet](https://www.nuget.org/packages/Microsoft.WindowsAppSdk/)

### Testing and Quality Assurance

Automated tests should cover service lifecycles, disposal ordering, crop state, constants, and error paths. Manual/integration tests must cover actual HDR monitor behavior, washed-out output regression, multi-monitor behavior, capture border behavior, cursor inclusion, and device-lost/recreation scenarios.

Quality gates should include:

- Assert capture frame pool pixel format is `R16G16B16A16Float`.
- Assert swap chain format is `DXGI_FORMAT_R16G16B16A16_FLOAT`.
- Assert color space is set with `IDXGISwapChain3.SetColorSpace1`.
- Verify `ISwapChainPanelNative.SetSwapChain(null)` is called on teardown.
- Verify `Direct3D11CaptureFrame.Dispose` happens promptly.

_Source:_ [Direct3D11CaptureFrame](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframe?view=winrt-26100), [ISwapChainPanelNative.SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain)

### Deployment and Operations Practices

This is a desktop application, so operations means install/update/runtime readiness rather than server operations. Decide whether MVP is packaged MSIX, packaged with external location, or unpackaged. If using unpackaged deployment, include Windows App Runtime installation strategy. Record minimum supported Windows build; direct HWND/HMONITOR capture item creation needs Windows 10 1903/build 18362 or later.

_Source:_ [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads), [CreateForWindow](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow)

### Team Organization and Skills

The project needs skills in C#/.NET, WinUI 3, Win32 windowing, COM/WinRT interop, Direct3D 11, DXGI swap chains, shader/render-target basics, and color management. This is a graphics/platform project hiding inside a screenshot utility; treating it as ordinary XAML app work will under-scope the hard parts.

_Source:_ [Use DirectX with Advanced Color](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range), [CreateSwapChainForComposition](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgifactory2-createswapchainforcomposition)

### Cost Optimization and Resource Management

The most important resource cost is GPU memory pressure from FP16 buffers. At 4K, an `R16G16B16A16_FLOAT` buffer is substantially larger than an 8-bit SDR buffer. Keep frame buffer counts minimal, release frames promptly, avoid duplicate CPU/GPU copies, and dispose/recreate resources predictably on target size changes.

_Source:_ [DXGI_FORMAT](https://learn.microsoft.com/en-us/windows/win32/api/dxgiformat/ne-dxgiformat-dxgi_format), [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-26100)

### Risk Assessment and Mitigation

| Risk | Severity | Mitigation |
| --- | --- | --- |
| HDR path silently degrades to SDR | High | Centralize constants, add startup validation, visual HDR test matrix |
| WGC frame lifetime misuse | High | Dispose frames immediately, never retain frame/surface after checkout lifetime |
| Wrong-thread XAML/swap-chain calls | High | Route UI work through `DispatcherQueue`; call `SetSwapChain` on UI thread only |
| COM/D3D resource leaks | High | Strict `IDisposable`, teardown tests, detach swap chain before releasing device |
| Hardware/display variance | High | Test on HDR on/off, SDR monitors, multi-monitor, different scaling modes |
| Vortice/.NET/Windows App SDK compatibility mismatch | Medium | Lock package versions after spike; prefer .NET 10 LTS with `net10.0-windows10.0.19041.0` + Windows App SDK 1.8 stable |
| Overlay click-through breaks crop selection | Medium | Treat `WS_EX_TRANSPARENT` as mode-specific, not a blanket window style |

_Sources:_ [Screen capture frame processing](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [ISwapChainPanelNative.SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain), [Extended Window Styles](https://learn.microsoft.com/en-us/windows/win32/winmsg/extended-window-styles)

## Technical Research Recommendations

### Implementation Roadmap

1. **Phase 0: HDR pipeline spike**
   Build the smallest possible WGC -> FP16 D3D11 texture -> scRGB swap chain -> `SwapChainPanel` prototype on HDR hardware.

2. **Phase 1: Infrastructure**
   Scaffold `.NET 10 LTS` + WinUI 3 + Windows App SDK 1.8 stable using `net10.0-windows10.0.19041.0`; add Vortice packages; implement D3D11 device provider, HRESULT/error helpers, disposal base patterns, and constants.

3. **Phase 2: CaptureService**
   Implement picker/HWND/HMONITOR capture target acquisition, FP16 frame pool, session lifecycle, frame event handling, frame disposal, and resize handling.

4. **Phase 3: GraphicsEngine**
   Implement composition swap chain, color-space setup, render target management, texture copy/shader draw, resize, device-lost handling, and `SwapChainPanel` attach/detach.

5. **Phase 4: OverlayUI**
   Implement transparent fullscreen overlay, crop rectangle, input mode handling, toolbar, and user-visible capture state.

6. **Phase 5: Export research and implementation**
   Research HDR still formats and SDR tone-mapping export separately; do not assume preview correctness implies export correctness.

### Technology Stack Recommendations

- Use `.NET 10 LTS` with `TargetFramework` `net10.0-windows10.0.19041.0` as the fixed runtime target unless a concrete Windows App SDK or tooling blocker is discovered during scaffolding.
- Use Windows App SDK `1.8.x` stable; avoid 2.0 preview for MVP unless a required bug fix/feature is identified.
- Use Vortice `3.8.3` family for `Direct3D11` and `DXGI` if compatible with selected TFM.
- Use `Microsoft.Windows.CsWinRT` stable `2.2.0` only if explicit C#/WinRT authoring/projection support is needed; avoid preview CsWinRT unless necessary.
- Use Windows SDK `10.0.26100.x` for general app development unless a specific newer API requires otherwise.

### Skill Development Requirements

- Direct3D 11 resource ownership and immediate context usage.
- DXGI swap-chain creation, resizing, color-space support, and composition swap chains.
- WinRT/COM interop from C#, especially `IDirect3DDevice`, `IDirect3DSurface`, and `IInspectable`/native interface access.
- WinUI 3 windowing, HWND access, `DispatcherQueue`, and overlay input behavior.
- HDR/scRGB concepts and test methodology.

### Success Metrics and KPIs

- HDR monitor preview does not appear washed out compared with source.
- Live preview path never uses 8-bit SDR formats.
- Capture/render pipeline survives repeated start/stop without GPU memory growth.
- Swap chain is detached and graphics resources are disposed on close/recreate.
- Frame arrival never causes wrong-thread UI exceptions.
- Crop interaction remains responsive over full-screen HDR preview.

---

## Strategic Technical Recommendations

### Technical Strategy and Decision Framework

Treat Lumiere as a graphics correctness product first and a screenshot UX second. Product scope should be gated by whether the Phase 0 spike proves the FP16/scRGB path on target hardware. If the spike succeeds, the planned architecture is strong. If it fails, the project should pivot into narrower research before PRD commitments.

_Architecture Recommendation:_ modular monolith with strict graphics/capture/UI boundaries.

_Technology Selection:_ C#/.NET + WinUI 3 + WGC + D3D11/DXGI + Vortice.

_Implementation Strategy:_ proof-first, constants-tested, resource-lifetime-driven.

_Sources:_ [Screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture), [Use DirectX with Advanced Color](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range), [DXGI_COLOR_SPACE_TYPE](https://learn.microsoft.com/en-us/windows/win32/api/dxgicommon/ne-dxgicommon-dxgi_color_space_type)

### Competitive Technical Advantage

The technical differentiator is not taking screenshots; it is avoiding the washed-out HDR failure mode that ordinary SDR capture paths can produce. The competitive advantage comes from preserving FP16/scRGB through capture, preview, crop, and eventually export. That requires product messaging and engineering validation to stay aligned.

### Future Technical Outlook and Innovation Opportunities

Near-term innovation should focus on proof and reliability: HDR preview correctness, multi-monitor handling, capture target UX, and crop interaction. Medium-term opportunities include HDR export, SDR tone mapping presets, side-by-side HDR/SDR comparison, color-managed annotation rendering, and hardware capability diagnostics.

Longer-term possibilities include video capture, OCR, annotation history, clipboard interoperability with HDR-aware formats, and GPU-based image processing, but these should not distract from the first proof of HDR-native capture.

---

## Source Verification Notes

**Primary sources used:**

- [Windows.Graphics.Capture screen capture](https://learn.microsoft.com/en-us/windows/uwp/audio-video-camera/screen-capture)
- [Windows.Graphics.Capture namespace](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture?view=winrt-28000)
- [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-26100)
- [Direct3D11CaptureFrame](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframe?view=winrt-26100)
- [Windows.Graphics.DirectX.Direct3D11 interop](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.directx.direct3d11.interop/)
- [IGraphicsCaptureItemInterop](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nn-windows-graphics-capture-interop-igraphicscaptureiteminterop)
- [IDXGIFactory2.CreateSwapChainForComposition](https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgifactory2-createswapchainforcomposition)
- [ISwapChainPanelNative.SetSwapChain](https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain)
- [DXGI_COLOR_SPACE_TYPE](https://learn.microsoft.com/en-us/windows/win32/api/dxgicommon/ne-dxgicommon-dxgi_color_space_type)
- [Use DirectX with Advanced Color](https://learn.microsoft.com/en-us/windows/win32/direct3darticles/high-dynamic-range)
- [Windows App SDK downloads](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
- [Vortice.Direct3D11 NuGet](https://www.nuget.org/packages/Vortice.Direct3D11/)
- [Vortice.DXGI NuGet](https://www.nuget.org/packages/Vortice.DXGI)

**Research limitations:**

- The research verifies API feasibility and architecture direction, but it cannot verify actual HDR visual correctness without running on HDR hardware.
- Microsoft Learn pages sometimes include prerelease disclaimers for API reference pages; implementation should target stable SDK/package versions and verify behavior in a prototype.
- Export format correctness was intentionally scoped out and should receive separate research before implementation.

**Completion Date:** 2026-04-20

---

<!-- Content will be appended sequentially through research workflow steps -->
