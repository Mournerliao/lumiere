import { describe, expect, it, vi } from 'vitest'
import { applyAfterCaptureBehavior } from './after-capture'
import type { CaptureCommandResult } from '../shared/capture-command'
import type { AfterCaptureBehavior } from '../shared/settings-command'

describe('applyAfterCaptureBehavior', () => {
  it('does nothing by default even when a file was saved', () => {
    const revealFile = vi.fn()
    const result = savedResult()

    expect(applyAfterCaptureBehavior(result, preferences('do-nothing'), revealFile)).toBe(result)
    expect(revealFile).not.toHaveBeenCalled()
  })

  it.each([
    savedResult(),
    {
      status: 'partial' as const,
      feedback: 'Saved the file, but couldn’t copy it',
      notice: {
        tone: 'caution' as const,
        title: 'Saved the file, but couldn’t copy it',
        detail: 'Check the failed output destination, then try again.',
      },
      filePath: '/tmp/Lumiere-partial.png',
    },
  ])('reveals a successfully saved file without changing the capture result', (result) => {
    const revealFile = vi.fn()
    expect(applyAfterCaptureBehavior(result, preferences('show-in-folder'), revealFile)).toBe(
      result,
    )
    expect(revealFile).toHaveBeenCalledOnce()
    expect(revealFile).toHaveBeenCalledWith(result.filePath)
  })

  it.each<CaptureCommandResult>([
    { status: 'success', feedback: 'Copied to clipboard' },
    { status: 'cancelled', feedback: 'Capture cancelled' },
    {
      status: 'failed',
      feedback: 'Capture failed',
      notice: { tone: 'critical', title: 'Capture failed', detail: 'Try again.' },
    },
  ])('does not reveal a file when no file was delivered', (result) => {
    const revealFile = vi.fn()

    expect(applyAfterCaptureBehavior(result, preferences('show-in-folder'), revealFile)).toBe(
      result,
    )
    expect(revealFile).not.toHaveBeenCalled()
  })

  it('reports a reveal failure without changing successful delivery semantics', () => {
    const result = savedResult()
    const revealFailure = new Error('Finder is unavailable')
    const reportFailure = vi.fn()
    expect(
      applyAfterCaptureBehavior(
        result,
        preferences('show-in-folder'),
        () => {
          throw revealFailure
        },
        reportFailure,
      ),
    ).toBe(result)
    expect(reportFailure).toHaveBeenCalledWith({
      event: 'after-capture-reveal-failed',
      filePath: result.filePath,
      error: revealFailure,
    })
  })
})

function preferences(behavior: AfterCaptureBehavior) {
  return { getAfterCaptureBehavior: () => behavior }
}

function savedResult(): CaptureCommandResult & { filePath: string } {
  return {
    status: 'success',
    feedback: 'Copied and saved to “Lumiere”',
    filePath: '/tmp/Lumiere.png',
  }
}
