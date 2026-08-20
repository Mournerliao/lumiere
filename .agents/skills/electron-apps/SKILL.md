---
name: electron-apps
description: Build, debug, harden, test, and ship Electron desktop applications across macOS and Windows. Use for main/preload/renderer architecture, typed IPC, secure windows, native hosts or sidecars, platform integration, performance, packaging, and release work. Preserve an existing repository's stack and contracts instead of imposing a scaffold.
metadata:
  source: https://github.com/kingsidharth/skills/tree/production/electron-apps
  adapted-for: Lumiere
---

# Electron Apps

Build production-grade desktop applications with Electron. TypeScript throughout, security-first, performance-aware, and native where platform truth requires it.

## Operating Posture

- Start by reading the repository instructions and inspecting its Electron version, package manager, bundler, process boundaries, and platform contracts. Existing architecture wins.
- Do not replace electron-vite, pnpm, Forge, builder, or another established tool merely because a reference uses a different example.
- Verify API details against the installed Electron types and the matching official documentation version. Treat snippets here as patterns, not upgrade instructions.
- Keep renderer concerns in the renderer, privileged orchestration in main/preload, and platform-owned behavior in the native host selected by the repository.
- Distinguish repository checks, packaged-app behavior, and real Windows/macOS observations. One platform never verifies the other.

For a genuinely greenfield app, see [Project Setup](references/project-setup/structure.md). Do not use that scaffold to restructure an existing project.

## Essential Patterns

### Secure BrowserWindow

Every window starts here. Confirm these settings explicitly and never weaken them without a documented reason:

```ts
const win = new BrowserWindow({
  webPreferences: {
    preload: path.join(__dirname, 'preload.js'),
    contextIsolation: true, // preload ≠ renderer context
    nodeIntegration: false, // no require() in renderer
    sandbox: true, // Chromium OS-level sandbox
  },
  show: false, // prevent white flash
})
win.once('ready-to-show', () => win.show())
```

### Module Loading Strategy

Synchronous module loading can dominate startup in large apps. Measure first, then bundle per process and defer genuinely heavy, optional work.

```ts
// ❌ Eager — blocks main thread on startup
import heavyModule from 'heavy-module'

// ✅ Deferred — load when actually needed
const getHeavy = () => import('heavy-module')

// ✅ Route-level code splitting in renderer
const Settings = lazy(() => import('./pages/Settings'))
```

Keep main, preload, and renderer as separate build targets; use renderer chunk splitting where it improves measured startup or interaction cost. Details in [Bundling](references/performance/bundling.md).

### Unblocking the Main Process

The main process is the control tower. Blocking it freezes every window.

```ts
// ❌ Sync I/O
const data = fs.readFileSync('large.json', 'utf-8')

// ✅ Async I/O
const data = await fs.promises.readFile('large.json', 'utf-8')

// ❌ Sync IPC (blocks entire renderer until main responds)
const result = ipcRenderer.sendSync('query', args)

// ✅ Async IPC
const result = await ipcRenderer.invoke('query', args)

// ✅ Heavy computation → separate process
const child = utilityProcess.fork(path.join(__dirname, 'worker.js'))
child.postMessage(payload)
```

See [Unblocking](references/performance/unblocking.md) for Web Workers, Worker↔Main direct channels, and `utilityProcess`.

### IPC Contract

Define channel names and request/response types once, expose only task-shaped preload methods, validate payloads at runtime, and authenticate the sending frame in main:

```ts
// src/shared/ipc-channels.ts
export const IPC = {
  GET_VERSION: 'get-version',
  SAVE_FILE: 'save-file',
} as const
```

An allowlisted channel is not authorization. Each privileged handler must reject unexpected `webContents`, subframes, origins, paths, and argument shapes before doing work. Preserve structured success and failure semantics across native-host boundaries.

For the full invoke/handle pattern, listener cleanup, and MessagePort for high-throughput, see [IPC Patterns](references/core/ipc-patterns.md).

---

## Reference Map

### Core Concepts

- [Process Model](references/core/process-model.md) — main process, renderer, preload scripts, TypeScript declarations
- [IPC Patterns](references/core/ipc-patterns.md) — invoke/handle, one-way, main→renderer, renderer↔renderer, MessagePort, listener cleanup

### Security

- [Process Isolation](references/security/process-isolation.md) — context isolation, sandboxing, secure defaults
- [Content Security Policy](references/security/csp.md) — meta tag setup, bundler compatibility, common gotchas
- [IPC Security & Safe Storage](references/security/ipc-and-storage.md) — sender authentication, input validation, navigation hardening, permissions, safeStorage, file:// avoidance, auditing checklist

### Performance

- [Unblocking Processes](references/performance/unblocking.md) — main process rules, renderer optimization, Web Workers, Worker↔Main direct channels, startup optimization, V8 snapshots, long-running app concerns
- [Bundling & Code Splitting](references/performance/bundling.md) — require() problem, one-bundle-per-process, code splitting, tree shaking, Bun (pkg mgr vs bundler), CSP implications
- [Native Code & WASM](references/performance/native-code.md) — WebAssembly, NAPI-RS (Rust 10x case study), Go bindings, Python sidecars, comparison table
- [Cache-First & Perceived Performance](references/performance/cache-first.md) — only for products whose measured startup or remote-data behavior warrants persistent renderer caching
- [Instrumentation & Profiling](references/performance/instrumentation.md) — Chrome DevTools, contentTracing API, CPU instruction counting, production monitoring (Slack/VSCode patterns), component-level CPU costs, React profiling tools

### Windows & UI

- [Window Management](references/windows/window-management.md) — custom title bars, traffic lights, frameless/transparent windows, drag regions, vibrancy, progress bars, multi-window patterns, navigation history, prevent-close dialogs

### Patterns & Recipes

- [Recipes](references/patterns/recipes.md) — keyboard shortcuts (local/global/window), deep links (custom protocol), notifications, spellchecker, device access (Bluetooth/HID/Serial), offscreen rendering, multithreading options, background server pattern, live reloading, Windows taskbar, environment variables

### Debugging

- [Debugging](references/debugging/debugging.md) — main process inspector, renderer DevTools, REPL, DevTools extensions, native addon debugging (Xcode/lldb), automated testing with Playwright, common debug techniques

### Native & Platform

- [Platform Integration](references/native/platform-integration.md) — device access (screen, camera, mic, clipboard), power monitoring, startup registration, storage paradigms, network interception, safe key storage, macOS (Swift sidecars, permissions, dock), Windows (taskbar, jump lists), native UI (menus, tray, notifications, theme)

### Electron Forge

- [Config & Plugins](references/forge/config-and-plugins.md) — read only when the repository already uses or has explicitly selected Electron Forge
- [Makers & Distribution](references/forge/makers-and-distribution.md) — read only after the packaging tool and release targets are decided

### Project Setup

- [Structure & Setup](references/project-setup/structure.md) — greenfield-only layout options plus IPC contracts and secure BrowserWindow patterns

---

## Decision Quick-Reference

| Need                                                  | Solution                                                                                                                 |
| ----------------------------------------------------- | ------------------------------------------------------------------------------------------------------------------------ |
| Background task (non-UI)                              | `utilityProcess`                                                                                                         |
| Background task (renderer-bound)                      | Web Worker                                                                                                               |
| Existing native engine or OS-only capability          | Typed external host/sidecar owned by the platform boundary                                                               |
| Heavy computation without an existing native boundary | Measure, then choose UtilityProcess, worker, WASM, or a native module                                                    |
| Data persistence                                      | IndexedDB (cache), SQLite (structured), `safeStorage` (secrets), `electron-store` (config)                               |
| IPC pattern                                           | `invoke`/`handle` (default), `send`/`on` (fire-forget), `MessagePort` (high-throughput)                                  |
| Integrated Windows title bar                          | Window Controls Overlay before `frame: false`; preserve native controls and Snap Layout                                  |
| Integrated macOS title bar                            | `hiddenInset` or another native title-bar style before frameless custom controls                                         |
| System-wide shortcut                                  | `globalShortcut.register()`                                                                                              |
| Deep link                                             | `app.setAsDefaultProtocolClient()`                                                                                       |
| Testing                                               | Existing test runner for units, Playwright or desktop automation for E2E, real OS observation for window/platform claims |
