'use client'

import { forwardRef, type SVGProps } from 'react'
import { Button, type ButtonProps } from '@/components/motion/button/base'
import { cn } from '@/lib/utils'

export interface FolderPickerButtonProps extends Omit<
  ButtonProps,
  'children' | 'hoverScale' | 'pressScale' | 'size' | 'variant'
> {
  path: string
}

export const FolderPickerButton = forwardRef<HTMLButtonElement, FolderPickerButtonProps>(
  function FolderPickerButton({ path, className, ...props }, ref) {
    const displayPath = compactFolderPath(path)

    return (
      <Button
        ref={ref}
        variant="secondary"
        size="sm"
        hoverScale={1}
        pressScale={0.98}
        className={cn('settings-control-trigger settings-folder-picker-trigger group', className)}
        aria-label={`Choose save folder. Current folder: ${path}`}
        title={path}
        {...props}
      >
        <span className="min-w-0 flex-1 truncate text-left">{displayPath}</span>
        <ChevronRightIcon className="shrink-0 transition-transform group-hover:translate-x-0.5 motion-reduce:transition-none" />
      </Button>
    )
  },
)

function compactFolderPath(path: string): string {
  const separator = path.includes('\\') ? '\\' : '/'
  const segments = path.split(/[\\/]+/).filter(Boolean)

  if (segments.length <= 2) return path

  return `…${separator}${segments.slice(-2).join(separator)}`
}

function ChevronRightIcon(props: SVGProps<SVGSVGElement>): React.JSX.Element {
  return (
    <svg
      viewBox="0 0 24 24"
      fill="none"
      stroke="currentColor"
      strokeLinecap="round"
      strokeLinejoin="round"
      strokeWidth="1.6"
      aria-hidden="true"
      {...props}
    >
      <path d="m9 18 6-6-6-6" />
    </svg>
  )
}
