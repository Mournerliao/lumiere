---
title: 'Output Validation Artifact Template'
type: 'feature'
created: '2026-06-22'
status: 'done'
route: 'one-shot'
---

# Output Validation Artifact Template

## Intent

**Problem:** Lumiere can load output validation artifacts, but Windows manual validators did not yet have a tested schema v4 sample to author evidence without drifting from the runtime contract.

**Approach:** Add a safe sample artifact, teach the validation loader to treat template placeholders as incomplete manual evidence, and bind the sample to a documentation test so schema, viewer coverage, and claim blocking stay aligned with code.

## Suggested Review Order

1. [Runtime contract](../../src/Lumiere.Graphics/Output/OutputValidationSessionArtifact.cs) -- placeholder handling, string enum JSON support, and incomplete-session evidence downgrade.
2. [Template artifact](../../docs/validation/templates/output-validation-session.schema-v4.sample.json) -- schema v4 fields, named HDR viewers, target-aware HDR evidence, and default non-passing statuses.
3. [Documentation](../../docs/validation/output-validation.md) -- local copy path and warning that the sample is not passing evidence.
4. [Contract test](../../tests/Lumiere.Graphics.Tests/Output/OutputValidationDocumentationTests.cs) -- parses the sample and proves it cannot allow visual-match or HDR-preserved claims.
