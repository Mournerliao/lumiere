# 0006: Cross-Platform Shell And Native Capture Hosts

Date: 2026-08-20

## Decision

Lumiere will target Windows and macOS with an Electron/React application shell and
one native capture host per operating system:

- the Windows host retains the existing C# WGC, D3D11, DXGI, and color-conversion
  implementation;
- the macOS host uses Swift, ScreenCaptureKit, and native Apple color/GPU facilities;
- the shell communicates with hosts through a versioned, narrow command/result
  interface and never owns capture textures, HDR pixel conversion, or public HDR
  truth.

The product will advance through three distinct stages:

1. **Cross-platform HDR-aware MVP** — capture HDR-aware sources on both platforms
   and produce one official RGBA8/sRGB Visual Match output for clipboard and files.
2. **HDR-preserved export** — add one named, documented export path with explicit
   formats, color semantics, metadata, viewers, and per-platform hardware evidence.
3. **Cross-platform HDR fidelity** — pursue measured visual consistency across the
   supported Windows and macOS display/viewer matrix without promising universal
   identity.

Electron desktop capture, `NativeImage`, Canvas, and renderer pixels are not valid
fallbacks for the official capture pipeline. A missing native host is an explicit,
typed unavailable state.

## Context

The original native Windows architecture concentrated implementation and every
meaningful feedback loop on a Windows machine. The primary development environment
is macOS, so even ordinary layout and interaction changes incurred a delayed
cross-machine validation cycle. WinUI also provides a smaller and less observable
iteration surface for agent-assisted UI work than Chromium tooling.

The product goal now includes macOS. Apple ScreenCaptureKit provides native HDR
capture modes, while Windows Graphics Capture requires a platform-specific FP16/scRGB
pipeline. A single cross-platform UI is therefore useful, but a single web capture
implementation would erase the color and resource semantics Lumiere needs to make
honest claims.

## Consequences

- Main, settings, result, and eligible overlay presentation move to the Electron
  shell and can be developed on macOS.
- Platform capture, permissions, target discovery, GPU resources, color conversion,
  clipboard delivery, and hardware capability stay behind native host adapters.
- IPC transports commands and compact results such as status and artifact paths;
  raw HDR frames and native handles do not cross it.
- ADR 0007 removes the transitional WinUI application. The retained Windows Capture,
  Graphics, and Interop libraries remain source material for the future host adapter.
- The application accepts Electron's runtime size, process model, security updates,
  and packaging cost in exchange for a much faster shared UI feedback loop.
- Renderer sandboxing, context isolation, disabled Node integration, named preload
  methods, sender validation, and local packaged content are required invariants.
- Windows and macOS runtime/hardware truth remain separate. Passing on one platform
  never projects to the other.
- ADR 0003 continues to require one conversion result for clipboard and folder within
  each host; it does not require the two operating systems to share one binary implementation.
- ADR 0004 continues to own the Windows setup artifact. ADR 0012 owns the direct,
  ad-hoc-signed macOS distribution path and its explicit Gatekeeper boundary.
