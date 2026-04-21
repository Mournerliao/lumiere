---
project_name: 'lumiere'
user_name: 'Asherliao'
date: '2026-04-20'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'quality_rules', 'workflow_rules', 'anti_patterns']
status: 'complete'
rule_count: 42
optimized_for_llm: true
existing_patterns_found: 3
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Build a native Windows desktop screenshot tool focused on HDR-correct capture and rendering.
- Target `.NET 10 LTS` for the application runtime with `TargetFramework` set to `net10.0-windows10.0.19041.0` unless scaffolding exposes a concrete tooling blocker.
- Use `WinUI 3` with Windows App SDK for the desktop UI.
- Use `Vortice.Windows` for Direct3D 11 and DXGI interop.
- Use `Microsoft.Windows.CsWinRT` for Windows Runtime / native API bridge work where needed.
- Use `Windows.Graphics.Capture` for screen capture.
- HDR capture and rendering must preserve FP16 data:
  - DXGI swap chain format: `DXGI_FORMAT_R16G16B16A16_FLOAT`
  - DXGI color space: `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`
  - WGC frame pool pixel format: `DirectXPixelFormat.R16G16B16A16Float`

## Critical Implementation Rules

### Language-Specific Rules

- Use C#/.NET patterns that make native resource ownership explicit; any class that owns Direct3D, DXGI, WGC, WinRT, or COM-backed objects must implement `IDisposable`.
- Prefer deterministic disposal over relying on finalizers or garbage collection for graphics resources.
- Keep capture/rendering APIs strongly typed around Direct3D concepts such as `ID3D11Texture2D`, device, context, frame pool, session, and swap chain objects.
- Never hide native interop failure paths behind silent null returns; surface failures with explicit exceptions or result states that identify the graphics/capture operation that failed.
- Treat `FrameArrived` callbacks as background-thread entry points. Code that touches WinUI objects, `SwapChainPanel`, or overlay state must marshal back through `DispatcherQueue`.
- Keep async boundaries explicit around UI/capture startup and teardown; avoid fire-and-forget operations that can outlive disposed graphics resources.
- Keep unsafe/native interop code isolated behind small service classes rather than spreading handles, COM pointers, or Win32 calls through UI code.

### Framework-Specific Rules

- Keep the architecture split into three modules:
  - `GraphicsEngine`: owns Direct3D 11 device/context, DXGI swap chain, shader rendering, and WinUI swap chain interop.
  - `CaptureService`: owns `Windows.Graphics.Capture`, frame pool/session lifecycle, and conversion to `ID3D11Texture2D`.
  - `OverlayUI`: owns the transparent fullscreen window, `SwapChainPanel`, overlay `Canvas`, crop mask, drag state, and toolbar interactions.
- `GraphicsEngine` must create an HDR-capable swap chain using `DXGI_FORMAT_R16G16B16A16_FLOAT`; do not downgrade to `B8G8R8A8`, `R8G8B8A8`, SDR, bitmap, or GDI paths for the main preview.
- `GraphicsEngine` must set the swap chain color space to `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` for scRGB linear HDR rendering.
- The WinUI preview surface must be a fullscreen `SwapChainPanel` connected through `ISwapChainPanelNative`; do not render the HDR preview through regular XAML images or software bitmaps.
- `CaptureService` must create `Direct3D11CaptureFramePool` with `DirectXPixelFormat.R16G16B16A16Float`.
- Captured frames must be converted to `ID3D11Texture2D` and passed to `GraphicsEngine` for shader rendering.
- `OverlayUI` should layer XAML interaction elements above the `SwapChainPanel`; the overlay `Canvas` handles drag/crop/tool UI without owning capture or rendering internals.
- Fullscreen transparent overlay behavior requires WinUI/Win32 interop. Keep `SetWindowLong`, layered/transparent styles, borderless presenter setup, and hit-test behavior contained in overlay/window infrastructure.

### Testing Rules

- No test framework is scaffolded yet; once added, follow the repository's actual test framework and naming conventions.
- Cover resource lifecycle behavior for `GraphicsEngine` and `CaptureService`: start, frame arrival, stop, repeated capture, disposal, and shutdown.
- Test that disposal paths release frame pools, sessions, textures, swap chains, and device-bound resources even when capture startup or rendering fails.
- Treat threading as a test boundary: frame callbacks must not update WinUI state directly and should route UI work through `DispatcherQueue`.
- Keep unit tests focused on lifecycle/state coordination and use integration/manual verification for real HDR capture, swap chain presentation, and monitor-specific behavior.
- Add explicit verification for the non-negotiable HDR constants whenever graphics initialization code is introduced.
- For overlay interaction tests, validate crop rectangle state, drag transitions, bounds handling, and toolbar commands separately from Direct3D rendering.

### Code Quality & Style Rules

- Keep module boundaries strict: UI code must not create capture sessions or Direct3D devices directly; capture code must not own XAML overlay state.
- Prefer small services with explicit ownership over global mutable graphics state, except for a deliberately scoped Direct3D device/provider if the architecture introduces one.
- Name classes by responsibility: use names like `GraphicsEngine`, `CaptureService`, `OverlayWindow`, `OverlayCanvas`, or similarly direct domain names.
- Keep comments focused on non-obvious HDR, threading, COM lifetime, or interop decisions; do not add comments that restate simple C# code.
- Centralize HDR constants so format/color-space requirements are not duplicated or accidentally weakened across the codebase.
- Keep Win32 interop declarations, window style changes, and COM bridge code in infrastructure files with narrow public APIs.
- When adding configuration, default toward HDR correctness and fail loudly if the platform cannot provide the required capture/render path.

### Development Workflow Rules

- The implementation plan is phased: scaffold infrastructure first, then capture, then rendering/WinUI bridge, then overlay crop interaction.
- Each phase should leave the app in a coherent state and avoid mixing unrelated future-phase behavior into early infrastructure work.
- When package/project files are introduced, update this context with exact target framework and dependency versions.
- Do not replace planned native Windows implementation with cross-platform UI, web UI, GDI, WPF bitmap preview, or SDR screenshot libraries.
- If a change touches HDR constants, capture pixel format, swap chain format, or resource lifetime semantics, call it out explicitly in review notes.
- There is no Git repository detected at project root as of 2026-04-20; establish repository workflow before relying on branch, commit, or PR conventions.

### Critical Don't-Miss Rules

- Never allow DWM tone-mapped SDR screenshots to become the primary capture path; the core product value is preserving HDR FP16 data.
- Never downgrade captured frames or preview rendering to 8-bit formats for convenience.
- Never render the main HDR preview through `BitmapImage`, `SoftwareBitmap`, GDI, or regular XAML image controls.
- Always dispose old frame pools, capture sessions, textures, render targets, swap chains, and device-bound resources before recreating capture/render pipelines.
- Always marshal from `FrameArrived` or capture background callbacks to the UI thread before touching WinUI objects.
- Handle monitor/HDR capability failures explicitly; do not silently fall back to washed-out SDR output without making the degraded state visible to the app.
- Keep overlay transparency and mouse hit-testing deliberate: full-window click-through behavior can break crop selection if applied indiscriminately.
- Preserve scRGB linear semantics in shader/rendering work; avoid hidden gamma conversion, color clamping, or tone mapping unless explicitly implemented as an export/preview option.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing any code.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive HDR-preserving option.
- Update this file if new implementation patterns emerge.

**For Humans:**

- Keep this file lean and focused on agent needs.
- Update it when technology stack, target framework, dependencies, or architecture changes.
- Review periodically for outdated rules.
- Remove rules that become obvious after the codebase establishes them.

Last Updated: 2026-04-20
