import { app, dialog } from 'electron'
import { access } from 'node:fs/promises'
import { join } from 'node:path'

const firstCheckDelayMilliseconds = 30_000
const checkIntervalMilliseconds = 6 * 60 * 60 * 1_000

export async function configureWindowsUpdates(): Promise<void> {
  if (process.platform !== 'win32' || !app.isPackaged) return

  try {
    await access(join(process.resourcesPath, 'windows-release.json'))
  } catch {
    return
  }

  const { autoUpdater } = await import('electron-updater')
  autoUpdater.autoDownload = true
  autoUpdater.autoInstallOnAppQuit = false
  autoUpdater.allowPrerelease = false
  autoUpdater.disableDifferentialDownload = true
  autoUpdater.logger = {
    info: (...args: unknown[]) => {
      console.info('operation=WindowsUpdate level=info', ...args)
    },
    warn: (...args: unknown[]) => {
      console.warn('operation=WindowsUpdate level=warning', ...args)
    },
    error: (...args: unknown[]) => {
      console.error('operation=WindowsUpdate level=error', ...args)
    },
    debug: (...args: unknown[]) => {
      console.debug('operation=WindowsUpdate level=debug', ...args)
    },
  }

  autoUpdater.on('error', (error) => {
    console.error('operation=WindowsUpdate stage=Failed', error)
  })
  autoUpdater.on('update-downloaded', () => {
    void dialog
      .showMessageBox({
        type: 'info',
        title: 'Lumiere update ready',
        message: 'A new version of Lumiere is ready to install.',
        detail: 'Restart now to finish the update, or install it later.',
        buttons: ['Restart now', 'Later'],
        defaultId: 0,
        cancelId: 1,
        noLink: true,
      })
      .then(({ response }) => {
        if (response === 0) autoUpdater.quitAndInstall(false, true)
      })
  })

  const check = (): void => {
    void autoUpdater.checkForUpdates().catch((error: unknown) => {
      console.error('operation=WindowsUpdate stage=CheckFailed', error)
    })
  }
  const firstCheck = setTimeout(() => {
    check()
    const interval = setInterval(check, checkIntervalMilliseconds)
    interval.unref()
  }, firstCheckDelayMilliseconds)
  firstCheck.unref()
}
