# Validation Evidence

This directory owns observed release and hardware validation. It does not own task
status, product requirements, or general engineering guidance.

Truth-level definitions live in `knowledge/contracts/engineering.md`.

Create one release-candidate record from
[`templates/mvp-release-evidence-template.md`](templates/mvp-release-evidence-template.md),
place it under a date-and-commit directory, and replace every placeholder and `NOT RUN`
row with an observation. Templates and drafts never count as passing evidence.

Supporting screenshots or logs may accompany the record when useful. Identify the
commit, operating system/build, GPU, display, HDR state, target application/version,
and output target for each platform. Separate a Lumiere artifact defect from later
receiving-app processing, and never use one platform's result as evidence for another.
