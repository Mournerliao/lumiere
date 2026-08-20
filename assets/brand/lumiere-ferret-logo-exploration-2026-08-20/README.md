# Lumiere white-ferret logo exploration

Generated on 2026-08-20 as a one-pass creative draw following `ip-as-logo-skill`.
All six returned PNG files are preserved as-is. They are exploration candidates,
not yet wired into the desktop application.

## Final selection

`A2-peeking-lower-right.png` superseded the earlier B1 selection as the Lumiere
logo on 2026-08-20.
The unchanged canonical copy is stored at `assets/brand/lumiere-logo.png`.

## Generation route

- Provider: Codex built-in ImageGen
- Model: not exposed by the runtime
- Constraint delivery: main-prompt constraints
- Native dimensions: 1254 × 1254 pixels for every candidate
- Semantic palette: coral red background `#E8665A`, warm white character
  `#F4F1E8`, deep graphite character details `#272523`

## Candidates

| Label | Direction                           | Product connection                | Assigned corner | File                          |
| ----- | ----------------------------------- | --------------------------------- | --------------- | ----------------------------- |
| A1    | White ferret peeking upward         | Notices and captures screen light | Lower-left      | `A1-peeking-lower-left.png`   |
| A2    | White ferret peeking upward         | Notices and captures screen light | Lower-right     | `A2-peeking-lower-right.png`  |
| B1    | White ferret glancing back          | Agile, instantaneous capture      | Lower-left      | `B1-glancing-lower-left.png`  |
| B2    | White ferret glancing back          | Agile, instantaneous capture      | Lower-right     | `B2-glancing-lower-right.png` |
| C1    | White ferret curled around its tail | Gathers and preserves light       | Lower-left      | `C1-curled-lower-left.png`    |
| C2    | White ferret curled around its tail | Gathers and preserves light       | Lower-right     | `C2-curled-lower-right.png`   |

## Shared prompt

```text
Create one complete full-bleed 1:1 square image.
Background: fill the entire square with solid muted coral red #E8665A. Keep coral red visible in every open area and in the corners not occupied by the character; the assigned emergence corner must be occupied by the character.
Complexity: use only 4–7 large basic shapes and at most two broad internal color regions. Use two simple eyes and add one tiny mouth only when it helps the expression. Remove every nonessential line, outline, anatomical detail, texture, and decoration. Keep the character readable at 32 × 32.
Color behavior: use exactly three semantic colors in the complete image: warm white #F4F1E8 and deep graphite #272523 for the character, plus the coral red background. Organize both character colors into broad purposeful masses and reuse deep graphite for the facial marks. Keep the character, facial marks, and background clearly separated.
Style: make simplification, cuteness, and lovable baby-like appeal the strongest qualities. Use large soft forms, compact proportions, thick rounded contours, and an ultra-clean graphic treatment. Prefer one clear shape over several explanatory details. Add an extremely, extremely subtle, almost imperceptible sense of depth through a barely-there neo-skeuomorphic treatment.
Finish: show only the character on the full-canvas background, with clean surfaces and normal square outer corners.
Constraints: Use no text or watermark. Add no borders, frames, cards, or presentation masks. Include one character only, with no extra subjects or scenery. Use no fragile lines, sharp tips, unnecessary outlines, tiny details, or decorative marks. Add no photorealistic material, dramatic bevel, glossy hotspot, deep occlusion, extrusion, strong three-dimensional rendering, or external cast shadow. Keep the background solid and uniform, with no texture, vignette, or lighting variation.
```

## Direction prompts

Each direction prompt below was appended to the shared prompt. The corner and
matching side-crop wording were changed for the second variant.

### A — Peeking

```text
Subject: place one extremely simplified, cute, endearing baby white ferret peeking upward on the background, reduced to one soft rounded continuous silhouette. Its defining feature is an oversized round face with both short blunt rounded ears visible, calmly observing as if it has just noticed light on a screen.
Composition: keep the character upright and emerging from the lower-<left|right>, filling about 85–95% of the square so it remains visually dominant. Cropping at the bottom or <left|right> is welcome. Preserve both ears. Never center or bottom-center the character.
```

### B — Glancing back

```text
Subject: place one extremely simplified, cute, endearing baby white ferret glancing back on the background, reduced to one soft rounded continuous bean-shaped silhouette. Its defining feature is one broad thick blunt tail curling gently beside its compact body, expressing agile instant capture without motion effects.
Composition: keep the character upright and emerging from the lower-<left|right>, filling about 85–95% of the square so it remains visually dominant. Cropping at the bottom or <left|right> is welcome. Keep the broad tail readable. Never center or bottom-center the character.
```

### C — Curled

```text
Subject: place one extremely simplified, cute, endearing baby white ferret curled into a compact near-circular pose on the background, reduced to one soft rounded continuous silhouette. Its defining feature is one broad rounded tail wrapping around the body to suggest gathering and preserving light, with the small face still clearly readable.
Composition: keep the character upright and emerging from the lower-<left|right>, filling about 85–95% of the square so it remains visually dominant. Cropping at the bottom or <left|right> is welcome. Keep the circular silhouette and face readable. Never center or bottom-center the character.
```
