# Story 1.3: Create D3D11 Device and WinRT/DXGI Interop Bridge

Status: done

<!-- Rewritten in English on 2026-05-04 to remove mojibake/encoding-corrupted text. -->

## Story

As a developer,
I want a narrow interop bridge for Direct3D, DXGI, WinRT, and COM objects,
so that capture and rendering code can share GPU resources without leaking native details into UI code.

## Acceptance Criteria

1. Given the graphics infrastructure is initialized, when the D3D11 device provider is created, then it creates a device/context suitable for WGC and DXGI swap-chain rendering.
2. Given WGC requires a WinRT Direct3D device, when the interop bridge wraps the DXGI device, then it returns a WinRT-compatible Direct3D device through a narrow infrastructure API.
3. Given interop calls fail, when HRESULT or COM failures occur, then diagnostics include operation name, stage, and technical detail.

## Tasks / Subtasks

- [x] Verify Story 1.1 and Story 1.2 prerequisites. (AC: 1, 2, 3)
  - [x] Confirm the solution, shared build files, `Lumiere.Graphics`, `Lumiere.Infrastructure`, and `tests/Lumiere.Graphics.Tests` exist.
  - [x] Confirm target/runtime settings remain `net10.0-windows10.0.19041.0`, `x64`, and `win-x64`.
  - [x] Confirm `HdrConstants` and `PreviewReadinessStatus` exist before adding device/interop code.
- [x] Add the D3D11 device provider inside `Lumiere.Graphics`. (AC: 1)
  - [x] Add `GraphicsDeviceProvider`, `GraphicsDeviceResources`, `GraphicsDeviceCreationOptions`, and device failure types.
  - [x] Create a BGRA-capable Direct3D 11 device and immediate context suitable for WinUI/DXGI presentation and WGC interop.
  - [x] Expose the selected feature level, D3D11 device, immediate context, and DXGI device through strongly typed resources.
  - [x] Make ownership and disposal deterministic.
- [x] Add WinRT/DXGI interop helpers inside `Lumiere.Infrastructure`. (AC: 2, 3)
  - [x] Wrap `CreateDirect3D11DeviceFromDXGIDevice` behind `Direct3D11Interop`.
  - [x] Keep COM pointer/HRESULT handling behind infrastructure APIs.
  - [x] Map native failures to `NativeInteropException` with operation, stage, and technical detail.
- [x] Add focused tests. (AC: 1, 2, 3)
  - [x] Test device creation options and guardrails.
  - [x] Test diagnostic mapping for graphics/interop failures where possible.
  - [x] Avoid pretending real HDR presentation or WGC capture was validated by unit tests.
- [x] Validate the story output. (AC: 1, 2, 3)
  - [x] Run restore, build, graphics tests, and format verification on Windows.

## Dev Notes

### Story Scope

This story establishes the GPU device and interop foundation needed by later WGC and swap-chain stories. It does not create a WGC frame pool, attach a swap chain, present live frames, implement crop UI, or add export/clipboard/hotkey/tray/annotation/history behavior.

### Architecture Compliance

- `Lumiere.Graphics` owns D3D11 device creation, feature-level selection, graphics-stage readiness, and graphics resource disposal.
- `Lumiere.Infrastructure/Interop` owns native interop declarations, WinRT/DXGI wrapping, COM/HRESULT translation, and narrow native failure types.
- `Lumiere.Capture` may later consume the WinRT Direct3D device but must not own the device provider.
- `Lumiere.App` and `Lumiere.Overlay` must not create D3D11 devices or manipulate COM pointers directly.

### Technical Guardrails

- Device creation must include BGRA support for WinUI/DXGI presentation compatibility.
- Resource ownership must be explicit and disposable.
- Interop failure paths must not be hidden behind null returns or generic booleans.
- Successful device creation is initialization evidence only; it does not mean the live preview is HDR-ready.
- The implementation must continue using the existing HDR constants from Story 1.2.

### Validation Notes

Windows validation was recorded as passed for the implementation:

- `dotnet restore Lumiere.sln --disable-parallel --verbosity minimal /nr:false`
- `dotnet build Lumiere.sln -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet test tests/Lumiere.Graphics.Tests/Lumiere.Graphics.Tests.csproj -p:Platform=x64 --no-restore --verbosity minimal /nr:false`
- `dotnet format Lumiere.sln --verify-no-changes --verbosity minimal`

Manual HDR preview validation was not in scope for this story.

## Dev Agent Record

### Agent Model Used

GPT-5

### Completion Notes List

- Implemented D3D11 device provider and strongly typed device resource ownership.
- Implemented WinRT/DXGI bridge through infrastructure interop APIs.
- Added native interop failure diagnostics with operation/stage/detail.
- Added focused graphics device tests.
- Kept WGC frame pool, swap-chain presentation, and live preview out of scope.

### File List

- src/Lumiere.Graphics/Devices/GraphicsDeviceCreationOptions.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceException.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs
- src/Lumiere.Graphics/Devices/GraphicsDeviceResources.cs
- src/Lumiere.Infrastructure/Interop/Direct3D11Interop.cs
- src/Lumiere.Infrastructure/Interop/NativeInteropException.cs
- src/Lumiere.Infrastructure/Lumiere.Infrastructure.csproj
- tests/Lumiere.Graphics.Tests/Devices/GraphicsDeviceProviderTests.cs
- _bmad-output/implementation-artifacts/1-3-create-d3d11-device-and-winrt-dxgi-interop-bridge.md
- _bmad-output/implementation-artifacts/sprint-status.yaml

### Change Log

- 2026-04-22: Implemented D3D11 device provider, WinRT/DXGI interop bridge, diagnostics mapping, focused tests, and marked story ready for review.
- 2026-04-22: Resolved review finding to ensure device creation cannot disable BGRA support.
- 2026-05-04: Rewrote story document in English to remove mojibake text.

### Review Findings

- [x] [Review][Patch] Ensure device creation cannot disable BGRA support [src/Lumiere.Graphics/Devices/GraphicsDeviceProvider.cs:31]
