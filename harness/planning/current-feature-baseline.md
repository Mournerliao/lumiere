# Lumiere 当前功能基线

> 来源：`harness/design/prototype/v0-public-fidelity-reference/`  
> 用途：为后续 story、实现拆分、验收检查提供稳定参考。  
> 边界：这是从 v0 UX reference 提炼的当前功能基线，不是 React、Tailwind、shadcn 或 Web 技术实现规格。

---

## 范围原则

当前基线聚焦低打扰的截图闭环：从主窗口、快捷键或托盘进入捕获，完成全屏或区域截图，再按设置输出到剪贴板、文件夹或二者。

实现时必须将原型中的布局意图、文案层级、信息架构翻译为 WinUI 3 / Fluent 原生体验。HDR、导出格式、显示能力与截图正确性仍以 Lumiere 的 FP16/scRGB 原生管线和 Windows 真机验证为准。

功能取舍按 **保真度 -> 流程速度 -> 可信状态反馈** 排序：

- 保真度决定产品差异：任何入口、设置或输出能力都不得绕开 HDR 不变量，或把未经验证的 SDR/导出语义伪装为 HDR 成功。
- 流程速度决定当前基线可用性：主窗口、快捷键、托盘与区域释放即提交都服务于一次截图闭环，不引入默认选取器、图库、重标注覆盖层或扩展导出向导。
- 可信状态反馈决定用户信任：HDR Ready、Enable HDR、HDR unavailable 等状态必须来自真实系统/显示能力映射，并用验证语言表达边界。

---

## 功能清单

| ID | 功能 | 当前基线要求 | 原型依据 | 落地注意事项 |
| --- | --- | --- | --- | --- |
| BASE-01 | 主窗口品牌与设置入口 | 主窗口展示 Lumiere 标识；提供进入设置的明确入口。 | `main-panel.tsx` header 中的 Lumiere 标识与 settings 按钮。 | 使用 WinUI 原生窗口与按钮模式，不复制 Web 图标按钮实现。 |
| BASE-02 | 全屏截图入口 | 主窗口提供全屏截图主操作；显示当前快捷键；捕获中禁用重复触发并显示进行中状态。 | `CaptureButton` 的 `mode="full"`、`Capturing...` 和 `disabled` 状态。 | 入口触发直接捕获目标显示器，不以系统选取器作为默认第一步。 |
| BASE-03 | 区域截图入口 | 主窗口提供区域截图操作；显示当前快捷键；捕获中禁用重复触发并显示进行中状态。 | `CaptureButton` 的 `mode="region"`。 | 区域选择需支持取消、有效区域提交、无效拖拽处理与多显示器边界。 |
| BASE-04 | 区域释放即提交 | 用户完成有效区域选择后，直接进入捕获与输出反馈。 | `design/index.md` 中的 release-to-copy 意图；原型通过捕获状态模拟短暂反馈。 | 覆盖层交互来自原生 WinUI/Win32/WGC 实现，需明确最小有效区域与取消语义。 |
| BASE-05 | HDR 状态摘要 | 主窗口底部展示当前 HDR 状态摘要。 | `main-panel.tsx` footer 使用 `HDR_STATUS_UI`。 | 文案不得承诺未验证的 HDR 正确性；状态需来自真实系统与显示能力映射。 |
| BASE-06 | 最小化意图 | 主窗口提供最小化到后台/托盘的明确路径。 | `main-panel.tsx` footer 的 `Minimize` 文案。 | 最小化后托盘菜单必须保留核心截图入口。 |
| BASE-07 | 托盘菜单状态头 | 托盘菜单展示 Lumiere 标识与 HDR 状态摘要。 | `tray-context-menu.tsx` header。 | 使用 Windows 托盘/上下文菜单惯例，保持紧凑、命令导向。 |
| BASE-08 | 托盘截图命令 | 托盘菜单提供全屏截图与区域截图命令，并展示对应快捷键。 | `captureItems` 中的 `full` / `region`。 | 托盘入口与主窗口入口共享捕获状态，避免并发捕获。 |
| BASE-09 | 托盘窗口命令 | 托盘菜单提供打开主窗口、打开设置、退出应用。 | `bottomItems` 的 `Open Lumiere`、`Settings`、`Quit`。 | 退出需确定性释放 WGC、D3D、DXGI、托盘与窗口资源。 |
| BASE-10 | 快捷键设置 | 设置页允许配置全屏截图与区域截图快捷键。 | `SettingsPanel` Shortcuts 区域与 `ShortcutInput`。 | 需要处理冲突、无效组合、注册失败与恢复默认的后续策略。 |
| BASE-11 | HDR 提示设置 | 设置页提供 HDR 不可用/未开启时提示的开关。 | `HDR alerts` toggle 与 `hdrWarnings`。 | 提示受该开关约束；错误与能力文案需符合验证语言。 |
| BASE-12 | 导出/色彩格式选项 | 设置页展示 HDR10、P3、sRGB 等导出格式选项。 | `colorFormatOptions`：`HDR10` / `P3` / `sRGB`。 | 这些是 UX 占位与讨论起点；实际是否开放、多档如何命名，必须以编码器、色彩元数据和 Windows HDR 验证为准。 |
| BASE-13 | 输出目标 | 设置页允许选择剪贴板、文件夹或二者。 | `SegmentedOutput`：`Clipboard` / `Folder` / `Both`。 | 输出管线需按同一设置驱动剪贴板与文件写入，避免 UI 状态和实际输出分离。 |
| BASE-14 | 保存路径 | 当输出目标包含文件夹时，显示并允许选择保存路径。 | `settings.outputTarget === "folder" || "both"` 时展示 Save Path。 | 使用原生文件夹选择器；处理路径不可写、路径不存在与权限错误。 |
| BASE-15 | 捕获后打开 | 设置页提供捕获完成后自动打开输出的开关。 | `Open after capture` toggle 与 `autoOpen`。 | 仅对有可打开产物的输出路径生效；剪贴板-only 场景需定义无操作或反馈。 |
| BASE-16 | 文件名时间戳 | 设置页提供时间戳命名偏好。 | `Timestamp` toggle 与 `includeMetadata`。 | 需落到稳定文件命名规则；避免覆盖已有文件。 |
| BASE-17 | 剪贴板图片复制 | 当输出目标包含剪贴板时，展示复制为图片选项。 | Clipboard section 的 `Copy as Image` toggle。 | 剪贴板格式需和 HDR/SDR 导出语义分开描述，不暗示未验证色彩保真。 |
| BASE-18 | 关于信息 | 设置页展示应用名称、版本与简短说明。 | About 区域：`Lumiere`、`v0.1.0`、HDR-first 说明。 | 版本号应来自构建或应用元数据，避免手写漂移。 |
| BASE-19 | 共享设置状态 | 主窗口、托盘和设置页共享快捷键、输出偏好与 HDR 状态。 | `AppShell` 中的 `settings`、`hdrStatus`、`capturingMode`。 | 生产实现需持久化设置，并保证 UI、快捷键注册和输出管线读取同一来源。 |
| BASE-20 | 捕获中反馈 | 捕获触发后提供短暂、低打扰的进行中状态，避免重复点击。 | `capturingMode` 与 800ms 模拟重置。 | 真实实现应由捕获生命周期驱动状态，不用固定延时表示完成。 |

---

## HDR 状态映射

| 原型状态 | 原型文案 | 产品语义 | 验证要求 |
| --- | --- | --- | --- |
| `ready` | HDR Ready | 当前系统与目标显示器满足 HDR 捕获前置条件。 | 需要 Windows 真机验证后才能称为就绪。 |
| `available` | Enable HDR | 检测到 HDR 能力，但 Windows HDR 当前未开启或不可用于目标。 | 文案应提示用户可采取的动作，不宣称已支持 HDR 捕获。 |
| `unavailable` | HDR unavailable | 未检测到可用 HDR 显示设备或能力不足。 | 需要明确是否仍允许非 HDR 输出，以及该路径是否属于当前 story 范围。 |

---

## 当前基线之外

除非后续 story 单独拉回范围，下列内容不作为当前默认功能：

- 引导页、权限教学或营销式 onboarding。
- 截图图库、历史记录、对比浏览或媒体管理。
- 重标注型覆盖层，如复杂标注、马赛克、箭头、撤销重做工具栏。
- 扩展导出工作流，如批量导出、模板化命名、多格式高级配置。
- Web UI、Electron、Tauri、React、Tailwind、Radix、shadcn 等生产依赖。

---

## 后续引用方式

Story 和实现任务可以引用上表 ID，例如：

- `BASE-02`：主窗口全屏截图入口。
- `BASE-13` + `BASE-14`：输出到文件夹及保存路径选择。
- `BASE-05` + HDR 状态映射：主窗口 HDR 状态摘要。

当功能涉及 WGC、DXGI、D3D11、HDR 显示行为、多显示器或色彩导出时，验收记录必须标注验证级别：Mac edit、Windows CI 或 Windows manual validation。
