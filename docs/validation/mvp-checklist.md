# MVP Validation Checklist

This checklist is the lightweight release gate for the HDR-aware MVP. It proves the supported screenshot workflow is usable and honest; it does not certify universal HDR preservation.

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
- Whether a visual difference appears to come from Lumiere's generated artifact or from the receiving target app's later processing.

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
| HDR visual match | Capture HDR content with bright highlights through clipboard, folder, and both-target output where configured. | Must avoid obvious overexposure, washed-out output, or gray output; should preserve usable shadow detail and overall contrast; saturation and fine highlight detail can be iterated after MVP if limitations are recorded. | NOT RUN |
| HDR honesty | Test with HDR enabled if available. | UI does not overclaim HDR-preserved output. | NOT RUN |
| SDR/degraded honesty | Test with HDR disabled or unavailable if possible. | UI reports unavailable/degraded without claiming HDR-ready. | NOT RUN |
| Repeat loop | Run 10 capture/cancel/output cycles. | No stuck state, stale overlay, or obvious resource growth. | NOT RUN |
| Exit | Quit from idle and after capture. | App exits cleanly. | NOT RUN |

## Visual Match Scenes

Use a small fixed scene set when tuning or validating sRGB Visual Match:

- Bright HDR scene: HDR video, game, or web content with strong highlights. Check for obvious overexposure, washed-out output, gray output, and lost highlight shape.
- Dark scene: dark UI, night scene, or low-key media. Check that shadow detail remains usable and the image is not crushed into black.
- Everyday desktop scene: normal desktop, browser, or app UI. Check that text, white surfaces, colors, and overall contrast look natural.

These scenes are a lightweight regression anchor for the MVP. They do not create a broad viewer, display-topology, or HDR-preserved export matrix.

## Target App Boundary

Lumiere is responsible for producing a consistent sRGB Visual Match artifact for clipboard, folder, and both-target output. If that artifact is obviously overexposed, washed out, gray, or inconsistent between output targets, treat it as a Lumiere issue. If the artifact looks correct in a normal file viewer but a receiving app later compresses, recolors, or otherwise changes it, record that as a target app limitation.

## Release Rule

The MVP can ship with documented limitations if the supported paths above are usable and the UI/release copy does not claim unsupported HDR preservation.

Do not ship the MVP by recording a limitation when any of these blocking failures are present:

- Bright HDR scenes produce obviously overexposed output, large dead-white regions, washed-out output, or gray output.
- Everyday desktop scenes produce ordinary UI that is visibly too dark, color-shifted, or collapsed in contrast.
- Clipboard and folder output for the same capture show obvious visual drift caused by Lumiere's conversion or encoding path.

Acceptable limitations are narrower: a receiving target app compresses or recolors an otherwise valid artifact, an extreme HDR scene loses fine highlight detail, or different displays produce subjective visual differences that do not break the supported path.
