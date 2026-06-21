# Lumiere Domain Language

This glossary defines Lumiere's product-domain terms. It records the language used to discuss HDR screenshot fidelity, release readiness, and validation without encoding implementation details.

## Language

**Perfect HDR Fidelity**:
Evidence-backed fidelity for explicitly supported capture, preview, and output paths. It does not mean every device, application, format, and viewing condition is guaranteed.
_Avoid_: Universal HDR perfection, all-app HDR guarantee, unbounded HDR promise

**Perfect HDR Fidelity Public Release**:
The fixed public release target for Lumiere. The first public release must include evidence-backed visual-match output and at least one HDR-preserved supported output path; SDR-compatible output may exist as a fallback or auxiliary path but cannot replace this target.
_Avoid_: Public SDR-Compatible Fidelity Release, downscoped public fidelity release, private-preview rebrand

**Supported Output Path**:
A capture output path that Lumiere publicly enables and validates with a written fidelity contract, target-app or viewer assumptions, and recorded Windows evidence.
_Avoid_: Export option, format toggle, output promise

**SDR-Compatible File Output**:
A supported file output path that intentionally converts captured content into an SDR-compatible artifact with explicit conversion semantics and validation. It is distinct from HDR-preserving output.
_Avoid_: HDR export, HDR-preserved file, generic PNG output

**Credible SDR Rendition**:
The public quality bar for SDR-compatible output: the artifact should be shareable, viewable, and free from obvious washed-out or blown-out HDR conversion failures. It is not a promise that the SDR artifact exactly matches the HDR display.
_Avoid_: HDR-preserved SDR, exact HDR match, perfect SDR copy

**Visual Match Target**:
The long-term tuning goal that Lumiere output should move toward the user's perceived HDR screen appearance where technically possible. It is an optimization target, not a release claim unless backed by validation evidence for a supported path.
_Avoid_: WYSIWYG guarantee, exact perceptual match, universal visual match

**Visual-Match Output**:
A validated output path whose SDR or HDR appearance is checked against real HDR screen scenarios and benchmark applications so it avoids obvious gray, washed-out, or blown-out results. It is a public release requirement alongside HDR-preserved output.
_Avoid_: Looks fine locally, screenshot seems okay, unvalidated visual match

**Fidelity Design Extension**:
A design pass that extends the existing v0 MVP workflow reference with Perfect HDR Fidelity states, output profiles, validation evidence, and release-copy boundaries. It must preserve the existing design language instead of replacing it with a separate visual system.
_Avoid_: New design direction, v1 redesign, replacement design spec

**Highlight Preservation Priority**:
The first tuning priority for SDR-compatible output: protect bright highlight detail from obvious clipping, washout, or blown-out conversion before optimizing overall contrast or color naturalness.
_Avoid_: Brightness boost, exposure match, simple downscale

**Real-Scene Validation Content**:
HDR validation content drawn from realistic user scenarios such as video frames, browser content, games, media playback, and mixed SDR/HDR desktop states. It is the primary evidence that Lumiere behaves well for users.
_Avoid_: Demo-only content, synthetic-only validation

**Diagnostic Test Content**:
Controlled charts, gradients, color patches, gray ramps, and similar assets used to explain failures, tune conversion, and detect regressions. It supports real-scene validation but does not replace it.
_Avoid_: Release proof by charts only, lab-only validation

**Named Viewer Set**:
The fixed set of applications used to validate a supported file output path for a release. The first set should include Windows Photos, Paint, and a Chromium-based browser.
_Avoid_: Opens locally, works in viewers, generic app compatibility

**Target-Aware HDR State**:
An HDR readiness state derived from the active capture target's display capability, not from a global display assumption or first-output probe. It must avoid reporting HDR Ready for an SDR target in a mixed HDR/SDR setup.
_Avoid_: Global HDR status, first-monitor HDR status, adapter-default HDR readiness

**Artifact Success**:
The claim that an output artifact was copied or saved successfully. It is separate from any claim that the artifact preserved HDR fidelity.
_Avoid_: HDR success, fidelity success, preserved output

**Unavailable Fidelity Mode**:
A fidelity mode that Lumiere may name as product direction but cannot enable or present as successful because its contract, implementation, or validation evidence is incomplete.
_Avoid_: Coming soon support, hidden HDR success, partially supported preservation

**SDR-Compatible Fidelity**:
A fallback or auxiliary fidelity level where Lumiere captures HDR-aware content and produces a validated SDR-compatible artifact with explicit conversion semantics. It is not sufficient by itself for Perfect HDR Fidelity Public Release.
_Avoid_: HDR-preserved output, exact HDR output, generic SDR fallback

**HDR-Preserved Fidelity**:
A public fidelity level where Lumiere preserves HDR semantics in the output artifact through a validated format, metadata policy, target-app assumptions, and Windows evidence. It is not implied by capture success or SDR-compatible output.
_Avoid_: HDR-looking output, copied HDR, saved HDR without contract
