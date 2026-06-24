# HDR / SDR Validation Scenarios

Updated: 2026-06-22

This document defines the standard Windows manual validation content and execution flow for **Public perfect-HDR-fidelity**. It is the shared scenario set for Epic 12 / Story 12-1 and should be used alongside:

- `release-validation-checklist.md` for release-gate status
- `overlay-validation.md` for overlay interaction details
- `output-validation.md` for output profile and viewer-specific behavior
- `settings-accessibility-validation.md` for settings-shell keyboard, screen reader, high contrast, and DPI checks

Use `templates/hdr-sdr-validation-session-template.md` to record a session. Lumiere also seeds this template into the local Windows validation workspace at `%LOCALAPPDATA%\Lumiere\validation\output\templates\hdr-sdr-validation-session-template.md` so a tester can start from the same machine-local folder that stores output-validation JSON evidence.

## Goal

Every Windows manual validation session should answer the same core questions:

1. Did Lumiere preserve honest target-aware HDR trust for the capture target actually used?
2. Did the overlay remain legible and stable across bright, dark, SDR, HDR, and mixed-display conditions?
3. Did output behavior separate artifact success, visual match, and HDR-preserved claims correctly?
4. Did settings, DPI, and accessibility behaviors remain usable under public-release conditions?

For target-specific trust-state verification, also run `target-aware-hdr-validation.md`.

## Session Metadata

Every recorded session should include:

- Date
- Tester
- Build / commit
- Windows version and edition
- Device name
- GPU model and driver version if known
- Display list with connection type, resolution, HDR capability, HDR on/off state, and desktop placement
- DPI scale(s)
- Target app names and versions
- Whether the session covered fullscreen capture, region capture, or both

## Standard Content Set

Use the same content families in every serious fidelity run. Real files, apps, or URLs can vary, but the *kind* of content must remain comparable across runs.

### Content Family A: Bright Highlight Stress

Purpose: validate clipping, overlay legibility, and trust state around intense highlights.

Required characteristics:

- Small bright highlight against darker background
- Visible gray ramp or mid-tone area near the highlight
- White UI chrome or text somewhere in the source content

Examples:

- HDR desktop wallpaper with sun reflection or specular light
- HDR photo in Windows Photos with visible bright highlight detail
- HDR video paused on a high-brightness frame

Checklist coverage:

- `REL-HDR-01`
- `REL-HDR-05`
- `REL-CAP-04`

### Content Family B: Dark Scene Stress

Purpose: validate low-luminance detail, crop-border visibility, and status readability against dark content.

Required characteristics:

- Mostly dark scene
- At least one near-black shadow transition
- A small brighter accent or highlight

Examples:

- Dark game scene
- Low-key HDR photo
- Night city or dark UI surface with highlights

Checklist coverage:

- `REL-HDR-05`
- `REL-CAP-04`
- `REL-CAP-05`

### Content Family C: SDR / HDR Mixed Desktop

Purpose: validate target-aware state and honest status copy when the desktop includes different content types.

Required characteristics:

- One SDR-looking app surface
- One HDR-capable viewer or window
- Region capture that can target different on-screen areas

Examples:

- Browser + Windows Photos
- SDR app window on HDR desktop
- Mixed-content desktop with HDR video or image viewer

Checklist coverage:

- `REL-HDR-01`
- `REL-HDR-03`
- `REL-HDR-04`

### Content Family D: Browser / Media / Game Scenarios

Purpose: validate that Lumiere does not accidentally overclaim fidelity across common public-facing content categories.

Required scenario types:

- Browser content
- Media viewer content
- Fullscreen or disruptive content such as a game or exclusive-feeling app where feasible

Checklist coverage:

- `REL-HDR-04`
- `REL-HDR-05`
- `REL-OUT-03`

### Content Family E: Output Target Apps

Purpose: validate artifact acceptance separately from visual match and HDR-preserved claims.

Minimum target apps:

- Microsoft Paint
- Windows Photos
- Microsoft Edge

Optional additions when relevant:

- Snipping Tool
- Explorer preview
- A known HDR-capable viewer

Checklist coverage:

- `REL-OUT-01`
- `REL-OUT-02`
- `REL-OUT-03`
- Public gate: Supported output compatibility matrix

## Display Topology Matrix

Prefer recording results against these topology buckets:

1. Single HDR-capable display with Windows HDR enabled
2. Single HDR-capable display with Windows HDR disabled
3. Single SDR-only display
4. Mixed HDR + SDR multi-monitor desktop
5. Multi-monitor same-DPI
6. Multi-monitor mixed-DPI

If a topology is unavailable, record it explicitly as `NOT RUN` rather than silently omitting it.
When writing an output validation JSON artifact, include the tested bucket label in `displaySetup` when possible, for example `Topology: Mixed HDR + SDR multi-monitor desktop; HDR primary, SDR secondary`. Lumiere's loaded-evidence summary uses those labels and DPI/display hints to show which topology buckets are still missing.

## Standard Execution Flow

Run the same high-level flow for each topology you can test.

1. Record session metadata and current display topology.
2. Choose one Bright Highlight Stress scene and one Dark Scene Stress scene.
3. Validate fullscreen and region capture entry without picker-first interruption.
4. Validate overlay legibility and paused-frame crop interaction over both bright and dark content.
5. Validate target-aware HDR state against the active capture target, not the desktop in general.
6. Validate clipboard output to the minimum target-app set.
7. Validate folder output and both-target output where supported.
8. Validate settings/export profile honesty and the native accessibility path through the settings shell.
9. Record any limitation separately for:
   - artifact written/copied
   - visual match
   - HDR-preserved behavior
   - accessibility/readability
10. Link evidence files, screenshots, or notes before closing the session.

## Required Result Language

When recording results, separate the following concepts:

- `Artifact success` - file/clipboard write happened
- `Visual match` - output looked acceptably similar in the tested viewer
- `HDR preserved` - only if supported profile contract and viewer evidence justify it
- `Unvalidated` - scenario was not proven
- `Limitation` - scenario worked with a clearly documented constraint

Do not collapse these into a single generic “worked” result.

## Scenario Map To Release Checklist

| Scenario family | Primary checklist rows |
|---|---|
| Bright Highlight Stress | `REL-HDR-01`, `REL-HDR-05`, `REL-CAP-04` |
| Dark Scene Stress | `REL-HDR-05`, `REL-CAP-04`, `REL-CAP-05` |
| SDR / HDR Mixed Desktop | `REL-HDR-01`, `REL-HDR-03`, `REL-HDR-04` |
| Browser / Media / Game | `REL-HDR-04`, `REL-HDR-05`, `REL-OUT-03` |
| Output Target Apps | `REL-OUT-01` through `REL-OUT-08` |
| Settings / Accessibility | `REL-HDR-06`, `REL-A11Y-01` through `REL-A11Y-05`, `REL-SET-01` through `REL-SET-06` |

## Exit Criteria For A Useful Session

A validation session is useful only if it records:

- At least one bright scene and one dark scene
- At least one capture entry point
- At least one output target app
- Explicit display topology and HDR mode
- DPI scale
- Observed result using the fidelity language above

If any of those are missing, the session is incomplete for Public perfect-HDR-fidelity evidence.
