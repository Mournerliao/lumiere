"use client"

import { AlertCircle, CheckCircle2, Eye, FileCheck2, Monitor, ShieldCheck } from "lucide-react"

export type HdrStatus = "ready" | "available" | "unavailable" | "target-unvalidated" | "mixed"
export type CaptureMode = "full" | "region"
export type OutputTarget = "clipboard" | "folder" | "both"
export type ColorFormat = "srgb" | "wide" | "hdr10"
export type FidelityClaim = "converted" | "visual-match" | "hdr-preserved" | "unvalidated"
export type ValidationStatus = "pass" | "limited" | "fail" | "not-run" | "na"

export interface PrototypeSettings {
  shortcuts: Record<CaptureMode, string>
  outputTarget: OutputTarget
  colorFormat: ColorFormat
  fidelityClaim: FidelityClaim
  hdrWarnings: boolean
  autoOpen: boolean
  includeMetadata: boolean
  copyImage: boolean
  savePath: string
}

export interface OutputProfile {
  id: ColorFormat
  label: string
  status: "ready" | "pending-validation" | "pending-implementation" | "compatibility"
  statusLabel: string
  claim: FidelityClaim
  detail: string
}

export interface ValidationRow {
  label: string
  status: ValidationStatus
  detail: string
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
  "target-unvalidated": {
    label: "Target unvalidated",
    detail: "Capture target needs evidence",
    icon: Eye,
    color: "text-[oklch(0.72_0.10_210)]",
    dot: "bg-[oklch(0.72_0.10_210)]",
  },
  mixed: {
    label: "Mixed display",
    detail: "HDR state follows selected target",
    icon: Monitor,
    color: "text-[oklch(0.78_0.16_70)]",
    dot: "bg-[oklch(0.78_0.16_70)]",
  },
}

export const OUTPUT_PROFILES: Record<ColorFormat, OutputProfile> = {
  hdr10: {
    id: "hdr10",
    label: "HDR10",
    status: "pending-validation",
    statusLabel: "Validate",
    claim: "unvalidated",
    detail: "Requires profile contract, metadata policy, and supported viewer evidence.",
  },
  wide: {
    id: "wide",
    label: "P3",
    status: "pending-implementation",
    statusLabel: "Build",
    claim: "unvalidated",
    detail: "Wide-gamut path is visible as intent, but not selectable as a claim yet.",
  },
  srgb: {
    id: "srgb",
    label: "sRGB",
    status: "compatibility",
    statusLabel: "Compat",
    claim: "converted",
    detail: "Compatibility output; useful fallback, not the public release target.",
  },
}

export const FIDELITY_CLAIM_UI: Record<
  FidelityClaim,
  { label: string; detail: string; icon: React.ElementType; color: string; dot: string }
> = {
  converted: {
    label: "Converted",
    detail: "Output is optimized for compatibility, not HDR preservation.",
    icon: FileCheck2,
    color: "text-[oklch(0.78_0.16_70)]",
    dot: "bg-[oklch(0.78_0.16_70)]",
  },
  "visual-match": {
    label: "Visual match",
    detail: "Appearance has viewer-specific validation evidence.",
    icon: Eye,
    color: "text-[oklch(0.72_0.10_210)]",
    dot: "bg-[oklch(0.72_0.10_210)]",
  },
  "hdr-preserved": {
    label: "HDR-preserved",
    detail: "Supported profile has target-aware validation evidence.",
    icon: ShieldCheck,
    color: "text-[oklch(0.70_0.16_155)]",
    dot: "bg-[oklch(0.70_0.16_155)]",
  },
  unvalidated: {
    label: "Unvalidated",
    detail: "No fidelity claim is made for this path.",
    icon: AlertCircle,
    color: "text-[oklch(0.65_0.22_27)]",
    dot: "bg-[oklch(0.65_0.22_27)]",
  },
}

export const VALIDATION_ROWS: ValidationRow[] = [
  {
    label: "Target-aware HDR",
    status: "not-run",
    detail: "Mixed HDR/SDR monitor evidence is required.",
  },
  {
    label: "Visual-match output",
    status: "limited",
    detail: "QQ-style gray, white, and highlight checks are the benchmark.",
  },
  {
    label: "HDR-preserved profile",
    status: "not-run",
    detail: "At least one supported profile must pass before public release.",
  },
  {
    label: "Target app matrix",
    status: "not-run",
    detail: "Named viewers must separate artifact success from fidelity.",
  },
]

export const VALIDATION_STATUS_UI: Record<ValidationStatus, { label: string; className: string }> = {
  pass: { label: "PASS", className: "text-[oklch(0.70_0.16_155)]" },
  limited: { label: "LIMITED", className: "text-[oklch(0.78_0.16_70)]" },
  fail: { label: "FAIL", className: "text-[oklch(0.65_0.22_27)]" },
  "not-run": { label: "NOT RUN", className: "text-muted-foreground" },
  na: { label: "N/A", className: "text-muted-foreground" },
}

export const RELEASE_TARGET = "Perfect HDR Fidelity Public Release"
