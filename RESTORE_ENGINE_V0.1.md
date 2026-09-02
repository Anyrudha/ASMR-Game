# Project Restore — Restore Engine v0.1

This iteration replaces the original on/off dirt-patch prototype with a runtime texture-alpha cleaning mask.

## What changed

- Dirt is stored as alpha in a texture and erased gradually by touch/drag.
- Cleaning progress is based on the remaining dirty pixels.
- The restoration flow is guided through Water → Foam → Brush → Rinse → Dryer.
- Tool actions have different strengths/radii.
- Camera now has an AudioListener.
- Input is touch-first with mouse fallback for Editor testing.
- UI now communicates the current restoration step.
- Audio and haptic services remain abstracted so production assets can be added later.
- Project now has a Unity-friendly `.gitignore`.

## Current prototype limitations

This is intentionally a technical vertical-slice prototype, not final art.

- Sneaker artwork is procedurally generated and will later be replaced by a production 3D/2D asset.
- Audio is still synthetic placeholder audio.
- Haptics currently use Unity's basic `Handheld.Vibrate()` fallback.
- Foam is represented by stage interaction/progress rather than a separate foam mask.
- There is no monetization, analytics, save system, or content pipeline yet.

## How to test

1. Open the project in Unity 6.0.6f1 or a compatible Unity 6 release.
2. Open `Assets/Scenes/Restore.unity`.
3. Press Play.
4. In the Editor, hold the left mouse button and drag across the sneaker.
5. On a phone, drag your finger over the sneaker.
6. Follow the guided tools until the progress reaches 100%.

## Next milestone

Restore Engine v0.2 will focus on making the interaction feel genuinely ASMR:

- real foam coverage mask
- better water spray
- brush bristles and contact feedback
- dirt edge/falloff tuning
- continuous drag interpolation
- proper audio assets
- subtle haptic patterns
- camera/lighting polish
- production-quality sneaker asset
