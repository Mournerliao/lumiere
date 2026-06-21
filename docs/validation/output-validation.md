# Output Validation

Lumiere Epic 6 output currently provides basic PNG/clipboard/file usability. It is not validated HDR-preserving output. Public HDR-preserving output requires Epic 11 output profile contracts and the public gates in `release-validation-checklist.md`.

## Current Output Scope

| Capability | Current validation level | Notes |
|---|---|---|
| Clipboard image output | Windows CI-pass for policy and failure mapping; Windows manual validation required for OS clipboard behavior | Basic usability only. No HDR-preserving claim. |
| Folder PNG output | Windows CI-pass for path policy, skip/failure semantics, and write abstraction; Windows manual validation required for filesystem edge cases | Uses configured save path and invariant timestamp naming. |
| Both-target output | Windows CI-pass for aggregate result semantics and per-target timeout/failure handling; Windows manual validation required for slow OS behavior and resource teardown | Partial success must identify the succeeded and failed targets. |
| Export profile settings surface | Visible design-reference controls; current output remains basic PNG usability | `HDR10`, `P3`, and `sRGB` may be shown in settings, but advanced profile semantics require Epic 11 encoder metadata, conversion policy, target-app assumptions, and Windows manual validation before they can be treated as real output behavior. |

## Output Profile Acceptance Record

Any enabled HDR10, P3, sRGB, ICC, HEIF, AVIF, JPEG XL, or similar export/color option must record:

- Format choice and file extension.
- Source color space and pixel format.
- Conversion policy from FP16 scRGB to the output format.
- Metadata policy, including HDR metadata, ICC profile, transfer function, mastering/display assumptions, and omissions.
- Target-app assumptions and compatibility matrix.
- Windows manual validation date, device/display configuration, app versions, and observed result.
- Known limitations and user-facing copy that avoids unsupported preservation claims.

### HDR10 JPEG XR Pending Codec Requirements

The HDR10 `.jxr` output path is not enabled until the codec readiness record proves all of the following:

- Native Windows WIC JPEG XR integration is implemented for `.jxr`/WMP container output.
- The codec accepts the captured `R16G16B16A16_FLOAT` source readback without routing through sRGB PNG conversion.
- HDR10 static metadata write policy is implemented and recorded.
- Windows manual viewer validation passes for every named target viewer before runtime capability is marked implemented.

## Manual Validation Scenarios

1. Clipboard output to Paint, Photos, and at least one Chromium-based app.
2. Folder output to a normal user-writable directory.
3. Folder output with missing, inaccessible, read-only, and long-path-adjacent save paths.
4. Both-target output with clipboard success plus file failure, then file success plus clipboard failure.
5. Slow clipboard or file output while confirming overlay, WGC session, and graphics resources tear down.
6. Any advanced export/color mode against the output profile acceptance record above.
