# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build & Validation

All commands require Windows and must be run from the repository root:

```bash
# Restore, build, test, and format check — the full CI sequence:
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal

# Run a single test:
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --filter "FullyQualifiedName~ClassName.TestName"
```

macOS can edit code, design interfaces, and write platform-neutral tests, but restore/build/test/format and all WinUI/WGC/DXGI/D3D11/HDR validation require Windows.

## Architecture

Lumiere is a native Windows HDR screenshot tool: WinUI 3 on Windows App SDK, .NET 10 (`net10.0-windows10.0.19041.0`), x64 only. The core pipeline is WGC FP16 capture → D3D11 interop → scRGB swap chain → SwapChainPanel preview.

### Module Boundaries (narrow, enforced)

| Project | Responsibility |
|---|---|
| `Lumiere.App` | WinUI startup, window composition, wires Graphics/Capture/Infrastructure |
| `Lumiere.Graphics` | D3D11 device, DXGI swap chain, HDR constants, presentation |
| `Lumiere.Capture` | WGC frame pool, capture session lifecycle, frame disposal |
| `Lumiere.Infrastructure` | COM/WinRT interop, native marshaling, Win32 bridge, `NativeInteropException` |
| `Lumiere.Overlay` | Full-screen overlay, crop UI, mouse/keyboard interaction (future) |
| `Lumiere.Settings` | Local preferences only |

**Rule:** New WGC, DXGI, COM, Win32, or WinUI calls must go into their boundary project first, then expose narrow interfaces. Do not scatter platform APIs into UI or test code.

### Key HDR Invariants

- Swap chain format: `DXGI_FORMAT_R16G16B16A16_FLOAT` (see `HdrConstants.DxgiSwapChainFormat`)
- Color space: `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709` (see `HdrConstants.DxgiColorSpace`)
- WGC frame pool pixel format: `DirectXPixelFormat.R16G16B16A16Float` (see `HdrConstants.WgcFramePoolPixelFormat`)
- Never introduce SDR fallback paths or non-scRGB swap chains.

### Data Flow

```
GraphicsCapturePicker → CaptureService.StartCapture()
  → Direct3D11CaptureFramePool (FP16 frames on background thread)
  → HandleFrameArrived: interop texture via Direct3D11SurfaceInterop
  → MainWindow.OnCapturedFrameArrived (stale check via previewGeneration)
  → PreviewFramePresenter.PresentFrame() (CopyFrame + Present on GPU)
  → SwapChainPanel (UI thread, via DispatcherQueue.TryEnqueue)
```

### Lifecycle & Stale-Frame Guard

`MainWindow` uses a `previewGeneration` counter under `previewSync`. Each `StartPreview` increments it; each `StopPreview` increments it. Frame handlers and UI dispatches check that the generation hasn't changed — if it has, the frame/session is discarded. This prevents stale frames from a previous capture session reaching the display.

### State Reporting

`PreviewReadinessStatus` (record, immutable) flows through the entire pipeline. It carries `State` (Initializing/Ready/Degraded/Unsupported/Failed), `Stage` (Capture/Graphics/Presentation/Overlay/Interop/Lifecycle/Unknown), a `UserMessage`, and an optional `TechnicalDetail`. Every pipeline component reports readiness the same way — no ad-hoc status strings.

### COM Resource Management

D3D11 devices, DXGI swap chains, WGC sessions/frame pools, and WinRT interop wrappers are unmanaged resources. All wrapper types that hold them implement `IDisposable` with deterministic cleanup. The `CaptureSessionResources` and `SwapChainResources` classes centralize disposal. `SwapChainPanelNativeInterop` manually manages `IUnknown` marshal/release for `ISwapChainPanelNative.SetSwapChain`.

## Testing

Tests live in `tests/Lumiere.Graphics.Tests/`, using xUnit. Current test categories:

- **HDR constants** — verify pixel format, DXGI format, color space values are correct
- **PreviewReadinessStatus** — factory methods and state transitions
- **Device/provider** — creation flags, feature levels, error mapping
- **Swap chain** — configuration, lifecycle, readiness mapping
- **Capture lifecycle** — disposal ordering, start result states
- **PreviewFramePresenter** — frame presentation success/failure paths, using `FakePreviewFrameOutput` (implements `IPreviewFrameOutput`)

Tests intentionally use fakes/stubs for platform boundaries (`IPreviewFrameOutput`, `ISwapChainColorSpaceController`) so they can exercise state machines and disposal ordering without real GPU or WGC. Do not write tests that require a real D3D11 device unless the test project explicitly adds a hardware-dependent category.

## Commit Convention

```
feat:  user-visible capability
fix:   defect fix
docs:  documentation only
chore: scaffold, build, repo maintenance
test:  test-only changes
```

## Key Files to Read First

- `AGENTS.md` — agent entrypoints and complete constraint list
- `harness/README.md` — durable harness map
- `harness/planning/project-plan.md` — product intent and phased implementation plan
- `harness/workflows/cross-platform-development.md` — Mac-edit/Windows-validate workflow and completion standards (Mac-pass / Windows CI-pass / Windows manual-pass)
