# macOS Native Development Runbook

This runbook owns the Swift ScreenCaptureKit host build, protocol smoke test, runtime
launch, permission recovery, and macOS-only verification boundary.

## Prerequisites

- macOS 15 or newer
- Xcode with a matching macOS SDK and Swift toolchain
- Apple Silicon for HDR ScreenCaptureKit acquisition; Intel remains an SDR-only path
- the shared-shell prerequisites in
  [`cross-platform-development.md`](cross-platform-development.md)

Confirm the selected toolchain:

```sh
xcodebuild -version
xcrun swift --version
xcrun --sdk macosx --show-sdk-version
```

If Command Line Tools are selected instead of the installed Xcode, either update the
global selection with `xcode-select` outside this repository or scope commands with
`DEVELOPER_DIR=/Applications/Xcode.app/Contents/Developer`.

## Build And Test

From the repository root:

```sh
pnpm test:macos
swift build --package-path hosts/macos
swift test --package-path hosts/macos
```

The Vitest command owns macOS path semantics. Swift tests own the native Host. Both
run only in macOS CI and are separate from the cross-platform `pnpm test:shared`
suite. The host targets macOS 15 or newer and builds for the active architecture.
Universal distribution, signing, and notarization belong to Milestone 1D.

## Protocol Smoke Test

Build the host, then send exactly one platform-host v2 JSON Lines request:

```sh
printf '%s\n' '{"version":2,"id":"capabilities-smoke","method":"getCapabilities","params":{}}' \
  | hosts/macos/.build/debug/LumiereMacHost
```

Standard output must contain only the matching protocol response. Structured native
diagnostics belong on standard error.

## Electron Runtime

During development, point Electron at the explicit Swift build:

```sh
LUMIERE_MAC_HOST_PATH="$PWD/hosts/macos/.build/debug/LumiereMacHost" pnpm start
```

The region and display actions should become available. The active target is the display
under the pointer when the capability or display-capture request reaches the native
host, with the current main screen and then the system primary display used only as
recovery fallbacks. Region selection uses the short-lived target token and target-local
logical geometry returned through platform-host v2; the Host rejects stale targets,
topology changes, and out-of-bounds rectangles. A successful capture writes an
RGBA8/sRGB PNG under `~/Pictures/Lumiere` using the
`Lumiere-yyyy-MM-dd-HHmmss.png` rule and returns the exact path through the
platform-host interface.

## Screen Recording Permission

The native executable owns Screen Recording permission. The first display capture may
show the macOS privacy prompt. After granting permission, quit and relaunch the owning
process before retrying if macOS requests it.

For a denied or stale development identity, inspect System Settings → Privacy &
Security → Screen & System Audio Recording. `tccutil reset ScreenCapture` may reset the
development permission database, but it also removes Screen Recording grants for other
applications and should be used only as an explicit recovery action.

Command-line host, development Electron, and a future signed application may be
treated as different identities by TCC. One identity's permission result does not
verify another.

## Verification Boundary

- Swift build and tests verify repository behavior only.
- A JSON Lines smoke test verifies host startup and protocol behavior only.
- An Electron button producing a valid PNG verifies the development integration path.
- Inspect the artifact's dimensions and embedded sRGB profile separately from visual
  match.
- Record the named Mac, display, source dynamic range, scene, receiving app, and
  observed result before claiming hardware verification.
- SDR capture on an SDR display does not verify HDR acquisition or HDR-to-sRGB tone
  mapping. Windows remains independently unverified.
