# Lumiere — HDR 原生 Windows 截图工具

> 项目规划文档 / AI 协作上下文 Harness
> 最后更新：2026-05-09
> 设计权威来源：`harness/design/v0-mvp-reference/`

---

## 项目目标

开发一款原生 Windows 桌面截图工具。核心痛点是 **完美支持 HDR 显示器截图**。

必须绕过 Windows DWM 的默认色调映射，获取真实的 16 位浮点（FP16）纹理，并使用支持 scRGB 色彩空间的 DirectX 交换链在前端高保真渲染，确保在高亮度硬件上颜色不发白、不失真。

---

## 技术栈

| 层级 | 技术 |
| --- | --- |
| 运行时 | .NET 10 LTS（`net10.0-windows10.0.19041.0`） |
| UI 框架 | WinUI 3 (Windows App SDK) |
| 图形 API | `Vortice.Windows`（Direct3D 11 + DXGI） |
| 系统桥接 | `Microsoft.Windows.CsWinRT` |
| 屏幕捕获 | `Windows.Graphics.Capture` (WGC) |
| 架构 | x64 only，不使用 Any CPU |

---

## MVP 设计规范（以设计稿为准）

### 设计系统

- **色彩方案**：Dark-first，使用 oklch 色彩空间
- **主色调**：`oklch(0.72 0.13 220)` — 冷天蓝色，象征 HDR/显示器精度
- **状态色**：
  - Ready（绿）：`oklch(0.70 0.16 155)`
  - Warning（黄）：`oklch(0.78 0.16 70)`
  - Error（红）：`oklch(0.65 0.22 27)`
- **背景**：`oklch(0.13 0.005 240)`
- **卡片**：`oklch(0.17 0.006 240)`
- **圆角基准**：`0.5rem`

### 数据模型

```typescript
type HdrStatus = "ready" | "available" | "unavailable"
type CaptureMode = "full" | "region"
type OutputTarget = "clipboard" | "folder" | "both"
type ColorFormat = "srgb" | "wide" | "hdr10"

interface Settings {
  shortcuts: Record<CaptureMode, string>   // e.g. { full: "Shift+S", region: "Shift+A" }
  outputTarget: OutputTarget
  colorFormat: ColorFormat
  hdrWarnings: boolean
  autoOpen: boolean        // 截图后自动打开
  includeMetadata: boolean // 包含时间戳
  copyImage: boolean       // 复制为图片
  savePath: string
}
```

---

## MVP UI 表面（3 个界面）

设计稿定义了 3 个独立 UI 表面，WinUI 3 实现时必须完整还原其布局、密度、文案和交互层级。

### 1. Main Panel（主面板）

> 参考：`components/lumiere/main-panel.tsx`

**布局**（自上而下）：

| 区域 | 内容 |
| --- | --- |
| Header | Lumiere 图标（Layers）+ 应用名 + 设置按钮（Settings 齿轮） |
| Body | 两个截图按钮，垂直排列 |
| Footer | HDR 状态指示器（图标 + 圆点 + 文字）+ "Minimize" 文字 |

**截图按钮规格**：

| 按钮 | 样式 | 图标 | 标签 | 快捷键 |
| --- | --- | --- | --- | --- |
| Full Screen | Primary（`bg-primary/10`, `border-primary/25`） | Camera | "Full Screen" | Shift+S |
| Region | Secondary（`bg-secondary/45`, `border-border`） | Layers | "Region" | Shift+A |

**交互行为**：
- 点击按钮触发截图，按钮显示 "Capturing..." 并禁用所有按钮
- 按钮有 `active:scale-[0.99]` 缩放反馈
- Primary 按钮 hover 时边框和背景变深
- 截图中按钮有 `animate-ping` 脉冲动画边框

**HDR 状态指示器**：

| 状态 | 图标 | 圆点颜色 | 文案 |
| --- | --- | --- | --- |
| ready | CheckCircle2 | 绿色 | "HDR Ready" |
| available | Monitor | 黄色 | "Enable HDR" |
| unavailable | AlertCircle | 红色 | "HDR unavailable" |

**宽度**：360px

### 2. Settings Panel（设置面板）

> 参考：`components/lumiere/settings-panel.tsx`

**布局**：

| 区域 | 内容 |
| --- | --- |
| Header | 返回箭头 + "Settings" 标题 |
| Body | 可滚动设置区域，分为 5 个 section |

**设置分区**：

#### Shortcuts 区
- Section 图标：Keyboard
- Full Screen 快捷键输入
- Region 快捷键输入
- 输入方式：点击显示编辑框，监听键盘事件组合键（Ctrl/Shift/Alt + Key）

#### HDR 区
- Section 图标：Monitor
- "HDR alerts" 开关（描述："When HDR is unavailable"）
- "Export" 色彩格式选择器：三段式 Segmented Control
  - HDR10 / P3 / sRGB

#### Output 区
- Section 图标：FolderOpen
- "Destination" 分段控件：Clipboard / Folder / Both
- "Save Path"（仅当 destination 为 Folder 或 Both 时显示）
  - 路径显示框 + "Browse" 按钮
- "Open after capture" 开关
- "Timestamp" 开关

#### Clipboard 区
- Section 图标：Clipboard
- "Copy as Image" 开关（仅当 destination 为 Clipboard 或 Both 时显示）

#### About 区
- Section 图标：Info
- 应用名 + 版本号（v0.1.0）
- 描述文字

**宽度**：360px，**高度**：640px

### 3. Tray Context Menu（托盘上下文菜单）

> 参考：`components/lumiere/tray-context-menu.tsx`

**布局**（自上而下）：

| 区域 | 内容 |
| --- | --- |
| Header | Lumiere 图标（"L"）+ 应用名 + HDR 状态（图标 + 文案） |
| Capture 区 | Full Screen 行（图标 + 标签 + 快捷键）、Region 行（图标 + 标签 + 快捷键） |
| 分割线 | 水平分割线 |
| 底部区 | Open Lumiere、Settings、Quit（红色 destructive 样式） |

**菜单项规格**：

| 项 | 图标 | 快捷键 | 特殊样式 |
| --- | --- | --- | --- |
| Full Screen | Camera | Shift+S | 截图中显示 "Capturing..." |
| Region | Layers | Shift+A | 截图中显示 "Capturing..." |
| Open Lumiere | AppWindow | — | — |
| Settings | Settings | — | — |
| Quit | Power | — | destructive 红色 |

**交互**：
- 截图中对应项高亮（`bg-primary/12`），图标变 primary 色
- Quit 项 hover 时背景为 `destructive/10`

**宽度**：224px（w-56）

---

## HDR 核心不变量（绝对不可修改）

| 项 | 值 |
| --- | --- |
| SwapChain 格式 | `DXGI_FORMAT_R16G16B16A16_FLOAT` |
| SwapChain 色彩空间 | `DXGI_COLOR_SPACE_RGB_FULL_G10_NONE_P709`（scRGB 线性） |
| WGC 帧池像素格式 | `DirectXPixelFormat.R16G16B16A16Float` |
| WinUI 互操作 | `ISwapChainPanelNative` |

---

## 核心架构（模块化）

### 模块边界

| 项目 | 职责 |
| --- | --- |
| `Lumiere.App` | WinUI 启动、窗口组合、连接各模块 |
| `Lumiere.Graphics` | D3D11 设备、DXGI 交换链、HDR 常量、呈现 |
| `Lumiere.Capture` | WGC 帧池、捕获会话生命周期、帧释放 |
| `Lumiere.Infrastructure` | COM/WinRT 互操作、原生编组、Win32 桥接 |
| `Lumiere.Overlay` | 全屏覆盖层、裁剪 UI、鼠标/键盘交互 |
| `Lumiere.Settings` | 本地偏好设置 |

### 数据流

```
Direct monitor target (HMONITOR → GraphicsCaptureItem)
  → CaptureService.StartCapture()
  → Direct3D11CaptureFramePool (FP16, 后台线程)
  → HandleFrameArrived: 通过 Direct3D11SurfaceInterop 互操作纹理
  → MainWindow.OnCapturedFrameArrived (stale check via previewGeneration)
  → PreviewFramePresenter.PresentFrame() (GPU CopyFrame + Present)
  → SwapChainPanel (UI 线程, via DispatcherQueue.TryEnqueue)
  → Overlay 裁剪交互
  → Release-to-copy → 剪贴板 / 文件输出
```

---

## 截图模式（以设计稿为准）

### Full Screen 截图
- **触发**：主面板 Full Screen 按钮 / 托盘菜单 Full Screen / 全局快捷键
- **行为**：直接截取整个屏幕 → 复制到剪贴板（或按设置输出）
- **无需 picker**：直接使用 HMONITOR 目标

### Region 截图
- **触发**：主面板 Region 按钮 / 托盘菜单 Region / 全局快捷键
- **行为**：进入全屏 overlay → 拖拽选择区域 → 松手即截图/复制
- **overlay 结构**：底层 SwapChainPanel（HDR 渲染）+ 顶层 Canvas（裁剪框/遮罩）

---

## 设置持久化

设置项对应设计稿 `PrototypeSettings`，使用本地偏好存储：

| 设置项 | 类型 | 默认值 | 说明 |
| --- | --- | --- | --- |
| `shortcuts.full` | string | "Shift+S" | 全屏截图快捷键 |
| `shortcuts.region` | string | "Shift+A" | 区域截图快捷键 |
| `outputTarget` | enum | "clipboard" | 输出目标 |
| `colorFormat` | enum | "hdr10" | 色彩格式导出 |
| `hdrWarnings` | bool | true | HDR 不可用时警告 |
| `autoOpen` | bool | false | 截图后自动打开 |
| `includeMetadata` | bool | true | 包含时间戳 |
| `copyImage` | bool | true | 复制为图片 |
| `savePath` | string | "C:\Users\...\Lumiere" | 保存路径 |

---

## AI 避坑指南

### COM 对象与非托管内存
WGC 和 Vortice 涉及大量 COM 接口，GC 无法自动管理。必须实现严格 `IDisposable` 模式。

### 线程同步
WGC 的 `FrameArrived` 在后台线程触发。更新 `SwapChainPanel` 必须通过 `DispatcherQueue` 调度回 UI 线程。

### 透明窗口
WinUI 3 窗口默认不透明。需要 Win32 API（`SetWindowLong`、`WS_EX_LAYERED`、`WS_EX_TRANSPARENT`）或 AppWindow Presenter 实现全屏无边框覆盖层。

### 模块边界
- 新 WGC / DXGI / COM / Win32 / WinUI 调用必须先放入对应边界项目
- 不要在 UI 或测试代码中散布平台 API

---

## 实现对齐表

将设计稿 UI 表面映射到实现 Epic：

| 设计稿表面 | 实现 Epic | 依赖 |
| --- | --- | --- |
| Main Panel | Epic 4: Main Panel UI Refactoring | Epic 1-3 |
| Full Screen 截图 | Epic 5: Full Screen Capture Mode | Epic 2 |
| Settings Panel | Epic 6: Settings Panel | Epic 4 |
| Tray Context Menu | Epic 7: Tray Context Menu | Epic 4 |
| 全色彩管理导出 | Epic 8: MVP Output & Validation | Epic 1-7 |
| 全局快捷键 | Epic 7/8（跨功能） | — |
| 完成门禁 | Epic 9: MVP Completion Gate | Epic 8 |
| 安装包 | Epic 10: Installer and 1.0 Release | Epic 9 |

---

## 当前状态

- [x] Harness 文档落盘
- [x] Epic 1: HDR Preview Foundation
- [x] Epic 2: Direct Capture Session Lifecycle
- [x] Epic 3: Release-to-Copy Overlay Workflow
- [ ] Epic 4: Main Panel UI Refactoring
- [ ] Epic 5: Full Screen Capture Mode
- [ ] Epic 6: Settings Panel
- [ ] Epic 7: Tray Context Menu
- [ ] Epic 8: MVP Output, Status, and Validation
- [ ] Epic 9: MVP Completion Gate
- [ ] Epic 10: Installer and 1.0 Release

---

## 验证命令

```bash
dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false
dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false
dotnet format Lumiere.sln --verify-no-changes --verbosity minimal
```

## 提交规范

```
feat:  用户可见能力
fix:   缺陷修复
docs:  仅文档
chore: 脚手架/构建/仓库维护
test:  仅测试
```
