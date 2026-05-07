---
workflowType: sprint-change-proposal
project_name: lumiere
user_name: lumiere
date: 2026-05-07
status: approved
mode: batch
change_scope: minor
approved_by_user: 2026-05-07
trigger:
  - confirm status of historical implementation spec
  - avoid stale BMad guidance after canonical MVP-to-1.0 rebaseline
affectedArtifact:
  - /Users/asherliao/Projects/lumiere/_bmad-output/implementation-artifacts/spec-apply-approved-epics-proposal.md
referenceArtifacts:
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md
  - /Users/asherliao/Projects/lumiere/_bmad-output/planning-artifacts/epics.md
---

# Sprint Change Proposal: Confirm Historical Epics Spec Status

## 1. Issue Summary

The implementation artifact `spec-apply-approved-epics-proposal.md` was created on 2026-04-21 as a one-shot execution spec for applying an older approved epics proposal. After the approved 2026-05-07 canonical MVP-to-1.0 rebaseline, the active epic route changed substantially.

The risk is that future BMad or implementation agents may read the old spec as active guidance and infer outdated MVP completion semantics, especially around the older Epic 5/Epic 6 boundaries.

## 2. Impact Analysis

### Epic Impact

No active epic changes are required. The active epic structure is already defined by the canonical 2026-05-07 rebaseline:

- Epic 1-4 done means MVP feature implementation is complete.
- Epic 5 done means MVP is complete and validated.
- Epic 6 done means the 1.0 installable release is complete.

### Story Impact

No story status or story content changes are required. The next active implementation route remains the current sprint status route, with Epic 3 in progress and Story 3.6 next.

### Artifact Impact

The old spec should remain in `implementation-artifacts` as historical execution evidence. It should not be rewritten into the active planning source of truth.

The only appropriate adjustment is to mark it as superseded by:

- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md`
- `_bmad-output/planning-artifacts/epics.md`

### Technical Impact

No production code, architecture, tests, or validation workflows are affected.

## 3. Recommended Approach

Use direct adjustment with a minor documentation status update.

The recommended state for `spec-apply-approved-epics-proposal.md` is:

- Keep `status: done` because the 2026-04-21 one-shot work was completed.
- Add `supersededBy` references to the canonical 2026-05-07 rebaseline and current `epics.md`.
- Add a visible note that the file is historical execution evidence, not the active implementation route.
- Do not modify PRD, epics, architecture, UX, or sprint status as part of this correction.

Rationale: this preserves audit history while preventing stale guidance from competing with the approved 2026-05-07 route.

## 4. Detailed Change Proposal

### Implementation Artifact

File: `_bmad-output/implementation-artifacts/spec-apply-approved-epics-proposal.md`

Section: frontmatter

OLD:

```yaml
status: 'done'
route: 'one-shot'
context:
  - '_bmad-output/planning-artifacts/archive/2026-05-cleanup/sprint-change-proposal-2026-04-21.md'
```

NEW:

```yaml
status: 'done'
route: 'one-shot'
context:
  - '_bmad-output/planning-artifacts/archive/2026-05-cleanup/sprint-change-proposal-2026-04-21.md'
supersededBy:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-canonical-mvp-1-0-rebaseline.md'
  - '_bmad-output/planning-artifacts/epics.md'
```

Section: document body

Add a short superseded note immediately after the title:

```markdown
> Superseded note (2026-05-07): this file records the completed 2026-04-21 one-shot application of an older epics proposal. It is historical execution evidence, not the active implementation route. Current sprint planning should use the canonical MVP-to-1.0 rebaseline in `_bmad-output/planning-artifacts/epics.md`, with Epic 1-4 as MVP feature implementation, Epic 5 as the MVP completion gate, and Epic 6 as installer/1.0 release.
```

Section: Suggested Review Order

Add:

```markdown
Historical review order for the 2026-04-21 change only:
```

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Developer agent for documentation status update only.

Success criteria:

- The spec remains `status: done`.
- The spec clearly states that it is historical and superseded.
- The current active route remains the canonical 2026-05-07 `epics.md`.
- No active sprint status, PRD, architecture, UX, or story content is changed by this correction.

## 6. Checklist Status

- [x] 1.1 Trigger identified: historical spec status ambiguity after 2026-05-07 rebaseline.
- [x] 1.2 Core problem defined: stale BMad artifact may be mistaken for active route.
- [x] 1.3 Evidence gathered: old spec references 2026-04-21 proposal and old Epic 5/Epic 6 semantics; current epics use the 2026-05-07 canonical route.
- [x] 2.1 Current epic impact assessed: no epic content changes needed.
- [x] 2.2 Required epic changes: none.
- [x] 2.3 Remaining epics reviewed: no dependency changes.
- [x] 2.4 Future epic validity checked: no new epics needed.
- [x] 2.5 Epic order checked: no resequencing needed.
- [x] 3.1 PRD conflicts checked: none.
- [x] 3.2 Architecture conflicts checked: none.
- [x] 3.3 UI/UX conflicts checked: none.
- [x] 3.4 Other artifacts checked: only the one implementation spec needs status clarification.
- [x] 4.1 Direct adjustment evaluated: viable, low effort, low risk.
- [x] 4.2 Rollback evaluated: not viable or necessary.
- [x] 4.3 MVP review evaluated: not needed.
- [x] 4.4 Recommended path selected: direct minor status clarification.
- [x] 5.1 Issue summary created.
- [x] 5.2 Artifact adjustment documented.
- [x] 5.3 Recommended path documented.
- [x] 5.4 MVP impact defined: no MVP scope impact.
- [x] 5.5 Handoff plan established.
- [x] 6.3 User approval received on 2026-05-07.

## 7. Workflow Completion

Correct Course workflow complete.

Issue addressed: historical status ambiguity for `spec-apply-approved-epics-proposal.md` after the 2026-05-07 canonical MVP-to-1.0 rebaseline.

Change scope: Minor.

Artifacts modified:

- `_bmad-output/implementation-artifacts/spec-apply-approved-epics-proposal.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-05-07-spec-apply-approved-epics-status.md`

Routed to: Developer agent for documentation status update only.

Success criteria remain:

- The spec remains `status: done`.
- The spec clearly states that it is historical and superseded.
- The current active route remains the canonical 2026-05-07 `epics.md`.
- No active sprint status, PRD, architecture, UX, or story content is changed by this correction.
