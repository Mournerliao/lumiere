# Platform Host Protocol

This directory owns the language-neutral seam between the Electron shell and each
native capture host. Versioned JSON Schemas are the source of truth; TypeScript, C#,
and Swift bindings must conform to the schema they declare.

## Transport

The protocol uses UTF-8 JSON Lines over a child process' standard input and output.
Each line is one complete request or response. Hosts reserve standard output for
protocol messages and write structured diagnostics to standard error.

Every request carries `version`, a caller-generated `id`, `method`, and `params`.
Every response echoes `version` and `id`, and contains exactly one of `result` or
`error`. The shell may have multiple requests in flight, so ordering is determined by
`id`, not line position.

Unknown versions, methods, fields, or enum values are protocol errors. Additive
changes require a new schema version when an older host cannot safely reject or ignore
them. Version 1 is frozen in [`v1.schema.json`](v1.schema.json); version 2 is the current
shared/macOS contract in [`v2.schema.json`](v2.schema.json).

## Version 2

`getCapabilities` reports capture modes and delivery targets independently. It may
also return the active capture target. A target id is an opaque, short-lived Host token,
not an Electron display id or native handle. Its HDR state and logical size describe
the same target snapshot.

Display capture has no target field. The Host resolves the display under the pointer
when the capture request reaches it, then uses the current main display and system
primary display only as recovery fallbacks.

Region capture requires the `targetId` previously returned by `getCapabilities` and a
`target-logical` rectangle:

- the origin is the target's top-left corner;
- coordinates are logical desktop units and may be fractional;
- width and height must be positive;
- the rectangle must fit within the advertised logical size;
- a stale target, changed topology, or out-of-bounds rectangle returns
  `capture-unavailable`.

The shell owns pointer geometry and the selection Overlay. Native coordinate
conversion, display scaling, pixel alignment, capture, conversion, and resource
lifetime stay inside the Host. Raw pixels and native handles never cross this seam.

### Delivery results

A `completed` capture result means acquisition and the single sRGB Visual Match
conversion completed. It does not mean every requested delivery succeeded. The
`deliveries` array contains exactly one result for each requested target:

- clipboard success has no artifact path;
- folder success includes its final file path;
- a failed target includes its own typed failure;
- target order is not significant and duplicate targets are invalid.

The Host must attempt both targets from the same conversion result when `both` is
requested. The shell derives full success, partial success, or total delivery failure
from these per-target results and never treats artifact delivery as proof of visual
match or HDR preservation.

### Cancellation ownership

Before a `capture` request is sent, the shell owns cancellation, Overlay teardown, and
selection cleanup. An invalid click or too-small selection cancels locally and must not
start native capture.

After the Host receives a `capture` request, the Host owns native work, cancellation
classification, deterministic resource disposal, and the final response. Native or
task cancellation returns `cancelled`. Application shutdown disposes the Host process
and pending shell state. Version 2 intentionally has no interrupt request because MVP
selection cancellation occurs before dispatch and native screenshot operations are
short-lived; add one in a later protocol version if an observable in-flight cancel
journey requires it.

## Fixtures

[`fixtures/v1`](fixtures/v1) and [`fixtures/v2`](fixtures/v2) are executable examples.
The desktop protocol tests validate every fixture against its owning schema. Fixtures
illustrate wire shape; they do not replace the cross-field checks described above.
