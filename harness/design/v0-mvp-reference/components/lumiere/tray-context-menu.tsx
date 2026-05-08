"use client"

import { AppWindow, Camera, Layers, Power, Settings } from "lucide-react"
import {
  CAPTURE_LABELS,
  HDR_STATUS_UI,
  type CaptureMode,
  type HdrStatus,
  type PrototypeSettings,
} from "./prototype-state"

interface TrayContextMenuProps {
  settings: PrototypeSettings
  hdrStatus: HdrStatus
  capturingMode: CaptureMode | null
  onCapture: (mode: CaptureMode) => void
  onOpenMain: () => void
  onOpenSettings: () => void
}

interface MenuItem {
  id: string
  label: string
  icon: React.ElementType
  shortcut?: string
  variant?: "default" | "destructive"
  disabled?: boolean
  active?: boolean
  onClick: () => void
}

export function TrayContextMenu({
  settings,
  hdrStatus,
  capturingMode,
  onCapture,
  onOpenMain,
  onOpenSettings,
}: TrayContextMenuProps) {
  const status = HDR_STATUS_UI[hdrStatus]
  const StatusIcon = status.icon

  const captureItems: MenuItem[] = [
    {
      id: "full",
      label: capturingMode === "full" ? "Capturing..." : CAPTURE_LABELS.full,
      icon: Camera,
      shortcut: settings.shortcuts.full,
      active: capturingMode === "full",
      disabled: capturingMode !== null,
      onClick: () => onCapture("full"),
    },
    {
      id: "region",
      label: capturingMode === "region" ? "Capturing..." : CAPTURE_LABELS.region,
      icon: Layers,
      shortcut: settings.shortcuts.region,
      active: capturingMode === "region",
      disabled: capturingMode !== null,
      onClick: () => onCapture("region"),
    },
  ]

  const bottomItems: MenuItem[] = [
    { id: "open", label: "Open Lumiere", icon: AppWindow, onClick: onOpenMain },
    { id: "settings", label: "Settings", icon: Settings, onClick: onOpenSettings },
    { id: "quit", label: "Quit", icon: Power, variant: "destructive", onClick: () => {} },
  ]

  return (
    <div className="w-56 rounded-xl bg-card border border-border shadow-2xl shadow-black/60 overflow-hidden py-1.5">
      <div className="px-3 pt-1.5 pb-2.5 border-b border-border mb-1">
        <div className="flex items-center gap-2.5">
          <div className="w-7 h-7 rounded-lg bg-primary/15 flex items-center justify-center flex-shrink-0">
            <span className="text-primary text-[13px] font-bold select-none">L</span>
          </div>
          <div className="flex-1 min-w-0">
            <p className="text-[12px] font-semibold text-foreground leading-tight">Lumiere</p>
            <div className="flex items-center gap-1.5 mt-0.5">
              <StatusIcon className={`w-3 h-3 flex-shrink-0 ${status.color}`} strokeWidth={2.5} />
              <span className={`text-[10px] truncate ${status.color}`}>{status.label}</span>
            </div>
          </div>
        </div>
      </div>

      {captureItems.map((item) => (
        <MenuRow key={item.id} item={item} />
      ))}

      <div className="my-1 mx-2 h-px bg-border" />

      {bottomItems.map((item) => (
        <MenuRow key={item.id} item={item} />
      ))}
    </div>
  )
}

function MenuRow({ item }: { item: MenuItem }) {
  const Icon = item.icon
  const isDestructive = item.variant === "destructive"

  return (
    <button
      onClick={item.onClick}
      disabled={item.disabled}
      className={`w-full flex items-center gap-2.5 px-3 py-1.5 text-left transition-colors duration-75 disabled:opacity-50 ${
        item.active
          ? "bg-primary/12"
          : isDestructive
          ? "hover:bg-destructive/10"
          : "hover:bg-secondary/70"
      }`}
    >
      <Icon
        className={`w-3.5 h-3.5 flex-shrink-0 transition-colors ${
          isDestructive
            ? "text-[oklch(0.65_0.22_27)]"
            : item.active
            ? "text-primary"
            : "text-muted-foreground"
        }`}
        strokeWidth={1.75}
      />
      <span
        className={`flex-1 text-[12px] font-medium ${
          isDestructive ? "text-[oklch(0.65_0.22_27)]" : "text-foreground"
        }`}
      >
        {item.label}
      </span>
      {item.shortcut && (
        <span className="text-[10px] font-mono text-muted-foreground/70 flex-shrink-0">
          {item.shortcut}
        </span>
      )}
    </button>
  )
}
