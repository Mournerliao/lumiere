# Lumiere Domain Language

This glossary defines the current product-domain terms for Lumiere's MVP-first direction.

## Language

**HDR-Aware MVP**:
The first release target. Lumiere should provide fast native Windows screenshots, an FP16/scRGB-oriented preview path, honest HDR status, and compatible clipboard/file output.
_Avoid_: Perfect HDR release, certification release, universal HDR guarantee

**HDR-Aware State**:
An app state that explains whether the active capture target appears HDR-ready, unavailable, degraded, or unvalidated. It should not imply HDR-preserved output.
_Avoid_: Global HDR ready, first-monitor HDR status

**Compatible Output**:
Clipboard or file output intended to be useful in common Windows consumers. It may be SDR-compatible and must not be described as HDR-preserved unless that path is separately validated.
_Avoid_: HDR success, preserved output, perfect copy

**Artifact Success**:
The claim that an output artifact was copied or saved successfully. It is separate from visual match and HDR preservation.
_Avoid_: Fidelity success, HDR preserved, viewer-certified

**HDR-Preserved Export**:
A future public capability where a named file export path preserves HDR semantics through a documented format, conversion policy, metadata policy, target-app assumptions, and Windows manual validation.
_Avoid_: HDR-looking output, JXR exists therefore HDR works

**Visual Match Goal**:
A long-term tuning goal: output should avoid obvious washed-out, gray, or blown-out results and move toward the user's perceived screen appearance where technically possible. It is not an MVP release guarantee.
_Avoid_: WYSIWYG guarantee, exact perceptual match

**Target-Aware HDR**:
HDR assessment should follow the active capture target, especially in mixed HDR/SDR display setups. For MVP, unresolved mixed-target behavior can be documented as a limitation instead of blocking release.
_Avoid_: Adapter-default readiness, global desktop assumption

**Future Validation Matrix**:
The broader viewer, display-topology, DPI, accessibility, and long-run stability evidence that should support later HDR-preserved export work. It is not the default MVP gate.
_Avoid_: Treating every future validation axis as a first-release blocker
