"use client"

interface StatusPillProps {
  icon: React.ElementType
  label: string
  detail?: string
  color: string
  dot: string
  compact?: boolean
}

export function StatusPill({ icon: Icon, label, detail, color, dot, compact }: StatusPillProps) {
  return (
    <div className="min-w-0 rounded-lg border border-border bg-secondary/25 px-2.5 py-2">
      <div className="flex items-center gap-2 min-w-0">
        <Icon className={`h-3.5 w-3.5 flex-shrink-0 ${color}`} strokeWidth={2} />
        <span className={`h-1.5 w-1.5 rounded-full flex-shrink-0 ${dot}`} />
        <span className="truncate text-[11px] font-semibold text-foreground">{label}</span>
      </div>
      {!compact && detail && <p className="mt-1 line-clamp-2 text-[10px] leading-snug text-muted-foreground">{detail}</p>}
    </div>
  )
}
