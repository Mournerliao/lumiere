# Changelog

All notable user-visible changes to Lumiere are documented in this file.

## [Unreleased]

## [0.1.0] - 2026-09-03

Release platforms: macOS

### Added

- First direct macOS release for Apple Silicon and Intel Macs running macOS 15 or newer.
- Display and frozen-frame Region capture with target-aware HDR status and compatible RGBA8/sRGB Visual Match output.
- Clipboard, folder, and combined delivery with configurable save location and optional reveal-after-capture behavior.
- Configurable global shortcuts, menu-bar commands, and non-blocking HDR status reminders.

### Known limitations

- The app is ad-hoc signed and not notarized, so first launch requires the documented manual Gatekeeper exception.
- HDR-preserved export is not supported; the official output is sRGB Visual Match.
- Windows release artifacts are not included in this version.

[0.1.0]: https://github.com/Mournerliao/lumiere/releases/tag/v0.1.0
