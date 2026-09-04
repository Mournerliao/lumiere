# Electron macOS 应用体积优化调研（2026-09-04）

## 结论摘要

Lumiere 当前约 213 MB 的 Universal DMG、安装后 500 MB 以上，首先应当视为
Electron Universal 应用的正常基线，而不是项目业务代码本身已经膨胀。Electron 明确
选择把 Chromium、V8 和 Node.js 直接随每个应用分发；应用即使只有一个小窗口，也不会
共享系统浏览器运行时。Electron 43.4.1 的官方 macOS 单架构 ZIP 本身已经分别达到：

- arm64：122,238,952 bytes（116.58 MiB）；
- x64：124,311,852 bytes（118.55 MiB）。

这两个数字来自 Electron 官方 GitHub Release 资产的 `Content-Length`（核对日期：
2026-09-04）。作为参照，Lumiere 当前安装的 arm64 Electron 43.4.1 开发运行时解压后约
295 MiB，其中 `Electron Framework.framework` 约 274 MiB。Universal 包再把关键 Mach-O
二进制的 arm64 与 x64 slice 合并进同一个 bundle，因此 500 MiB 量级并不反常。

对 Lumiere 最有意义的、仍受支持的优化顺序是：

1. **发布 arm64、x64 两个原生 DMG**，这是唯一有希望把单次下载和安装占用近似砍半的
   常规方案；也可以同时保留 Universal 作为“最省心”入口。
2. **继续启用 ASAR，并确认 Universal 构建启用了 `mergeASARs`**（electron-builder 默认
   为 `true`），避免把架构无关的 JS 和资产复制两份。
3. **审计最终 bundle，而不是只看源码仓库**：收紧 `files` / `extraResources`，清除 source
   map、测试资源和误入包内的大文件；同时只把真正需要在运行时从 `node_modules` 加载的包
   放在 `dependencies`。
4. **通过 electron-builder 的 `electronLanguages` 只保留产品支持并验证过的 Chromium
   locales**。这是官方支持的裁剪点，但收益通常只是几十 MiB 的解压体积，远小于拆分架构。
5. **不要把 Electron fuses 当成瘦身工具，也不要手工删除 Chromium 核心资源**。fuses 是
   安全加固开关；手删 `resources.pak`、`icudtl.dat`、V8 snapshot、framework 或 helper 等
   文件不是受支持的应用级优化路径，容易产生启动或功能故障。

Electron 官方没有一个与完整 Electron API 兼容的“轻量运行时”发行物。要显著低于单架构
Electron 基线，需要改用系统 WebView 或原生 UI；那是框架迁移，不是打包参数优化。

## 1. 为什么小项目也会很大

Electron 的产品定位就是将 Chromium、V8、Node.js 与应用一同交付，而不是调用 macOS 已有
的 WebKit。Electron 官方解释称，这样做是为了跨平台的稳定性、安全性和一致性能；代价是
每个 Electron 应用都携带完整运行时。参见：

- [Electron: Why Electron](https://www.electronjs.org/docs/latest/why-electron)
- [Electron introduction](https://www.electronjs.org/docs/latest/)
- [Electron process model](https://www.electronjs.org/docs/latest/tutorial/process-model)

Electron 官方分发文档也说明，手工发布的基础就是完整的预编译 Electron 二进制目录，再将
应用代码放到其 Resources 中；使用 ASAR 只是把应用代码目录换成 `app.asar`，并不会换掉
Chromium/Node 运行时：

- [Electron: Application Packaging](https://www.electronjs.org/docs/latest/tutorial/application-distribution)
- [Electron: ASAR Archives](https://www.electronjs.org/docs/latest/tutorial/asar-archives)

当前项目使用 Electron 43.4.1。该版本官方单架构运行时压缩包可直接核对：

- [electron-v43.4.1-darwin-arm64.zip](https://github.com/electron/electron/releases/download/v43.4.1/electron-v43.4.1-darwin-arm64.zip)
- [electron-v43.4.1-darwin-x64.zip](https://github.com/electron/electron/releases/download/v43.4.1/electron-v43.4.1-darwin-x64.zip)
- [Electron v43.4.1 Release](https://github.com/electron/electron/releases/tag/v43.4.1)

因此不能用“业务代码只有多少 KB/MB”推导 Electron `.app` 的体积。更有意义的指标是：

- 单架构 Electron 基线；
- `app.asar` 与 `app.asar.unpacked`；
- `extraResources`；
- native host / `.node` 模块；
- locales；
- Universal 相对两个单架构产物的增量。

DMG 是压缩交付物，安装后的 `.app` 是解压后的 bundle；213 MB 下载对应 500 MB 以上安装
占用并不矛盾。

## 2. Universal 与按架构分发

Apple 的 Universal binary 是在一个应用中容纳 x86_64 和 arm64 架构。Apple 官方说明可以
使用 `lipo` 检查或合并架构；`@electron/universal` 的官方 FAQ 则直接说明，Universal Electron
应用本质上是把 x64 与 arm64 应用合在一起，所以接近两倍大。参见：

- [Apple: Building a universal macOS binary](https://developer.apple.com/documentation/apple-silicon/building-a-universal-macos-binary)
- [`@electron/universal`](https://github.com/electron/universal)
- [electron-builder architecture documentation](https://www.electron.build/docs/architecture/)

`@electron/universal` 支持 `mergeASARs: true`，将两边架构无关的 ASAR 合并成一份，从而避免
JS、CSS、图片等再复制一次；electron-builder 的 Universal 构建默认采用这一设置。需要按
架构保留的 native module / binary，则通过 `singleArchFiles` 等机制处理。它只能消除应用层
重复，无法把 Electron Framework 中必须同时存在的两个 CPU slice 变成一份。

### 可选发布策略

| 策略 | 单次下载/安装体积 | 用户体验 | 结论 |
|---|---:|---|---|
| 只发 Universal | 最大 | 一个链接，两类 Mac 原生运行 | 最简单，当前做法合理 |
| 分别发 arm64 与 x64 | 约为 Universal 的一半 | 用户或下载页需选对架构 | **体积优化首选** |
| Universal + 两个单架构包 | 用户可选 | 资产与发布验证增多 | 兼顾简单入口和小包 |
| 只发 arm64 | 最小且原生 | Intel Mac 无法运行 | 只有明确放弃 Intel 时才可选 |
| 只发 x64 | 单包可覆盖旧 Intel，并暂时经 Rosetta 跑在 Apple silicon | 非原生且未来不可持续 | **不建议** |

Apple 已公告 macOS 27 是 Apple silicon 上完整 Rosetta 支持的最后一个主要版本；从 macOS 28
起只为某些旧游戏保留有限兼容。因此 2026 年不能再把“只发 x64”当长期的单包策略。应发布
Universal，或分别发布 arm64/x64 原生版本：

- [Apple Support: Using Intel-based apps on a Mac with Apple silicon](https://support.apple.com/en-us/102527)
- [Apple Developer News: Upcoming changes to Rosetta support](https://developer.apple.com/news/)

若应用启用自动更新，拆分架构还必须验证下载页、release asset 命名和 updater 是否持续把
正确架构的更新交给现有安装；不能只验证首次下载。

## 3. ASAR：应该保留，但不要误判它的作用

Electron 将 ASAR 描述为一种应用源码归档格式，主要收益是规避 Windows 长路径问题、改善
读取/`require` 性能并减少随手浏览源码；官方没有将它描述为压缩算法。electron-builder 默认
`asar: true`，并自动检测应放到 `app.asar.unpacked` 的 native module / executable：

- [Electron: ASAR Archives](https://www.electronjs.org/docs/latest/tutorial/asar-archives)
- [electron-builder: Application Contents / ASAR](https://www.electron.build/docs/contents/)
- [electron-builder configuration: `asar`](https://www.electron.build/docs/configuration/)

所以：

- `asar: true` 应继续保留；关闭它通常不会减小体积；
- Universal 中最重要的是 `mergeASARs: true`，不是 ASAR 文件本身；
- 不要为了“压缩”而把必须直接执行或随机访问的 native binary 强塞回 ASAR；
- `asarUnpack`/smart unpack 解决的是运行兼容性，通常不是体积优化，误配还可能造成重复或故障。

electron-builder 的总体 `compression` 默认为 `normal`，其官方文档明确指出 `maximum` 不会带来
明显的体积差异，却会增加构建时间。因此把压缩级别调到 maximum 不应列为主要方案：

- [electron-builder configuration: `compression`](https://www.electron.build/docs/configuration/)

## 4. `files`、`extraResources` 与 npm 依赖裁剪

electron-builder 的内容模型分两层：`files` 进入应用内容（通常是 ASAR），`extraResources`
直接复制到 `Contents/Resources`。`extraResources` 不会因为启用 ASAR 而自动变小，native host、
模型、视频、测试 fixture 等若被宽泛 glob 命中，会原样进入应用。官方建议直接检查构建后的
bundle，并指出 source map、TypeScript 源码和测试 fixture 是常见的大文件来源：

- [electron-builder: Application Contents](https://www.electron.build/docs/contents/)

依赖规则有一个容易误判的细节：

- `devDependencies` 不会被复制；
- 解析到的 production dependency tree 会被复制；
- 即使自定义了 `files`，`package.json` 和 production `node_modules` 仍会被追加包含。

这意味着使用 Vite/esbuild/webpack 后，某个库如果已经被内联到 `out/`，但仍在
`dependencies`，就可能同时以 bundle 代码和 `node_modules` 两种形式进入产物。应按最终 main、
preload、renderer 的 external/bundle 结果来分类依赖：只把运行时仍需通过 Node resolver 加载的
包留在 `dependencies`；构建工具和已完全内联且不需要外部解析的前端包应避免被重复打包。

不要把 `npm prune --production` 当作 electron-builder 之前的必需步骤：builder 已按 production
依赖收集；在 pnpm workspace 中额外原地 prune 还可能破坏开发安装。`npmRebuild` 的职责是为
目标 Electron/架构重建 native modules，不是依赖裁剪或瘦身。参见：

- [electron-builder: Application Contents / default inclusion](https://www.electron.build/docs/contents/)
- [electron-builder: Two package structure](https://www.electron.build/v26/docs/tutorials/two-package-structure/)
- [electron-builder: Architecture and native modules](https://www.electron.build/v26/docs/architecture/)

对 Lumiere 当前配置的含义：`files: ["out/**/*"]` 已经很好地限制了项目自身文件，但不能单凭
这一项断定 production `node_modules` 没有进入 ASAR；仍应列出 `app.asar` 内容并按包统计。
`extraResources` 当前显式包含 icons 与 macOS native host，方向也正确，重点是核实二者的实测
大小，而不是猜测。

## 5. Locales 与其他 Chromium 资源

electron-builder 正式支持 `electronLanguages`，定义要保留的 Electron locales；默认保留全部。
这是比构建后手动删除 `.lproj`/`.pak` 更稳妥的方案：

- [electron-builder configuration: `electronLanguages`](https://www.electron.build/docs/configuration/#electronlanguages)

本机 Electron 43.4.1 arm64 分发物中，Electron Framework 的所有 locale resources 合计约
47 MiB（解压体积）；单个 locale 多为约 0.5–1.6 MiB。因此只保留例如 `en-US`、`zh-CN`、
`zh-TW` 可能节省数十 MiB 解压体积，但在压缩后的 DMG 里收益会更小，而且 Universal 合并时
架构无关资源本来就不会简单翻倍。它是“值得做的小优化”，不是把 213 MB 变成几十 MB 的手段。

裁剪时应至少做到：

- 保留产品承诺支持的语言和可靠 fallback（通常包括 `en-US`）；
- 在不同 macOS 首选语言下冷启动并检查 Electron/Chromium 原生菜单、dialog、permission、
  WebAuthn 等路径；
- 校验 `app.getLocale()` 与应用自身 i18n fallback；
- 每次 Electron 大版本升级重新验证。

Electron 的 `app.getLocale()` 文档明确要求发布时携带 locales 目录。Electron 42–44 在 2026 年
还修复过 macOS 上 locale 资源未加载导致字符串为空及 WebAuthn Touch ID prompt 崩溃的问题，
说明“能打开主窗口”不足以证明激进 locale 删除安全：

- [Electron `app.getLocale()`](https://www.electronjs.org/docs/latest/api/app#appgetlocale)
- [Electron v42.11.0 release notes](https://github.com/electron/electron/releases/tag/v42.11.0)

除 locales 外，不建议手工删除 `resources.pak`、`icudtl.dat`、V8 snapshots、Electron Framework、
helper app 或 ffmpeg 等官方分发内容。Electron 的发布 manifest 将这些列为运行时组成部分；例如
缺少 `icudtl.dat` 会在 ICU 初始化阶段直接退出。没有官方配置声明“未调用某项 Electron API，
即可删除对应 Chromium 资源”：

- [Electron release ZIP manifest example](https://github.com/electron/electron/blob/main/script/zip_manifests/dist_zip.win.x64.manifest)
- [Electron issue showing ICU startup failure when `icudtl.dat` is unavailable](https://github.com/electron/electron/issues/42624)

## 6. Electron fuses 不是体积优化

Electron fuses 是在打包、签名前翻转 Electron binary 中的 magic bits，用于关闭危险能力或
强化加载约束，例如禁用 `ELECTRON_RUN_AS_NODE`、启用 ASAR integrity、只允许从 ASAR 加载。
这类开关不会把对应实现从预编译 Electron Framework 中移除，因此不应预期产生可测的包体积
下降：

- [Electron: Fuses](https://www.electronjs.org/docs/latest/tutorial/fuses)
- [electron-builder: `electronFuses`](https://www.electron.build/docs/configuration/)

fuses 仍然值得作为安全工作单独评估，但必须按功能影响验证。例如关闭 `runAsNode` 会影响依赖
它的 `child_process.fork`；官方建议相应场景改用 Utility Process。不要为了几乎不存在的体积
收益混入本次瘦身。

## 7. 是否存在官方轻量 Electron 运行时

截至 2026-09-04，Electron 官方发布和文档没有提供一个 API 兼容的 lite/minimal runtime。
官方支持路径仍是按 platform/architecture 下载完整预编译 Electron，再用 Forge、builder 或
其他工具打包应用。Electron 官方也允许从源码构建自定义二进制，但文档明确提醒这套环境复杂、
耗时，不推荐把它作为普通应用的分发做法；维护自定义 Chromium/Electron fork 还会放大每个
安全升级周期的成本：

- [Electron: Installing prebuilt binaries and supported architectures](https://www.electronjs.org/docs/latest/tutorial/installation)
- [Electron: Build Instructions](https://www.electronjs.org/docs/latest/development/build-instructions-gn)
- [Electron: Application Packaging](https://www.electronjs.org/docs/latest/tutorial/application-distribution)

如果业务目标变为“安装后必须显著低于约 200–300 MiB 的单架构 Electron 基线”，可行方向是
Tauri/WKWebView、纯 Swift/AppKit/SwiftUI 等系统 WebView 或原生技术；这会改变渲染内核、IPC、
安全模型、跨平台复用与验证范围，属于架构迁移，不是 Electron 优化。本文不据此建议 Lumiere
在基础体验打磨阶段迁移框架。

## 8. 建议的最小验证实验

不先改产品架构，只生成并比较以下产物即可决定是否值得实施：

1. 当前 Universal DMG（基准）；
2. arm64 DMG；
3. x64 DMG；
4. Universal + 限定 `electronLanguages`；
5. 在第 4 项基础上，移除确认已被 Vite 内联的重复 production dependencies。

每个产物记录：DMG bytes、`.app` apparent size、`.app` allocated size、`Electron Framework`、
`app.asar`、`app.asar.unpacked`、locales、`extraResources`，并分别做目标架构冷启动、Display、
Region、clipboard、folder、设置持久化和退出验证。不要只以 DMG 能挂载或主窗口能打开作为通过。

预期排序：**拆分架构的收益远大于 locales，locales 大于应用 JS 的细枝末节，fuses 对体积近似
无收益**。如果单架构包仍异常大，再从 `app.asar` / `extraResources` 的实测清单寻找项目自身
问题；不要先怀疑 Electron 的 Universal 基线。
