# Native UI Contract

## Direction

Lumiere should feel like a calm, precise Windows 11 tool. Capture speed, accuracy,
and trust take priority over decorative product presentation.

- Follow Fluent and WinUI 3 conventions.
- Use compact tool surfaces with clear hierarchy and stable dimensions.
- Use accent color rarely and functionally for action, selection, focus, or critical status.
- Keep motion brief, stateful, and non-blocking.
- Use materials/elevation only to clarify real layering.

Avoid generic SaaS dashboards, oversized heroes, nested decorative cards,
purple-blue AI gradients, glow-heavy surfaces, low-contrast copy, and motion that
delays capture or confirmation.

## Controls And Layout

- Prefer built-in WinUI controls before custom controls.
- Use native settings, dialogs, flyouts, command bars, and navigation patterns.
- Custom UI is appropriate for overlay, crop handles, magnifier, GPU preview,
  and future annotation canvas.
- Overlay geometry and controls must remain stable while the pointer moves.
- Support Windows theme behavior, keyboard access, visible focus, and platform contrast guidance.

## Writing

Use calm, concise, sentence-case copy. Tell the user what happened and what they
can do next. Keep capture, preview, conversion, delivery, and validation distinct.
Normal capture surfaces may identify sRGB Visual Match but must not expose internal
validation vocabulary or imply P3/HDR10 support.
