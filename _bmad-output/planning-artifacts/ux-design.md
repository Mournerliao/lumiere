# Lumiere MVP UX Specification

## Source Inputs

- PRD user journeys and NFR21/NFR22/NFR24.
- `UX-DR1` through `UX-DR20` from `epics.md`.
- Architecture guidance for native WinUI/Fluent implementation.
- `harness/design/v0-mvp-reference` as UX reference only.

## Main Panel

| State / mode | User sees | Primary actions | Non-color cues |
|--------------|-----------|-----------------|----------------|
| Idle, HDR ready | Branding, HDR summary “ready”, shortcut labels | Fullscreen capture, region capture, settings | Icon + text for HDR; capture buttons enabled |
| Idle, HDR not enabled | Summary prompts to enable HDR (per PRD vocabulary) | Same; capture may proceed with trust states | Distinct copy from “ready”; no success styling for degraded paths |
| Idle, capture unavailable / degraded | Concise status + optional alert per FR12/FR13 | Capture disabled or gated; settings available | Warning/info glyph + label; not only red/green |
| Active capture (from this entry) | Session indicator; consistent with overlay/tray | Cancel path where applicable | Text names state (“Capturing…”) |

- Required content: compact layout, capture entries, HDR status summary, settings entry, current shortcut display (per PRD/Epic 5).
- Capture button states: enabled only when session rules allow; disabled with short reason string when not.

## Settings

| Area | Before Epic 6/7 behavior exists | After behavior lands |
|------|----------------------------------|----------------------|
| Shortcuts | Controls read-only, disabled, or labeled pending registration (Story 5.3) | Editable; conflicts surfaced per Epic 7 |
| Output target, path, timestamp, copy-as-image, after-capture | Hidden, disabled, read-only, or scoped pending Epic 6 (Story 5.4) | Enabled controls consumed by output pipeline |
| HDR alerts | Toggle persists; affects alert surfacing | Same |
| About | Name, version, description from build metadata | Same |

- Shortcut controls and pending-registration states must never imply active hotkeys when Epic 7 is not done.
- Output controls must not imply clipboard HDR preservation (NFR8); copy-as-image is usability-only unless validated.

## Tray Menu

| State | Commands | HDR summary |
|-------|----------|-------------|
| Background idle | Fullscreen, region, open main, settings, quit | Same vocabulary as main panel |
| Capture active | Reflect session; avoid duplicate capture | Degraded/unsupported/failed wording matches FR11 |
| Degraded / blocked | Actionable tray copy; optional disable | Matches trust model |

- Required commands: capture entries, HDR status, settings, open main window, quit (per PRD/Epic 7).
- Tray and main window share one session truth (FR41).

## Overlay

| Phase | User action | Feedback |
|-------|-------------|----------|
| Drag select | Pointer drag | Continuous crop visuals (NFR2); invalid geometry flagged |
| Invalid / too-small | Adjust or retry | No output; recoverable messaging (FR19) |
| Valid release | Release pointer | Proceed to capture/output per settings |
| Escape / cancel | Keyboard or control | Overlay closes; idle recoverable (FR18) |
| Degraded / unsupported / failed | — | Status chrome + trust language; interactive crop rules per FR21 |

- Region selection states: active, invalid-region, completed, canceled, degraded, unsupported, failed (FR20).
- Release-to-capture only on valid region (FR17).

## Status and Copy Inventory

Use **text + icon** (or glyph) for every row; do not rely on hue alone (NFR21).

| User-facing label (intent) | Typical role | Notes |
|-----------------------------|--------------|--------|
| HDR ready | Success path | Evidence-backed only |
| Enable HDR | Actionable | Points user to system HDR |
| HDR unavailable | Blocked / honest | No HDR-equivalent claim |
| Degraded preview | Trust warning | Not “success” |
| Unsupported capture | Trust warning | |
| Preview failed | Error | Recoverable where possible |
| Output complete | Completion | Which targets succeeded (FR24) |
| Output failed | Failure | Which target; retry/settings (FR25) |
| Partial output success | Mixed result | Per-target clarity |

## Accessibility and Review Criteria

- State discrimination cannot rely on color alone.
- Text/icon alternatives required for key statuses.
- Capture flow must remain low-interruption.
- Settings must not imply unsupported behavior (NFR24).
- UX review should walk this document against a rendered state checklist before Epic 5–8 acceptance.
