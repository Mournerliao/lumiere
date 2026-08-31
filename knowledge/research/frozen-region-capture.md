# Frozen Region Capture：先冻结、后选区、同帧交付

- 调研日期：2026-08-31
- 范围：macOS ScreenCaptureKit、Windows.Graphics.Capture/D3D11、Electron 本地预览边界，以及主流开源截图工具的实现证据
- 目标：判断 Lumiere 如何在快捷键触发后冻结桌面，再让用户从同一时刻的画面中选区，且不把 raw HDR frame 送进 Electron

## 结论

Lumiere 应采用 **capture-before-overlay**：先由 native Host 捕获并持有一张不可变的完整目标显示器帧，确认冻结帧可用后才显示 Region Overlay。Overlay 展示的是该帧派生的 sRGB 预览；用户确认后，Host 必须从同一张 native 冻结帧裁剪，不能再次调用系统截图 API。Apple 将 `SCScreenshotManager` 定义为从指定 filter/configuration 捕获单帧的 API；Windows.Graphics.Capture 则通过 session/frame pool 异步送达帧，因此 Windows 的可兑现语义应是“触发命令后收到的第一张完整可用帧”，不能宣称与物理按键时间戳严格相同。[Apple `captureImage`](https://developer.apple.com/documentation/screencapturekit/scscreenshotmanager/captureimage%28contentfilter%3Aconfiguration%3Acompletionhandler%3A%29) · [Microsoft screen capture](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture) · [WGC `StartCapture`](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.graphicscapturesession.startcapture)

推荐的语义边界是：

```text
Idle
  -> Preparing (resolve target + native acquisition)
  -> Frozen (immutable native frame + derived SDR preview token)
  -> Selecting (overlay visible)
  -> Completing | Cancelling | Expired
  -> Disposed
```

这不是“给透明 Overlay 加一张背景图”的 UI 修补，而是一个短生命周期的 native **frozen capture session**。session 是唯一事实来源，并同时拥有 target snapshot、冻结帧、预览 token、几何换算参数和清理责任。

## 1. Capture-before-overlay，而不是 overlay-before-capture

### 为什么必须先捕获

如果先显示 Overlay 再捕获，至少会产生两个问题：Overlay 可能进入系统捕获结果；即使平台 filter 能排除本应用，窗口合成、显示与捕获的异步调度仍会引入不必要的竞态。Apple 的 filter 可以按 display 选择内容并排除应用/窗口；Lumiere 应使用 `SCContentFilter(display:excludingApplications:exceptingWindows:)` 排除自身进程，但这只是防御，不能取代正确时序。[Apple `SCContentFilter` initializer](https://developer.apple.com/documentation/screencapturekit/sccontentfilter/init%28display%3Aexcludingapplications%3Aexceptingwindows%3A%29) · [Apple ScreenCaptureKit sample：content filter](https://developer.apple.com/documentation/screencapturekit/capturing-screen-content-in-macos)

正确时序是：

1. 快捷键/菜单命令进入 `Preparing`，此时 Overlay 尚未创建或至少不可见。
2. native Host 获取一张完整目标显示器帧，并将其固化到 Host 自有资源。
3. Host 生成 renderer 可显示的 SDR 编码预览，返回 opaque session/token 与几何元数据。
4. main 验证 session 后创建 Overlay；Overlay 首帧就显示冻结背景，不短暂露出仍在运动的真实桌面。
5. 确认选区时只向原 session 提交逻辑坐标；取消时释放 session。

主流开源工具也把“已抓取的桌面位图”当作选择界面的上下文，而不是在选区结束后再获取画面。Flameshot 的 `CaptureContext` 同时保存 `screenshot` 与 `origScreenshot`，并从上下文返回 selected area；ksnip 的官方 changelog 明确记录了 “Freeze image while selecting rectangular area”。这些不是平台契约，但可作为成熟截图 UX 的实现旁证。[Flameshot `CaptureContext`](https://flameshot.org/docs/dev/flameshot/capturecontext_8h_source/) · [ksnip changelog](https://github.com/ksnip/ksnip/blob/master/CHANGELOG.md)

### 不推荐的方案

- **透明 Overlay + 松手后 capture**：用户看到的时刻与输出时刻不同，是当前问题本身。
- **先显示 Overlay，再依赖 exclude-self capture**：能降低 Overlay 入帧概率，但不能消除调度竞态。
- **Overlay 显示冻结预览，确认时重新 capture**：视觉上静止，输出却来自另一时刻，属于错误的“伪冻结”。
- **Electron `desktopCapturer` / Canvas / `NativeImage` 截图**：违反 Lumiere native HDR acquisition 与模块边界；也会形成第二条色彩和资源生命周期路径。

## 2. 如何保证预览与输出来自同一帧

需要区分两个强度不同的保证：

1. **时间同帧**：预览与输出都从同一个 immutable native frame 派生。这是必须满足的产品契约。
2. **像素同源**：预览缩放前的 sRGB 像素与最终文件完全来自同一次 Visual Match 转换。这是更强、也更容易测试的契约。

Lumiere 当前推荐实现是保留一张 authoritative HDR/SDR native frame，预览从它 tone-map/downscale，确认时从它裁剪并走正式 Visual Match。这样时间严格同帧，并继续满足“保留 native HDR acquisition 直到正式 Visual Match 完成”的架构边界；但如果 tone mapping 会根据输入区域统计内容，`全帧 -> preview` 与 `crop -> output` 的局部亮度可能有轻微差异。renderer preview 始终只是同帧的视觉引导，不是 artifact truth。

如果产品未来要求“预览缩放前与成品逐像素同源”，则需要在冻结阶段对完整帧只做一次正式 Visual Match，保留 full-resolution immutable sRGB master，再让 preview 与 region artifact 都从它派生。这会改变当前 crop-then-convert 的处理顺序并增加完整 5K 帧的转换/驻留成本，应作为明确的色彩管线决策验证，不能为了冻结体验顺手引入。

### macOS

`SCScreenshotManager.captureImage` 一次返回一张 `CGImage`；`captureSampleBuffer` 则返回 `CMSampleBuffer`。macOS 15 的 HDR screenshot preset 可直接用于单帧 HDR 捕获，Apple 的 WWDC24 示例使用 `.captureHDRScreenshotLocalDisplay`，随后调用同一个 `SCScreenshotManager` screenshot API。[Apple `SCScreenshotManager`](https://developer.apple.com/documentation/screencapturekit/scscreenshotmanager) · [WWDC24: Capture HDR content with ScreenCaptureKit](https://developer.apple.com/videos/play/wwdc2024/10088/)

选择完成后应从原始冻结图像裁剪，不能以 `sourceRect` 再调用一次 `SCScreenshotManager`。Core Graphics 的 `CGImageCreateWithImageInRect` 从已有图像的子矩形创建图像，且子图保留对原图数据的引用，适合作为同帧裁剪边界。[Apple `CGImageCreateWithImageInRect`](https://developer.apple.com/documentation/coregraphics/1454683-cgimagecreatewithimageinrect)

macOS 26 新增的 screenshot API 还提供 `SCScreenshotConfiguration`、`captureScreenshot` 与 `SCScreenshotOutput`；`dynamicRange = .bothSDRAndHDR` 可在同一次 screenshot request 中得到 SDR/HDR 两个版本，是未来同时服务 Overlay preview 与 HDR source 的最干净分支。但这些 symbol 是 macOS 26 availability，Lumiere 当前 macOS 15 baseline 不应依赖它们。[Apple `SCScreenshotConfiguration`](https://developer.apple.com/documentation/screencapturekit/scscreenshotconfiguration) · [Apple `DynamicRange`](https://developer.apple.com/documentation/screencapturekit/scscreenshotconfiguration/dynamicrange-swift.enum) · [Apple `captureScreenshot`](https://developer.apple.com/documentation/screencapturekit/scscreenshotmanager/capturescreenshot%28contentfilter%3Aconfiguration%3Acompletionhandler%3A%29) · [Apple `SCScreenshotOutput`](https://developer.apple.com/documentation/screencapturekit/scscreenshotoutput)

### Windows

WGC 的 `Direct3D11CaptureFrame` 是 frame pool 借出的缓冲。Microsoft 明确要求及时 `Dispose` 将缓冲归还池，并且在 frame 归还后不得继续持有 frame 或其底层 surface 引用。因此收到第一张可用帧时，必须在其有效期内把 `ContentSize` 范围复制到应用自有 `ID3D11Texture2D`，然后立即释放 WGC frame；这个自有 texture 才是冻结 session 的 authoritative frame。[Microsoft：Acquire/Process capture frames](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture)

完整帧固化可用 `ID3D11DeviceContext::CopyResource`，确认选区后的 GPU 局部裁剪可用 `CopySubresourceRegion`。后者不做 stretch、blend 或 filter；logical selection 必须先按冻结 session 的 scale 换算、clamp 并 outward-align 到合法 texel box，越界参数不能交给 D3D11。[Microsoft `CopyResource`](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-copyresource) · [Microsoft `CopySubresourceRegion`](https://learn.microsoft.com/en-us/windows/win32/api/d3d11/nf-d3d11-id3d11devicecontext-copysubresourceregion)

Windows HD Color 捕获链应保留 `DXGI_FORMAT_R16G16B16A16_FLOAT`，直到 Lumiere 自己的 HDR -> sRGB Visual Match 转换完成；Microsoft 明确警告，HDR 内容若不使用 float capture pipeline，结果可能过度裁剪或发白。renderer 只接收它派生出的 SDR preview。[Microsoft screen capture：HDR content](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture)

## 3. Native 资源、会话生命周期与取消

### 统一状态与所有权

每个平台 Host 都只允许一个 active frozen-region session（至少 MVP 如此）。新建 session 前必须显式取消并清理旧 session，session ID 不复用。所有终止路径——确认成功、Esc、Overlay close/crash、Host disconnect、target topology change、超时、应用退出——进入同一个幂等 `disposeOnce`：

1. 将 session 标为 terminal，拒绝迟到的 commit/cancel。
2. 撤销 frame callback / event subscription，阻止新工作进入。
3. 停止并释放 capture session/frame pool。
4. 释放 native frozen frame、转换中间资源与编码器对象。
5. 删除内存或受控临时预览，撤销 preview token。
6. 清除 main 与 Overlay 的 session 映射。

Windows 官方 sample 的关闭路径采用撤销 `FrameArrived`、关闭 frame pool 和 capture session 的确定性顺序，并用原子状态让 Close 幂等；Lumiere 应沿用这一资源模型，而不是依赖 GC/finalizer。[Microsoft ScreenCaptureforHWND `SimpleCapture.cpp`](https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/blob/master/cpp/ScreenCaptureforHWND/ScreenCaptureforHWND/SimpleCapture.cpp) · [`SimpleCapture.h`](https://github.com/microsoft/Windows.UI.Composition-Win32-Samples/blob/master/cpp/ScreenCaptureforHWND/ScreenCaptureforHWND/SimpleCapture.h)

WGC frame pool 的 `Recreate` 会丢弃现有 frames；底层内容尺寸也可能小于 pool surface，此时多余区域是 undefined。因此冻结时应严格复制该 frame 的 `ContentSize`，复制完成即结束 WGC session，不让冻结资源依赖一个持续运行的 frame pool。[Microsoft screen capture：resize/device lost](https://learn.microsoft.com/en-us/windows/apps/develop/media-authoring-processing/screen-capture)

`SCScreenshotManager` 的公开单帧方法没有返回可取消 operation。因而 macOS 的 `Preparing` 取消应通过 session generation/token 忽略迟到 completion，并在 completion 到达后立即释放结果；不能声称底层 capture 已被系统取消。若使用 `SCStream` 作为兼容路径，则应调用 `stopCapture` 并移除 output。[Apple `captureImage`](https://developer.apple.com/documentation/screencapturekit/scscreenshotmanager/captureimage%28contentfilter%3Aconfiguration%3Acompletionhandler%3A%29) · [Apple `SCStream.stopCapture`](https://developer.apple.com/documentation/screencapturekit/scstream/stopcapture%28completionhandler%3A%29)

建议为 Frozen/Selecting 设置短超时（例如 60 秒，具体值由产品决定），以防 Overlay 丢失后长期保留 5K HDR texture。超时属于本地资源治理策略，不是平台 API 的要求。

## 4. 跨进程预览：传 capability，不传 raw HDR frame

renderer 只接收小型、可验证的 capability 元数据：

```ts
type FrozenRegionPrepared = {
  sessionId: string
  previewToken: string
  targetId: string
  logicalSize: { width: number; height: number }
  pixelSize: { width: number; height: number }
  scaleFactor: number
}
```

不要经 JSON Lines、Electron IPC 或 `contextBridge` 发送 raw HDR bytes、D3D texture、IOSurface 或 `CGImage`。Electron IPC 使用 Structured Clone；大像素数组会产生复制/序列化成本，DOM/Electron 特殊对象也不能作为普通 IPC 参数发送。[Electron `webContents.send`](https://www.electronjs.org/docs/latest/api/web-contents/#contentssendchannel-args) · [Electron `ipcRenderer`](https://www.electronjs.org/docs/latest/api/ipc-renderer)

符合现有 JSON Lines Host 边界的实际数据路径应是：Host 在受控临时目录写入 encoded SDR preview，并只把内部 preview descriptor（可含绝对路径）交给受信任的 main；main 建立 `opaque token -> validated path/session` 映射，renderer 只看到 token URL，永远看不到绝对路径。取消、完成或过期时，main/Host 按单一所有权协议删除预览文件并撤销 token。若以后引入 Host-main 二进制 side channel，可改为内存响应，但不应为了这个功能把 base64 图片塞入 JSON。

推荐由 main 注册受限的 `lumiere-preview://frame/<opaque-token>` custom protocol，并用 `protocol.handle` 返回带精确 `Content-Type` 与 `Cache-Control: no-store` 的 encoded SDR preview。token 只映射当前 Overlay 所属 session 的内存/受控临时对象；main 在读取前再次确认规范化路径仍位于专用临时目录。未知、过期、跨 session token 返回 404/410。URL path 绝不能被当作任意文件路径。[Electron `protocol.handle`](https://www.electronjs.org/docs/latest/api/protocol) · [Electron security：avoid `file://`](https://www.electronjs.org/docs/latest/tutorial/security#18-avoid-usage-of-the-file-protocol-and-prefer-usage-of-custom-protocols)

scheme 应在 app ready 前声明，仅启用实际需要的 `standard`、`secure`、`supportFetchAPI`；不要开启 `bypassCSP`。handler 在 app ready 后注册；若 Overlay 使用独立 Electron session/partition，protocol 也必须注册到该 session。Overlay CSP 只放行自身脚本与该 image scheme。[Electron protocol：privileges 与 session scope](https://www.electronjs.org/docs/latest/api/protocol)

preload 只暴露任务形状的窄 API，例如 `completeFrozenRegion(sessionId, rect)` 与 `cancelFrozenRegion(sessionId)`，绝不能暴露通用 `ipcRenderer.send`。main 必须校验 sender/frame、session 归属、当前状态、target 和 rect 边界。Electron 官方安全指南明确要求验证 IPC sender，并指出直接暴露通用 IPC 是危险边界。[Electron security：validate IPC sender](https://www.electronjs.org/docs/latest/tutorial/security#17-validate-the-sender-of-all-ipc-messages) · [Electron context isolation：security considerations](https://www.electronjs.org/docs/latest/tutorial/context-isolation#security-considerations)

## 5. API 可行性与性能权衡

| 平台 | 推荐 acquisition | 冻结资源 | 优点 | 主要权衡 |
|---|---|---|---|---|
| macOS 15+ | `SCScreenshotManager.captureImage` + SDR/HDR screenshot preset | Host-owned frozen `CGImage` | 官方 one-shot API；无需维持 stream；现有 Lumiere baseline 可用 | API 本身不可取消；完整帧 preview 派生与 encode 会增加 Overlay 出现前延迟 |
| Windows 10 1903+ | `CreateForMonitor` + free-threaded WGC frame pool，取命令后的首张 frame | Host-owned D3D11 texture | 可复用现有 WGC/D3D11/HDR pipeline；GPU copy 后立即关闭 session | “按键时刻”只能近似为触发后首帧；必须正确处理 pool frame 所有权和 `ContentSize` |

Windows 可用 `IGraphicsCaptureItemInterop::CreateForMonitor(HMONITOR, ...)` 直接构造 monitor capture item（Windows 10 1903 起）；`CreateFreeThreaded` 让 `FrameArrived` 在 frame pool 自己的 worker thread 触发，避免依赖 UI DispatcherQueue，适合降低首帧处理调度延迟。[Microsoft `CreateForMonitor`](https://learn.microsoft.com/en-us/windows/win32/api/windows.graphics.capture.interop/nf-windows-graphics-capture-interop-igraphicscaptureiteminterop-createformonitor) · [Microsoft `CreateFreeThreaded`](https://learn.microsoft.com/en-us/uwp/api/windows.graphics.capture.direct3d11captureframepool.createfreethreaded)

性能原则：

- native authoritative frame 留在 GPU/native 内存，不经 renderer 往返。
- Windows 首帧只做一次 GPU full-frame copy；确认时做一次 GPU subresource copy。必要 readback/tone-map/encode 不在 `FrameArrived` 或 UI thread 做。
- preview 按目标逻辑尺寸生成；Retina/HiDPI 不需要把 2x/3x full-resolution PNG 解码后再让 Chromium 缩小。最终文件仍从 full-resolution master 裁剪。
- encoded preview 优先无损 PNG，以保持文字边缘和选区判断；如果首帧延迟无法达标，再评估高质量有损格式，但必须记录预览非 artifact truth。
- 避免频繁 D3D staging `Map`；Microsoft 说明 GPU/CPU 间访问可能引入同步与 pipeline stall。[Microsoft：Copying and accessing resource data](https://learn.microsoft.com/en-us/windows/uwp/graphics-concepts/copying-and-accessing-resource-data)
- Apple 的 stream sample 指出增加 queue depth 会增加 WindowServer memory，默认 3 且不应超过 8；Region one-shot 不应为了单帧冻结维持多帧队列。[Apple ScreenCaptureKit sample：queue depth](https://developer.apple.com/documentation/screencapturekit/capturing-screen-content-in-macos)

## 6. 对 Lumiere 的推荐契约

建议把协议建模为三步，而不是继续复用“一次 capture 请求完成全部工作”的 Region 语义：

```text
prepareRegion
  -> prepared(sessionId, previewToken, target snapshot, logical/pixel geometry)

commitRegion(sessionId, target-logical geometry, delivery)
  -> completed | cancelled | failed

cancelRegion(sessionId)
  -> cancelled (idempotent)
```

关键不变量：

- `prepared` 只在 authoritative frozen frame 和可显示 preview 都已就绪后返回。
- Overlay 只在 `prepared` 后显示。
- `commitRegion` 消耗 session 一次；重放、跨 Overlay、过期 session 均失败。
- target topology 在 prepare 后变化时，仍从冻结帧裁剪；geometry 解释依赖 prepare 时 snapshot，不重新解析“当前显示器”。若产品选择在 topology change 时取消，也必须显式、可测试，不能静默换帧。
- preview token 与 session 同寿命，不是文件路径，也不是再次 capture 的许可。
- commit、cancel、timeout、Host/main/Overlay teardown 共享一个幂等释放路径。
- Display capture 继续保持单请求路径；冻结 session 只服务交互式 Region，避免扩大普通 capture 的协议复杂度。

## 7. 建议验收与测量

1. 在视频、CSS animation、毫秒计时器上触发 Region，等待 3 秒再框选；Overlay 与最终 PNG 必须保持 prepare 时刻。
2. Overlay 本身不得出现在 preview 或 artifact；首次显示不得闪过实时桌面。
3. 用 preview 原始像素到选区的映射与最终 PNG 做固定 fixture 比对，覆盖 1x、1.25x、1.5x、2x 与多显示器负坐标。
4. Esc、右键取消、Overlay close、renderer crash、Host disconnect、超时、应用退出都不留 preview token、临时文件、CG/D3D/WGC 资源或 active session。
5. 连续 100 次 prepare/cancel 和 prepare/commit 后，Host retained memory 回到稳定平台，不随次数增长。
6. 分别记录 macOS 与 Windows 的 command -> prepared P50/P95；这是 Overlay 体验指标。不要用一个平台的结果替代另一个平台验证。
7. Windows 报告“触发后首张完整 WGC frame”，macOS 报告“SCScreenshotManager 完成的单帧”；产品文案不承诺硬件级按键时间戳同步。

## 最终判断

两端 API 都足以实现优雅、可验证的冻结选区，不需要 Electron 参与 capture。最稳健的架构是 **native Host 持有事实帧，Electron 只持有 session capability 和 SDR preview**。macOS 可以直接把现有 one-shot `SCScreenshotManager` capture 前移到 prepare；Windows 则复用现有 WGC/D3D11 首帧 acquisition，但在 WGC frame 生命周期内复制出 session-owned texture。两端统一在 commit 时从冻结资源裁剪，并在任何 terminal path 确定性释放。
