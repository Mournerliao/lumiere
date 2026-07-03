# MVP Validation Checklist

This checklist is the lightweight release gate for the HDR-aware MVP. It proves the supported screenshot workflow is usable and honest; it does not certify perfect HDR preservation.

## Evidence Standard

Record:

- Date and tester.
- Build or commit.
- Windows version.
- Device, GPU, and display setup.
- HDR setting for the tested display.
- Output target: clipboard, folder, or both.
- Target apps used to paste or open output.
- Observed result and limitation notes.

## Required MVP Checks

| Area | Scenario | Expected result | Status |
|---|---|---|---|
| Build | Restore, build, tests, and format pass on Windows. | No source or formatting failures. | NOT RUN |
| Launch | Start the app from a local build. | Main window opens and logging initializes. | NOT RUN |
| Region capture | Start region capture, drag a valid crop, release. | Output starts, overlay closes, app returns to ready state. | NOT RUN |
| Fullscreen capture | Start fullscreen capture. | Active target is captured and output is attempted. | NOT RUN |
| Cancel | Press Escape before and during capture. | Capture cancels and resources tear down cleanly. | NOT RUN |
| Clipboard output | Copy output and paste into a common Windows consumer. | Consumer accepts the output or limitation is recorded. | NOT RUN |
| Folder output | Save output to the configured folder. | File is written with expected naming or failure is explained. | NOT RUN |
| Both output | Use clipboard and folder output together. | Success and partial failure are reported clearly. | NOT RUN |
| HDR honesty | Test with HDR enabled if available. | UI does not overclaim HDR-preserved output. | NOT RUN |
| SDR/degraded honesty | Test with HDR disabled or unavailable if possible. | UI reports unavailable/degraded without claiming HDR-ready. | NOT RUN |
| Repeat loop | Run 10 capture/cancel/output cycles. | No stuck state, stale overlay, or obvious resource growth. | NOT RUN |
| Exit | Quit from idle and after capture. | App exits cleanly. | NOT RUN |

## Release Rule

The MVP can ship with documented limitations if the supported paths above are usable and the UI/release copy does not claim unsupported HDR preservation.
