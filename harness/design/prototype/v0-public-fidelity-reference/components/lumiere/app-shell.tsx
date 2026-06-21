"use client"

import { useState } from "react"
import { MainPanel } from "./main-panel"
import { OverlayPreview } from "./overlay-preview"
import { SettingsPanel } from "./settings-panel"
import { TrayContextMenu } from "./tray-context-menu"
import { FIDELITY_CLAIM_UI, HDR_STATUS_UI, RELEASE_TARGET, type CaptureMode, type HdrStatus, type PrototypeSettings } from "./prototype-state"

type View = "main" | "settings"

const HDR_STATUS_OPTIONS: { value: HdrStatus; label: string }[] = [
  { value: "ready", label: "HDR Ready" },
  { value: "mixed", label: "Mixed Target" },
  { value: "target-unvalidated", label: "Unvalidated" },
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
  fidelityClaim: "unvalidated",
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
    <div className="min-h-screen bg-background p-4 sm:p-6">
      <div className="mx-auto flex max-w-[1320px] flex-col gap-6">
        <header className="flex flex-col gap-3 border-b border-border pb-4 lg:flex-row lg:items-center lg:justify-between">
          <div>
            <p className="text-sm font-semibold text-foreground">Lumiere v0 + Perfect HDR Fidelity extension</p>
            <p className="mt-1 max-w-[64ch] text-[12px] leading-relaxed text-muted-foreground">
              Extends the existing prototype without replacing its native Windows utility direction. {RELEASE_TARGET} remains fixed.
            </p>
          </div>

          <div className="flex flex-wrap items-center gap-2 rounded-xl border border-border/70 bg-card/60 px-3 py-2">
            <span className="text-[10px] font-semibold uppercase tracking-widest text-muted-foreground">Scenario</span>
            {HDR_STATUS_OPTIONS.map((opt) => (
              <button
                key={opt.value}
                onClick={() => setHdrStatus(opt.value)}
                className={`rounded-md px-2.5 py-1 text-[10px] font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${hdrStatus === opt.value
                  ? "bg-primary/15 text-foreground"
                  : "text-muted-foreground hover:bg-secondary/70 hover:text-foreground"
                  }`}
              >
                {opt.label}
              </button>
            ))}
          </div>
        </header>

        <div className="grid items-start gap-6 xl:grid-cols-[360px_360px_1fr]">
          <div className="flex flex-col gap-3">
            <SurfaceLabel title="Main Panel" detail="Capture entry, target-aware status, output feedback" />
            <div className={`min-h-[680px] w-full max-w-[360px] overflow-hidden rounded-2xl border bg-card shadow-xl shadow-black/30 transition-colors ${view === "main" ? "border-border" : "border-border/70"}`}>
              <MainPanel
                settings={settings}
                hdrStatus={hdrStatus}
                capturingMode={capturingMode}
                onCapture={handleCapture}
                onOpenSettings={() => setView("settings")}
              />
            </div>
          </div>

          <div className="flex flex-col gap-3">
            <SurfaceLabel title="Settings Panel" detail="Profiles, validation scope, output configuration" />
            <div className={`h-[680px] w-full max-w-[360px] overflow-hidden rounded-2xl border bg-card shadow-xl shadow-black/30 transition-colors ${view === "settings" ? "border-border" : "border-border/70"}`}>
              <SettingsPanel
                settings={settings}
                onClose={() => setView("main")}
                onSettingsChange={updateSettings}
                onShortcutChange={updateShortcut}
              />
            </div>
          </div>

          <div className="grid gap-6 lg:grid-cols-2 xl:grid-cols-1 2xl:grid-cols-2">
            <section className="flex flex-col gap-3">
              <SurfaceLabel title="Overlay Preview" detail="Minimal trust cue over bright/dark HDR-like content" />
              <OverlayPreview hdrStatus={hdrStatus} fidelityClaim={settings.fidelityClaim} />
            </section>

            <section className="flex flex-col gap-3">
              <SurfaceLabel title="Tray Menu" detail="Mirrors the same target and fidelity vocabulary" />
              <div className="w-full max-w-64 overflow-hidden rounded-2xl border border-border bg-[oklch(0.10_0.005_240)] shadow-xl shadow-black/30">
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
            </section>

            <section className="rounded-xl border border-border bg-card px-4 py-3 lg:col-span-2 xl:col-span-1 2xl:col-span-2">
              <p className="text-[12px] font-semibold text-foreground">Current scenario contract</p>
              <div className="mt-3 grid gap-2 sm:grid-cols-2">
                <ScenarioRow label="Target HDR" value={HDR_STATUS_UI[hdrStatus].label} />
                <ScenarioRow label="Fidelity claim" value={FIDELITY_CLAIM_UI[settings.fidelityClaim].label} />
                <ScenarioRow label="Output target" value={settings.outputTarget} />
                <ScenarioRow label="Public target" value="Perfect HDR Fidelity" />
              </div>
            </section>
          </div>
        </div>
      </div>
    </div>
  )
}

function SurfaceLabel({ title, detail }: { title: string; detail: string }) {
  return (
    <div>
      <p className="text-[11px] font-semibold uppercase tracking-widest text-muted-foreground">{title}</p>
      <p className="mt-0.5 text-[10px] leading-snug text-muted-foreground">{detail}</p>
    </div>
  )
}

function ScenarioRow({ label, value }: { label: string; value: string }) {
  return (
    <div className="flex items-center justify-between gap-3 rounded-lg border border-border/80 bg-secondary/20 px-3 py-2">
      <span className="text-[10px] text-muted-foreground">{label}</span>
      <span className="truncate text-[11px] font-semibold text-foreground">{value}</span>
    </div>
  )
}
