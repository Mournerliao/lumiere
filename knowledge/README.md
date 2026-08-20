# Lumiere Knowledge Map

`knowledge/` is Lumiere's repository-visible, tool-agnostic source of project
truth. Start with this map and disclose detail only when the current task needs it.

## Resume Work

1. Read [`state/CURRENT.md`](state/CURRENT.md).
2. Read the current GitHub Issue, if one exists.
3. Follow only the contract, ADR, or runbook links relevant to that Issue.

## Ownership Map

| Question | Source of truth |
|---|---|
| What product are we shipping? | [`contracts/product.md`](contracts/product.md) |
| What can the product honestly claim? | [`contracts/claims.md`](contracts/claims.md) |
| Where do platform APIs and dependencies belong? | [`contracts/architecture.md`](contracts/architecture.md) |
| How should engineering work be performed? | [`contracts/engineering.md`](contracts/engineering.md) |
| How should the shared desktop UI look and behave? | [`contracts/ui.md`](contracts/ui.md) |
| What phase are we in and what is the frontier? | [`state/CURRENT.md`](state/CURRENT.md) |
| What route and milestone gates are we following? | [`roadmap.md`](roadmap.md) |
| Why was a durable choice made? | [`decisions/`](decisions/) |
| How is shared-shell development performed? | [`runbooks/cross-platform-development.md`](runbooks/cross-platform-development.md) |
| How is Windows native development performed? | [`runbooks/windows-development.md`](runbooks/windows-development.md) |
| What exact checks apply to this platform? | [`runbooks/`](runbooks/) |

## Maintenance Rules

- GitHub Issues own executable tasks, acceptance criteria, dependencies, and task status.
- `CURRENT.md` owns only the one-screen project snapshot and contains no history.
- Contracts own stable invariants, not progress updates.
- ADRs own important decision rationale, one decision per file.
- GitHub Issues and CI own observed verification; the repository contains no reusable
  validation template until the release-validation phase is redesigned.
- Git owns change history. Do not duplicate it with session or loop logs.
- Update only the source that owns the changed fact, and delete superseded documents.
