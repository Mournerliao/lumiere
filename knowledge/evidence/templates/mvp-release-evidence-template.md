# MVP Release Evidence Template

Template status: NOT RUN. Copy this file into a date-and-commit evidence directory;
never edit the template into a passing record.

## Environment

- Date/tester: REPLACE
- Commit/build: REPLACE
- Windows version: REPLACE
- Device/GPU: REPLACE
- Display/topology: REPLACE
- HDR state per tested display: REPLACE
- Receiving apps and versions: REPLACE

## Repository And Windows Gates

| Gate | Result | Evidence/notes |
|---|---|---|
| Restore | NOT RUN | REPLACE |
| x64 build | NOT RUN | REPLACE |
| Graphics tests | NOT RUN | REPLACE |
| Overlay tests | NOT RUN | REPLACE |
| Format check | NOT RUN | REPLACE |

## Runtime Smoke

| Scenario | Expected result | Result | Notes |
|---|---|---|---|
| Launch | Main window opens and logging initializes | NOT RUN | REPLACE |
| Region capture | Valid drag releases into output and ready state | NOT RUN | REPLACE |
| Fullscreen capture | Active target is captured and output attempted | NOT RUN | REPLACE |
| Cancel | Escape cancels and tears resources down | NOT RUN | REPLACE |
| Clipboard | Named consumer accepts the image | NOT RUN | REPLACE |
| Folder | Correctly named file is written or failure is explicit | NOT RUN | REPLACE |
| Both targets | Success/partial failure is reported accurately | NOT RUN | REPLACE |
| HDR honesty | UI does not imply HDR-preserved output | NOT RUN | REPLACE |
| SDR/degraded honesty | Unavailable/degraded state is accurate | NOT RUN | REPLACE |
| Repeat loop | 10 capture/cancel/output cycles have no stuck state or obvious growth | NOT RUN | REPLACE |
| Exit | Idle and post-capture exit are clean | NOT RUN | REPLACE |

## Visual Match

Use the fixed scenes in `hdr-validation-scenarios.md` through clipboard, folder,
and both-target policies where configured.

| Scene | Output target | Receiving app | Result | Artifact vs receiving-app notes |
|---|---|---|---|---|
| Bright HDR | Clipboard | REPLACE | NOT RUN | REPLACE |
| Bright HDR | Folder | REPLACE | NOT RUN | REPLACE |
| Bright HDR | Both | REPLACE | NOT RUN | REPLACE |
| Dark | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| Everyday desktop | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |

## Limitations And Conclusion

- Known limitations: REPLACE
- Blocking failures: REPLACE_WITH_NONE_OR_DETAILS
- Repository done: NOT RUN
- Windows verified: NOT RUN
- Hardware evidenced: NOT RUN
- Release conclusion: NOT RUN
