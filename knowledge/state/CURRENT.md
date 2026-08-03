# Current Project State

- Updated: 2026-08-03
- Release target: HDR-aware MVP with sRGB Visual Match
- Operating model: Contract → Frontier → Evidence

## Current Position

The repository contains the intended MVP capture baseline, shared sRGB Visual Match
output conversion, default fixed tone mapper, clipboard/folder policies, settings,
main/tray/shortcut entry points, and simplified native capture UI.

Packaging direction is settled as a traditional setup executable with custom install
path support. Installer technology and artifact work have not started.

## Verification Truth

- **Repository done:** implementation and platform-neutral tests are present, but the
  current documentation migration has not yet been verified on Windows.
- **Windows verified:** no passing release-candidate record exists for the current tree.
- **Hardware evidenced:** no passing HDR visual-match record exists for the current tree.

Do not describe the MVP as release-ready until the applicable evidence is committed.

## Frontier

Run and record the Windows MVP release validation for the post-migration commit:

1. Publish or claim one GitHub Issue using the repository task template.
2. Run all commands in `knowledge/runbooks/windows-development.md`.
3. Exercise launch, region/fullscreen capture, cancel, clipboard/folder/both output,
   repeat loop, and clean exit.
4. Record bright HDR, dark, and everyday desktop scenes using the evidence template.
5. Fix blocking defects before starting packaging implementation.

After Windows and hardware evidence pass, the next product frontier is installer
technology selection and an internal installer artifact.
