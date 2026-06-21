"use client"

import { Camera, Info, Layers, Settings } from "lucide-react"
import {
  CAPTURE_LABELS,
  FIDELITY_CLAIM_UI,
  HDR_STATUS_UI,
  OUTPUT_PROFILES,
  RELEASE_TARGET,
  type CaptureMode,
  type HdrStatus,
  type PrototypeSettings,
} from "./prototype-state"
import { OutputResult } from "./output-result"
import { StatusPill } from "./status-pill"

interface MainPanelProps {
  settings: PrototypeSettings
  hdrStatus: HdrStatus
  capturingMode: CaptureMode | null
  onCapture: (mode: CaptureMode) => void
  onOpenSettings: () => void
}

export function MainPanel({
  settings,
  hdrStatus,
  capturingMode,
  onCapture,
  onOpenSettings,
}: MainPanelProps) {
  const status = HDR_STATUS_UI[hdrStatus]
  const StatusIcon = status.icon
  const profile = OUTPUT_PROFILES[settings.colorFormat]
  const fidelity = FIDELITY_CLAIM_UI[settings.fidelityClaim]

  return (
    <div className="flex flex-col h-full">
      <header className="flex items-center justify-between px-5 pt-5 pb-4 border-b border-border">
        <div className="flex items-center gap-2.5">
          <div className="w-6 h-6 rounded-md bg-primary/15 flex items-center justify-center">
            <Layers className="w-3.5 h-3.5 text-primary" />
          </div>
          <span className="text-sm font-semibold tracking-wide text-foreground">Lumiere</span>
        </div>
        <button
          onClick={onOpenSettings}
          className="w-7 h-7 rounded-md flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors"
          aria-label="Open settings"
        >
          <Settings className="w-4 h-4" />
        </button>
      </header>

      <div className="flex-1 flex flex-col justify-center px-5 py-5 gap-4">
        <div className="flex flex-col gap-2">
          <CaptureButton
            mode="full"
            icon={Camera}
            primary
            shortcut={settings.shortcuts.full}
            capturingMode={capturingMode}
            onCapture={onCapture}
          />
          <CaptureButton
            mode="region"
            icon={Layers}
            shortcut={settings.shortcuts.region}
            capturingMode={capturingMode}
            onCapture={onCapture}
          />
        </div>

        <div className="grid grid-cols-2 gap-2">
          <StatusPill
            icon={status.icon}
            label={status.label}
            detail={status.detail}
            color={status.color}
            dot={status.dot}
          />
          <StatusPill
            icon={fidelity.icon}
            label={fidelity.label}
            detail={fidelity.detail}
            color={fidelity.color}
            dot={fidelity.dot}
          />
        </div>

        <div className="rounded-xl border border-border bg-secondary/20 px-3 py-2.5">
          <div className="flex items-start justify-between gap-3">
            <div className="min-w-0">
              <p className="truncate text-[11px] font-semibold text-foreground">Output profile: {profile.label}</p>
              <p className="mt-1 line-clamp-2 text-[10px] leading-snug text-muted-foreground">{profile.detail}</p>
            </div>
            <span className="flex-shrink-0 rounded-md border border-border bg-card px-1.5 py-1 text-[9px] font-bold text-muted-foreground">
              {profile.statusLabel}
            </span>
          </div>
        </div>

        <OutputResult
          target={settings.outputTarget}
          fidelityClaim={settings.fidelityClaim}
          visible={capturingMode === null}
        />
      </div>

      <footer className="px-5 py-3 border-t border-border flex items-center justify-between gap-3">
        <div className="flex items-center gap-2 min-w-0">
          <StatusIcon className={`w-3.5 h-3.5 flex-shrink-0 ${status.color}`} strokeWidth={2} />
          <span className={`w-1.5 h-1.5 rounded-full flex-shrink-0 ${status.dot}`} />
          <span className="text-[11px] font-medium text-muted-foreground whitespace-nowrap">{status.label}</span>
        </div>
        <div className="flex min-w-0 items-center gap-1.5 text-[10px] text-muted-foreground">
          <Info className="h-3 w-3 flex-shrink-0" />
          <span className="truncate">{RELEASE_TARGET}</span>
        </div>
      </footer>
    </div>
  )
}

function CaptureButton({
  mode,
  icon: Icon,
  primary,
  shortcut,
  capturingMode,
  onCapture,
}: {
  mode: CaptureMode
  icon: React.ElementType
  primary?: boolean
  shortcut: string
  capturingMode: CaptureMode | null
  onCapture: (mode: CaptureMode) => void
}) {
  const isActive = capturingMode === mode
  const isDisabled = capturingMode !== null

  return (
    <button
      onClick={() => onCapture(mode)}
      disabled={isDisabled}
      className={`group relative w-full flex items-center gap-3.5 px-4 py-3.5 rounded-xl border transition-all duration-150 active:scale-[0.99] disabled:cursor-not-allowed focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${
        primary
          ? "bg-primary/10 border-primary/25 hover:bg-primary/18 hover:border-primary/45 disabled:opacity-70"
          : "bg-secondary/45 border-border hover:bg-secondary hover:border-border/80 disabled:opacity-50"
      }`}
      aria-label={`${CAPTURE_LABELS[mode]} capture`}
    >
      <div className={`flex-shrink-0 transition-transform duration-150 ${isActive ? "scale-90" : "group-hover:scale-110"}`}>
        <Icon
          className={`w-5 h-5 transition-colors ${primary ? "text-primary" : "text-muted-foreground group-hover:text-foreground"}`}
          strokeWidth={1.5}
        />
      </div>
      <div className="flex-1 min-w-0 text-left">
        <p className="text-[13px] font-semibold text-foreground leading-tight">
          {isActive ? "Capturing..." : CAPTURE_LABELS[mode]}
        </p>
        <p className="text-[11px] text-muted-foreground mt-0.5">
          Shortcut <kbd className="px-1 py-0.5 rounded bg-secondary text-secondary-foreground text-[10px] font-mono border border-border">{shortcut}</kbd>
        </p>
      </div>
      {isActive && <span className="absolute inset-0 rounded-xl border border-primary/50 animate-ping opacity-30" />}
    </button>
  )
}
