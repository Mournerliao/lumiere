'use client'
// beui.dev/components/motion/dock

import { motion, useReducedMotion } from 'motion/react'
import { createContext, useContext, useId, useMemo, type ReactNode } from 'react'
import { SPRING_LAYOUT } from '@/lib/ease'
import { cn } from '@/lib/utils'

interface DockContextValue {
  fill: boolean
  itemHeight: number
  itemWidth: number
  pillLayoutId: string
}

const DockContext = createContext<DockContextValue | null>(null)

export interface DockProps {
  children: ReactNode
  className?: string
  /** Evenly distribute items across the available dock width. */
  fill?: boolean
  /** Size of each item in px. */
  size?: number
  /** Optional rectangular item width for compact navigation docks. */
  itemWidth?: number
  /** Optional rectangular item height for compact navigation docks. */
  itemHeight?: number
}

export function Dock({
  children,
  fill = false,
  size = 44,
  itemWidth = size,
  itemHeight = size,
  className,
}: DockProps) {
  const pillLayoutId = useId()
  const ctx = useMemo<DockContextValue>(
    () => ({ fill, itemHeight, itemWidth, pillLayoutId }),
    [fill, itemHeight, itemWidth, pillLayoutId],
  )

  return (
    <DockContext.Provider value={ctx}>
      <div
        className={cn(
          'inline-flex h-auto items-end gap-1.5 rounded-2xl border border-border bg-card/80 px-2 py-1 shadow-2xl backdrop-blur-xl',
          className,
        )}
      >
        {children}
      </div>
    </DockContext.Provider>
  )
}

export interface DockItemProps {
  children: ReactNode
  className?: string
  /** When set, the item renders as a <button>. Omit when children carry their own link or button. */
  onClick?: () => void
  active?: boolean
  'aria-label'?: string
}

export function DockItem({ children, className, onClick, active, ...rest }: DockItemProps) {
  const dock = useContext(DockContext)
  const reduce = useReducedMotion()
  const itemHeight = dock?.itemHeight ?? 44
  const itemWidth = dock?.itemWidth ?? 44
  const pillLayoutId = dock?.pillLayoutId ?? 'dock-pill'

  const pill = active ? (
    <motion.span
      layoutId={pillLayoutId}
      transition={reduce ? { duration: 0 } : SPRING_LAYOUT}
      className="dock-active-pill absolute inset-0.5 -z-10 rounded-xl bg-primary/5"
    />
  ) : null
  const sharedStyle = dock?.fill
    ? { flex: '1 1 0%', minWidth: 0, height: itemHeight }
    : { width: itemWidth, height: itemHeight }
  const sharedClass = cn(
    'relative flex shrink-0 items-center justify-center rounded-full text-foreground',
    className,
  )

  if (onClick) {
    return (
      <button
        type="button"
        onClick={onClick}
        aria-label={rest['aria-label']}
        aria-pressed={active}
        style={sharedStyle}
        className={cn(
          sharedClass,
          'cursor-pointer border-0 bg-transparent p-0 outline-none',
          'focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 focus-visible:ring-offset-background',
        )}
      >
        {pill}
        {children}
      </button>
    )
  }

  // Children carry their own link or button (and its accessible name).
  return (
    <div style={sharedStyle} className={sharedClass}>
      {pill}
      {children}
    </div>
  )
}

export function DockSeparator({ className }: { className?: string }) {
  return <span aria-hidden className={cn('mx-1 h-6 w-px self-center bg-border', className)} />
}
