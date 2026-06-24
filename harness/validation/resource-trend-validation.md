# Resource Trend Validation

Use this workflow when Story `12-3` or the Public perfect-HDR-fidelity release gate needs long-run capture/output evidence. It extends the lighter-weight checks in `lifecycle-validation.md` with repeatable sampling for private bytes, handles, threads, and GPU process memory trends.

This workflow does not itself prove HDR fidelity. It proves that repeated capture and output exercise does not quietly undermine public release with resource leakage or stuck lifecycle behavior.

## When To Use This

- Before moving Story `12-3` to `done`.
- Before counting the `Long-run lifecycle evidence` gate as anything other than `NOT RUN`.
- After changes that touch capture lifecycle, overlay teardown, swap-chain recreation, clipboard/file output, or session ownership.

## Evidence Targets

Record all of the following for each run:

- The session notes from `templates/resource-trend-session-template.md`
- The CSV sample file emitted by `scripts/collect-resource-trend-samples.ps1`
- The summary JSON emitted by `scripts/collect-resource-trend-samples.ps1`
- Relevant Lumiere logs
- Any screenshots or video used to explain a limitation or failure

## Metrics Recorded

| Metric | Why it matters | Role in release judgement |
|---|---|---|
| `Handles` | Detects leaking Win32 / COM / DXGI / WGC-adjacent object growth. | Primary |
| `PrivateBytes` | Best simple signal for retained process memory growth. | Primary |
| `Threads` | Flags worker or callback paths that fail to settle. | Secondary |
| `WorkingSetBytes` | Helps separate retained memory from paging noise. | Secondary |
| `PagedMemoryBytes` | Adds context for pageable allocation drift. | Secondary |
| `GpuDedicatedUsageBytes` | Helps catch retained dedicated GPU allocations. | Primary when non-zero |
| `GpuSharedUsageBytes` | Helps catch retained shared GPU allocations. | Primary when non-zero |
| `GpuTotalCommittedBytes` | Best aggregated GPU memory trend when available. | Primary |

## Prerequisites

1. Use a Windows machine that matches the validation session record.
2. Build the target commit and launch Lumiere normally.
3. Decide whether the run covers:
   - a `50+` cycle regression sweep, or
   - a `100+` cycle release-candidate run.
4. Pick a writable output folder for artifacts, for example:
   - `%LOCALAPPDATA%\Lumiere\validation\resource-trends`
5. Prefer targeting a specific PID if more than one `Lumiere.App` process could exist.

Lumiere's Settings > Validation surface can now help with this setup directly:

- `Create trend draft` generates a session-local markdown record in the validation workspace with the current PID, output configuration, seeded sampler command, and current-session context hints already filled in.
- `Trend template` opens the seeded session template in the local validation workspace.
- `Trend script` opens the seeded sampler script in the same workspace.
- `Copy trend cmd` copies a current-process PowerShell command that already targets the running Lumiere PID and the workspace-local `resource-trends` folder.

Use these helpers to start a run faster, but still review the copied command before execution and still record the real resulting artifacts in the session notes. A generated draft is only a structured starting point; it does not count toward Story `12-3` or the public release gate until its placeholders are replaced with real Windows manual observations.

If the workspace-local `resource-trends` folder already contains sampler `*-summary.json` files, `Create trend draft` prefers the latest readable summary whose PID matches the current Lumiere process. If no matching-PID summary exists, it may import the latest readable summary but marks the draft with a scope warning. Imported summaries fill the CSV path, summary JSON path, duration, sample interval, sample count context, and metric baseline/final/delta/min/max rows. Metric classification and session classification remain explicit `REPLACE_WITH_PASS_FAIL_LIMITATION` review fields so sampler output cannot accidentally become a passing public-release claim without human judgement.

## Sampler Command

Example:

```powershell
& .\harness\validation\scripts\collect-resource-trend-samples.ps1 `
  -ProcessName Lumiere.App `
  -DurationSeconds 900 `
  -SampleIntervalSeconds 5 `
  -OutputDirectory "$env:LOCALAPPDATA\Lumiere\validation\resource-trends"
```

If multiple instances might exist, resolve the PID first and pass `-ProcessId`.

The script produces:

- `resource-trend-<process>-pid<PID>-<timestamp>.csv`
- `resource-trend-<process>-pid<PID>-<timestamp>-summary.json`

## Recommended Run Shapes

### 50+ Cycle Regression Sweep

Use this after significant lifecycle or output changes.

Minimum coverage:

1. 20 region captures committed to output.
2. 10 region captures canceled with `Escape`.
3. 10 fullscreen captures.
4. 10 mixed output cases covering clipboard-only, folder-only, and both-target output.

### 100+ Cycle Release-Candidate Run

Use this before counting the public release gate as validated.

Minimum coverage:

1. Alternate region and fullscreen capture entry points.
2. Include committed crop, cancel, invalid crop recovery, and repeated reopen paths.
3. Include clipboard, folder, and both-target output according to the intended release configuration.
4. Include at least one save-path/open-after-capture path if file artifacts are in scope.
5. Keep the app running for the full run; do not restart between phases.

## Manual Execution Flow

1. Start Lumiere and confirm the intended display/output configuration.
2. Start the sampler script and note the exact command in the session record.
3. Execute the chosen cycle plan while also watching for:
   - stuck overlay windows
   - stale status updates from prior sessions
   - failed teardown after clipboard/file output
   - visible crashes or session dead-ends
4. Stop after the planned cycle count or sampler duration completes.
5. Copy the CSV path, summary JSON path, and relevant logs into the session record.
6. Classify the run as `PASS`, `PASS with limitation`, `FAIL`, or `NOT RUN`.

If you generate or refresh the draft after the sampler completes, verify that the imported summary paths, PID scope, and metric rows match the run you intend to count. A draft that contains a scope warning must not count toward Story `12-3` until the validator confirms that the imported summary belongs to the intended run. Keep the CSV and summary JSON files in `resource-trends\` and attach any additional screenshots, logs, or notes under the same validation workspace.

## How To Judge The Result

Use engineering judgement, but keep the classification honest and repeatable.

### PASS

Use `PASS` when all of the following are true:

- No crash, stuck session, or unrecoverable lifecycle state occurred.
- Private bytes, handles, and GPU totals do not show continuing monotonic growth through the end of the run.
- Any early warm-up growth settles into a band rather than climbing every phase.
- Final notes do not require release-copy caveats.

### PASS with limitation

Use `PASS with limitation` when:

- a metric rises during warm-up or one specific scenario, but later settles, or
- one counter is unavailable on the test machine and the limitation is recorded, or
- a bounded drift exists but does not currently block the scoped release claim.

Document the limitation precisely. "Seems okay" is not enough.

### FAIL

Use `FAIL` when any of the following are true:

- The app crashes, hangs, or leaves overlay/capture/output state stuck.
- Private bytes, handles, or GPU totals keep climbing through the last phase with no convincing stabilization.
- Resource growth is large enough that release readiness would rely on hope rather than evidence.
- The result cannot support the intended release copy without hiding real risk.

### NOT RUN

Use `NOT RUN` when the cycle plan, hardware context, or sampler artifacts are incomplete.

## Interpreting GPU Counter Gaps

Some Windows environments do not expose `GPU Process Memory(*)` counters reliably. When that happens, the sampler records GPU values as `0` and warns once.

Treat that as:

- `PASS with limitation` if CPU-process metrics are valid and the missing GPU counters are recorded explicitly.
- `NOT RUN` for the GPU portion of the gate if the release decision really depends on GPU trend evidence.

Do not silently interpret missing GPU counters as proof of zero GPU growth.

## Follow-up And Retest Triggers

Re-run this workflow after changes to:

- WGC capture session ownership or teardown
- overlay confirm/cancel/reopen behavior
- swap-chain recreate or preview detach sequencing
- clipboard/file output orchestration
- after-capture shell actions
- target-aware HDR display session routing if it changes lifecycle behavior

Also re-run when a previous session was `PASS with limitation` or `FAIL`.
