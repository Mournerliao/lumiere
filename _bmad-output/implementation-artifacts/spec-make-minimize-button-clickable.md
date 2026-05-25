---
title: 'Make minimize button clickable to hide to background'
type: 'feature'
created: '2026-05-26'
status: 'done'
route: 'one-shot'
---

# Make minimize button clickable to hide to background

## Intent

**Problem:** The bottom-right status bar contained a "Minimize" TextBlock placeholder (pre-Epic 7) that was not interactive. After Epic 7 completed background/tray support, the label remained non-functional.

**Approach:** Replace the TextBlock with a Button that calls `HideToBackground("minimize")`. The button starts disabled and is enabled once tray or hotkey infrastructure attaches. Uses a minimize FontIcon glyph for visual consistency with the header buttons.

## Suggested Review Order

1. `../../src/Lumiere.App/MainWindow.xaml` — Button XAML: glyph, sizing, IsEnabled default, automation properties
2. `../../src/Lumiere.App/MainWindow.xaml.cs:166` — `OnMinimizeButtonClick` handler: guard and delegation to `HideToBackground`
3. `../../src/Lumiere.App/MainWindow.xaml.cs:138` — `AttachTrayMenu`: enables button after tray attaches
4. `../../src/Lumiere.App/MainWindow.xaml.cs:151` — `AttachGlobalHotkeys`: enables button after hotkeys register
