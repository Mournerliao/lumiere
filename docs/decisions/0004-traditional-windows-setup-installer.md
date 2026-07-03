# 0004: Traditional Windows Setup Installer For MVP

Date: 2026-07-03

## Decision

Lumiere's MVP will target a traditional Windows setup installer flow rather than pure MSIX as the primary user-facing distribution path. The intended artifact is a setup `.exe` that can install Lumiere on a non-development Windows machine, support a user-selectable installation directory, create normal launch/uninstall entries, and handle required runtime prerequisites.

## Context

The desired user experience is the familiar desktop software flow: download an installer, choose an install location, install, launch, upgrade, and uninstall. MSIX offers clean per-user deployment, package identity, differential updates, and strong install/uninstall behavior, but it does not match the classic custom-install-directory setup flow as directly as a traditional installer. Lumiere also uses WinUI 3 / Windows App SDK, so the installer must account for Windows App SDK runtime prerequisites if the app is deployed unpackaged or packaged with external location.

## Consequences

- Packaging work should focus on a setup `.exe` / traditional installer flow for MVP.
- The installer must explicitly define ownership of app files, shortcuts, Start Menu entry, uninstall entry, runtime prerequisites, upgrade behavior, and install path selection.
- MSIX can remain a future option for Store, enterprise, or package-identity-driven distribution, but it is not the MVP's primary installer target.
