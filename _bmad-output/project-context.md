---
project_name: 'lumiere'
user_name: 'lumiere'
date: '2026-05-09'
sections_completed:
  - discovery
  - technology_stack
  - language_specific_rules
  - framework_specific_rules
  - testing_rules
  - code_quality_rules
  - development_workflow_rules
  - critical_dont_miss_rules
existing_patterns_found: 9
status: 'complete'
rule_count: 55
optimized_for_llm: true
---

# Project Context for AI Agents

_This file contains critical rules and patterns that AI agents must follow when implementing code in this project. Focus on unobvious details that agents might otherwise miss._

---

## Technology Stack & Versions

- Language/runtime: C# on .NET 10.
- Target framework: `net10.0-windows10.0.19041.0`.
- Platform: Windows x64 only; `PlatformTarget=x64`, `Platforms=x64`, `RuntimeIdentifier=win-x64`.
- UI stack: WinUI 3 with Windows App SDK `1.8.260317003`.
- Capture stack: Windows Graphics Capture (WGC).
- Graphics stack: Direct3D 11 and DXGI through Vortice `3.8.3`.
- Interop stack: WinRT/COM/Win32 interop isolated behind infrastructure boundaries.
- Logging: `Microsoft.Extensions.Logging.Abstractions` `9.0.4` via `LumiereLoggerFactory`.
- Testing: xUnit `2.9.3`, xUnit runner `3.1.5`, Microsoft.NET.Test.Sdk `18.4.0`.
- Build configuration: nullable reference types enabled, implicit usings enabled, deterministic builds enabled, central package management enabled.
- Production code must not adopt the React, Tailwind, shadcn, Radix, Next.js, Electron, Tauri, WPF, WinForms, or web stack from design references.

## Critical Implementation Rules

### Language-Specific Rules

- Use C# nullable-aware APIs; do not silence nullability warnings by widening contracts or using nullable booleans for lifecycle state.
- Prefer immutable records or focused value types for capture state, readiness, crop geometry, diagnostics evidence, and result payloads.
- Use typed result/state models for expected outcomes: unsupported, degraded, failed, canceled, disposed, output-complete, and output-failed.
- Exceptions are appropriate for invariant violations, programming errors, invalid native resource state, and unrecoverable interop failures, not routine user cancellation.
- Keep user-facing text out of low-level capture, graphics, and interop types unless the existing typed status model already carries a concise user message.
- Use `ILogger` through `LumiereLoggerFactory`; never add `Console.WriteLine` for product diagnostics.

### Framework-Specific Rules

- Preserve the HDR-first capture and preview path: WGC `R16G16B16A16Float`, DXGI `R16G16B16A16_Float`, scRGB `RgbFullG10NoneP709`, and GPU-resident preview.
- Treat public perfect-HDR-fidelity release as stricter than MVP feature completion: public claims require target-aware HDR detection, output profile contracts, compatibility evidence, and Windows manual validation.
- Never introduce `BitmapImage`, `SoftwareBitmap`, GDI, WIC, CPU bitmap readback, SDR texture fallback, or ordinary XAML `Image` presentation as the authoritative live preview path.
- Keep platform APIs in their owning modules: WGC/session lifecycle in `Lumiere.Capture`, D3D11/DXGI/HDR constants in `Lumiere.Graphics`, WinRT/COM/Win32 interop in `Lumiere.Infrastructure`, overlay/crop UI in `Lumiere.Overlay`, local preferences in `Lumiere.Settings`.
- `Lumiere.App` may orchestrate windows and workflows, but it must not own COM pointers, HMONITOR/HWND semantics, frame pools, D3D devices, DXGI swap chains, or low-level output conversion policy.
- UI updates from capture callbacks must be marshalled to the WinUI UI thread through `DispatcherQueue` or the established equivalent.
- Capture callbacks, output completions, overlay updates, and diagnostics that can arrive late must be generation-scoped or session-token-scoped before mutating app-visible state.
- Direct monitor capture is the default MVP path. Picker-first behavior is fallback/debug only unless product requirements change.
- Clipboard output may be basic usability, but it must not be described as HDR-preserving without encoder, metadata, conversion policy, target-app compatibility, and Windows manual validation evidence.
- "Copied", "saved", "converted", and "HDR-preserved" are separate claims. Do not collapse artifact success into fidelity success.

### Testing Rules

- Protect HDR constants and readiness mapping with automated tests; changes to FP16/scRGB constants must update or break tests.
- Add hardware-independent tests for pure logic: state transitions, crop geometry, coordinate mapping, lifecycle evidence, output decisions, settings validation, and stale callback rejection.
- Keep pure capture/graphics tests in `tests/Lumiere.Graphics.Tests` while that is the established repository pattern.
- Keep overlay, crop, pointer, keyboard, and release-to-capture logic tests in `tests/Lumiere.Overlay.Tests`.
- Do not claim real WGC, DXGI, WinUI, tray, hotkey, HDR display, multi-monitor, DPI, clipboard, or file-output behavior from unit tests alone.
- Platform behavior must be recorded separately as Windows manual validation when hardware or OS integration is involved.
- Prefer tests for typed state transitions and failure recovery over tests that only assert UI strings.

### Code Quality & Style Rules

- File placement follows ownership boundaries before caller convenience; avoid adding shared helper files under `Lumiere.App` because UI happens to call them first.
- Public types, members, records, enums, methods, properties, and events use PascalCase. Private fields, locals, and parameters use camelCase following existing code.
- Use responsibility-specific names such as `CaptureSessionState`, `PreviewReadinessStatus`, `SwapChainDisposalEvidence`, and `CropCoordinateMapper`; avoid vague `Helper`, `Manager`, or `Utils` names unless the local code already establishes the pattern.
- Reuse existing typed vocabularies before adding new ones: `CaptureSessionState`, `CaptureSessionStatus`, `PreviewReadinessStatus`, `OverlayState`, and crop/result payloads.
- Do not create parallel status enums or ad hoc status strings in tray, settings, output, or UI code.
- Keep comments rare and focused on non-obvious native ownership, threading, teardown ordering, or validation rationale.
- Preserve central package management in `Directory.Packages.props`; do not pin versions in individual project files without a deliberate dependency-management change.

### Development Workflow Rules

- Validation levels are distinct: Mac edit, Windows CI-pass, and Windows manual-pass. Never collapse them into a generic "done" claim.
- Before claiming readiness, use the repository gates where applicable: restore, build, tests, and format verification from `AGENTS.md`.
- Before claiming public release readiness, use `docs/validation/release-validation-checklist.md` and require the Perfect HDR Fidelity gates to pass or be explicitly excluded from release copy.
- Windows manual validation is required for WGC timing, D3D11/DXGI/scRGB presentation, HDR displays, overlay topmost/input behavior, tray, global hotkeys, multi-monitor behavior, DPI scaling, clipboard/file output, and resource trends.
- Generated planning, story, sprint, and readiness artifacts belong in `_bmad-output/`; durable reusable guidance belongs in `harness/`.
- Preserve Epic 1-3 implementation and validation artifacts as historical foundation. Rebaselined MVP implementation planning continues from Epic 4.
- Commit messages should follow the repository convention: `feat:`, `fix:`, `docs:`, `chore:`, or `test:`.

### Critical Don't-Miss Rules

- Do not weaken the HDR invariant for implementation convenience. SDR fallback must be explicit, justified, and never silently replace the primary preview path.
- Do not let UI code directly dispose or recreate native graphics/capture resources unless ownership is explicitly delegated by the owning boundary.
- Teardown must be deterministic: frames, frame pools, sessions, swap chains, overlays, tray icons, hotkeys, and native handles need clear ownership and disposal ordering.
- Preview teardown must detach from the UI surface before releasing DXGI swap-chain resources.
- Ordinary capture stop/restart must not dispose the shared graphics device unless the app is shutting down or executing a documented device-loss recovery path.
- Overlay status updates must not resize, rescale, displace, or destabilize the preview surface or crop coordinate mapping.
- Invalid crop, Escape/cancel, failed capture startup, failed direct monitor resolution, failed overlay creation, failed clipboard write, and failed file write must leave the app in a recoverable idle or disposed state.
- Logs and diagnostics must not include screenshot pixels, raw frame dumps, or captured screen content.
- Settings must be a shared local source of truth consumed by main window, tray, hotkeys, output, and HDR alerts; do not create parallel settings state.
- The v0 MVP reference is UX guidance only. Translate layout intent, wording hierarchy, and state inventory into native WinUI/Fluent patterns without importing web dependencies.

---

## Usage Guidelines

**For AI Agents:**

- Read this file before implementing code in Lumiere.
- Follow all rules exactly as documented.
- When in doubt, prefer the more restrictive HDR, boundary, lifecycle, or validation interpretation.
- Update this file when durable implementation patterns change.

**For Humans:**

- Keep this file lean and focused on non-obvious agent guidance.
- Update it when the technology stack, package versions, module boundaries, or validation policy changes.
- Remove rules that become redundant with stronger repository automation.

Last Updated: 2026-05-09
