# IPC Patterns

Inter-process communication is the only sanctioned bridge between main and renderer. All patterns below assume `contextIsolation: true`.

Before registering privileged handlers, bind them to the intended window and its main frame:

```ts
function assertTrustedSender(
  event: Electron.IpcMainEvent | Electron.IpcMainInvokeEvent,
  win: BrowserWindow,
) {
  if (event.sender !== win.webContents || event.senderFrame !== event.sender.mainFrame) {
    throw new Error('Rejected IPC from an untrusted renderer')
  }
}
```

Also validate request payloads at runtime. TypeScript types disappear at the process boundary.

## Pattern 1: Renderer → Main (One-Way)

Use `ipcRenderer.send` / `ipcMain.on` for fire-and-forget messages.

```ts
// preload.ts
contextBridge.exposeInMainWorld('api', {
  setTitle: (title: string) => ipcRenderer.send('set-title', title),
})

// main.ts
ipcMain.on('set-title', (event, title: unknown) => {
  assertTrustedSender(event, mainWindow)
  if (typeof title !== 'string' || title.length > 200) return
  const win = BrowserWindow.fromWebContents(event.sender)
  win?.setTitle(title)
})
```

## Pattern 2: Renderer → Main (Two-Way / Invoke)

Use `ipcRenderer.invoke` / `ipcMain.handle` for request-response. Returns a `Promise`.

```ts
// preload.ts
contextBridge.exposeInMainWorld('api', {
  readConfig: () => ipcRenderer.invoke('read-config'),
})

// main.ts
ipcMain.handle('read-config', async (event) => {
  assertTrustedSender(event, mainWindow)
  return JSON.parse(await fs.promises.readFile('config.json', 'utf-8'))
})
```

**Always prefer `invoke` over `sendSync`.** `sendSync` blocks the entire renderer until the main process responds.

## Pattern 3: Main → Renderer

Use `webContents.send` to push data to a specific renderer.

```ts
// main.ts
win.webContents.send('update-available', version)

// preload.ts
contextBridge.exposeInMainWorld('api', {
  onUpdateAvailable: (cb: (v: string) => void) =>
    ipcRenderer.on('update-available', (_e, v) => cb(v)),
})
```

## Pattern 4: Renderer ↔ Renderer (via Main)

Renderers cannot talk directly. Route through main:

```ts
// In main process — relay from worker window to UI window
ipcMain.on('worker-result', (_event, data) => {
  uiWindow.webContents.send('worker-result', data)
})
```

For high-throughput renderer-to-renderer communication, use `MessagePort` pairs via `MessageChannelMain`:

```ts
// main.ts
import { MessageChannelMain } from 'electron'

const { port1, port2 } = new MessageChannelMain()
uiWindow.webContents.postMessage('port', null, [port1])
workerWindow.webContents.postMessage('port', null, [port2])
```

## IPC Security Rules

- **Never expose raw `ipcRenderer`** to the renderer — always wrap specific channels
- Authenticate the sender window and main frame before privileged work
- Validate all inputs in `ipcMain.handle` like HTTP request validation
- Use an allowlist of channel names; reject unknown channels
- Don't pass callbacks directly through IPC (leaks `IpcRendererEvent`)

## Removing IPC Listeners

Critical for long-running apps to prevent memory leaks:

```ts
// preload.ts — provide cleanup function
contextBridge.exposeInMainWorld('api', {
  onData: (cb: (d: unknown) => void) => {
    const handler = (_e: IpcRendererEvent, d: unknown) => cb(d)
    ipcRenderer.on('data', handler)
    return () => ipcRenderer.removeListener('data', handler)
  },
})

// renderer — React cleanup
useEffect(() => {
  const unsub = window.api.onData(setData)
  return unsub
}, [])
```

> **Ref:** [IPC Tutorial](https://www.electronjs.org/docs/latest/tutorial/ipc) · [MessageChannelMain](https://www.electronjs.org/docs/latest/api/message-channel-main)
