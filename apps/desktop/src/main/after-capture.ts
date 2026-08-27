import type { CaptureCommandResult } from '../shared/capture-command'
import type { AfterCaptureBehavior } from '../shared/settings-command'

export interface AfterCapturePreferencesReader {
  getAfterCaptureBehavior(): AfterCaptureBehavior
}

export interface AfterCaptureFailure {
  event: 'after-capture-reveal-failed'
  filePath: string
  error: unknown
}

export function applyAfterCaptureBehavior(
  result: CaptureCommandResult,
  preferences: AfterCapturePreferencesReader,
  revealFile: (filePath: string) => void,
  reportFailure: (failure: AfterCaptureFailure) => void = defaultFailureReporter,
): CaptureCommandResult {
  if (
    preferences.getAfterCaptureBehavior() !== 'show-in-folder' ||
    !('filePath' in result) ||
    !result.filePath
  ) {
    return result
  }

  try {
    revealFile(result.filePath)
  } catch (error) {
    reportFailure({ event: 'after-capture-reveal-failed', filePath: result.filePath, error })
  }
  return result
}

function defaultFailureReporter(failure: AfterCaptureFailure): void {
  console.warn('After-capture action failed.', failure)
}
