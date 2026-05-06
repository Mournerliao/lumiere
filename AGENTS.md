# AGENTS.md

## Project Overview

Lumiere is a native Windows desktop screenshot tool focused on HDR-correct capture and preview. The application foundation is WinUI 3 on Windows App SDK with Windows Graphics Capture, Direct3D 11, DXGI, and Vortice for the future GPU-resident HDR pipeline.

## Agent Entrypoints

- Start with `README.md` for the repository overview, platform constraints, validation commands, and commit convention.
- Read `harness/README.md` for durable project context and reusable guidance.
- Use `harness/planning/project-plan.md` for long-lived product and architecture intent.
- Use `harness/workflows/cross-platform-development.md` for the supported macOS editing, Windows CI, and Windows hardware validation workflow.
- Treat `_bmad-output/` as generated or stage-specific planning output, not as the durable source of truth unless a task explicitly points there.

## Platform Constraints

- Target `.NET 10` with `net10.0-windows10.0.19041.0`.
- Keep the primary architecture as `x64` / `win-x64`; do not use `Any CPU`.
- Preserve the native Windows foundation: WinUI 3, Windows App SDK, Windows Graphics Capture, Direct3D 11, DXGI, and Vortice.
- The main preview path must preserve FP16/scRGB HDR data.

## Architecture

Lumiere is a native Windows HDR screenshot tool: WinUI 3 on Windows App SDK, .NET 10 (`net10.0-windows10.0.19041.0`), x64 only. The core pipeline is WGC FP16 capture → D3D11 interop → scRGB swap chain → SwapChainPanel preview.

### Module Boundaries

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

## Coding Constraints

- Keep module boundaries narrow:
  - `Lumiere.App` wires WinUI startup and composition.
  - `Lumiere.Overlay` owns overlay and crop UI behavior.
  - `Lumiere.Capture` owns Windows Graphics Capture lifecycle.
  - `Lumiere.Graphics` owns D3D11/DXGI rendering and presentation.
  - `Lumiere.Infrastructure` owns interop, diagnostics, result types, and UI-thread helpers.
  - `Lumiere.Settings` owns local preferences only.
- Do not introduce Electron, Tauri, WPF bitmap-first, WinForms, GDI, web UI, cloud upload, telemetry, or SDR screenshot-library foundations.
- Put platform APIs behind the existing boundary projects before exposing small interfaces to other modules.
- Manage WGC, Vortice, DXGI, and COM resources explicitly with correct disposal semantics.

## Validation Commands

Full validation requires Windows. From the repository root, run:

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

macOS is suitable for editing, documentation, refactoring, API design, and platform-neutral test design. WinUI, WGC, DXGI, D3D11, HDR display behavior, and multi-monitor behavior require Windows validation.

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

## Collaboration Rules

- Follow the user's requested language for responses; this repository currently expects Chinese replies unless the user asks otherwise.
- Read the relevant project, tests, and harness documents before changing code.
- Keep edits scoped to the requested behavior and existing architecture.
- Do not claim full completion for HDR, WinUI, WGC, DXGI, or D3D11 behavior unless the result is clearly labeled with the validation level: Mac edit, Windows CI, or Windows manual validation.

## Skills & Workflows

本项目使用以下 skills 辅助开发：

### 通用 Skills（需自行安装，不提交仓库）

| Skill | 用途 | 触发方式 |
|---|---|---|
| `bmad-*` | BMad 工作流（需求分析、架构设计、故事创建、冲刺规划等） | 说 "talk to John/Winston/Bob" 或 "create PRD/story" |
| `impeccable` | 前端 UI 设计、审查、优化 | 说 "improve UI" 或 "review this design" |

通用 skills 由各开发者按自己的 AI 工具文档自行安装，安装位置因工具而异（`.agents/skills/` 或 `.claude/skills/` 等），已在 `.gitignore` 中排除。

### 项目专属 Skills（随仓库提交）

| Skill | 位置 | 用途 | 触发方式 |
|---|---|---|---|
| `winui-gallery-reference` | `harness/skills/winui-gallery-reference/` | 从官方 WinUI Gallery 获取组件参考、代码示例和最佳实践 | 说 "参考 NavigationView 用法" 或 "如何实现导航菜单" |

## Key Files to Read First

- `AGENTS.md` — this file, agent entrypoints and complete constraint list
- `harness/README.md` — durable harness map
- `harness/planning/project-plan.md` — product intent and phased implementation plan
- `harness/workflows/cross-platform-development.md` — Mac-edit/Windows-validate workflow and completion standards (Mac-pass / Windows CI-pass / Windows manual-pass)
