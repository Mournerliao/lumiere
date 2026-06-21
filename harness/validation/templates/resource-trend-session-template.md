# Resource Trend Validation Session Template

Use this template for Story `12-3` runs and for the `Long-run lifecycle evidence` release gate.

## Session Metadata

- Date:
- Tester:
- Build / commit:
- Windows version:
- Device:
- GPU:
- Display setup:
- HDR state:
- DPI scale(s):
- Lumiere process ID:
- Output configuration:

## Sampler Configuration

- Command:
- Duration seconds:
- Sample interval seconds:
- Output directory:
- CSV path:
- Summary JSON path:
- GPU counter availability:

## Cycle Plan

| Phase | Scenario mix | Planned count | Completed count | Notes |
|---|---|---|---|---|
| Phase 1 | Warm-up / startup baseline |  |  |  |
| Phase 2 | Region capture to output |  |  |  |
| Phase 3 | Region cancel / invalid crop recovery |  |  |  |
| Phase 4 | Fullscreen capture |  |  |  |
| Phase 5 | Clipboard / folder / both-target output mix |  |  |  |
| Phase 6 | Extra release-candidate cycles if applicable |  |  |  |

## Checklist Rows Covered

- `REL-STAB-01`:
- `REL-STAB-02`:
- `REL-STAB-03`:
- `REL-STAB-04`:
- Public gate `Long-run lifecycle evidence`:

## Metric Summary

| Metric | Baseline | Final | Delta | Min | Max | Classification | Notes |
|---|---|---|---|---|---|---|---|
| Handles |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| Private bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| Threads |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| Working set bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| Paged memory bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| GPU dedicated usage bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| GPU shared usage bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |
| GPU total committed bytes |  |  |  |  |  | PASS / PASS with limitation / FAIL / NOT RUN |  |

## Runtime Observations

- Stuck state or crash observations:
- Overlay teardown observations:
- Output teardown observations:
- Warm-up or stabilization notes:
- Suspected leak pattern:

## Evidence Paths

- Logs:
- Screenshots:
- Video:
- Additional notes:

## Final Result

- Session classification: PASS / PASS with limitation / FAIL / NOT RUN
- Release impact:
- Known limitations:
- Follow-up stories / issues:
