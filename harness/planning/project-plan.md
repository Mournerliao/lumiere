# Lumiere - HDR 原生 Windows 截图工具

> 项目规划文档 / AI 协作上下文 Harness
> 最后更新：2026-05-07

---

## 项目目标

开发一款原生的 Windows 桌面截图工具。核心痛点是 **完美支持 HDR 显示器截图**。

必须绕过 Windows DWM 的默认色调映射，获取真实的 16 位浮点（FP16）纹理，并使用支持 scRGB 色彩空间的 DirectX 交换链在前端高保真渲染，确保在高亮度硬件（如 RTX 5080 + Mini-LED HDR 显示器）上颜色不发白、不失真。

---

## 技术栈选型

| 层级 | 技术 |
| --- | --- |
| 运行时 | .NET 10 LTS（目标 TFM：`net10.0-windows10.0.19041.0`） |
| UI 框架 | WinUI 3 (Windows App SDK) |
| 图形与捕获 API 封装 | `Vortice.Windows`（用于调用 Direct3D 11 和 DXGI） |
| 系统 API 桥接 | `Microsoft.Windows.CsWinRT` |
| 屏幕捕获技术 | `Windows.Graphics.Capture` (WGC) |

---

## 核心架构设计（模块化）

生成代码时必须严格遵循以下模块划分。

### 1. GraphicsEngine（图形渲染引擎层）

- **职责：** 管理 Direct3D 11 Device、DeviceContext 和 DXGI SwapChain。
- **核心配置要求（绝对不可修改）：**
  - `SwapChain` 格式必须为：`DXGI_FORMAT_R16G16B16A16_FLOAT`
  - `SwapChain` 色彩空间必须为：`DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`（scRGB 线性空间）
  - 必须支持与 WinUI 3 的 `ISwapChainPanelNative` 接口互操作

### 2. CaptureService（屏幕捕获服务层）

- **职责：** 封装 `Windows.Graphics.Capture` 逻辑。
- **核心配置要求（绝对不可修改）：**
  - 使用 `Direct3D11CaptureFramePool` 时，像素格式必须强制指定为 `DirectXPixelFormat.R16G16B16A16Float`
  - 获取帧后，将其转换为 `ID3D11Texture2D`，传递给 GraphicsEngine 进行 Shader 渲染

### 3. OverlayUI（UI 覆盖层）

- **职责：** 提供用户交互的透明全屏窗口。
- **结构：**
  - 底层：充满全屏的 `SwapChainPanel`，用于硬件级高保真渲染捕获到的 HDR 纹理
  - 顶层：`Canvas`，用于绘制用户的鼠标拖拽状态、裁剪框（Mask）和工具栏操作

---

## ⚠️ AI 避坑指南（Critical Constraints）

编写实现代码时必须时刻注意以下问题。

### 1. COM 对象与非托管内存泄漏

WGC 和 Vortice.Windows 涉及大量底层 COM 接口，C# 的 GC 无法自动管理这些图形资源。

**必须**实现严格的 `IDisposable` 模式，确保每一个 `Texture2D`、`FramePool`、`CaptureSession` 都在重新捕获或关闭时被显式 `Dispose()`。

### 2. 线程同步

`Windows.Graphics.Capture` 的帧到达事件（`FrameArrived`）在后台线程触发。当获取到纹理并要求 UI 层的 `SwapChainPanel` 更新时，**必须**使用 `DispatcherQueue` 调度回 UI 线程，否则会引发线程崩溃。

### 3. 透明窗口问题

WinUI 3 窗口默认不透明。需要通过 Win32 API（`SetWindowLong`、`WS_EX_LAYERED`、`WS_EX_TRANSPARENT`）或 AppWindow 的 `Presenter` 设置，实现全屏无边框且能接收特定区域鼠标事件的覆盖层。

---

## 执行计划（分步实现逻辑）

> 2026-05-07 路线重整：当前执行来源以 `_bmad-output/planning-artifacts/epics.md` 为准。下面 Phase 1-4 保留为技术演进背景，不再作为 MVP 完成判定清单。

按以下阶段逐步推进，每一阶段需等待确认后再进入下一步。

### Phase 1 · 基础设施搭建
初始化 .NET 10 LTS + WinUI 3 项目（目标 TFM：`net10.0-windows10.0.19041.0`），引入 Vortice 依赖，编写 `Direct3D 11 Device` 初始化的单例类。

### Phase 2 · 捕获模块实现
编写 `Windows.Graphics.Capture` 辅助类，实现全屏 FP16 帧的捕获并输出 `ID3D11Texture2D`。

### Phase 3 · 渲染与桥接（最难点）
创建 `SwapChainPanel`，配置 HDR 交换链，并通过 COM 接口挂载到 UI 上，将 Phase 2 捕获的纹理画上去。

### Phase 4 · UI 交互与裁剪
在 `SwapChainPanel` 上方叠加 Canvas，实现鼠标按下、拖动、松开的区域裁剪逻辑。

---

## 当前状态

- [x] Harness 文档落盘
- [x] Phase 1：基础设施搭建
- [x] Phase 2：捕获模块
- [x] Phase 3：渲染与桥接
- [~] Phase 4：UI 交互与裁剪（基础 overlay/crop 已建立，MVP 仍需 direct monitor capture 与 release-to-copy 收尾）

## 2026-05-08 v0 MVP 范围扩展

Lumiere 当前路线已从 6 个 canonical epic 扩展为 10 个，新增了 v0 MVP 设计参考中的功能：主面板 UI 重构、全屏截图模式、设置面板、托盘上下文菜单。完整需求与故事以 `_bmad-output/planning-artifacts/prd.md`、`_bmad-output/planning-artifacts/epics.md`、`_bmad-output/planning-artifacts/architecture.md` 和 `_bmad-output/planning-artifacts/ux-design-specification.md` 为准。

- Epic 1-7 完成表示 MVP 功能实现完成。
- Epic 8 完成表示 MVP 输出和验证完成。
- Epic 9 完成表示 MVP 已通过完成门禁与验证，可以进入安装包流程。
- Epic 10 完成表示可安装的 1.0 发布完成。

MVP 的默认截图体验包括：
- **全屏截图**：点击 Full Screen 按钮，直接截取整个屏幕并复制到剪贴板。
- **区域截图**：点击 Region 按钮，进入全屏 overlay，拖拽区域并松手即截图/复制。
- **设置面板**：配置快捷键、HDR 设置、输出目标、保存路径。
- **托盘菜单**：从系统托盘快速访问截图、打开、设置、退出。

`GraphicsCapturePicker` 仅保留为 fallback/debug 或未来显式选择路径。

MVP 设计参考已纳入实现规划：

- `harness/design/v0-mvp-reference/`

当前 durable 状态：

- Epic 1：HDR Preview Foundation 已完成。
- Epic 2：Direct Capture Session Lifecycle 已完成。
- Epic 3：Release-to-Copy Overlay Workflow 已完成。
- Epic 4：Main Panel UI Refactoring 待实现。
- Epic 5：Full Screen Capture Mode 待实现。
- Epic 6：Settings Panel 待实现。
- Epic 7：Tray Context Menu 待实现。
- Epic 8：MVP Output, Status, and Validation 待实现。
- Epic 9：MVP Completion Gate 待实现。
- Epic 10：Installer and 1.0 Release 待实现。
- Epic 6：Installer and 1.0 Release 待实现。
