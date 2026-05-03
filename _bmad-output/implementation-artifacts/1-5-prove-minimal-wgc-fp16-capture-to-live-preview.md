# Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview

Status: done

<!-- Ultimate context engine analysis completed - comprehensive developer guide created. -->

## Story

作为 HDR 显示器用户，
我希望应用能提供一个保留源显示外观的最小实时预览，
以便在构建更广泛工作流功能之前，先证明 Lumiere 的核心产品承诺成立。

## Acceptance Criteria

1. Given 已为技术验证路径选择了一个 capture target，when WGC capture 启动，then `Direct3D11CaptureFramePool` 使用 `DirectXPixelFormat.R16G16B16A16Float`。
2. Given 有新 frame 到达，when frame 被送入预览渲染，then 主预览路径保持 GPU-resident，且常规显示过程中不使用 `BitmapImage`、`SoftwareBitmap`、GDI 或 CPU readback。
3. Given 预览运行在 HDR 硬件上，when 应用报告 readiness，then 用户可以看到预览是 `HDR-ready`、`Degraded`、`Unsupported` 或 `Failed`。

## Tasks / Subtasks

- [x] 确认 Story 1.1-1.4 的现有边界与复用点，不重复创建 graphics / interop 基础。 (AC: 1, 2, 3)
  - [x] 复用 `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs`、`GraphicsDeviceResources.cs` 和 `src/Lumiere.Graphics/Presentation/GraphicsEngine.cs`，不重新创建独立 D3D11 device。
  - [x] 复用 `src/Lumiere.Graphics/Hdr/HdrConstants.cs`、`PreviewReadinessStatus.cs`、`PreviewReadinessStage.cs` 与现有 presentation readiness 语义，不复制 HDR format / color-space / stage 常量。
  - [x] 复用 `src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs`、`ISwapChainPreviewSurface.cs`、`SwapChainPanelPreviewSurface.cs` 与 `NativeInteropException.cs`，保持 WinRT / DXGI / WinUI interop 在 Infrastructure 边界内。

- [x] 在 `Lumiere.Capture` 边界内建立最小 WGC capture session 生命周期。 (AC: 1, 2)
  - [x] 在 `src/Lumiere.Capture/` 下添加职责清晰的类型，例如 `CaptureService`、`CaptureSessionResources`、`CaptureTarget`、`CaptureStartResult` 或同等窄接口；不要把 WGC 生命周期塞进 `MainWindow`。
  - [x] 使用 Story 1.3 的 WinRT D3D device bridge 创建 `IDirect3DDevice`，并以 `DirectXPixelFormat.R16G16B16A16Float` 创建 `Direct3D11CaptureFramePool`。
  - [x] `FrameArrived` 处理流程显式区分后台线程与 UI 线程；不得直接从 frame 回调操作 WinUI 对象或 `SwapChainPanel`。
  - [x] 所有 `GraphicsCaptureSession`、`Direct3D11CaptureFramePool`、captured frame 及其持有的 COM/WinRT 资源都有确定性 `IDisposable` / `Close` 路径。

- [x] 在 graphics 边界内把 WGC frame 接入现有 swap-chain preview，而不引入 SDR fallback。 (AC: 2)
  - [x] 在 `src/Lumiere.Graphics/Presentation/` 添加最小 frame presentation 协调类型，例如 `PreviewFramePresenter`、`CapturedFrameTexture`、`PreviewRenderResult` 或同等职责类型。
  - [x] 从 captured frame 的 `IDirect3DSurface` 获取可供 D3D11 使用的 `ID3D11Texture2D`，并将内容复制或渲染到 Story 1.4 创建的 FP16/scRGB swap chain back buffer。
  - [x] 常规 frame 呈现全过程保持 GPU-resident；不得通过 `SoftwareBitmap`、`BitmapImage`、WIC、GDI、CPU map/readback 或 XAML image control 作为主预览路径。
  - [x] 初版仅需证明最小 live preview 成立；不要提前实现 tone mapping、导出、剪贴板、注释、历史记录或完整 crop overlay。

- [x] 在最小 app 接缝中挂接 preview host 与状态显示，证明用户可见的 readiness 反馈。 (AC: 3)
  - [x] 更新 `src/Lumiere.App/MainWindow.xaml` / `MainWindow.xaml.cs`，将当前占位内容替换为最小可验证 preview host，包含 `SwapChainPanel` 与不破坏后续 overlay 坐标的简洁状态展示。
  - [x] readiness 展示沿用 UX 规定的明确标签：`HDR-ready`、`Degraded preview`、`Unsupported capture`、`Preview failed`、`Initializing preview`；不要使用模糊成功文案。
  - [x] 若当前故事需要最小化 target 选择，可接受临时 spike 入口，但选择逻辑必须保持在 `Lumiere.Capture` / Infrastructure 边界，不在 UI 代码中直连底层 interop。

- [x] 为 capture-to-preview 管线添加可执行测试与验证说明。 (AC: 1, 2, 3)
  - [x] 在 `tests/` 下新增针对 capture 配置、frame handoff、readiness mapping 与资源释放顺序的单元测试；至少锁定 frame pool pixel format、无 SDR fallback guardrail、以及 stop/dispose 顺序。
  - [x] 如果真实 `GraphicsCaptureItem` / `Direct3D11CaptureFramePool` 无法在纯单元测试环境中完整运行，则通过窄 abstraction 做状态/生命周期测试，并在 Completion Notes 明确哪些部分仍需 Windows 手动验证。
  - [x] 按仓库规范准备验证命令：`dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`、`dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`、`dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`、`dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`。

### Review Findings

- [x] [Review][Decision] Story/sprint status overstates completion before Windows manual HDR validation - resolved: Story 1.5 remains implementation `done`; Windows manual HDR hardware validation is tracked separately and remains required before claiming final HDR behavior.
- [x] [Review][Patch] StopPreview can deadlock with an in-flight free-threaded frame callback [src/Lumiere.App/MainWindow.xaml.cs:138]
- [x] [Review][Patch] StartCapture failure leaves newly attached preview resources alive [src/Lumiere.App/MainWindow.xaml.cs:80]
- [x] [Review][Patch] StartPreview can race StopPreview and lose the newly started capture session [src/Lumiere.App/MainWindow.xaml.cs:94]
- [x] [Review][Patch] Add direct test coverage for `CaptureStartResult.StartSucceeded` [src/Lumiere.Capture/CaptureStartResult.cs:21]
- [x] [Review][Patch] Frame surface lifetime crosses the `Direct3D11CaptureFrame` lifetime [src/Lumiere.Capture/CaptureService.cs:103]
## Dev Notes

### Story Scope

本 story 的目标是把 Story 1.3 的 D3D11/WinRT bridge 与 Story 1.4 的 FP16/scRGB swap chain 真正串起来，形成最小可运行的 `WGC -> FP16 frame -> GPU-resident preview` 证明链路。它关注的是“技术验证级 live preview”，而不是完整产品流程：不要求完成正式 target picker UX、全屏 overlay 裁剪交互、导出、剪贴板、注释、托盘或热键。后续 Epic 2 和 Epic 3 会继续补 capture target/session lifecycle 与 overlay crop workflow。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Epic 1: Trusted HDR Preview Foundation`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview`]

Epic 1 的业务目标是证明 Lumiere 的核心价值确实成立：WGC FP16 frame 能到达 Direct3D/DXGI scRGB swap-chain preview，且应用能诚实表达预览是否可信。本 story 是 Epic 1 收口的关键证明点，完成后才值得进入后续更复杂的 target/session/crop 功能。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Epic 1: Trusted HDR Preview Foundation`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#Technical Success`]

### Current Repository Context

当前仓库已经具备以下可直接复用的基础：

- `src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs` 与 `GraphicsDeviceResources.cs`：已建立 BGRA-capable hardware D3D11 device、DXGI device、immediate context 与确定性释放。
- `src/Lumiere.Graphics/Hdr/HdrConstants.cs`：已集中定义 WGC pixel format、DXGI swap-chain format 与 scRGB color space。
- `src/Lumiere.Graphics/Hdr/PreviewReadinessStatus.cs`、`PreviewReadinessStage.cs`：已建立 typed readiness/degraded/unsupported/failed 状态模型。
- `src/Lumiere.Graphics/Presentation/GraphicsEngine.cs`、`SwapChainManager.cs`、`SwapChainResources.cs`：已可创建并附加 FP16/scRGB composition swap chain。
- `src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs`：已提供 D3D11 / WinRT bridge；`ISwapChainPreviewSurface.cs` 与 `SwapChainPanelPreviewSurface.cs` 已提供 preview surface attach seam。
- `src/Lumiere.App/MainWindow.xaml` 目前仍是简单占位 UI；`src/Lumiere.Capture/CaptureBoundary.cs` 仍为空壳边界，说明 capture 生命周期正是本 story 要首次落地的重点。

这些现状意味着实现应当“补上 Capture + Frame Presentation 的最小闭环”，而不是重新翻修 graphics foundation。[Source: local repository inspection on 2026-04-23; `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md#Current Repository Context`]

### Technical Requirements

- `Direct3D11CaptureFramePool` 必须使用 `DirectXPixelFormat.R16G16B16A16Float`，这是 story AC 之一，也是 HDR preview proof path 的基础约束。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/prd.md#Technical Success`]
- 主预览路径必须保持 GPU-resident，不得在常规 frame 呈现中使用 `BitmapImage`、`SoftwareBitmap`、GDI 或 CPU readback；这既是 AC，也直接对应 NFR1 / NFR6。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md#Story 1.5: Prove Minimal WGC FP16 Capture to Live Preview`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- `FrameArrived` 是后台线程入口；任何触碰 WinUI、`SwapChainPanel` 或用户可见状态的逻辑都必须通过 `DispatcherQueue` 切回 UI 线程。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Language-Specific Rules`; [Direct3D11CaptureFramePool.CreateFreeThreaded](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded?view=winrt-26100)]
- WGC frames 必须及时 `Dispose` / `Close`，不得跨越其有效生命周期持有旧 frame 或 surface；重复启动/停止时必须有明确 teardown 路径。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Critical Don't-Miss Rules`]
- readiness 报告必须对用户可见，并使用明确状态，而不是在无法证明 HDR 正确性时假装成功。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Experience Principles`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Design Implications`]

### Architecture Compliance

边界必须保持严格：

- `Lumiere.Capture`：拥有 `GraphicsCaptureItem`、`Direct3D11CaptureFramePool`、`GraphicsCaptureSession`、frame arrival、session stop/restart、frame disposal。不要让 `Lumiere.App` 直接 new 或持有这些对象。
- `Lumiere.Graphics`：拥有 swap chain、back buffer、frame-to-preview copy/render、presentation readiness 与 graphics resource lifetime。不要让 capture 层直接操作 `SwapChainPanel`。
- `Lumiere.Infrastructure`：拥有 WinRT / COM / HWND / HMONITOR / WinUI interop，包括 D3D bridge 与后续若需要的 `GraphicsCaptureItem` Win32 interop。不要把 raw COM pointer 或 interop 细节扩散到 App/UI。
- `Lumiere.App`：仅负责最小 wiring、preview host 承载、状态绑定/显示；不得在 code-behind 中拼装底层 capture/render 管线细节。

如果故事中加入最小 target 选择入口，Win32 `IGraphicsCaptureItemInterop::CreateForWindow` / `CreateForMonitor` 仍应被封装在 infrastructure boundary 后，且仅支持 Windows 10 Version 1903 / Build 18362 及以上；当前项目 TFM `net10.0-windows10.0.19041.0` 满足该下限。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Executive Architecture Summary`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Technical Constraints & Dependencies`; [IGraphicsCaptureItemInterop::CreateForWindow](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createforwindow); [IGraphicsCaptureItemInterop::CreateForMonitor](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor)]

### Library / Framework Requirements

- 继续使用既定平台与版本：`.NET 10`、`net10.0-windows10.0.19041.0`、Windows App SDK `1.8.260317003`、`Vortice.Direct3D11` `3.8.3`、`Vortice.DXGI` `3.8.3`、`Microsoft.Windows.CsWinRT` `2.2.0`（仅在具体 interop 需要时）。不要在此 story 中引入替代截图库或跨平台 UI 方案。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Selected Starter: WinUI 3 Blank App with Custom Graphics/Capture Infrastructure`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Technology Stack & Versions`]
- Microsoft Learn 当前文档说明 `Direct3D11CaptureFramePool.CreateFreeThreaded` 会移除对 `DispatcherQueue` 的依赖，并在内部工作线程引发 `FrameArrived`；如果实现选择该 API，必须显式处理线程切换和资源生存期。如果使用 `Create`，则也要保持同样的线程边界和像素格式约束。[Source: [Direct3D11CaptureFramePool.CreateFreeThreaded](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded?view=winrt-26100); [Direct3D11CaptureFramePool](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool?view=winrt-26100)]
- 不要把 `SwapChainPanel` 或 XAML image control 当成 texture transform 工具；其职责只是在 UI 线程承载已存在的 swap chain。frame copy/render 仍应留在 D3D11 / DXGI 层。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Framework-Specific Rules`; `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md#Architecture Compliance`]

### File Structure Requirements

预期落点可根据实现细节微调，但职责必须接近下列结构：

```text
src/
  Lumiere.Capture/
    CaptureService.cs
    CaptureSessionResources.cs
    CaptureStartResult.cs
    Targets/
      CaptureTarget.cs
  Lumiere.Graphics/
    Presentation/
      PreviewFramePresenter.cs
      PreviewRenderResult.cs
  Lumiere.App/
    MainWindow.xaml
    MainWindow.xaml.cs
tests/
  Lumiere.Graphics.Tests/
    Presentation/
      PreviewFramePresentationTests.cs
  Lumiere.Capture.Tests/   (仅当当前解法引入独立 capture tests 时)
```

如果只打算在现有测试项目中增加 guardrail tests，也可以把 capture-lifecycle 单元测试放入 `tests/Lumiere.Graphics.Tests/` 的新目录下，但不要为了省事把 capture 类型塞回 `Lumiere.App` 或 `Lumiere.Graphics`。项目依赖版本仍需走集中包管理，不在单个 `.csproj` 硬编码临时版本。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/architecture.md#Code Organization`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Development Workflow Rules`]

### UX Requirements Relevant to This Story

- 用户默认最关心的是“这个预览能不能信”，因此状态展示要用明确的系统词汇，而不是含糊成功文案。推荐沿用 UX 文档中的 `HDR-ready`、`Degraded preview`、`Unsupported capture`、`Preview failed`、`Initializing preview`。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Critical Success Moments`; `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md#UX Requirements Relevant to This Story`]
- preview surface 必须保持内容为中心，状态信息不能挤压、移动或重建 `SwapChainPanel` 边界，否则会破坏后续 crop 坐标稳定性。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Core User Experience`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Design Implications`]
- 失败或 degraded 不是坏 UX，相反是建立信任的组成部分；只要 capture 仍可诊断或重试，就不要用“静默降级成功”的方式掩盖问题。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Desired Emotional Response`; `/Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/ux-design-specification.md#Emotional Design Principles`]

### Testing Requirements

- 自动化测试优先覆盖：frame pool 像素格式配置、frame handoff 后的 readiness 语义、GPU-resident 呈现 guardrail、以及 stop/dispose 顺序。
- 如果 `FrameArrived` 或真实 WGC item 需要 Windows 集成环境，单元测试应通过 abstraction/fake 验证生命周期和线程边界，不要伪造“真实 HDR 预览已验证”。
- Windows 端最终仍需执行仓库标准验证链：restore、build、test、format；另外应补充一次真实 Windows 手动验证，至少证明 minimal preview 能打开、状态可见、退出后资源能释放。
- Completion Notes 必须明确区分 `Mac edit`、`Windows CI`、`Windows manual validation`，因为本 story 直接触及 WinUI、WGC、DXGI 和 HDR 预览行为。[Source: `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Testing Rules`; `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md#Development Workflow Rules`]

### Previous Story Intelligence

Previous story: `/Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/1-4-attach-an-fp16-scrgb-swap-chain-to-swapchainpanel.md`

可直接继承的经验与 guardrails：

- Story 1.4 已经把 preview attach path 收敛为 `GraphicsEngine.CreatePreviewSwapChain(..., ISwapChainPreviewSurface)`；1.5 不应绕过这个 seam，直接在 UI 层创建或操作 swap chain。
- Story 1.4 特别修复了 detach-before-release 与可重试 disposal 语义；1.5 的 capture stop / preview teardown 必须保留这一顺序，不能在 session 停止时把 graphics 资源释放顺序弄乱。
- Story 1.4 明确 presentation 初始化成功并不等于 live preview 已经 ready；1.5 才能在 frame 真正到达并成功呈现后，把 readiness 推进到更可信的状态。
- Story 1.4 的测试已经覆盖 swap-chain descriptor、color-space 与 attach failure mapping；1.5 应在这些基础上补 frame arrival / render handoff / lifecycle tests，而不是重写已有测试主题。

### Git Intelligence

最近相关提交：

- `f1b172f feat: attach FP16 scRGB swap chain`
- `214605b feat: add D3D11 interop foundation`
- `c9f6f46 docs: add agents entrypoint and reorganize harness docs`

这说明当前代码模式偏向“小而窄的强类型服务 + 明确的边界命名 + 先有 guardrail tests 再补实现”。本 story 延续该模式会比一次性做大更稳妥：先把 `CaptureService`、frame presenter 和最小 App wiring 建立好，再在现有边界上证明 live preview。[Source: local `git log --oneline -5` on 2026-04-23]

### Anti-Patterns to Avoid

- 不要在 `MainWindow.xaml.cs` 里直接 new `Direct3D11CaptureFramePool`、`GraphicsCaptureSession`、`ID3D11Device` 或写 WinRT/COM interop。
- 不要把 captured frame 转成 `SoftwareBitmap`、`BitmapSource`、PNG、byte[] 或其他 CPU 形式再显示。
- 不要引入新的 SDR swap chain、GDI blit、XAML `Image` 预览路径，哪怕只是“临时先通一下”。
- 不要从 `FrameArrived` 后台线程直接操作 `SwapChainPanel`、XAML 状态控件或 `PreviewReadinessStatus` 绑定对象。
- 不要在本 story 顺手实现 export、clipboard、overlay crop handles、global hotkey、tray、annotation 或 multi-monitor full UX。
- 不要吞掉 capture / presentation 失败细节；错误必须能落入现有 readiness/diagnostic 语义。

### Project Context Reference

实现前应重读 `/Users/asherliao/Projects/lumiere/_bmad-output/project-context.md`。本 story 最重要的项目级规则：

- HDR correctness 高于便利 fallback。
- 所有 WGC / WinRT / COM / D3D11 / DXGI 资源都需要明确 owner 和确定性释放。
- `FrameArrived` 与 WinUI UI-thread 边界必须显式处理。
- 主预览路径禁止 SDR bitmap / GDI / `SoftwareBitmap`。
- 完成说明不能把 macOS 编辑或 CI 当成 HDR 行为的最终证明。

## Dev Agent Record

### Agent Model Used

GPT-5

### Debug Log References

- 2026-04-23: Loaded `bmad-create-story` workflow, sprint status, Epic 1 / Story 1.5 source context, project plan, architecture, PRD, UX spec, project context, and previous Story 1.4 implementation notes.
- 2026-04-23: Inspected current repository state for `MainWindow`, `Lumiere.Capture` boundary, `GraphicsEngine`, and preview-surface interop seams to tailor file-structure guidance.
- 2026-04-23: Verified latest official Microsoft documentation for `Direct3D11CaptureFramePool.CreateFreeThreaded` and `IGraphicsCaptureItemInterop` Win32 capture item creation constraints.
- 2026-04-23: Started `bmad-dev-story`; moved Story 1.5 to `in-progress` and updated sprint tracking accordingly.
- 2026-04-23: Added minimal WGC capture session types, frame-surface interop, GPU-resident frame presenter, WinUI picker-driven preview wiring, and guardrail tests for capture configuration / lifecycle / frame presentation.
- 2026-04-23: Validation blocked locally because `dotnet` is not installed on this macOS workspace (`dotnet --info` returned `command not found`); Windows restore/build/test/manual validation remains pending.
- 2026-04-24: Continued `bmad-dev-story` on Windows; loaded BMad config, project context, Story 1.5, and sprint status.
- 2026-04-24: Ran Windows validation; initial build failed because `CaptureStartResult` had both a `Started` property and `Started(...)` factory method.
- 2026-04-24: Renamed the static factory to `StartSucceeded(...)`, updated `CaptureService`, ran `dotnet format`, and re-ran build/test/format successfully.
- 2026-04-24: Addressed code review finding by presenting captured frame textures synchronously inside the `Direct3D11CaptureFrame` lifetime and queuing only readiness updates to the UI thread.
- 2026-04-24: Addressed Edge Case Hunter findings by moving capture/session and swap-chain disposal outside `previewSync`, rolling back preview resources when capture start fails, synchronizing accepted capture session assignment, and adding direct `StartSucceeded` test coverage.

### Completion Notes List

- Added `CaptureService`, `CaptureSessionOptions`, `CaptureSessionResources`, `CaptureTarget`, and related lifecycle helpers under `Lumiere.Capture` for a minimal `GraphicsCapturePicker -> Direct3D11CaptureFramePool -> frame callback` flow.
- Added `Direct3D11SurfaceInterop` and `GraphicsCapturePickerInterop` under `Lumiere.Infrastructure` so WinRT surface unwrapping and desktop picker initialization stay behind narrow interop boundaries.
- Added `CapturedFrameTexture`, `PreviewFramePresenter`, `PreviewRenderResult`, and `SwapChainFrameOutput` under `Lumiere.Graphics.Presentation` to copy live frames into the existing FP16/scRGB swap chain without CPU readback.
- Updated `MainWindow` to host a `SwapChainPanel`, surface explicit preview readiness labels, and start the minimal live preview through the capture picker.
- Added guardrail tests for capture configuration defaults, capture disposal ordering, and preview frame presentation readiness mapping.
- Fixed the Windows compile blocker by renaming `CaptureStartResult.Started(...)` to `CaptureStartResult.StartSucceeded(...)`, preserving the `Started` boolean status property.
- Windows local validation pass: `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`, `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`, `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false` (37 passed), and `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`.
- Validation level: Mac edit not used in this session; Windows local build/test/format passed; Windows manual-pass completed on 2026-05-04 for the Epic 1 minimal HDR preview proof path only. The app reached `HDR-ready` after target selection, and diagnostics reported that `Direct3D11CaptureFrame.Surface` reached the FP16 scRGB swap chain without CPU readback. Epic 2 lifecycle behavior remains out of scope and not started.
- Resolved review finding: frame presentation now happens before `Direct3D11CaptureFrame` disposal; UI dispatch no longer carries `CapturedFrameTexture` beyond the WGC frame callback.
- Resolved Edge Case Hunter findings: `StopPreview` now detaches state under `previewSync` and disposes capture/swap-chain resources after releasing the lock; failed capture startup rolls back the newly attached preview resources; successful capture session assignment is synchronized and stale startup results are disposed; `StartSucceeded` has direct unit coverage.

### File List

- _bmad-output/implementation-artifacts/1-5-prove-minimal-wgc-fp16-capture-to-live-preview.md
- _bmad-output/implementation-artifacts/sprint-status.yaml
- src/Lumiere.App/MainWindow.xaml
- src/Lumiere.App/MainWindow.xaml.cs
- src/Lumiere.Capture/CaptureService.cs
- src/Lumiere.Capture/CaptureSessionDisposalCoordinator.cs
- src/Lumiere.Capture/CaptureSessionOptions.cs
- src/Lumiere.Capture/CaptureSessionResources.cs
- src/Lumiere.Capture/CaptureStartResult.cs
- src/Lumiere.Capture/CaptureTarget.cs
- src/Lumiere.Capture/Properties/AssemblyInfo.cs
- src/Lumiere.Graphics/Presentation/CapturedFrameTexture.cs
- src/Lumiere.Graphics/Presentation/IPreviewFrameOutput.cs
- src/Lumiere.Graphics/Presentation/PreviewFramePresenter.cs
- src/Lumiere.Graphics/Presentation/PreviewRenderResult.cs
- src/Lumiere.Graphics/Presentation/SwapChainFrameOutput.cs
- src/Lumiere.Graphics/Properties/AssemblyInfo.cs
- src/Lumiere.Infrastructure/Interop/Direct3D11SurfaceInterop.cs
- src/Lumiere.Infrastructure/Interop/GraphicsCapturePickerInterop.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureLifecycleTests.cs
- tests/Lumiere.Graphics.Tests/Capture/CaptureSessionConfigurationTests.cs
- tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj
- tests/Lumiere.Graphics.Tests/Presentation/PreviewFramePresenterTests.cs

### Change Log

- 2026-04-24: Completed Story 1.5 implementation validation on Windows, fixed `CaptureStartResult` factory naming conflict, updated task checkboxes, and moved the story to review.
- 2026-04-24: Resolved code review finding for captured frame lifetime and moved the story to done.
- 2026-04-24: Resolved follow-up Edge Case Hunter findings for stop/start races, failed capture rollback, and success-factory coverage.
