"use client"

import { ClipboardList, ShieldCheck } from "lucide-react"
import { RELEASE_TARGET, VALIDATION_ROWS, VALIDATION_STATUS_UI } from "./prototype-state"

export function ValidationPanel() {
  return (
    <section className="rounded-xl border border-border bg-card px-4 py-3">
      <div className="mb-3 flex items-start gap-2.5">
        <div className="flex h-7 w-7 flex-shrink-0 items-center justify-center rounded-lg bg-primary/15">
          <ShieldCheck className="h-4 w-4 text-primary" strokeWidth={1.75} />
        </div>
        <div className="min-w-0">
          <p className="text-[12px] font-semibold text-foreground">{RELEASE_TARGET}</p>
          <p className="mt-0.5 text-[10px] leading-snug text-muted-foreground">
            Public release waits for evidence; SDR compatibility is fallback only.
          </p>
        </div>
      </div>
      <div className="grid gap-2">
        {VALIDATION_ROWS.map((row) => {
          const status = VALIDATION_STATUS_UI[row.status]
          return (
            <div key={row.label} className="rounded-lg border border-border/80 bg-secondary/20 px-3 py-2">
              <div className="flex items-center justify-between gap-3">
                <div className="flex min-w-0 items-center gap-2">
                  <ClipboardList className="h-3.5 w-3.5 flex-shrink-0 text-muted-foreground" />
                  <span className="truncate text-[11px] font-medium text-foreground">{row.label}</span>
                </div>
                <span className={`flex-shrink-0 text-[9px] font-bold ${status.className}`}>{status.label}</span>
              </div>
              <p className="mt-1 text-[10px] leading-snug text-muted-foreground">{row.detail}</p>
            </div>
          )
        })}
      </div>
    </section>
  )
}
