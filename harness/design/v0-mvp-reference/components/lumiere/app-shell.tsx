"use client"

import { useState } from "react"
import { MainPanel } from "./main-panel"
import { SettingsPanel } from "./settings-panel"
import { TrayContextMenu } from "./tray-context-menu"
import type { CaptureMode, HdrStatus, PrototypeSettings } from "./prototype-state"

type View = "main" | "settings"

const HDR_STATUS_OPTIONS: { value: HdrStatus; label: string }[] = [
  { value: "ready", label: "HDR Ready" },
  { value: "available", label: "Available, Not Enabled" },
  { value: "unavailable", label: "No HDR" },
]

const INITIAL_SETTINGS: PrototypeSettings = {
  shortcuts: {
    full: "Shift+S",
    region: "Shift+A",
  },
  outputTarget: "clipboard",
  colorFormat: "hdr10",
  hdrWarnings: true,
  autoOpen: false,
  includeMetadata: true,
  copyImage: true,
  savePath: "C:\\Users\\You\\Pictures\\Lumiere",
}

export function AppShell() {
  const [view, setView] = useState<View>("main")
  const [hdrStatus, setHdrStatus] = useState<HdrStatus>("ready")
  const [settings, setSettings] = useState<PrototypeSettings>(INITIAL_SETTINGS)
  const [capturingMode, setCapturingMode] = useState<CaptureMode | null>(null)

  function updateSettings(next: Partial<PrototypeSettings>) {
    setSettings((current) => ({ ...current, ...next }))
  }

  function updateShortcut(mode: CaptureMode, shortcut: string) {
    setSettings((current) => ({
      ...current,
      shortcuts: {
        ...current.shortcuts,
        [mode]: shortcut,
      },
    }))
  }

  function handleCapture(mode: CaptureMode) {
    if (capturingMode) return

    setCapturingMode(mode)
    window.setTimeout(() => {
      setCapturingMode(null)
    }, 800)
  }

  return (
    <div className="min-h-screen flex flex-col items-center justify-center bg-background p-8 gap-8">
      <div className="flex items-center gap-3 rounded-full border border-border/70 bg-card/60 px-3 py-2 shadow-lg shadow-black/20">
        <span className="text-[10px] text-muted-foreground uppercase tracking-widest font-semibold">Demo HDR</span>
        <div className="flex gap-1">
          {HDR_STATUS_OPTIONS.map((opt) => (
            <button
              key={opt.value}
              onClick={() => setHdrStatus(opt.value)}
              className={`px-2.5 py-1 rounded-full text-[10px] font-medium transition-colors ${hdrStatus === opt.value
                ? "bg-primary/15 text-foreground"
                : "text-muted-foreground hover:text-foreground hover:bg-secondary/70"
                }`}
            >
              {opt.label}
            </button>
          ))}
        </div>
      </div>

      <div className="flex flex-col xl:flex-row items-start gap-8 justify-center">
        <div className="flex flex-col items-center gap-3">
          <span className="text-[11px] text-muted-foreground uppercase tracking-widest font-semibold">Main Panel</span>
          <div className={`w-[360px] rounded-2xl bg-card border shadow-2xl shadow-black/50 overflow-hidden transition-colors ${view === "main" ? "border-border" : "border-border/70"}`}>
            <MainPanel
              settings={settings}
              hdrStatus={hdrStatus}
              capturingMode={capturingMode}
              onCapture={handleCapture}
              onOpenSettings={() => setView("settings")}
            />
          </div>
        </div>

        <div className="flex flex-col items-center gap-3">
          <span className="text-[11px] text-muted-foreground uppercase tracking-widest font-semibold">Settings Panel</span>
          <div className={`w-[360px] h-[640px] rounded-2xl bg-card border shadow-2xl shadow-black/50 overflow-hidden transition-colors ${view === "settings" ? "border-border" : "border-border/70"}`}>
            <SettingsPanel
              settings={settings}
              onClose={() => setView("main")}
              onSettingsChange={updateSettings}
              onShortcutChange={updateShortcut}
            />
          </div>
        </div>

        <div className="flex flex-col items-center gap-3">
          <span className="text-[11px] text-muted-foreground uppercase tracking-widest font-semibold">Tray Menu</span>
          <div className="w-64 rounded-2xl overflow-hidden border border-border shadow-2xl shadow-black/50 bg-[oklch(0.10_0.005_240)]">
            <div className="h-80 flex items-center justify-center px-4 py-5">
              <TrayContextMenu
                settings={settings}
                hdrStatus={hdrStatus}
                capturingMode={capturingMode}
                onCapture={handleCapture}
                onOpenMain={() => setView("main")}
                onOpenSettings={() => setView("settings")}
              />
            </div>
            <div className="h-8 bg-[oklch(0.11_0.006_240)] border-t border-border flex items-center justify-end px-3 gap-2">
              <div className="w-3.5 h-3.5 rounded-sm bg-primary/20 flex items-center justify-center">
                <span className="text-primary text-[8px] font-bold leading-none">L</span>
              </div>
              <span className="text-[9px] text-muted-foreground font-mono">11:30</span>
            </div>
          </div>
          <p className="text-[10px] text-muted-foreground text-center max-w-[14rem] leading-relaxed">
            Right-click the tray icon after minimizing to show this menu.
          </p>
        </div>
      </div>

      <p className="text-[11px] text-muted-foreground text-center max-w-md leading-relaxed">
        Capture actions, shortcuts, output preferences, and status copy are shared across the prototype.
      </p>
    </div>
  )
}
