# Lumiere 项目计划

Updated: 2026-06-22

## 当前目标

Lumiere 的当前唯一公开发布口径是 `Public perfect-HDR-fidelity`。

这不是一句营销话术，而是一个必须被验证支撑的发布目标。当前已实现的捕获与工作流能力构成了产品基线，但公开发布还需要补齐目标显示器级 HDR 判断、输出保真契约、目标应用兼容性证据，以及 Windows 真机验证记录。

## 产品方向

Lumiere 是一个原生 Windows HDR 截图工具。它的核心价值不是“功能很多”，而是三件事：

1. HDR 内容捕获与预览尽量可信。
2. 截图流程足够快，不打断用户原本工作。
3. UI 对能力边界说实话，不把未验证能力包装成“已支持”。

当前设计参考原型位于 `harness/design/prototype/v0-public-fidelity-reference/`。它是可运行的 React 设计原型，用来表达信息架构、布局密度和交互意图，不是生产实现。

## 当前基线

当前基线对应 Epic 1-9，已经形成一个可工作的 capture/workflow foundation：

- 主窗口提供全屏截图、区域截图、HDR 状态摘要和设置入口。
- 托盘提供一致的截图入口、窗口入口和退出入口。
- 区域截图采用直接进入覆盖层、拖拽选区、释放即完成的默认路径。
- 输出支持剪贴板、文件夹、或两者同时输出。
- 设置承载快捷键、输出目标、保存路径、时间戳、HDR 提示等基础偏好。

当前基线的详细拆分见 `current-feature-baseline.md`。

## 不属于当前基线的内容

以下内容默认不算当前基线，除非后续故事明确拉回范围：

- onboarding、图库、历史记录、分享型工作流
- 注释型重覆盖层和复杂编辑工具
- 未经验证的 HDR10 / P3 / HDR-preserved 输出宣称
- 任何把 Web 技术栈迁入生产实现的方案

## Public perfect-HDR-fidelity 仍需完成的工作

公开发布要在当前基线之上补齐更严格的能力与证据：

1. 目标感知的 HDR 状态判断  
   不能再只看全局或默认显示器，必须和实际捕获目标对齐。
2. 输出保真契约  
   必须明确区分 data-preserving capture、visual-match output、SDR-compatible output、HDR-preserved output。
3. 至少一个受支持的 HDR-preserved 输出路径  
   需要格式、转换、元数据、查看器假设、Windows 手工验证全链路成立。
4. 目标应用兼容性矩阵  
   必须把“文件写出成功”“视觉匹配”“HDR 保留”分开记录。
5. 多显示器、DPI、长周期稳定性证据  
   这是公开发布可信度的一部分，不是事后补注。
6. 发布文案复核  
   UI 与 release copy 只能宣称已经通过验证的能力。

这些门槛的 live 入口在 `../validation/release-validation-checklist.md`。

## 设计与实现边界

- 设计来源：`../design/index.md`
- 设计补充：`../design/perfect-hdr-fidelity-extension.md`
- 当前原型：`../design/prototype/v0-public-fidelity-reference/`
- 验证入口：`../validation/index.md`
- 架构决策：`../architecture/adr/`

实现必须继续遵守原生边界：

- WinUI 3 / Windows App SDK 负责应用壳层与界面
- WGC / DXGI / D3D11 / Vortice 负责捕获与渲染链路
- 平台 API 留在各自模块边界内，不向 UI 层泄漏

## 验证规则

- Windows CI 只能证明代码与自动化检查通过，不能替代硬件行为结论。
- 涉及 WGC、DXGI、HDR 显示器、托盘、快捷键、多显示器、DPI、剪贴板、文件系统的能力，必须有 Windows 手工验证记录。
- `NOT RUN` 不算证据。
- `PASS with limitation` 只有在限制被清楚写进文案时才可用于发布判断。

## 当前状态

- 已完成：文档入口统一到 `harness/`，设计原型位置明确，历史验证资料已归档。
- 已完成：当前基线文档、设计文档、验证入口已统一围绕 `Public perfect-HDR-fidelity`。
- 未完成：Public perfect-HDR-fidelity 的核心验证门槛尚未全部闭环。
- 未完成：Windows 真机证据、目标应用兼容性、HDR-preserved 输出路径、长周期稳定性记录仍需补齐。

## 相关阅读

- `current-feature-baseline.md`
- `../README.md`
- `../design/index.md`
- `../validation/index.md`
- `../validation/release-validation-checklist.md`
