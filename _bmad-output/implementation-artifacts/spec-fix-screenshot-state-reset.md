---
title: 'Fix screenshot state reset after capture'
type: 'bugfix'
created: '2026-05-11'
status: 'in-review'
route: 'one-shot'
---

# Fix screenshot state reset after capture

## Intent

**Problem:** First screenshot works, but second click on screenshot button has no response. After first screenshot completes, the state is set to `Disposed` but never resets back to `Idle`. The `CanAcceptCommand` method only accepts `Idle`, `Failed`, and `Unsupported` states, so subsequent screenshot commands are rejected.

**Approach:** Add `ApplySessionState(CaptureSessionState.Idle())` after closing the overlay window in `OnOverlayCaptureConfirmed` and `OnOverlayCloseRequested` methods. Remove the intermediate `Disposed` state that was immediately overwritten.

## Suggested Review Order

1. `src/Lumiere.App/MainWindow.xaml.cs:669-698` -- OnOverlayCaptureConfirmed method: removed Disposed state, added Idle reset with isClosed guard
2. `src/Lumiere.App/MainWindow.xaml.cs:657-668` -- OnOverlayCloseRequested method: added Idle reset with logging
3. `src/Lumiere.App/MainWindow.xaml.cs:755-769` -- OnOverlayClosed method: added state check before StopPreview
4. `_bmad-output/implementation-artifacts/deferred-work.md` -- Deferred pre-existing issues from review