---
project_name: 'lumiere'
user_name: 'Asherliao'
date: '2026-05-09'
sections_completed: ['technology_stack', 'language_rules', 'framework_rules', 'testing_rules', 'quality_rules', 'workflow_rules', 'anti_patterns']
status: 'complete'
rule_count: 52
optimized_for_llm: true
existing_patterns_found: 6
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Build a native Windows desktop screenshot tool focused on HDR-correct capture and rendering.
- Supported development workflow is Mac-edit/Windows-validate: macOS may be used for source editing, documentation, refactoring, and platform-neutral design work; Windows must be used for restore/build/test/format and all WinUI/WGC/DXGI/HDR validation.
- Target `.NET 10 LTS` with `TargetFramework` set to `net10.0-windows10.0.19041.0` (set centrally in `Directory.Build.props`; do not override per project).
- Platform: `x64` only (`Platforms`, `PlatformTarget`, `RuntimeIdentifier` all set to win-x64 in `Directory.Build.props`). Never use `Any CPU`.
- Use `WinUI 3` with Windows App SDK for the desktop UI.
- Use `Vortice.Windows` for Direct3D 11 and DXGI interop.
- Use `Microsoft.Windows.CsWinRT` for Windows Runtime / native API bridge work where needed.
- Use `Windows.Graphics.Capture` for screen capture.
- Package versions are managed centrally in `Directory.Packages.props`; do not add `<Version>` in individual `.csproj` files.
- Current pinned versions:
  - `Microsoft.WindowsAppSDK` 1.8.260317003
  - `Vortice.Direct3D11` 3.8.3
  - `Vortice.DXGI` 3.8.3
  - `Microsoft.Extensions.Logging.Abstractions` 9.0.4
  - `xunit` 2.9.3
  - `xunit.runner.visualstudio` 3.1.5
  - `Microsoft.NET.Test.Sdk` 18.4.0
- HDR capture and rendering must preserve FP16 data:
  - DXGI swap chain format: `DXGI_FORMAT_R16G16B16A16_FLOAT`
  - DXGI color space: `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`
  - WGC frame pool pixel format: `DirectXPixelFormat.R16G16B16A16Float`
- Nullable reference types are enabled globally (`<Nullable>enable</Nullable>` in `Directory.Build.props`).
- Implicit usings are enabled globally.

## Critical Implementation Rules

### Language-Specific Rules

- Use C#/.NET patterns that make native resource ownership explicit; any class that owns Direct3D, DXGI, WGC, WinRT, or COM-backed objects must implement `IDisposable`.
- Prefer deterministic disposal over relying on finalizers or garbage collection for graphics resources.
- Keep capture/rendering APIs strongly typed around Direct3D concepts such as `ID3D11Texture2D`, device, context, frame pool, session, and swap chain objects.
- Never hide native interop failure paths behind silent null returns; surface failures with explicit exceptions or result states that identify the graphics/capture operation that failed.
- Treat `FrameArrived` callbacks as background-thread entry points. Code that touches WinUI objects, `SwapChainPanel`, or overlay state must marshal back through `DispatcherQueue`.
- Keep async boundaries explicit around UI/capture startup and teardown; avoid fire-and-forget operations that can outlive disposed graphics resources.
- Keep unsafe/native interop code isolated behind small service classes rather than spreading handles, COM pointers, or Win32 calls through UI code.
- Use file-scoped namespaces (`namespace X;` not `namespace X { }`), as suggested by `.editorconfig`.
- Prefer `var` when the type is apparent from the right side of the assignment; use explicit type otherwise (`.editorconfig` convention).

### Logging Rules

- All production code MUST use `ILogger` via `LumiereLoggerFactory`. Do NOT use `Console.WriteLine`, `Debug.WriteLine`, or create new static logger classes.
- Declare a static logger per class: `private static readonly ILogger Logger = LumiereLoggerFactory.CreateLogger(LogCategories.Graphics);`
- Use the category matching the module: `LogCategories.App`, `.Capture`, `.Graphics`, `.Overlay`, `.Infrastructure`.
- Log levels: `LogDebug` for device/swap chain config details; `LogInformation` for lifecycle events; `LogWarning` for degraded but recoverable states; `LogError` for failures with exceptions; `LogCritical` for fatal device loss.
- Use structured message templates with `{Placeholder}` syntax, never string interpolation in log calls.

### Framework-Specific Rules

- Keep the architecture split into six boundary projects:
  - `Lumiere.App`: owns WinUI app startup, window composition, wires Graphics/Capture/Infrastructure.
  - `Lumiere.Graphics`: owns D3D11 device/context, DXGI swap chain, HDR constants, shader rendering, presentation.
  - `Lumiere.Capture`: owns WGC frame pool, capture session lifecycle, frame disposal, direct monitor target selection.
  - `Lumiere.Infrastructure`: owns COM/WinRT interop, native marshaling, Win32 bridge, `NativeInteropException`, diagnostics/logging.
  - `Lumiere.Overlay`: owns fullscreen overlay window, crop UI, mouse/keyboard interaction.
  - `Lumiere.Settings`: owns local preferences only.
- The default MVP capture path is direct monitor capture (`DirectMonitorCaptureTargetSelectionService`). `GraphicsCapturePicker` is retained only for fallback/debug; the default action must not require picker-first display/window selection.
- `GraphicsEngine` must create an HDR-capable swap chain using `DXGI_FORMAT_R16G16B16A16_FLOAT`; do not downgrade to `B8G8R8A8`, `R8G8B8A8`, SDR, bitmap, or GDI paths for the main preview.
- `GraphicsEngine` must set the swap chain color space to `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` for scRGB linear HDR rendering.
- The WinUI preview surface must be a fullscreen `SwapChainPanel` connected through `ISwapChainPanelNative`; do not render the HDR preview through regular XAML images or software bitmaps.
- `CaptureService` must create `Direct3D11CaptureFramePool` with `DirectXPixelFormat.R16G16B16A16Float`.
- Captured frames must be converted to `ID3D11Texture2D` and passed to `GraphicsEngine` for shader rendering.
- `OverlayUI` should layer XAML interaction elements above the `SwapChainPanel`; the overlay `Canvas` handles drag/crop/tool UI without owning capture or rendering internals.
- Fullscreen transparent overlay behavior requires WinUI/Win32 interop. Keep `SetWindowLong`, layered/transparent styles, borderless presenter setup, and hit-test behavior contained in overlay/window infrastructure.
- New WGC, DXGI, COM, Win32, or WinUI calls must go into their boundary project first, then expose narrow interfaces. Do not scatter platform APIs into UI or test code.

### Testing Rules

- The repository uses xUnit for automated tests under `tests/`.
- Test projects: `Lumiere.Graphics.Tests` (covers Graphics + Capture) and `Lumiere.Overlay.Tests` (covers Overlay).
- Windows CI or a Windows development machine must run:
  ```
  dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
  dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
  dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
  dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
  ```
- macOS-only work may produce code and tests, but completion notes must say when Windows validation was not run.
- Cover resource lifecycle behavior for `GraphicsEngine` and `CaptureService`: start, frame arrival, stop, repeated capture, disposal, and shutdown.
- Test that disposal paths release frame pools, sessions, textures, swap chains, and device-bound resources even when capture startup or rendering fails.
- Treat threading as a test boundary: frame callbacks must not update WinUI state directly and should route UI work through `DispatcherQueue`.
- Keep unit tests focused on lifecycle/state coordination and use integration/manual verification for real HDR capture, swap chain presentation, and monitor-specific behavior.
- Add explicit verification for the non-negotiable HDR constants whenever graphics initialization code is introduced.
- For overlay interaction tests, validate crop rectangle state, drag transitions, bounds handling, and toolbar commands separately from Direct3D rendering.
- Tests use Fake/Stub implementations for platform boundaries (e.g., `FakePreviewFrameOutput` implementing `IPreviewFrameOutput`, `ISwapChainColorSpaceController`). Do not write tests that require a real D3D11 device unless the test project adds a hardware-dependent category.

### Code Quality & Style Rules

- Keep module boundaries strict: UI code must not create capture sessions or Direct3D devices directly; capture code must not own XAML overlay state.
- Prefer small services with explicit ownership over global mutable graphics state, except for a deliberately scoped Direct3D device/provider if the architecture introduces one.
- Name classes by responsibility: use names like `GraphicsEngine`, `CaptureService`, `OverlayWindow`, `OverlayCanvas`, or similarly direct domain names.
- Keep comments focused on non-obvious HDR, threading, COM lifetime, or interop decisions; do not add comments that restate simple C# code.
- Centralize HDR constants in `Lumiere.Graphics.Hdr.HdrConstants` so format/color-space requirements are not duplicated or accidentally weakened across the codebase.
- Keep Win32 interop declarations, window style changes, and COM bridge code in `Lumiere.Infrastructure` with narrow public APIs.
- When adding configuration, default toward HDR correctness and fail loudly if the platform cannot provide the required capture/render path.
- `.editorconfig` conventions: UTF-8, CRLF line endings, 4-space indent for `.cs`, 2-space indent for `.xaml`/`.xml`/`.csproj`/`.props`, LF line endings for `.md`/`.yml`/`.json`/`.sh`/`.ps1`.
- Do not suppress `CA1416` (platform compatibility) without explicit justification; it is set to `suggestion` level.

### Development Workflow Rules

- The active implementation plan is the ten-epic MVP-to-1.0 route documented in `_bmad-output/planning-artifacts/epics.md`.
- Each phase should leave the app in a coherent state and avoid mixing unrelated future-phase behavior into early infrastructure work.
- When package/project files are introduced, update this context with exact target framework and dependency versions.
- Keep platform-specific APIs behind narrow boundaries so macOS editing remains practical and Windows validation remains focused.
- Story completion notes must distinguish `Mac-pass`, `Windows CI-pass`, and `Windows manual-pass` when work crosses WinUI, WGC, DXGI, D3D11, HDR, or multi-monitor behavior.
- Do not mark WinUI/WGC/DXGI/HDR behavior as fully done based only on macOS editing or CI; CI cannot replace real Windows hardware validation.
- Do not replace planned native Windows implementation with cross-platform UI, web UI, GDI, WPF bitmap preview, or SDR screenshot libraries.
- If a change touches HDR constants, capture pixel format, swap chain format, or resource lifetime semantics, call it out explicitly in review notes.
- Git repository workflow exists; keep commits scoped and use Windows CI or a Windows development machine as the build verification gate.
- Settings panel, tray context menu, and full screen capture mode are part of the expanded MVP scope.
- MVP completion is not claimed when feature stories alone are done; it is claimed only after the MVP completion gate epic validates Windows manual scenarios and deferred-work triage.

### Critical Don't-Miss Rules

- Never allow DWM tone-mapped SDR screenshots to become the primary capture path; the core product value is preserving HDR FP16 data.
- Never downgrade captured frames or preview rendering to 8-bit formats for convenience.
- Never render the main HDR preview through `BitmapImage`, `SoftwareBitmap`, GDI, or regular XAML image controls.
- Always dispose old frame pools, capture sessions, textures, render targets, swap chains, and device-bound resources before recreating capture/render pipelines.
- Always marshal from `FrameArrived` or capture background callbacks to the UI thread before touching WinUI objects.
- Handle monitor/HDR capability failures explicitly; do not silently fall back to washed-out SDR output without making the degraded state visible to the app.
- Keep overlay transparency and mouse hit-testing deliberate: full-window click-through behavior can break crop selection if applied indiscriminately.
- Preserve scRGB linear semantics in shader/rendering work; avoid hidden gamma conversion, color clamping, or tone mapping unless explicitly implemented as an export/preview option.
- **MVP Export Exception:** Tone mapping and color space conversion (scRGB to sRGB/P3/HDR10) are explicitly implemented in the Export Pipeline (Epic 8 Story 8.2) and are required for MVP.
- `SetSwapChain(null)` must be called before graphics teardown to properly release the SwapChainPanel binding.
- WGC frames must be disposed promptly and not retained after checkout lifetime.
- Do not use `GraphicsCapturePicker` as the default capture entry point; direct monitor capture is the default MVP path.

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

Last Updated: 2026-05-09
