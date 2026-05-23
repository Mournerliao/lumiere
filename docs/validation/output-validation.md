# Output Validation

Lumiere Epic 6 output currently provides basic PNG/clipboard/file usability. It is not validated HDR-preserving output.

## Current Output Scope

| Capability | Current validation level | Notes |
|---|---|---|
| Clipboard image output | Windows CI-pass for policy and failure mapping; Windows manual validation required for OS clipboard behavior | Basic usability only. No HDR-preserving claim. |
| Folder PNG output | Windows CI-pass for path policy, skip/failure semantics, and write abstraction; Windows manual validation required for filesystem edge cases | Uses configured save path and invariant timestamp naming. |
| Both-target output | Windows CI-pass for aggregate result semantics and per-target timeout/failure handling; Windows manual validation required for slow OS behavior and resource teardown | Partial success must identify the succeeded and failed targets. |
| Advanced color/export options | Not available | Requires future encoder metadata, conversion policy, target-app assumptions, and Windows manual validation. |

## Future Format Acceptance Record

Any future HDR10, P3, sRGB, ICC, HEIF, AVIF, JPEG XL, or similar export/color option must record:

- Format choice and file extension.
- Source color space and pixel format.
- Conversion policy from FP16 scRGB to the output format.
- Metadata policy, including HDR metadata, ICC profile, transfer function, mastering/display assumptions, and omissions.
- Target-app assumptions and compatibility matrix.
- Windows manual validation date, device/display configuration, app versions, and observed result.
- Known limitations and user-facing copy that avoids unsupported preservation claims.

## Manual Validation Scenarios

1. Clipboard output to Paint, Photos, and at least one Chromium-based app.
2. Folder output to a normal user-writable directory.
3. Folder output with missing, inaccessible, read-only, and long-path-adjacent save paths.
4. Both-target output with clipboard success plus file failure, then file success plus clipboard failure.
5. Slow clipboard or file output while confirming overlay, WGC session, and graphics resources tear down.
6. Any future advanced export/color mode against the future format acceptance record above.
