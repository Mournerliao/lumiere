# 0005: Contract, Frontier, And Evidence Workflow

Date: 2026-08-03

## Decision

Lumiere uses a thin, risk-adaptive agent harness based on **Contract → Frontier →
Evidence**. Repository Markdown remains tool-agnostic; GitHub Issues are the task
control plane; Git is history; observed release results are separate evidence.

## Context

The previous BMAD workflow generated role, planning, story, sprint, and status artifacts
that consumed context and duplicated project state. A smaller replacement checklist
still mixed roadmap, task status, implementation history, and verification confidence.
Root product/design/context files also duplicated the knowledge base and existed mainly
to satisfy a vendored tool.

Research into current harness engineering favors small entry maps, progressive context
disclosure, machine-enforced feedback, structured handoff for long work, and complexity
that increases only when the task exceeds a model's reliable solo range.

## Consequences

- `AGENTS.md` is a stable map rather than a complete manual.
- Agents read `CURRENT.md` and task-linked context instead of the entire knowledge base.
- GitHub Issues own non-trivial tasks, acceptance criteria, dependencies, and status.
- The repository does not maintain duplicate backlog, notes, sprint, checkbox plan, or session logs.
- Repository, Windows, and hardware truth levels remain separate.
- Planner/evaluator/sub-agent/Ralph mechanisms are conditional escalation tools.
- Superseded docs and scaffolding are deleted rather than kept as a compatibility layer.
