---
title: 'Epic 4 Retro Guardrails and Deferred Tracking'
type: 'chore'
created: '2026-05-13'
status: 'done'
baseline_commit: 'b006dfaefeb97a18ac6ca367bd226e88e2aaf20b'
context:
  - '{project-root}/_bmad-output/project-context.md'
  - '{project-root}/_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture.md'
  - '{project-root}/_bmad-output/planning-artifacts/epics.md'
---

<frozen-after-approval reason="human-owned intent - do not modify unless human renegotiates">

## Intent

**Problem:** Epic 4 retrospective found that the foundation is ready for Epic 5, but follow-through risks are still mostly prose: `MainWindow.xaml.cs` could keep absorbing new responsibilities, and `deferred-work.md` mixes real future work with accepted decisions and encoded/mojibake review notes.

**Approach:** Create a durable Epic 5 implementation guardrail document and rewrite deferred tracking into clear buckets: active technical debt, future-story candidates, accepted decisions, validation gaps, and documentation cleanup.

## Boundaries & Constraints

**Always:** Preserve the existing BMad artifact locations under `_bmad-output/implementation-artifacts`. Keep documents in English. Make guidance concrete enough for future story creation and code review. Keep HDR, capture, graphics, overlay, settings, output, tray, and hotkey ownership aligned with architecture boundaries.

**Ask First:** Any change to source code, sprint story statuses beyond documentation/tracking, Epic 5 story definitions, or public product requirements must be explicitly approved first.

**Never:** Do not refactor `MainWindow.xaml.cs` in this task. Do not implement settings persistence, output policy changes, tray/hotkeys, InvalidCrop tests, or Epic 8 validation work here. Do not delete historical story review records.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Epic 5 guardrail | Epic 4 retro says `MainWindow` is the main architecture pressure point | New implementation guidance states what may stay in `MainWindow` and what must move behind owning-module services/coordinators | If guidance conflicts with architecture.md, prefer architecture.md and note the constraint |
| Deferred work triage | Existing `deferred-work.md` contains mixed, mojibake-heavy items | File is reorganized into actionable buckets with owner epic/story hints and accepted-tech-debt labels | Preserve unresolved meaning; do not silently drop uncertain items |
| Future scope | Retro actions include Epic 6/8 decisions and tests | Out-of-scope items remain tracked as future-story candidates, not implemented | Mark target epic/story where known |

</frozen-after-approval>

## Code Map

- `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md` -- source of action items and architecture assessment.
- `_bmad-output/implementation-artifacts/deferred-work.md` -- current unstructured deferred item tracker to normalize.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- current sprint state; it contains pre-existing Epic 4 closure updates from the preceding retrospective task and is not modified by this spec's implementation work.
- `_bmad-output/planning-artifacts/architecture.md` -- source of module/service boundary rules.
- `_bmad-output/planning-artifacts/epics.md` -- source of target epic/story mapping for future work.
- `harness/README.md` and `harness/workflows/cross-platform-development.md` -- durable guidance locations to inspect if adding non-generated guidance would be better than another `_bmad-output` artifact.

## Tasks & Acceptance

**Execution:**
- [x] `_bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md` -- create a concise guardrail document for Story 5.1+ agents -- prevents UI work from re-centralizing settings/output/tray/hotkey behavior in `MainWindow.xaml.cs`.
- [x] `_bmad-output/implementation-artifacts/deferred-work.md` -- rewrite into clean actionable sections while preserving unresolved items -- turns retro/code-review findings into usable planning input.
- [x] `_bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md` -- append a short follow-through note pointing to the new guardrail and reorganized deferred tracker -- closes the loop without altering the retrospective findings.
- [x] `_bmad-output/implementation-artifacts/spec-epic4-retro-guardrails-and-deferred-tracking.md` -- maintain workflow status/checklist metadata for this Quick Dev run -- keeps review state aligned with the workflow.

**Acceptance Criteria:**
- Given a future agent starts Story 5.1, when they read the guardrail document, then they can tell which responsibilities may stay in `MainWindow.xaml.cs` and which must be implemented behind owning-module services or coordinators.
- Given a deferred item came from Epic 4 review or retrospective, when it remains unresolved, then `deferred-work.md` identifies whether it is active technical debt, a future-story candidate, validation work, accepted decision, or documentation cleanup.
- Given an item is intentionally not implemented now, when the task completes, then it remains visible with a target epic/story hint instead of disappearing.
- Given `sprint-status.yaml` already has approved Epic 4 retrospective closure changes from the preceding task, when this spec is reviewed, then those changes are treated as pre-existing context rather than implementation output for this spec.
- Given this is documentation/planning work, when verification runs, then repository build/tests are not required unless source files are changed; format/lint impact is checked by reviewing Markdown structure and git diff.

## Verification

**Commands:**
- `git diff -- _bmad-output/implementation-artifacts/epic-5-implementation-guardrails.md _bmad-output/implementation-artifacts/deferred-work.md _bmad-output/implementation-artifacts/epic-4-retro-2026-05-13.md` -- expected: only the planned documentation/tracking changes appear.
- `git status --short` -- expected: expected BMad artifact changes are present; `sprint-status.yaml` may still appear from the preceding Epic 4 retrospective closure.

**Manual checks:**
- Confirm the guardrail document does not introduce new architecture rules that contradict `architecture.md`.
- Confirm deferred items are not silently lost; unresolved work is either tracked or explicitly labeled as an accepted decision.

## Suggested Review Order

**Epic 5 Guardrails**

- Start with design intent.
  [`epic-5-implementation-guardrails.md:13`](epic-5-implementation-guardrails.md#L13)

- Check allowed UI scope.
  [`epic-5-implementation-guardrails.md:19`](epic-5-implementation-guardrails.md#L19)

- Check forbidden UI scope.
  [`epic-5-implementation-guardrails.md:28`](epic-5-implementation-guardrails.md#L28)

- Review Story 5 risks.
  [`epic-5-implementation-guardrails.md:52`](epic-5-implementation-guardrails.md#L52)

**Deferred Work Tracking**

- Verify active debt retained.
  [`deferred-work.md:13`](deferred-work.md#L13)

- Verify workflow gap retained.
  [`deferred-work.md:57`](deferred-work.md#L57)

- Verify validation gaps carried.
  [`deferred-work.md:115`](deferred-work.md#L115)

- Verify accepted decisions separated.
  [`deferred-work.md:129`](deferred-work.md#L129)

**Retrospective Follow-Through**

- Confirm retro loop closure.
  [`epic-4-retro-2026-05-13.md:166`](epic-4-retro-2026-05-13.md#L166)
