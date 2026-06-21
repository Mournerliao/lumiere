"use client"

import { ClipboardCheck, FolderCheck, Info } from "lucide-react"
import { FIDELITY_CLAIM_UI, type FidelityClaim, type OutputTarget } from "./prototype-state"
import { StatusPill } from "./status-pill"

interface OutputResultProps {
  target: OutputTarget
  fidelityClaim: FidelityClaim
  visible: boolean
}

export function OutputResult({ target, fidelityClaim, visible }: OutputResultProps) {
  const fidelity = FIDELITY_CLAIM_UI[fidelityClaim]
  const targets =
    target === "both"
      ? ["Clipboard copied", "File saved"]
      : target === "folder"
        ? ["File saved"]
        : ["Clipboard copied"]

  return (
    <div
      className={`rounded-xl border border-border bg-card px-3.5 py-3 transition-opacity duration-150 ${
        visible ? "opacity-100" : "opacity-65"
      }`}
    >
      <div className="mb-2 flex items-center gap-2">
        {target === "clipboard" ? (
          <ClipboardCheck className="h-4 w-4 text-[oklch(0.70_0.16_155)]" />
        ) : (
          <FolderCheck className="h-4 w-4 text-[oklch(0.70_0.16_155)]" />
        )}
        <div className="min-w-0">
          <p className="text-[12px] font-semibold text-foreground">
            {target === "both" ? "Copied and saved" : target === "folder" ? "Saved" : "Copied"}
          </p>
          <p className="text-[10px] text-muted-foreground">Artifact result first, fidelity claim second.</p>
        </div>
      </div>
      <div className="mb-2 grid gap-1.5">
        {targets.map((item) => (
          <div key={item} className="flex items-center gap-2 text-[11px] text-secondary-foreground">
            <span className="h-1.5 w-1.5 rounded-full bg-[oklch(0.70_0.16_155)]" />
            {item}
          </div>
        ))}
      </div>
      <StatusPill
        icon={fidelity.icon}
        label={fidelity.label}
        detail={fidelity.detail}
        color={fidelity.color}
        dot={fidelity.dot}
      />
      <div className="mt-2 flex items-start gap-1.5 text-[10px] leading-snug text-muted-foreground">
        <Info className="mt-0.5 h-3 w-3 flex-shrink-0" />
        <span>Never implies HDR preservation unless the selected profile has target-aware evidence.</span>
      </div>
    </div>
  )
}
