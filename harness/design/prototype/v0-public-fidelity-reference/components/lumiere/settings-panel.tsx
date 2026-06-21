"use client"

import { useState } from "react"
import { ArrowLeft, Clipboard, FileCheck2, FolderOpen, Info, Keyboard, Monitor, ShieldCheck } from "lucide-react"
import {
  FIDELITY_CLAIM_UI,
  OUTPUT_PROFILES,
  type CaptureMode,
  type ColorFormat,
  type FidelityClaim,
  type OutputTarget,
  type PrototypeSettings,
} from "./prototype-state"
import { ValidationPanel } from "./validation-panel"

interface SettingsPanelProps {
  settings: PrototypeSettings
  onClose: () => void
  onSettingsChange: (next: Partial<PrototypeSettings>) => void
  onShortcutChange: (mode: CaptureMode, shortcut: string) => void
}

function Toggle({
  checked,
  onChange,
  id,
  label,
}: {
  checked: boolean
  onChange: (v: boolean) => void
  id: string
  label: string
}) {
  return (
    <button
      id={id}
      role="switch"
      aria-checked={checked}
      aria-label={label}
      onClick={() => onChange(!checked)}
      className={`relative inline-flex w-9 h-5 rounded-full flex-shrink-0 transition-colors duration-200 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${checked ? "bg-primary" : "bg-muted border border-border"}`}
    >
      <span
        className={`absolute top-0.5 left-0.5 w-4 h-4 rounded-full bg-white shadow transition-transform duration-200 ${checked ? "translate-x-4" : "translate-x-0"}`}
      />
    </button>
  )
}

function SectionHeader({ icon: Icon, title }: { icon: React.ElementType; title: string }) {
  return (
    <div className="flex items-center gap-2 mb-1">
      <Icon className="w-3.5 h-3.5 text-primary" strokeWidth={2} />
      <span className="text-[11px] font-semibold text-muted-foreground uppercase tracking-wider">{title}</span>
    </div>
  )
}

function SettingRow({
  label,
  description,
  children,
}: {
  label: string
  description?: string
  children: React.ReactNode
}) {
  return (
    <div className="flex items-center justify-between gap-4 py-3 border-b border-border last:border-0">
      <div className="flex-1 min-w-0 pr-1">
        <span className="text-[13px] text-foreground font-medium block leading-snug">{label}</span>
        {description && <span className="text-[11px] text-muted-foreground mt-0.5 block">{description}</span>}
      </div>
      <div className="flex-shrink-0">{children}</div>
    </div>
  )
}

export function SettingsPanel({
  settings,
  onClose,
  onSettingsChange,
  onShortcutChange,
}: SettingsPanelProps) {
  const colorFormatOptions = Object.values(OUTPUT_PROFILES)
  const fidelityOptions: { value: FidelityClaim; label: string }[] = [
    { value: "converted", label: "Converted" },
    { value: "visual-match", label: "Visual" },
    { value: "hdr-preserved", label: "HDR" },
    { value: "unvalidated", label: "None" },
  ]

  return (
    <div className="flex flex-col h-full">
      <header className="flex items-center gap-3 px-6 pt-5 pb-4 border-b border-border">
        <button
          onClick={onClose}
          className="w-7 h-7 rounded-md flex items-center justify-center text-muted-foreground hover:text-foreground hover:bg-secondary transition-colors"
          aria-label="Back to main panel"
        >
          <ArrowLeft className="w-4 h-4" />
        </button>
        <span className="text-sm font-semibold text-foreground">Settings</span>
      </header>

      <div className="flex-1 overflow-y-auto px-6 py-5 space-y-5">
        <section>
          <SectionHeader icon={Keyboard} title="Shortcuts" />
          <div className="rounded-xl bg-card border border-border px-4">
            <SettingRow label="Full Screen">
              <ShortcutInput value={settings.shortcuts.full} onChange={(value) => onShortcutChange("full", value)} />
            </SettingRow>
            <SettingRow label="Region">
              <ShortcutInput value={settings.shortcuts.region} onChange={(value) => onShortcutChange("region", value)} />
            </SettingRow>
          </div>
        </section>

        <section>
          <SectionHeader icon={Monitor} title="HDR" />
          <div className="rounded-xl bg-card border border-border px-4">
            <SettingRow label="HDR alerts" description="When HDR is unavailable">
              <Toggle
                id="hdr-warnings"
                label="HDR alerts"
                checked={settings.hdrWarnings}
                onChange={(hdrWarnings) => onSettingsChange({ hdrWarnings })}
              />
            </SettingRow>

            <SettingRow label="Target-aware state" description="Public release cannot use a global HDR guess">
              <span className="rounded-md border border-border bg-secondary px-2 py-1 text-[10px] font-semibold text-secondary-foreground">
                Required
              </span>
            </SettingRow>
          </div>
        </section>

        <section>
          <SectionHeader icon={ShieldCheck} title="Fidelity" />
          <div className="rounded-xl bg-card border border-border px-4">
            <div className="py-3 border-b border-border">
              <span className="text-[13px] text-foreground font-medium block mb-1.5">Output profile</span>
              <div className="grid grid-cols-3 gap-1 rounded-lg bg-secondary/35 border border-border/70 p-1">
                {colorFormatOptions.map((opt) => (
                  <button
                    key={opt.id}
                    onClick={() => onSettingsChange({ colorFormat: opt.id, fidelityClaim: opt.claim })}
                    className={`min-h-10 rounded-md px-2 text-center transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${
                      settings.colorFormat === opt.id
                        ? "bg-primary/14 text-foreground"
                        : "text-muted-foreground hover:text-foreground"
                    }`}
                  >
                    <span className="block text-[10px] font-semibold">{opt.label}</span>
                    <span className="block text-[8px] uppercase text-muted-foreground">{opt.statusLabel}</span>
                  </button>
                ))}
              </div>
              <p className="mt-2 text-[10px] leading-snug text-muted-foreground">
                HDR-preserved options stay scoped until profile contract, metadata policy, viewer matrix, and Windows validation exist.
              </p>
            </div>

            <div className="py-3 border-b border-border">
              <span className="text-[13px] text-foreground font-medium block mb-1.5">Feedback claim</span>
              <div className="grid grid-cols-4 gap-1 rounded-lg bg-secondary/35 border border-border/70 p-1">
                {fidelityOptions.map((opt) => {
                  const ui = FIDELITY_CLAIM_UI[opt.value]
                  const Icon = ui.icon
                  return (
                    <button
                      key={opt.value}
                      onClick={() => onSettingsChange({ fidelityClaim: opt.value })}
                      className={`min-h-10 rounded-md px-1.5 transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${
                        settings.fidelityClaim === opt.value
                          ? "bg-primary/14 text-foreground"
                          : "text-muted-foreground hover:text-foreground"
                      }`}
                    >
                      <Icon className={`mx-auto h-3.5 w-3.5 ${settings.fidelityClaim === opt.value ? ui.color : ""}`} />
                      <span className="mt-0.5 block text-[9px] font-medium">{opt.label}</span>
                    </button>
                  )
                })}
              </div>
            </div>

            <SettingRow label="QQ benchmark" description="Gray, white, and highlight behavior must not regress">
              <span className="rounded-md border border-border bg-secondary px-2 py-1 text-[10px] font-semibold text-secondary-foreground">
                Visual match
              </span>
            </SettingRow>
          </div>
        </section>

        <section>
          <SectionHeader icon={FolderOpen} title="Output" />
          <div className="rounded-xl bg-card border border-border px-4">
            <div className="py-3 border-b border-border">
              <span className="text-[11px] font-medium text-muted-foreground uppercase tracking-wider block mb-2">Destination</span>
              <SegmentedOutput
                value={settings.outputTarget}
                onChange={(outputTarget) => onSettingsChange({ outputTarget })}
              />
            </div>

            {(settings.outputTarget === "folder" || settings.outputTarget === "both") && (
              <div className="py-3 border-b border-border">
                <span className="text-[13px] text-foreground font-medium block mb-1.5">Save Path</span>
                <div className="flex gap-2 items-center">
                  <div className="flex-1 min-w-0 bg-input border border-border rounded-lg px-3 py-1.5">
                    <span className="text-[11px] font-mono text-muted-foreground truncate block">{settings.savePath}</span>
                  </div>
                  <button
                    onClick={() => onSettingsChange({ savePath: "C:\\Users\\You\\Pictures\\Lumiere" })}
                    className="flex-shrink-0 px-2.5 py-1.5 rounded-lg bg-secondary border border-border text-[11px] text-secondary-foreground hover:bg-secondary/80 transition-colors flex items-center gap-1"
                  >
                    <FolderOpen className="w-3 h-3" />
                    Browse
                  </button>
                </div>
              </div>
            )}

            <SettingRow label="Open after capture">
              <Toggle
                id="auto-open"
                label="Open after capture"
                checked={settings.autoOpen}
                onChange={(autoOpen) => onSettingsChange({ autoOpen })}
              />
            </SettingRow>
            <SettingRow label="Timestamp">
              <Toggle
                id="include-metadata"
                label="Timestamp"
                checked={settings.includeMetadata}
                onChange={(includeMetadata) => onSettingsChange({ includeMetadata })}
              />
            </SettingRow>
          </div>
        </section>

        {(settings.outputTarget === "clipboard" || settings.outputTarget === "both") && (
          <section>
            <SectionHeader icon={Clipboard} title="Clipboard" />
            <div className="rounded-xl bg-card border border-border px-4">
              <SettingRow label="Copy as Image" description="Compatibility output; not an HDR-preserved claim">
                <Toggle
                  id="copy-image"
                  label="Copy as Image"
                  checked={settings.copyImage}
                  onChange={(copyImage) => onSettingsChange({ copyImage })}
                />
              </SettingRow>
            </div>
          </section>
        )}

        <section>
          <SectionHeader icon={FileCheck2} title="Validation" />
          <ValidationPanel />
        </section>

        <section>
          <SectionHeader icon={Info} title="About" />
          <div className="rounded-xl bg-card border border-border px-4 py-3">
            <div className="flex items-center justify-between">
              <span className="text-[13px] text-foreground">Lumiere</span>
              <span className="text-[12px] text-muted-foreground font-mono">v0.1.0</span>
            </div>
            <p className="text-[11px] text-muted-foreground mt-1 leading-relaxed">
              Native screenshot reference for HDR-first capture.
            </p>
          </div>
        </section>

        <div className="h-4" />
      </div>
    </div>
  )
}

function SegmentedOutput({
  value,
  onChange,
}: {
  value: OutputTarget
  onChange: (value: OutputTarget) => void
}) {
  const labels: Record<OutputTarget, string> = {
    clipboard: "Clipboard",
    folder: "Folder",
    both: "Both",
  }

  return (
    <div className="grid grid-cols-3 gap-1 rounded-lg bg-secondary/35 border border-border/70 p-1">
      {(["clipboard", "folder", "both"] as OutputTarget[]).map((target) => (
        <button
          key={target}
          onClick={() => onChange(target)}
          className={`h-8 rounded-md text-[11px] font-medium transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60 ${
            value === target
              ? "bg-primary/14 text-foreground shadow-sm shadow-black/20"
              : "text-muted-foreground hover:text-foreground"
          }`}
        >
          {labels[target]}
        </button>
      ))}
    </div>
  )
}

function ShortcutInput({ value, onChange }: { value: string; onChange: (v: string) => void }) {
  const [editing, setEditing] = useState(false)

  function handleKeyDown(e: React.KeyboardEvent<HTMLInputElement>) {
    e.preventDefault()
    const parts: string[] = []
    if (e.ctrlKey) parts.push("Ctrl")
    if (e.shiftKey) parts.push("Shift")
    if (e.altKey) parts.push("Alt")
    const key = e.key
    if (key && !["Control", "Shift", "Alt", "Meta"].includes(key)) {
      parts.push(key.toUpperCase())
    }
    if (parts.length > 0) {
      onChange(parts.join("+"))
      setEditing(false)
    }
  }

  if (editing) {
    return (
      <input
        autoFocus
        onKeyDown={handleKeyDown}
        onBlur={() => setEditing(false)}
        className="w-24 px-2 py-1 rounded-lg bg-input border border-primary/60 text-[11px] font-mono text-center text-foreground outline-none focus:ring-1 focus:ring-primary/50"
        placeholder="Press shortcut..."
        readOnly
      />
    )
  }

  return (
    <button
      onClick={() => setEditing(true)}
      className="w-24 px-2 py-1 rounded-lg bg-secondary border border-border text-[11px] font-mono text-center text-secondary-foreground hover:border-primary/40 hover:text-foreground transition-colors focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60"
    >
      {value}
    </button>
  )
}
