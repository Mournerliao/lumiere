# 0004: Traditional Windows Setup Installer For MVP

Date: 2026-07-03

## Decision

Lumiere's MVP will target a traditional Windows setup installer flow rather than pure MSIX as the primary user-facing distribution path. The intended artifact is a setup `.exe` that can install Lumiere on a non-development Windows machine, support a user-selectable installation directory, create normal launch/uninstall entries, and handle required runtime prerequisites.

## Context

The desired user experience is the familiar desktop software flow: download an installer, choose an install location, install, launch, upgrade, and uninstall. MSIX offers clean per-user deployment, package identity, differential updates, and strong install/uninstall behavior, but it does not match the classic custom-install-directory setup flow as directly as a traditional installer. The final Electron application will bundle its Windows native host and must install both as one coherent product.

Windows Graphics Capture can suppress its system capture border only after an identity-bearing app declares `graphicsCaptureWithoutBorder` and receives user consent. Lumiere therefore needs package identity for borderless capture even though a full MSIX package remains the wrong primary installer experience. An external-location sparse package supplies that identity without taking ownership of the installed binaries or replacing the traditional setup executable.

## Consequences

- Packaging work should focus on a setup `.exe` / traditional installer flow for MVP.
- The installer must explicitly define ownership of app files, shortcuts, Start Menu entry, uninstall entry, runtime prerequisites, upgrade behavior, and install path selection.
- Milestone 1D must add, sign, register, upgrade, and remove an external-location sparse package that identifies the installed Electron application and Windows native Host and declares `graphicsCaptureWithoutBorder`.
- The Windows Host requests borderless-capture consent through `GraphicsCaptureAccess` before disabling `GraphicsCaptureSession.IsBorderRequired`. User denial, unavailable identity, or an unsupported Windows build must retain the system capture border without preventing capture.
- Development and unpackaged builds keep the system border; borderless behavior is not complete until the signed installer path passes clean-machine install, consent, capture, upgrade, and uninstall verification.
- Full MSIX can remain a future option for Store or enterprise distribution, but it is not the MVP's primary installer target.
