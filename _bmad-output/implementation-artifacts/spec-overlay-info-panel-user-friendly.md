---
title: 'Overlay Info Panel User-Friendly Optimization'
type: 'feature'
created: '2026-06-03'
status: 'done'
route: 'one-shot'
---

# Overlay Info Panel User-Friendly Optimization

## Intent

**Problem:** The current overlay info panel displays too much technical information that is difficult for users to understand, making the interface unfriendly for non-technical users.

**Approach:** Implement a layered display approach where user-friendly information is shown by default, and technical details are hidden behind a collapsible section. This makes the interface more accessible while preserving diagnostic information for developers.

## Boundaries & Constraints

**Always:** 
- Maintain all existing functionality
- Preserve technical details for debugging purposes
- Keep the overlay window's current layout and positioning

**Ask First:** 
- None - this is a straightforward UI optimization

**Never:** 
- Remove technical details completely
- Change the overlay window's core behavior
- Modify the capture or preview functionality

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Default State | Overlay with HDR ready status | Clean status display with minimal technical details | N/A |
| Degraded State | Overlay with degraded preview | User-friendly message with fix suggestions | N/A |
| Technical Details Expanded | User clicks to expand technical details | Full technical information displayed | N/A |
| Technical Details Collapsed | User clicks to collapse technical details | Technical information hidden | N/A |

## Code Map

- `src/Lumiere.Overlay/OverlayWindow.xaml` -- UI layout for the overlay info panel
- `src/Lumiere.Overlay/OverlayWindow.xaml.cs` -- Logic for toggling technical details visibility
- `src/Lumiere.App/MainWindow.xaml.cs` -- Logic for creating user-friendly messages
- `src/Lumiere.Overlay/OverlayState.cs` -- State definitions and message templates

## Tasks & Acceptance

**Execution:**
- [ ] `src/Lumiere.Overlay/OverlayWindow.xaml` -- Add collapsible section for technical details -- Implement UI for toggling technical details visibility
- [ ] `src/Lumiere.Overlay/OverlayWindow.xaml.cs` -- Add logic for toggling technical details -- Implement click handler and visibility state management
- [ ] `src/Lumiere.App/MainWindow.xaml.cs` -- Improve message content for user-friendly display -- Replace technical jargon with clear, actionable messages
- [ ] `src/Lumiere.Overlay/OverlayState.cs` -- Add user-friendly message templates -- Create templates for different overlay states

**Acceptance Criteria:**
- Given the overlay is displayed with degraded preview status, when the user views the info panel, then they see a user-friendly message instead of technical jargon
- Given the user wants to see technical details, when they click the "Technical Details" button, then the technical information is displayed in a collapsible section
- Given the technical details are expanded, when the user clicks the button again, then the technical information is hidden
- Given the overlay is displayed with any status, when the user views the info panel, then the layout remains clean and uncluttered

## Verification

**Commands:**
- `dotnet build Lumiere.sln -p:Platform=x64 --verbosity minimal` -- expected: Build succeeds
- `dotnet test tests/Lumiere.Graphics.Tests/ -p:Platform=x64 --verbosity minimal` -- expected: All tests pass (note: 2 pre-existing test failures unrelated to this change)

**Manual checks:**
- Launch the application and start a capture
- Verify the overlay info panel shows user-friendly messages instead of technical details
- Click the "Technical Details" button and verify technical information appears
- Click the button again and verify technical information is hidden
- Test with different capture states (HDR ready, degraded, unsupported)

## Review Findings

1. **Missing accessibility support**: The toggle button lacks `AutomationProperties.Name` and `AutomationProperties.HelpText` for screen readers.
2. **Hardcoded Chinese text**: User-facing messages are hardcoded in Chinese, which may not be appropriate for internationalization.
3. **No animation**: The collapsible section lacks smooth animation for better UX.
4. **No test coverage**: No unit tests were added for the new toggle functionality.
5. **Missing hover state**: The toggle button doesn't have a hover state for better interactivity feedback.
