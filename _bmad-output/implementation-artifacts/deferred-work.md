## Deferred from: code review of 2-1-start-capture-and-select-a-display-or-window-target (2026-05-04)

- D1-HIGH: `CreateForTest` 使用 `null!` 传递 `GraphicsCaptureItem` ——已知变通方案（`CreateForTesting` 在目标 SDK 不可用），故事文档已记录 [src/Lumiere.Capture/CaptureTarget.cs:27]
- D2-HIGH: 窗口关闭期间 `deviceResources` use-after-dispose——选择器显示中关闭窗口时有已有竞态 [src/Lumiere.App/MainWindow.xaml.cs:293]
- D3-MEDIUM: `GraphicsCaptureSession.IsSupported()` 是不可测试的静态依赖——超出本故事范围 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:17]
- D4-MEDIUM: `CaptureTarget` 持有 `IDisposable`（`GraphicsCaptureItem`）但自身未实现 `IDisposable`——已有问题 [src/Lumiere.Capture/CaptureTarget.cs:8]
- D5-MEDIUM: 取消选择时显示 "Initializing preview" 而非空闲状态——已有 UX 问题 [src/Lumiere.Capture/CaptureTargetSelectionService.cs:28]
- D6-MEDIUM: `CaptureTargetKind` 在生产代码中始终为 `Unknown`——规格明确说本故事不实现分类 [src/Lumiere.Capture/CaptureTarget.cs:47]
- D7-MEDIUM: 捕获尺寸无上限校验——已有问题 [src/Lumiere.Capture/CaptureTarget.cs:33]
- D8-LOW: `previewGeneration` 在 UI dispatcher 回调中未同步读取——已有模式 [src/Lumiere.App/MainWindow.xaml.cs:138]
