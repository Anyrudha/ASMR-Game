# RESTORE: Dirty Sneaker Prototype

Open this folder in **Unity 2022.3 LTS** or newer. Unity is not installed in the current environment, so the project could not be launched here. Open `Assets/Scenes/Restore.unity` and press Play. The prototype uses only built-in Unity primitives and runtime-generated placeholder audio, so no asset import is required.

## Interaction

Hold the mouse button in the editor, or drag a finger on a device, across the sneaker. Each mud patch is an independent `DirtPatch`; progress is calculated from the number of patches actually removed, rather than from pointer distance. The reusable `DirtLayer` accepts a `CleaningTool`, and `RestorationManager` owns the level sequence and completion state.

The current prototype demonstrates Water, Foam, Brush, Rinse, and Air Dry labels, generated ASMR placeholder tones, particle-ready tool boundaries, and haptic calls. The visual is intentionally runtime-generated so the project is immediately inspectable without external art.

## Extending it

- Add another restoration object by creating a second `DirtLayer` subclass or replacing `BuildSneaker()` with a new arrangement of `DirtPatch` objects.
- Add a tool by extending `CleaningTool`, then define its radius and behavior in `RestorationManager` and its label in `CleaningToolRules`.
- Add a level by creating a scene with `RestoreBootstrap`, or by adding a level data object and passing its target layer to `RestorationManager.Initialise()`.
- Replace the generated clip in `AudioManager` with `water.wav`, `foam.wav`, `brush.wav`, `dryer.wav`, and `completion.wav` AudioClips assigned through an inspector-backed audio catalog.

## Production follow-up

The prototype's patch mask is a lightweight stand-in for a GPU texture mask. For production, replace `DirtLayer`'s patch list with a low-resolution `RenderTexture` brush mask and read progress from periodic GPU reduction, preserving the same `CleanAt` API. Add pooled water/foam particles and authored sneaker materials. `Handheld.Vibrate()` is the platform abstraction; tune Android/iOS native haptic patterns during device testing.