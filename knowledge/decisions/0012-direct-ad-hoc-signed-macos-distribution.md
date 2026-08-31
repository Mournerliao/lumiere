# 0012: Direct Ad-Hoc-Signed macOS Distribution

Date: 2026-08-31

## Decision

Lumiere will publish its normal macOS releases directly through GitHub Releases rather
than the Mac App Store or Homebrew. The application bundle will use a stable bundle
identifier, be ad-hoc signed as one coherent Electron/Swift bundle, and ship in a
downloadable disk image with a published SHA-256 checksum. It will not require an Apple
Developer Program membership, Developer ID certificate, or Apple notarization.

The macOS release is a normal supported Lumiere release, not a Developer Preview. Because
Gatekeeper cannot establish a known developer for an ad-hoc signature, the installation
instructions and clean-machine verification must include Apple's supported Privacy &
Security → Open Anyway flow. Product language must not describe the artifact as Developer
ID signed, notarized, Apple verified, or free of the manual first-launch exception.

## Context

Lumiere does not plan to use the Mac App Store and does not currently have an Apple
Developer Program membership. Developer ID signing and notarization would give ordinary
Gatekeeper launch behavior outside the Store, but they require a paid membership. Homebrew
can transport an application but cannot supply the missing Gatekeeper identity, so adding a
tap would not remove the manual trust step and is not part of the release path.

## Consequences

- Milestone 1D owns reproducible packaging, ad-hoc signing, the disk image, checksum,
  installation guidance, upgrade behavior, and clean-machine verification.
- Every bundled executable, including Electron helpers and `LumiereMacHost`, must be
  covered by the final ad-hoc code-signing pass and verified before release.
- Screen Recording permission and settings persistence must be exercised across install,
  replacement upgrade, uninstall, and reinstall using the stable bundle identity.
- A future Developer ID and notarization upgrade may reuse the same bundle identity and
  direct-release channel, but it is not an MVP exit criterion.
