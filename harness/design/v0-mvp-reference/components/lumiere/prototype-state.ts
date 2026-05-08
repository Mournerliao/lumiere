"use client"

import { AlertCircle, CheckCircle2, Monitor } from "lucide-react"

export type HdrStatus = "ready" | "available" | "unavailable"
export type CaptureMode = "full" | "region"
export type OutputTarget = "clipboard" | "folder" | "both"
export type ColorFormat = "srgb" | "wide" | "hdr10"

export interface PrototypeSettings {
  shortcuts: Record<CaptureMode, string>
  outputTarget: OutputTarget
  colorFormat: ColorFormat
  hdrWarnings: boolean
  autoOpen: boolean
  includeMetadata: boolean
  copyImage: boolean
  savePath: string
}

export const CAPTURE_LABELS: Record<CaptureMode, string> = {
  full: "Full Screen",
  region: "Region",
}

export const HDR_STATUS_UI: Record<
  HdrStatus,
  { label: string; detail: string; icon: React.ElementType; color: string; dot: string }
> = {
  ready: {
    label: "HDR Ready",
    detail: "HDR capture is available",
    icon: CheckCircle2,
    color: "text-[oklch(0.70_0.16_155)]",
    dot: "bg-[oklch(0.70_0.16_155)]",
  },
  available: {
    label: "Enable HDR",
    detail: "Windows HDR is off",
    icon: Monitor,
    color: "text-[oklch(0.78_0.16_70)]",
    dot: "bg-[oklch(0.78_0.16_70)]",
  },
  unavailable: {
    label: "HDR unavailable",
    detail: "No HDR display detected",
    icon: AlertCircle,
    color: "text-[oklch(0.65_0.22_27)]",
    dot: "bg-[oklch(0.65_0.22_27)]",
  },
}
