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
- Per-viewer HDR10 metadata recognition status, separate from artifact handling, visual match, and HDR preservation.
- Windows manual validation date, device/display configuration, app versions, and observed result.
- Known limitations and user-facing copy that avoids unsupported preservation claims.

### HDR10 JPEG XR Pending Codec Requirements

The HDR10 `.jxr` output path is not enabled until the codec readiness record proves all of the following:

- Native Windows WIC JPEG XR integration is implemented for `.jxr`/WMP container output.
- The codec accepts the captured `R16G16B16A16_FLOAT` source readback without routing through sRGB PNG conversion.
- The Lumiere audit metadata write/read path round-trips from a real encoded `.jxr` artifact, including metadata source, BT.2020 primaries, white point, mastering luminance, MaxCLL, MaxFALL, and a rationale/detail string.
- Viewer-recognized HDR10 static metadata is implemented or validated for the JPEG XR container. Lumiere audit metadata alone is not sufficient.
- Codec readiness is blocked unless the HDR10 static metadata policy is complete and auditable; an `AttachHdr10StaticMetadata` enum value alone is not enough to enable the profile.
- Windows manual viewer validation passes for every named target viewer before runtime capability is marked implemented.

The current built-in HDR10 reference policy is `Bt2020PqReference1000Nit`: BT.2020 primaries, D65 white point, PQ/ST 2084 transfer, 0.005 to 1000 nit mastering luminance, 1000 nit MaxCLL, and 400 nit MaxFALL. This is an explicit policy input for the future encoder, not proof of target display fidelity or viewer compatibility.

As of the current implementation, Lumiere has a Windows WIC-backed JPEG XR adapter for `64bppRGBAHalf` byte streams and the app wires the HDR10 JXR codec through that adapter. The adapter embeds Lumiere audit metadata under the JPEG XR XMP query namespace `/ifd/xmp/{wstr=https://lumiere.app/ns/hdr10-audit/1.0/}:*` so emitted artifacts can carry the selected HDR10 policy values for inspection. A WIC metadata inspection seam reads those same query paths back from an encoded `.jxr` artifact, proving audit metadata round-trip at the artifact level. Runtime HDR10 export remains disabled because this audit metadata is not yet proven equivalent to viewer-recognized HDR10 static metadata, and Windows manual viewer validation gates have not passed.

## Validation Artifact Loading

During a local Windows validation run, Lumiere reads output validation session JSON files from `%LOCALAPPDATA%\Lumiere\validation\output\*.json`. Each file must use the `OutputValidationSessionArtifact` schema and include the manual evidence fields listed above, target-aware HDR evidence, per-viewer artifact handling, visual match, HDR preservation, and HDR10 metadata recognition status. Invalid JSON files are ignored for the session and logged as validation artifact load issues; valid files are applied to the settings, main panel, tray, and output policy projections. Runtime HDR10 output is still gated separately by codec execution capabilities, so loading a validation artifact cannot enable HDR-preserved output by itself.

## Manual Validation Scenarios

1. Clipboard output to Paint, Photos, and at least one Chromium-based app.
2. Folder output to a normal user-writable directory.
3. Folder output with missing, inaccessible, read-only, and long-path-adjacent save paths.
4. Both-target output with clipboard success plus file failure, then file success plus clipboard failure.
5. Slow clipboard or file output while confirming overlay, WGC session, and graphics resources tear down.
6. Any advanced export/color mode against the output profile acceptance record above.
