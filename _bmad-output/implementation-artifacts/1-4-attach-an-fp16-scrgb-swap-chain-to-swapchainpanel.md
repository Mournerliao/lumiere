# Story 1.4: Attach an FP16 scRGB Swap Chain to SwapChainPanel

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

作为 HDR 截图用户，
我希望预览表面通过 HDR-capable swap chain 进行硬件渲染，
以便应用能够保留 HDR 外观，而不是显示被 SDR bitmap 路径洗掉的预览。

## Acceptance Criteria

1. Given 一个 `SwapChainPanel` 在 UI 线程可用，when graphics engine 附加 composition swap chain，then swap chain 使用 `DXGI_FORMAT_R16G16B16A16_FLOAT`。
2. Given swap chain 已创建，when 配置 color space，then 设置 `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`，或者产生可见的 degraded/failed diagnostic。
3. Given graphics teardown 开始，when 预览被 detach，then 在释放 device-bound resources 之前，必须在 UI 线程调用 `SetSwapChain(null)`。

## Tasks / Subtasks

- [x] 确认 Story 1.1-1.3 前置条件和现有边界。 (AC: 1, 2, 3)
  - [x] 确认 `Lumiere.sln`、`Directory.Build.props`、`Directory.Packages.props`、`src/Lumiere.App/`、`src/Lumiere.Graphics/`、`src/Lumiere.Infrastructure/` 和 `tests/Lumiere.Graphics.Tests/` 存在。
  - [x] 复用 Story 1.2 的 `HdrConstants`、`PreviewReadinessStatus`、`PreviewReadinessStage`，没有重复定义 HDR format/color-space/readiness state。
  - [x] 复用 Story 1.3 的 `GraphicsDeviceProvider`、`GraphicsDeviceResources`、`Direct3D11Interop` 和 `NativeInteropException`，未让 UI 或 overlay 直接创建 D3D11 device。

- [x] 在 graphics 边界内实现 FP16 composition swap-chain 创建和生命周期。 (AC: 1, 2, 3)
  - [x] 在 `src/Lumiere.Graphics/Presentation/` 添加 `SwapChainManager`、`GraphicsEngine`、`SwapChainResources` 等职责明确的类型。
  - [x] 使用现有 `GraphicsDeviceResources.Device` 创建 DXGI composition swap chain；没有创建第二个独立 D3D11 device。
  - [x] 使用 `HdrConstants.DxgiSwapChainFormat`，确保 `DXGI_SWAP_CHAIN_DESC1.Format` 为 `DXGI_FORMAT_R16G16B16A16_FLOAT`。
  - [x] composition swap chain 使用 flip model，`SampleDescription.Count = 1`、`SampleDescription.Quality = 0`，buffer count 默认为 2。
  - [x] 尺寸来自显式 `SwapChainCreationOptions` preview pixel size；0 或负数 width/height 会被拒绝。
  - [x] swap chain owner `SwapChainResources` 实现确定性 `IDisposable`，并通过 coordinator 保证 detach-before-release。

- [x] 在 infrastructure 边界内实现 WinUI `SwapChainPanel` attach/detach interop。 (AC: 1, 3)
  - [x] 在 `src/Lumiere.Infrastructure/Interop/` 添加 `SwapChainPanelNativeInterop.cs`，封装 `ISwapChainPanelNative.SetSwapChain`。
  - [x] interop API 接收 `Microsoft.UI.Xaml.Controls.SwapChainPanel` 和 `IDXGISwapChain`，不把 raw COM pointer 暴露给 app/overlay 代码。
  - [x] `SetSwapChain(swapChain)` 和 `SetSwapChain(null)` 被隔离在 infrastructure API 后；调用方仍必须在拥有该 `SwapChainPanel` 的 UI 线程调用。
  - [x] `SetSwapChain` HRESULT failure 会映射为包含 operation name、Interop stage、HRESULT 和 technical detail 的 `NativeInteropException`。

- [x] 配置 scRGB color space 并报告 readiness。 (AC: 2)
  - [x] 对 swap chain 查询 `IDXGISwapChain3`，调用 `CheckColorSpaceSupport(HdrConstants.DxgiColorSpace)` 并记录结果。
  - [x] 调用 `SetColorSpace1(HdrConstants.DxgiColorSpace)`；成功后报告 presentation initialization evidence，不假装 WGC live preview 已完成。
  - [x] 如果 color-space support 或 `SetColorSpace1` 失败，返回 `PreviewReadinessStatus.Degraded(...)` 或 `Failed(...)`，technical detail 包含 operation/HRESULT/API 名称。
  - [x] 没有 fallback 到 SDR color space、8-bit swap chain、`BitmapImage`、`SoftwareBitmap`、GDI 或 CPU readback。

- [x] 在最小 app/overlay 接缝中暴露可 attach 的 preview host，但不实现后续 WGC live frame rendering。 (AC: 1, 3)
  - [x] 未改 `MainWindow.xaml` 或 overlay placeholder；本 story 只提供可由后续 UI 接入的 graphics/infrastructure API。
  - [x] 未引入会改变 preview bounds 的 status/diagnostic UI。
  - [x] Loading/readiness 状态在 color-space 和 attachment 验证完成前不会显示为 `Ready` 或 `HDR-ready`。

- [x] 添加聚焦测试和可执行验证。 (AC: 1, 2, 3)
  - [x] 在 `tests/Lumiere.Graphics.Tests/Presentation/` 添加 swap-chain description/configuration 测试，锁定 FP16 format、flip model、buffer count、sample count 和 scRGB color space。
  - [x] 添加 readiness mapping 测试，覆盖 color-space failure、attach failure 和 presentation-stage diagnostic。
  - [x] 通过窄 abstraction/coordinator 验证 detach-before-dispose 顺序；真实 WinUI `SwapChainPanel` UI-thread attachment 仍需后续手动/集成验证。
  - [x] 保留现有 HDR constants/readiness/device-provider tests。
  - [x] 运行 `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`。
  - [x] 运行 `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`。
  - [x] 运行 `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`。
  - [x] 运行 `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`。

### Review Findings

- [x] [Review][Decision] Decide whether Story 1.4 requires a real `SwapChainPanel` attach path or only attachable APIs - resolved by adding a mandatory preview surface attach path through `ISwapChainPreviewSurface` / `SwapChainPanelPreviewSurface` and `GraphicsEngine.CreatePreviewSwapChain(..., previewSurface)`.
- [x] [Review][Patch] `SwapChainResources` can release an attached swap chain without a mandatory detach action [src/Lumiere.Graphics/Presentation/SwapChainResources.cs:11]
- [x] [Review][Patch] `SwapChainResources.Dispose` marks the object disposed before detach succeeds, preventing retry after UI-thread or COM detach failure [src/Lumiere.Graphics/Presentation/SwapChainResources.cs:25]

## Dev Notes

### Story Scope

本 story 只建立 presentation 基础：FP16/scRGB DXGI composition swap chain、`SwapChainPanel` attach/detach、color-space validation、presentation readiness diagnostic 和 deterministic teardown。它不创建 WGC frame pool，不接收 live frame，不写 shader copy/rendering，不实现 crop overlay，也不输出截图。Story 1.5 才证明 WGC FP16 capture 到 live preview 的完整链路。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\epics.md#Story 1.4: Attach an FP16 scRGB Swap Chain to SwapChainPanel`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\epics.md#Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview`]

Epic 1 的目标是证明 WGC FP16 frame 能到达 Direct3D/DXGI scRGB swap-chain preview，且 app 能清楚表达 preview 是否 HDR-ready。本 story 是 Story 1.3 D3D11/WinRT/DXGI bridge 和 Story 1.5 live preview 之间的 presentation 接缝。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\epics.md#Epic 1: Trusted HDR Preview Foundation`]

### Current Repository Context

当前仓库已经有：

- `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs`：创建 BGRA-capable hardware D3D11 device，并暴露 `ID3D11Device`、immediate context、`IDXGIDevice`、feature level 和 initialization evidence。
- `src/Lumiere.Graphics/Devices/GraphicsDeviceResources.cs`：拥有并按 `DxgiDevice`、`ImmediateContext`、`Device` 顺序释放 device resources。
- `src/Lumiere.Graphics/Hdr/HdrConstants.cs`：集中定义 WGC pixel format、DXGI swap-chain format 和 scRGB color space。
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs` 及相关 enum：提供 initializing/ready/degraded/unsupported/failed 状态和 stage/detail。
- `src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs` 与 `NativeInteropException.cs`：隔离 WinRT/DXGI interop failure。
- `tests/Lumiere.Graphics.Tests/`：已有 HDR constants/readiness/device-provider 测试。

这些类型是 1.4 的复用点。不要复制 device creation 或 HDR constants，也不要把 COM pointer handling 扩散到 UI 层。[Source: local repository inspection on 2026-04-22; `D:\UGit\lumiere\_bmad-output\implementation-artifacts\1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md#Dev Agent Record`]

### Technical Requirements

- `GraphicsEngine`/presentation code owns DXGI swap chain、render targets、resize handling、presentation 和 WinUI swap-chain interop；`OverlayUI` 只拥有 WinUI window、`SwapChainPanel`、overlay canvas、crop/toolbar/user state。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Executive Architecture Summary`]
- HDR preview path 必须使用 `DXGI_FORMAT_R16G16B16A16_FLOAT` 和 `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`，并避免 CPU readback/bitmap conversion。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\prd.md#Technical Success`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\prd.md#Non-Functional Requirements`]
- `SwapChainPanel` attachment 必须通过 `ISwapChainPanelNative.SetSwapChain`，且 `SetSwapChain` 与 WinUI state mutation 必须在 UI 线程执行。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Technical Constraints & Dependencies`]
- `SetSwapChain(null)` 必须在释放 swap-chain/device-bound resources 前执行。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Technical Constraints & Dependencies`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\prd.md#Risk Mitigations`]
- Expected degraded/unsupported states 使用 typed result/status；exceptions 用于 programmer errors 或 unrecoverable native failures。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Error Handling Standard`]

### Architecture Compliance

边界规则：

- `Lumiere.Graphics`：创建和拥有 swap chain、back buffer/render target、presentation validation、graphics/presentation readiness evidence。
- `Lumiere.Infrastructure/Interop`：封装 `ISwapChainPanelNative`、COM QueryInterface、HRESULT translation、UI-thread attach/detach helper。文件命名要具体，例如 `SwapChainPanelNativeInterop.cs`，不要用 `Helpers.cs`。
- `Lumiere.App` / `Lumiere.Overlay`：可以承载 `SwapChainPanel` 和调用窄 presenter API，但不得知道 DXGI descriptor、COM pointer 或 D3D11 creation details。
- `Lumiere.Capture`：本 story 不触碰；WGC frame pool 和 frame arrival 留给后续 story。

项目结构应遵守 one primary type per file，责任命名使用 `GraphicsEngine`、`SwapChainManager`、`SwapChainPanelPresenter`、`SwapChainResources` 等清晰名称。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Structure Patterns`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Project Structure & Boundaries`]

### Library / Framework Requirements

使用既定版本：

- `.NET 10 LTS` / `net10.0-windows10.0.19041.0`
- Windows App SDK `1.8.260317003`
- `Vortice.Direct3D11` `3.8.3`
- `Vortice.DXGI` `3.8.3`
- `Microsoft.Windows.CsWinRT` `2.2.0` 仅在 interop 具体需要时使用

官方 API 核查：

- Microsoft Learn 说明 `IDXGIFactory2::CreateSwapChainForComposition` 可将 Direct3D 内容送入 WinUI XAML composition，并要求 `DXGI_SWAP_EFFECT_FLIP_SEQUENTIAL` 与 `DXGI_SCALING_STRETCH`；WinUI XAML 的 `ISwapChainPanelNative` 声明在 `microsoft.ui.xaml.media.dxinterop.h`。[Source: https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/nf-dxgi1_2-idxgifactory2-createswapchainforcomposition]
- Microsoft Learn 说明 composition swap chain 的 `DXGI_SWAP_CHAIN_DESC1` 不能使用 0 width/height；flip-model swap chain 支持 `DXGI_FORMAT_R16G16B16A16_FLOAT`，MSAA 不支持，`SampleDesc.Count` 必须为 1，`Quality` 为 0，`BufferCount` 为 2 到 16。[Source: https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_2/ns-dxgi1_2-dxgi_swap_chain_desc1]
- Microsoft Learn 说明 `IDXGISwapChain3::SetColorSpace1` 设置 swap chain color space，成功返回 `S_OK`，否则返回 DXGI/HRESULT error。[Source: https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/nf-dxgi1_4-idxgiswapchain3-setcolorspace1]
- Microsoft Learn 说明 `IDXGISwapChain3::CheckColorSpaceSupport` 根据当前 adapter output 检查 color-space support；即使查询未返回支持，某些 color space 仍可能被设置并显示，但可能产生 clipping，因此 failure/degraded diagnostic 必须诚实。[Source: https://learn.microsoft.com/en-us/windows/win32/api/dxgi1_4/nf-dxgi1_4-idxgiswapchain3-checkcolorspacesupport]
- Microsoft Learn 说明 `ISwapChainPanelNative.SetSwapChain` 必须在拥有 panel 的 UI 线程调用；传入 `null` 可释放 `SwapChainPanel` 添加到 swap chain/device graph 上的引用，这对 teardown 和 device-lost recovery 很重要。[Source: https://learn.microsoft.com/en-us/windows/win32/api/windows.ui.xaml.media.dxinterop/nf-windows-ui-xaml-media-dxinterop-iswapchainpanelnative-setswapchain]

### File Structure Requirements

预期实现位置可按现有代码微调，但责任边界不能变：

```text
src/
  Lumiere.Graphics/
    Presentation/
      GraphicsEngine.cs
      SwapChainCreationOptions.cs
      SwapChainManager.cs
      SwapChainResources.cs
      SwapChainPresentationResult.cs
  Lumiere.Infrastructure/
    Interop/
      SwapChainPanelNativeInterop.cs
      SwapChainPanelInteropException.cs   (only if NativeInteropException is not enough)
tests/
  Lumiere.Graphics.Tests/
    Presentation/
      SwapChainConfigurationTests.cs
      SwapChainReadinessTests.cs
      SwapChainLifecycleTests.cs
```

如果 Vortice 类型需要新增 centralized package reference，只能通过 `Directory.Packages.props` 统一管理版本；不要在单个 `.csproj` 写临时版本号。[Source: `D:\UGit\lumiere\Directory.Packages.props`; `D:\UGit\lumiere\_bmad-output\project-context.md#Development Workflow Rules`]

### UX Requirements Relevant to This Story

本 story 的用户可见范围很小，但 readiness 语义要为后续 overlay 做对：

- Loading states 应该明确是 `Initializing graphics` 或 `Preparing preview`，不得暗示 HDR readiness 已经成立。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Loading States`]
- 状态文案应使用明确标签：`HDR-ready`、`Degraded preview`、`Unsupported capture`、`Preview failed`、`Initializing preview`；不要使用 `Looks good`、`Maybe HDR`、`Success` 这类模糊标签。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Copy Patterns`]
- preview surface 是底层硬件层；status 或 diagnostic UI 不能改变 `SwapChainPanel` bounds，也不能让后续 crop coordinate mapping 不稳定。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Overlay Behavior`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Implementation Guidelines`]
- Degraded/failed 状态要足够显眼以防 false trust，但不应在 preview/capture 仍可用时阻塞后续 crop flow。[Source: `D:\UGit\lumiere\_bmad-output\planning-artifacts\ux-design-specification.md#Feedback Patterns`]

### Testing Requirements

- 自动化测试优先覆盖 descriptor/configuration、status mapping、lifecycle ordering 和不回退到 SDR 的 guardrails。
- 不要把真实 HDR 显示效果伪装成自动化测试；manual HDR hardware validation 仍属于后续 Phase 0/live preview 验证。
- 若 `SetSwapChain` 只能在真实 WinUI UI 线程下可靠验证，使用可注入 interop abstraction 测试调用顺序，并在 Completion Notes 中记录未自动化的 manual validation。
- 测试必须继续锁定 `HdrConstants.SwapChainFormat == Format.R16G16B16A16_Float` 与 `HdrConstants.SwapChainColorSpace == ColorSpaceType.RgbFullG10NoneP709`。
- 任何触及 disposal order、HDR constants、swap-chain format 或 readiness semantics 的变化，都要在 review notes/Completion Notes 中明确说明。[Source: `D:\UGit\lumiere\_bmad-output\project-context.md#Testing Rules`; `D:\UGit\lumiere\_bmad-output\planning-artifacts\architecture.md#Pattern Enforcement`]

### Previous Story Intelligence

Previous story: `D:\UGit\lumiere\_bmad-output\implementation-artifacts\1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md`

可继承的实现经验：

- Story 1.3 已经完成 D3D11 device provider、deterministic disposal 和 WinRT/DXGI bridge；1.4 应在这些资源上建立 swap chain，不要重新发明 device creation。
- 失败路径已经倾向于 typed exception + readiness mapping；1.4 应延续 operation name、stage、technical detail 的格式。
- 1.3 明确成功创建 device 只是 initialization evidence，不代表 preview `Ready`；1.4 也只能在 attachment/color-space presentation 验证后推进 presentation evidence，不能代表 WGC live preview 已完成。
- 1.3 遇到过 restore/build/test 顺序和 test DLL lock 问题；验证时优先顺序执行 restore、build、test、format。
- 1.3 没有加入 WGC frame pool、swap chain、live frame rendering、export、clipboard、hotkey、tray、annotation 或 history；1.4 只新增 swap-chain presentation，继续保持范围收窄。

### Git Intelligence

最近提交：

- `214605b feat: add D3D11 interop foundation`
- `21a2cea chore: update .gitignore and remove deprecated agent files`
- `06f20db chore: initial project scaffold`

执行含义：

- Git 仓库已存在；`project-context.md` 中“未检测到 Git”的旧备注不再适用。
- 最近真正相关的模式是小型、强类型、职责清晰的 graphics/infrastructure 类型，加 xUnit guardrail tests。
- 继续遵守现有 `x64`、`net10.0-windows10.0.19041.0`、central package management 和 module boundary。

### Anti-Patterns to Avoid

- 不要在 `Lumiere.App`、`Lumiere.Overlay` 或 `Lumiere.Capture` 中创建 D3D11 device、DXGI factory 或 raw swap-chain COM interop。
- 不要创建 8-bit SDR swap chain，也不要把 format 改成 `B8G8R8A8`、`R8G8B8A8` 或普通 SDR format。
- 不要使用 `BitmapImage`、`SoftwareBitmap`、GDI、CPU readback 或 XAML image control 作为 main live preview 路径。
- 不要从 capture callback 或后台线程调用 `SetSwapChain`。
- 不要在释放 device/swap-chain resources 前忘记 `SetSwapChain(null)`。
- 不要吞掉 HRESULT，也不要用 `null`/boolean failure 代替带 operation/stage/detail 的诊断。
- 不要提前实现 Story 1.5 的 WGC frame pool、frame rendering、shader path 或真实 live preview。
- 不要加入 export、clipboard、hotkey、tray、annotation、history、cloud 或 telemetry 行为。

### Project Context Reference

实现前请阅读 `D:\UGit\lumiere\_bmad-output\project-context.md`。本 story 的最高优先级规则：

- HDR correctness 优先于便利 fallback。
- 所有 Direct3D/DXGI/WinRT/COM/swap-chain owner 必须可确定性释放。
- `SwapChainPanel` 和 WinUI state 只能在 UI 线程触碰。
- main preview 不允许 SDR/GDI/bitmap 路径。
- 触及 HDR constants、swap-chain format、color-space 或 resource lifetime semantics 时，必须在 completion/review notes 中点名。

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-04-23: Loaded sprint status, Story 1.4 context, and project context; marked `1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel` as in-progress.
- 2026-04-23: Added failing presentation tests for FP16 composition swap-chain description, scRGB color-space readiness mapping, attach failure diagnostics, and detach-before-release ordering.
- 2026-04-23: Implemented graphics presentation types, color-space controller/configurator, swap-chain resource ownership, and Infrastructure `ISwapChainPanelNative.SetSwapChain` wrapper.
- 2026-04-23: Initial `dotnet format --verify-no-changes` failed on line endings for new files; ran `dotnet format` and re-verified clean.
- 2026-04-23: Addressed code review findings by adding a mandatory preview surface attach path and making detach required/retryable before swap-chain release.

### Completion Notes List

- Implemented `SwapChainCreationOptions` to produce a DXGI composition swap-chain descriptor with FP16 `HdrConstants.DxgiSwapChainFormat`, flip sequential presentation, stretch scaling, two buffers, and no MSAA.
- Implemented `SwapChainManager` and `GraphicsEngine` presentation entry points that reuse existing `GraphicsDeviceResources.Device` instead of creating a second D3D11 device.
- Implemented `SwapChainColorSpaceConfigurator` and `SwapChainColorSpaceController` to check and set `HdrConstants.DxgiColorSpace`, returning presentation-stage initializing/degraded/failed readiness without marking live preview ready.
- Added `SwapChainResources` and `SwapChainDisposalCoordinator` so presentation detach is ordered before swap-chain release.
- Added `SwapChainPanelNativeInterop` under Infrastructure to encapsulate `ISwapChainPanelNative.SetSwapChain` / `SetSwapChain(null)` and map HRESULT failures to `NativeInteropException`.
- Added `ISwapChainPreviewSurface` and `SwapChainPanelPreviewSurface`, and changed `GraphicsEngine.CreatePreviewSwapChain` to attach through a preview surface before returning resources.
- Made `SwapChainResources` require a detach action and mark itself disposed only after detach and release complete, so UI-thread detach failures can be retried.
- Added `PreviewReadinessStatus.Initializing(PreviewReadinessStage, ...)` overload so presentation validation evidence can remain non-ready while carrying the correct stage.
- Did not implement WGC frame pool, live frame rendering, shader path, crop UI, export, clipboard, hotkey, tray, annotation, history, cloud, telemetry, or SDR fallback behavior.
- Real `SwapChainPanel` UI-thread attachment was encapsulated but not exercised by automated tests; current tests cover descriptor/configuration, readiness mapping, attach failure diagnostic mapping, and detach-before-release ordering.
- Verification passed: `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`; `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`; `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`; `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.

### File List

- src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs
- src/Lumiere.Graphics/Presentation/GraphicsEngine.cs
- src/Lumiere.Graphics/Presentation/ISwapChainColorSpaceController.cs
- src/Lumiere.Graphics/Presentation/SwapChainColorSpaceConfigurator.cs
- src/Lumiere.Graphics/Presentation/SwapChainColorSpaceController.cs
- src/Lumiere.Graphics/Presentation/SwapChainCreationOptions.cs
- src/Lumiere.Graphics/Presentation/SwapChainDisposalCoordinator.cs
- src/Lumiere.Graphics/Presentation/SwapChainManager.cs
- src/Lumiere.Graphics/Presentation/SwapChainPresentationException.cs
- src/Lumiere.Graphics/Presentation/SwapChainResources.cs
- src/Lumiere.Infrastructure/Interop/SwapChainPanelNativeInterop.cs
- src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainConfigurationTests.cs
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainLifecycleTests.cs
- tests/Lumiere.Graphics.Tests/Presentation/SwapChainReadinessTests.cs
- _bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-04-23: Implemented Story 1.4 FP16/scRGB composition swap-chain presentation foundation and marked story ready for review.
