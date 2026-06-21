"use client"

import { MousePointer2, X } from "lucide-react"
import { HDR_STATUS_UI, FIDELITY_CLAIM_UI, type FidelityClaim, type HdrStatus } from "./prototype-state"

interface OverlayPreviewProps {
  hdrStatus: HdrStatus
  fidelityClaim: FidelityClaim
}

export function OverlayPreview({ hdrStatus, fidelityClaim }: OverlayPreviewProps) {
  const hdr = HDR_STATUS_UI[hdrStatus]
  const fidelity = FIDELITY_CLAIM_UI[fidelityClaim]
  const HdrIcon = hdr.icon
  const FidelityIcon = fidelity.icon

  return (
    <div className="relative h-[268px] overflow-hidden rounded-xl border border-border bg-[oklch(0.11_0.006_240)]">
      <div className="absolute inset-0 bg-[linear-gradient(135deg,oklch(0.18_0.04_255),oklch(0.11_0.006_240)_46%,oklch(0.38_0.05_90))]" />
      <div className="absolute left-7 top-7 h-16 w-28 rounded-md bg-white/90" />
      <div className="absolute bottom-8 right-8 h-20 w-32 rounded-lg bg-[oklch(0.72_0.13_65)]" />
      <div className="absolute inset-0 bg-black/35" />
      <div className="absolute left-[22%] top-[24%] h-[48%] w-[56%] border border-white/90 bg-white/[0.03] shadow-[0_0_0_999px_rgb(0_0_0/0.18)]">
        <div className="absolute -top-7 left-0 flex items-center gap-1.5 rounded-md border border-border bg-card px-2 py-1 text-[10px] text-foreground">
          <MousePointer2 className="h-3 w-3 text-primary" />
          1280 x 720
        </div>
        <span className="absolute -bottom-1.5 -right-1.5 h-3 w-3 rounded-sm border border-white bg-primary" />
      </div>
      <div className="absolute left-3 top-3 flex max-w-[76%] items-center gap-2 rounded-lg border border-border bg-card/95 px-2.5 py-2">
        <HdrIcon className={`h-3.5 w-3.5 flex-shrink-0 ${hdr.color}`} />
        <div className="min-w-0">
          <p className="truncate text-[11px] font-semibold text-foreground">{hdr.label}</p>
          <p className="truncate text-[10px] text-muted-foreground">{hdr.detail}</p>
        </div>
      </div>
      <div className="absolute bottom-3 left-3 right-3 flex items-center justify-between gap-2 rounded-lg border border-border bg-card/95 px-2.5 py-2">
        <div className="flex min-w-0 items-center gap-2">
          <FidelityIcon className={`h-3.5 w-3.5 flex-shrink-0 ${fidelity.color}`} />
          <span className="truncate text-[11px] font-medium text-foreground">{fidelity.label}</span>
          <span className="truncate text-[10px] text-muted-foreground">{fidelity.detail}</span>
        </div>
        <button className="flex h-6 w-6 flex-shrink-0 items-center justify-center rounded-md text-muted-foreground hover:bg-secondary hover:text-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-primary/60" aria-label="Cancel capture">
          <X className="h-3.5 w-3.5" />
        </button>
      </div>
    </div>
  )
}
