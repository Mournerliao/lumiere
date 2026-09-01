# Code-signing policy

Official Lumiere Windows releases are built from public tagged source by GitHub Actions.
SignPath signs only release artifacts produced by that workflow. Maintainers require
multi-factor authentication and review signing requests according to the configured
SignPath policy. Private keys are not stored in this repository or in GitHub Actions.

Security-relevant signing or release-workflow changes require maintainer review. If an
official release is suspected of compromise, publishing stops until the affected
certificate, workflow, and artifacts have been investigated and replaced or revoked.
