# macOS Host

This directory owns the future Swift ScreenCaptureKit adapter at the platform-host
seam. It will acquire HDR-aware frames, convert the official output to sRGB Visual
Match, and perform native clipboard and file delivery.

The host is intentionally not implemented in Milestone 0. No `Package.swift`, stub
executable, or simulated capture path lives here. Until the first native vertical
slice is complete, the Electron shell reports `host-unavailable`.
