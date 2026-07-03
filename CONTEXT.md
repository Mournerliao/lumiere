# Lumiere Domain Language

This glossary defines the current product-domain terms for Lumiere's MVP-first direction.

## Language

**HDR-Aware MVP**:
The first release target. Lumiere should provide fast native Windows screenshots, an FP16/scRGB-oriented preview path, honest HDR status, and compatible clipboard/file output.
_Avoid_: Broad HDR-preserved release, certification release, universal HDR guarantee

**HDR-Aware State**:
An app state that explains whether the active capture target appears HDR-ready, unavailable, degraded, or unvalidated. It should not imply HDR-preserved output.
_Avoid_: Global HDR ready, first-monitor HDR status

**Compatible Output**:
Clipboard or file output intended to be useful in common Windows consumers. It may be SDR-compatible and must not be described as HDR-preserved unless that path is separately validated.
_Avoid_: HDR success, preserved output, perfect copy

**sRGB Visual Match**:
The official MVP output path. Lumiere converts the FP16/scRGB capture source into an SDR-compatible sRGB artifact while preserving the user's perceived screen appearance as closely as practical, especially by avoiding obvious HDR overexposure, washed-out output, or gray output.
_Avoid_: Basic sRGB dump, HDR-preserved output, exact replay

**Default Visual Match Conversion**:
The MVP's built-in, non-user-adjustable HDR/scRGB to SDR/sRGB conversion strategy. It should make common HDR screenshots look normal by default instead of exposing tone-mapping controls as a first-release workflow.
_Avoid_: Manual exposure controls, screenshot color editor, per-capture tuning

**Artifact Success**:
The claim that an output artifact was copied or saved successfully. It is separate from visual match and HDR preservation.
_Avoid_: Fidelity success, HDR preserved, viewer-certified

**Target App Limitation**:
A visual or compatibility difference introduced by the app that receives or opens Lumiere's output after Lumiere has produced a valid sRGB Visual Match artifact. It should be documented, but it is different from Lumiere producing an obviously overexposed, washed-out, gray, or inconsistent artifact.
_Avoid_: Blaming target apps for Lumiere output bugs, universal app compatibility

**HDR-Preserved Export**:
A future public capability where a named file export path preserves HDR semantics through a documented format, conversion policy, metadata policy, target-app assumptions, and Windows manual validation.
_Avoid_: HDR-looking output, JXR exists therefore HDR works

**Visual Match Goal**:
An MVP quality guardrail: HDR screenshots should avoid obvious washed-out, gray, or blown-out output and should look close to the user's perceived screen appearance in common supported paths. It is not a display-independent guarantee of exact visual identity or HDR preservation.
_Avoid_: Universal WYSIWYG guarantee, exact perceptual match, HDR-preserved proof

**Target-Aware HDR**:
HDR assessment should follow the active capture target, especially in mixed HDR/SDR display setups. For MVP, unresolved mixed-target behavior can be documented as a limitation instead of blocking release.
_Avoid_: Adapter-default readiness, global desktop assumption

**Future Validation Matrix**:
The broader viewer, display-topology, DPI, accessibility, and long-run stability evidence that should support later HDR-preserved export work. It is not the default MVP gate.
_Avoid_: Treating every future validation axis as a first-release blocker

**Planned Output Profile**:
An output profile visible in product direction or UI as future capability but not released as an MVP-supported mode until implementation and validation evidence exist. P3 and HDR10 are planned profiles during the MVP.
_Avoid_: Released mode, supported profile, hidden guarantee
