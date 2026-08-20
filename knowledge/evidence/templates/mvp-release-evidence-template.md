# MVP Release Evidence Template

Template status: NOT RUN. Copy this file into a date-and-commit evidence directory;
never edit the template into a passing record.

## Shared Build

- Date/tester: REPLACE
- Commit/build: REPLACE
- Electron/Node/pnpm versions: REPLACE
- Named receiving apps and versions per platform: REPLACE

| Gate | Result | Evidence/notes |
|---|---|---|
| Frozen dependency install | NOT RUN | REPLACE |
| Type check | NOT RUN | REPLACE |
| Protocol/shell tests | NOT RUN | REPLACE |
| Production shell build | NOT RUN | REPLACE |

## Windows Environment And Gates

- Windows version: REPLACE
- Device/GPU: REPLACE
- Display/topology: REPLACE
- HDR state per tested display: REPLACE

| Gate | Result | Evidence/notes |
|---|---|---|
| Native restore | NOT RUN | REPLACE |
| x64 host build | NOT RUN | REPLACE |
| Graphics tests | NOT RUN | REPLACE |
| Overlay tests | NOT RUN | REPLACE |
| Format check | NOT RUN | REPLACE |

## macOS Environment And Gates

- macOS version/build: REPLACE
- Mac model/GPU/architecture: REPLACE
- Display/topology: REPLACE
- EDR/HDR capability per tested display: REPLACE
- Screen Recording permission state: REPLACE

| Gate | Result | Evidence/notes |
|---|---|---|
| Swift host build | NOT RUN | REPLACE |
| Swift host tests | NOT RUN | REPLACE |
| Signed host/shell integration | NOT RUN | REPLACE |
| Permission-denied recovery | NOT RUN | REPLACE |

## Runtime Smoke Per Platform

Run every row independently on Windows and macOS.

| Scenario | Expected result | Windows | macOS | Notes |
|---|---|---|---|---|
| Launch | Main window opens and logging initializes | NOT RUN | NOT RUN | REPLACE |
| Region capture | Valid drag releases into output and ready state | NOT RUN | NOT RUN | REPLACE |
| Display capture | Active target is captured and output attempted | NOT RUN | NOT RUN | REPLACE |
| Cancel | Escape cancels and tears resources down | NOT RUN | NOT RUN | REPLACE |
| Clipboard | Named consumer accepts the image | NOT RUN | NOT RUN | REPLACE |
| Folder | Correctly named file is written or failure is explicit | NOT RUN | NOT RUN | REPLACE |
| Both targets | Success/partial failure is reported accurately | NOT RUN | NOT RUN | REPLACE |
| HDR honesty | UI does not imply HDR-preserved output | NOT RUN | NOT RUN | REPLACE |
| SDR/degraded honesty | Unavailable/degraded state is accurate | NOT RUN | NOT RUN | REPLACE |
| Repeat loop | 10 capture/cancel/output cycles have no stuck state or obvious growth | NOT RUN | NOT RUN | REPLACE |
| Exit | Idle and post-capture exit are clean | NOT RUN | NOT RUN | REPLACE |

## Visual Match Per Platform

Use the fixed scenes in `hdr-validation-scenarios.md` through clipboard, folder,
and both-target policies where configured. Record platform, display, receiving app,
and artifact separately; similar-looking screenshots are not cross-platform proof.

| Platform | Scene | Output target | Receiving app | Result | Artifact vs receiving-app notes |
|---|---|---|---|---|---|
| Windows | Bright HDR | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| Windows | Dark | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| Windows | Everyday desktop | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| macOS | Bright HDR | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| macOS | Dark | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |
| macOS | Everyday desktop | Clipboard/folder/both | REPLACE | NOT RUN | REPLACE |

## Limitations And Conclusion

- Known limitations: REPLACE
- Blocking failures: REPLACE_WITH_NONE_OR_DETAILS
- Repository done: NOT RUN
- Windows verified: NOT RUN
- macOS verified: NOT RUN
- Windows hardware evidenced: NOT RUN
- macOS hardware evidenced: NOT RUN
- Release conclusion: NOT RUN
