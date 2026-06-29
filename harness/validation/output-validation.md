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

`Hdr10JxrViewerValidationEvidence` evaluates loaded output validation artifacts against the JXR viewer-facing gates. It requires complete target-aware HDR evidence, complete target-app version evidence for the named viewers under test, a complete HDR10 format contract, all named viewers to pass artifact handling, visual match, HDR preservation, and viewer-recognized HDR10 metadata status, and Windows manual evidence rather than automated-only evidence. For the current HDR10 JXR path, the evidence must cover `Folder` output; clipboard-only evidence cannot count as proof for the file-based HDR-preserved path. Session-level `outputTargetsTested` still provides the default scope for the artifact, but a profile record may now narrow that scope further through `outputTargetsCovered` when one session validates different target semantics for different profiles. The current app now combines this evaluator with codec implementation readiness when deciding whether HDR10 can become executable for the current session; artifacts alone are still not enough, and codec implementation alone is still not enough.

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

During a local Windows validation run, Lumiere reads output validation session JSON files from `%LOCALAPPDATA%\Lumiere\validation\output\*.json`. Each file must use the `OutputValidationSessionArtifact` schema and include the manual evidence fields listed above, target-aware HDR evidence including the observed target color space, per-viewer artifact handling, visual match, HDR preservation, and HDR10 metadata recognition status. Invalid JSON files are ignored for the session, logged as validation artifact load issues, and surfaced in the settings validation record so the tester can fix ignored evidence files; valid files are applied to the settings, main panel, tray, and output policy projections. Runtime HDR10 output still requires codec implementation readiness as well, so loading artifacts alone cannot enable HDR-preserved output.

Workspace-local companion evidence is now part of that loading contract. Every app-loaded artifact evidence path must resolve inside the same local validation workspace `evidence\` directory. Relative paths must start with `evidence\...`; absolute paths must stay inside the workspace evidence directory. Lumiere ignores the JSON artifact when the markdown notes, screenshots, logs, or other referenced file are missing. Workspace-local markdown evidence must also be filled in: empty markdown, `REPLACE_WITH_*` placeholders, `Template only` language, the generated `Draft status: NOT RUN until...` sentinel, or unresolved scenario result choices such as `PASS / PASS with limitation / FAIL / NOT RUN` make the JSON artifact a load issue instead of current-session evidence. Markdown load issues include specific repair guidance, such as replacing placeholders, removing the draft sentinel after real observations are recorded, or selecting one observed result per scenario. This keeps a generated or edited JSON file from advancing `Build` / `Validate` / `Ready` projections when its Story `12-1` scenario-session notes have been moved, deleted, left as a template, or replaced by an external/repo-relative review link. Repo-relative references such as `docs/validation/evidence/...` can be mentioned in notes for human review, but they must not appear as the app-loaded `evidencePaths` that unlock runtime validation state.

The loaded-evidence summary names incomplete target-aware HDR fields when no loaded artifact has a complete target-aware record. For example, if the JSON still contains `REPLACE_WITH_OBSERVED_TARGET_COLOR_SPACE`, Settings > Validation should show that `target-aware HDR evidence color space` is missing. This summary text is guidance for fixing the artifact; it is not a PASS condition and does not replace Windows manual display validation.

The HDR10 JXR viewer gate uses the same target-aware field vocabulary in its blockers. A loaded artifact with placeholder target color-space evidence must keep HDR10 at `Validate` / fallback state and should report the missing `color space` field before the validator reruns or edits the artifact.

The same viewer gate also names incomplete manual-session fields that are not covered by more specific blockers. For example, if an artifact is readable but `evidencePaths` is empty, HDR10 must stay at `Validate` / fallback state and the blocker should mention missing `evidence paths`. This keeps scenario notes and workspace-local evidence links part of the runtime release gate instead of only a documentation checklist item.

The loaded-evidence summary in Settings > Validation uses the same manual-session field vocabulary for gaps that are not already covered by dedicated target-aware HDR or target-app-version lines. A readable artifact with empty `evidencePaths` should therefore show `Manual validation session evidence is incomplete: evidence paths.` in the summary. This is review guidance for fixing the artifact; it is not a PASS condition and does not replace Windows manual scenario evidence.

On startup, Lumiere now also prepares the local validation workspace under `%LOCALAPPDATA%\Lumiere\validation\output\`. It seeds:

- `templates\output-validation-session.schema-v4.sample.json` as the local copy-ready sample
- `templates\hdr-sdr-validation-session-template.md` as the local markdown record for focused Story `12-1` scenario runs
- `guidance\release-validation-checklist.md` as the local public-release gate checklist
- `guidance\hdr-sdr-validation-scenarios.md` as the local standard fidelity scenario guide
- `guidance\settings-accessibility-validation.md` as the local settings-shell accessibility workflow
- `evidence\` as the default place for supporting screenshots, notes, or logs
- `README.txt` with the short local workflow reminder

The settings validation record now reports that workspace path and the seeded sample path for the current machine. Workspace readiness is not evidence by itself; it only reduces setup friction for real Windows manual validation.

The settings validation section keeps the evidence rows as the primary surface and exposes a compact native command surface for the local evidence flow. The primary commands are `Create draft` and `Reload`; workspace, template, checklist, scenario, accessibility, and trend helpers live in overflow so the UI stays aligned with the compact validation-panel prototype instead of becoming a grid of standalone buttons. These actions let a Windows validator open the current public-release guides from the same workspace, generate a session-local draft, edit local evidence, and refresh the current session without restarting Lumiere.

For Story `12-3`, the same settings surface now also exposes app-local resource-trend workflow helpers:

- `Create trend draft` generates a workspace-local long-run session markdown draft prefilled with the current process ID, output target, current-session hints, and a seeded sampler command. If readable `resource-trends\*-summary.json` files exist, the draft prefers a summary matching the current Lumiere PID; fallback imports are marked with a scope warning. Imported summaries carry sampler CSV/summary paths and metric baseline/final/delta/min/max rows while leaving pass/fail classification for human review.
- `Trend template` opens the seeded long-run session record template from the local validation workspace.
- `Trend script` opens the seeded PowerShell sampler script from the local validation workspace.
- `Copy trend cmd` copies a current-process sampler command that already points at Lumiere's seeded script, the current app PID, and the workspace-local `resource-trends` output folder.

These actions reduce setup friction for long-run lifecycle validation, but they do not count as evidence by themselves. The generated draft is still a `NOT RUN` or explicit `REPLACE_WITH_PASS_FAIL_LIMITATION` placeholder until a validator replaces its manual fields and observed results. The copied command is only a launch helper; Story `12-3` still requires real Windows `50+` or `100+` runs, captured CSV/summary artifacts, and an honest pass/fail/limitation record.

The seeded guide actions also do not count as evidence by themselves. They only keep the release checklist, scenario set, and accessibility workflow close to the same local workspace that stores the JSON artifacts and long-run session notes.

The seeded `templates\hdr-sdr-validation-session-template.md` file is a markdown note template for manual scenario execution, not a runtime artifact. It should be copied or renamed for a specific Windows run and linked from the JSON evidence or release checklist when useful; the output-validation loader intentionally continues to count only valid workspace JSON artifacts as release evidence.

The same settings surface now also summarizes the currently loaded evidence for the active session instead of forcing the tester to infer everything from the lower gate rows alone. The loaded-evidence summary is intentionally compact and evidence-first:

- latest loaded artifact date, tester, build, and recorded result summary
- a distinct rejected-evidence state when validation files are present but none can load, making clear that ignored files do not count as Windows manual evidence and runtime gates stay blocked
- entry points, display topology buckets, DPI scales, display setups, and HDR states already covered by the loaded evidence
- whether the latest loaded artifact matches the current app build, is stale for the current build, or cannot yet be aligned to a comparable build token
- current coverage across output targets, named viewers, and checklist IDs
- missing display topology buckets from `hdr-sdr-validation-scenarios.md`
- missing HDR10 named viewer targets expected by the public HDR-preserved profile
- public-release checklist groups that are still missing from the loaded evidence
- recommended next native guide or action for those missing checklist groups
- a concrete next Windows run suggestion combining the next uncovered entry point, display topology, output target, and HDR10 viewer target set
- known limitations and follow-up stories/issues carried by the loaded artifacts
- ignored-file warnings when invalid JSON, schema problems, or rejected companion evidence were skipped during load; when valid artifacts also load, the first rejected file's repair guidance stays visible in the gap text while the latest valid artifact remains openable for review
- workspace-readiness guidance when the local validation folder exists but is not yet usable
- direct open access to the latest loaded evidence file when the current session has one

This summary is not a release pass/fail dashboard. It is a review surface that helps a Windows validator answer "what evidence is actually loaded right now?" before interpreting `Build`, `Validate`, `Ready`, or lower validation rows.

The validation rows now also surface target-app version evidence as a first-class item instead of leaving it only inside the loaded-evidence summary. This keeps the review UI aligned with the runtime HDR10 JXR gate: if a named viewer/app is missing a concrete recorded version, the review surface should say so directly and the runtime gate should remain blocked.

Build alignment is intentionally strict:

- Lumiere may only call loaded evidence "matched" when the current app build exposes a comparable commit token and the latest artifact records the same token.
- If the latest artifact records a different commit token, the app must surface the evidence as stale for the current build instead of quietly reusing it as though it still proved release support.
- If either side lacks a comparable token, Lumiere should keep the state limited/unknown rather than inventing freshness.

For the HDR10 JXR release path, current-build alignment is now part of the runtime gate as well, not only the review UI:

- when Lumiere can compare the current build token against the loaded HDR10 evidence, stale evidence must keep HDR10 at `PendingValidation`
- complete but stale artifacts must not promote HDR10 into a `Ready` / executable session
- only current-build-aligned manual evidence may unlock the first HDR-preserved file-output path
- every app-loaded artifact that participates in the HDR10 JXR folder-output viewer gate must align with the current build token; a current artifact for one named viewer must not mask stale evidence for another named viewer

`Create draft` is intentionally a workflow accelerator, not a release-evidence shortcut. Lumiere pre-fills only context it already knows for the current session, such as:

- current app version
- current build commit token when the informational version exposes one
- selected output target
- selected output profile
- named viewer skeleton for that profile
- suggested next Windows run scope from the currently loaded evidence, including the next missing display topology, entry point, output target, and HDR10 viewer targets
- known target-app versions when Lumiere can identify them locally, otherwise placeholders for the named viewers
- current capture-target display name and bounds when available
- current graphics adapter label when the session can resolve it
- current window DPI scale when the active XAML root can report it
- current display-topology hint when the active session can summarize the current display setup

When `Create draft` writes the JSON artifact draft, it also creates a companion markdown scenario-session draft under `evidence\` using `templates\hdr-sdr-validation-session-template.md`. The JSON artifact's `evidencePaths` points at that local markdown file, and the markdown file links back to the JSON artifact name. This keeps Story `12-1` scenario notes, checklist IDs, output targets, and JSON evidence in one workspace-local chain without adding another Settings UI action.

The generated file still keeps manual-observation fields honest:

- tester, Windows version, device, GPU, and DPI remain explicit placeholders
- current-session GPU and DPI values may appear as `current session: ...` hints, but they still do not count as recorded manual evidence
- current-session display-topology values may also appear as `current session: ...` hints, but they still do not count as recorded mixed-monitor validation evidence
- suggested topology / entry-point / output-target / viewer-scope values may appear as planning hints, but they still do not count as recorded evidence until the validator runs that session and replaces the placeholders
- when recent compatible local artifacts exist, Lumiere may append `latest local artifact: ...` hints to those placeholders so the validator can reuse stable machine context without losing the explicit manual-replacement prompt
- if Lumiere cannot prove a comparable current build token, the draft keeps `REPLACE_WITH_GIT_COMMIT` instead of inventing one
- target-app versions stay placeholders for any viewer/app Lumiere cannot identify locally, and even prefilled values still need the validator to confirm they match the actual tested app/session
- target-aware HDR state, observed target color space, and target-aware detail stay placeholders until the validator records the Windows display evidence for the active capture target
- viewer artifact handling, visual match, HDR preservation, and HDR10 metadata recognition stay `NotRun` until observed
- the companion markdown scenario-session draft is also a placeholder until a Windows validator fills in observed scenario results, evidence paths, and limitations, and removes the `Draft status: NOT RUN until...` sentinel
- the draft is written into `%LOCALAPPDATA%\Lumiere\validation\output\` as a normal artifact file, but it should not be counted as passing evidence until the placeholders and observed results are replaced with real Windows manual validation data
- Lumiere does not auto-reload the draft after creation, so an untouched draft does not immediately change the current evidence gate view

Use `templates/output-validation-session.schema-v4.sample.json` or the in-app `Create draft` action as the starting point for a local validation artifact. Copy the sample, or generate a prefilled draft directly into `%LOCALAPPDATA%\Lumiere\validation\output\`, rename it for the session if needed, replace every `REPLACE_WITH_*` value, and change each viewer status only after observing that viewer on the tested Windows machine. The sample's `evidencePaths` value is intentionally workspace-local under `evidence\`; keep app-loaded evidence paths in that workspace and use repo/harness links only inside human-readable notes. The `displaySetup` field should name the tested Display Topology Matrix bucket when possible, such as `Topology: Single HDR-capable display with Windows HDR enabled` or `Topology: Multi-monitor mixed-DPI`, so the in-app loaded-evidence summary can identify missing topology buckets. The sample and the generated draft intentionally keep viewer statuses at `NotRun`; do not commit or share them as passing release evidence until the target-aware HDR evidence including observed target color space, companion scenario-session notes, visual-match result, HDR preservation result, and HDR10 metadata recognition result have all been replaced with real observations. If the same validation session covers different output targets for different profiles, keep `outputTargetsTested` as the session summary and use per-record `outputTargetsCovered` to declare the narrower profile-specific scope.

Missing target-app versions now also count as incomplete manual evidence, not only as a review-surface gap. If `targetAppsTested` names a viewer or app but `targetAppVersions` does not carry a real version for it, Lumiere downgrades that artifact to incomplete manual evidence and the HDR10 JXR runtime gate remains blocked.

Next-run guidance uses the same profile-aware target scope. A broad session-level `Both` value is only enough for a profile when that profile record does not narrow `outputTargetsCovered`. If an HDR10 record says it covered only `Clipboard`, Settings > Validation and generated draft hints must still ask for `Folder` evidence before the first HDR-preserved file-output path can be treated as covered.

Viewer-target guidance also requires complete profile-specific evidence, not just a recorded viewer name. For HDR10, a viewer remains missing from the next-run plan until artifact handling, visual match, HDR preservation, and HDR10 metadata recognition all pass for that viewer. `NotRun`, `Limited`, or `Fail` rows stay visible as work for the next Windows manual run.

## Manual Validation Scenarios

1. Clipboard output to Paint, Photos, and Microsoft Edge.
2. Folder output to a normal user-writable directory.
3. Folder output with missing, inaccessible, read-only, and long-path-adjacent save paths.
4. Both-target output with clipboard success plus file failure, then file success plus clipboard failure.
5. Slow clipboard or file output while confirming overlay, WGC session, and graphics resources tear down.
6. Any advanced export/color mode against the output profile acceptance record above.
