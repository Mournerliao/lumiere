# 0009: Targeted Capture And Per-Delivery Results

Date: 2026-08-25

## Decision

Lumiere platform-host protocol v2 will make region targeting, delivery availability,
delivery results, and cancellation ownership explicit.

- `getCapabilities` reports capture modes and delivery targets separately and may
  return one active target snapshot with an opaque Host id and logical size.
- Display capture remains targetless on the wire. The Host resolves the display under
  the pointer when the command arrives.
- Region capture carries the active target id and a top-left-origin rectangle in
  target-local logical units. The Host validates target freshness and bounds, then owns
  platform coordinate and pixel conversion.
- A completed capture reports one result per requested delivery target. Completion
  proves capture and the single sRGB Visual Match conversion finished; clipboard and
  folder success or failure remain independent artifact-delivery facts.
- The shared shell owns cancellation and cleanup until it dispatches a native capture.
  The Host owns native cancellation classification and resource cleanup after dispatch.
  Protocol v2 does not add an interrupt command.

The MVP interaction paired with this contract cancels a click without a valid drag,
captures immediately on pointer release, defaults future settings to clipboard and
folder, and uses `Lumiere-yyyy-MM-dd-HHmmss.png` filenames with a numeric suffix on a
same-second collision.

## Context

Protocol v1 accepted `region` without identifying the display or coordinate space and
collapsed `both` delivery into one success artifact. That shape could not safely drive
the planned shared Overlay, detect a stale target, advertise an unavailable clipboard
adapter, or preserve one successful output when the other failed.

Passing global desktop pixels would couple the shell to native scale and coordinate
rules. Passing an Electron display id would assume an undocumented identity mapping to
Core Graphics and Windows display targets. Letting the Host open its own selection UI
would duplicate the shared product surface and its lifecycle state. An opaque target
token plus target-local logical geometry keeps those responsibilities separated.

An interrupt command was considered, but the confirmed MVP cancels selection before a
native request and captures immediately after release. Native screenshot work is
short-lived, while application shutdown already tears down the process boundary. A
concurrent interrupt protocol would add operation races before a user-visible need has
been established.

## Consequences

- v1 remains frozen for compatibility history; v2 becomes the shared/macOS protocol.
- Region Overlay work can use a stable logical rectangle without owning native scaling
  or raw pixels.
- Hosts must reject stale targets and out-of-bounds geometry with a typed failure.
- Hosts must return exactly one delivery result for every requested target, including
  when all deliveries fail after conversion.
- Product feedback can distinguish full, partial, and total delivery failure without
  exposing native diagnostics or weakening HDR language.
- If users later need to cancel an observable in-flight native operation, that change
  requires a new versioned interrupt contract and explicit race semantics.
