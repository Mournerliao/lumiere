# Output Validation

Lumiere Epic 6 output currently provides basic PNG/clipboard/file usability. It is not validated HDR-preserving output. Public HDR-preserving output requires Epic 11 output profile contracts and the public gates in `release-validation-checklist.md`.

## Current Output Scope

| Capability | Current validation level | Notes |
|---|---|---|
| Clipboard image output | Windows CI-pass for policy and failure mapping; Windows manual validation required for OS clipboard behavior | Basic usability only. No HDR-preserving claim. |
| Folder PNG output | Windows CI-pass for path policy, skip/failure semantics, and write abstraction; Windows manual validation required for filesystem edge cases | Uses configured save path and invariant timestamp naming. |
| Both-target output | Windows CI-pass for aggregate result semantics and per-target timeout/failure handling; Windows manual validation required for slow OS behavior and resource teardown | Partial success must identify the succeeded and failed targets. Mixed-profile sessions must also preserve per-target fidelity semantics instead of implying one runtime profile for clipboard and file output together. |
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

The HDR10 `.jxr` output path is not enabled until implementation readiness and loaded Windows manual validation evidence prove all of the following:

- Native Windows WIC JPEG XR integration is implemented for `.jxr`/WMP container output.
- The codec accepts the captured `R16G16B16A16_FLOAT` source readback without routing through sRGB PNG conversion.
- The Lumiere audit metadata write/read path round-trips from a real encoded `.jxr` artifact, including metadata source, BT.2020 primaries, white point, mastering luminance, MaxCLL, MaxFALL, and a rationale/detail string.
- Viewer-recognized HDR10 static metadata is implemented or validated for the JPEG XR container. Lumiere audit metadata alone is not sufficient.
- Codec readiness is blocked unless the HDR10 static metadata policy is complete and auditable; an `AttachHdr10StaticMetadata` enum value alone is not enough to enable the profile.
- Windows manual viewer validation passes for every named target viewer before runtime capability is marked implemented.

The current built-in HDR10 reference policy is `Bt2020PqReference1000Nit`: BT.2020 primaries, D65 white point, PQ/ST 2084 transfer, 0.005 to 1000 nit mastering luminance, 1000 nit MaxCLL, and 400 nit MaxFALL. This is an explicit policy input for the future encoder, not proof of target display fidelity or viewer compatibility.

As of the current implementation, Lumiere has a Windows WIC-backed JPEG XR adapter for `64bppRGBAHalf` byte streams and the app wires the HDR10 JXR codec through that adapter. The adapter embeds Lumiere audit metadata under the JPEG XR XMP query namespace `/ifd/xmp/{wstr=https://lumiere.app/ns/hdr10-audit/1.0/}:*` so emitted artifacts can carry the selected HDR10 policy values for inspection. A WIC metadata inspection seam reads those same query paths back from an encoded `.jxr` artifact, proving audit metadata round-trip at the artifact level. Runtime HDR10 export is still disabled by default; it becomes executable only when the codec implementation prerequisites are satisfied and the loaded validation artifacts also prove target-aware HDR evidence, complete format contract evidence, viewer-recognized HDR10 metadata evidence, and Windows manual viewer validation.

`Hdr10JxrViewerValidationEvidence` evaluates loaded output validation artifacts against the JXR viewer-facing gates. It requires complete target-aware HDR evidence, a complete HDR10 format contract, all named viewers to pass artifact handling, visual match, HDR preservation, and viewer-recognized HDR10 metadata status, and Windows manual evidence rather than automated-only evidence. For the current HDR10 JXR path, the evidence must cover `Folder` output; clipboard-only evidence cannot count as proof for the file-based HDR-preserved path. Session-level `outputTargetsTested` still provides the default scope for the artifact, but a profile record may now narrow that scope further through `outputTargetsCovered` when one session validates different target semantics for different profiles. The current app now combines this evaluator with codec implementation readiness when deciding whether HDR10 can become executable for the current session; artifacts alone are still not enough, and codec implementation alone is still not enough.

In the current UI, these gate states appear as:

- `Build` - implementation prerequisites, profile-contract wiring, metadata policy work, or Windows validation prerequisites are still incomplete
- `Validate` - build prerequisites are ready, but Windows manual viewer evidence is still incomplete for the current session
- `Ready` - the current session has both implementation readiness and complete loaded manual evidence

The validation panel now surfaces the current output-profile gate directly so testers can see whether the selected session is blocked at `Build`, waiting at `Validate`, or truly `Ready` before interpreting the lower evidence rows.

Selected-profile UI projections now also have to respect the current output target instead of only the requested format name. In particular:

- `Clipboard` sessions must continue to project `sRGB` compatibility output for HDR10/P3 requests, even if folder-only HDR evidence exists for the same build.
- `Both` sessions may still show the folder-side HDR10 gate progress, but they must not collapse the whole mixed session into one uniform HDR-preserved claim because the clipboard side still stays compatibility-scoped.

Runtime output policy must now follow that same target-aware artifact scope. Request-time profile evidence cannot be applied more broadly than the active output target:

- `Clipboard` requests must not absorb folder-only HDR10 validation into their requested-profile evidence.
- `Both` requests may still use folder-side HDR10 evidence for the file artifact path, but their aggregate runtime policy must remain compatibility-first unless every attempted target resolves to the same executable profile.

Output completion feedback must preserve that same distinction. A `Copied` or `Saved` result only means artifact delivery succeeded for the effective runtime profile. When the requested profile is still blocked, the output-result detail should continue to report the requested profile gate (`Build` or `Validate`) and the active compatibility fallback instead of implying that artifact success promoted the profile to HDR-preserved readiness. When `Both` mode produces different effective paths per target, the result surface must say so explicitly rather than compressing clipboard and folder output into one synthetic profile.

Where the selected output profile also exposes a detailed contract surface, that contract text should mirror the same distinction:

- `Build` - the HDR10 contract may already be partially modeled, but executable HDR10 output is still blocked by build/runtime prerequisites
- `Validate` - the HDR10 format contract is defined, but Windows manual viewer evidence is still incomplete
- `Ready` - the validated HDR10-preserved contract is active for the current session

## Validation Artifact Loading

During a local Windows validation run, Lumiere reads output validation session JSON files from `%LOCALAPPDATA%\Lumiere\validation\output\*.json`. Each file must use the `OutputValidationSessionArtifact` schema and include the manual evidence fields listed above, target-aware HDR evidence, per-viewer artifact handling, visual match, HDR preservation, and HDR10 metadata recognition status. Invalid JSON files are ignored for the session, logged as validation artifact load issues, and surfaced in the settings validation record so the tester can fix ignored evidence files; valid files are applied to the settings, main panel, tray, and output policy projections. Runtime HDR10 output still requires codec implementation readiness as well, so loading artifacts alone cannot enable HDR-preserved output.

On startup, Lumiere now also prepares the local validation workspace under `%LOCALAPPDATA%\Lumiere\validation\output\`. It seeds:

- `templates\output-validation-session.schema-v4.sample.json` as the local copy-ready sample
- `evidence\` as the default place for supporting screenshots, notes, or logs
- `README.txt` with the short local workflow reminder

The settings validation record now reports that workspace path and the seeded sample path for the current machine. Workspace readiness is not evidence by itself; it only reduces setup friction for real Windows manual validation.

Use `templates/output-validation-session.schema-v4.sample.json` as the starting point for a local validation artifact. Copy it to `%LOCALAPPDATA%\Lumiere\validation\output\`, rename it for the session, replace every `REPLACE_WITH_*` value, and change each viewer status only after observing that viewer on the tested Windows machine. The sample intentionally keeps viewer statuses at `NotRun`; do not commit or share it as passing release evidence until the target-aware HDR evidence, evidence paths, visual-match result, HDR preservation result, and HDR10 metadata recognition result have all been replaced with real observations. If the same validation session covers different output targets for different profiles, keep `outputTargetsTested` as the session summary and use per-record `outputTargetsCovered` to declare the narrower profile-specific scope.

## Manual Validation Scenarios

1. Clipboard output to Paint, Photos, and at least one Chromium-based app.
2. Folder output to a normal user-writable directory.
3. Folder output with missing, inaccessible, read-only, and long-path-adjacent save paths.
4. Both-target output with clipboard success plus file failure, then file success plus clipboard failure.
5. Slow clipboard or file output while confirming overlay, WGC session, and graphics resources tear down.
6. Any advanced export/color mode against the output profile acceptance record above.
