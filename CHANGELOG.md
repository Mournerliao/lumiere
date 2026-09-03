# Changelog

All notable user-visible changes to Lumiere are documented in this file.

## [Unreleased]

Target version: `0.2.0-preview.1`
Release platforms: Windows

### Added

- First unsigned Windows preview for x64 PCs running Windows 10 or newer.
- Assisted per-user installation with a selectable destination and desktop and Start menu shortcuts.
- Display and frozen-frame Region capture with Clipboard, folder, and combined delivery.

### Known limitations

- The installer is unsigned and Windows will show an unknown-publisher warning; verify `SHA256SUMS` before running it.
- Production borderless capture identity and automatic updates are intentionally disabled in this preview.
- HDR-preserved export is not supported; the official output is sRGB Visual Match.

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
