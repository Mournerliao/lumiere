# 0011: Shell-Owned Save Directory Preference

Date: 2026-08-31

## Decision

The Electron main process owns the user's save-directory preference and directory
selection UI. The renderer receives only an effective display path and one task-shaped
command that opens the native directory picker; it never receives filesystem access or
supplies an arbitrary path over IPC.

When Folder or Both delivery is requested, the shell may add an optional absolute
`saveDirectory` to the platform-host v2 capture parameters. If it is absent, each Host
resolves its existing platform default under Pictures/Lumiere. Clipboard-only requests
must not carry a save directory.

The native Host remains the delivery adapter. It creates the selected directory when
possible, applies its existing fixed Lumiere timestamp filename and collision rule, writes the artifact,
and returns the final path or a per-target delivery failure. Failure to create or write
the folder must not erase an independently successful clipboard delivery.

## Context

Issue #9 requires users to view and modify the save directory. The current settings
store already owns output, shortcut, after-capture, and HDR-reminder preferences, while
the native Hosts own platform clipboard and folder delivery. Moving file writing into
Electron would split delivery ownership and weaken the existing per-target result seam.

The v2 schema is current rather than frozen. Older strict Hosts safely reject the new
field as an unknown request instead of silently writing elsewhere, while the shell and
Hosts shipped from one repository revision understand the same additive contract.

## Consequences

- Directory picking and persistence stay behind one narrow main-process interface.
- Renderer IPC remains task-shaped and does not accept a renderer-provided path.
- Existing requests without `saveDirectory` retain their platform default behavior.
- Existing platform filename rules remain unchanged; editable naming is not introduced
  by this slice.
- macOS and Windows must verify custom directory delivery independently before Issue #9
  can close.
