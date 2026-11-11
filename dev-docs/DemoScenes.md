# Demo scenes

Demo scenes are saved assets. The `.unity` file is the source of truth: UI, configured components, data references and
persistent UnityEvent listeners live in the scene and are edited in the Editor like any other scene.

## Rule: no scene builders in the repository

- Scripts that generate or rebuild a scene (scene builders, fixers, "rebuild" menus, builder kits and their tests) are
  not committed, neither in the package nor in `Assets/NeoxiderDemos`.
- A one-off editor script is fine as a tool: write it, run it once to set the scene up (batch mode,
  `-executeMethod`), check the result, save the scene, then delete the script before committing. Only the scene and its
  assets are committed.
- Nothing is assembled at Play start either: no runtime bootstrap builds demo UI or objects.
- Until 12.2.0 the demo carried `Neo.Demo.Editor` (60 builders, the DemoSceneKit and their tests). It was removed in
  12.3.0. The code is in git history at tag `v12.2.0` under `Assets/NeoxiderDemos/Demo/Editor`, should a helper be
  useful for a one-off script.

## What a demo scene contains

- `[Demo] <Key>` objects (`[Demo] Frame`, `[Demo] Services`, `[Demo] World`, `[Demo] HUD`, `[Demo] Info Card`): the
  demo's own UI and helpers.
- Objects without the prefix: the module or game objects the demo shows.
- A `ModuleDemoSceneInfo` with its authored overlay card on every scene of the Demo download.
- Buttons call module components directly through persistent listeners where possible; a `<Module>DemoController`
  only holds serialized references and public listener targets.
- NoCode reference games (`Shooter2D`, `ClickerExample`, `adventure`, `SurvivorDemo`) have no game scripts at all:
  only library components and UnityEvents.
- Demo save keys are `NeoDemo.<Demo>.<Data>`.
- uGUI + TextMeshPro only, never IMGUI.

## Checks

- `SampleScenePlaySmokeTests` loads every demo scene for 12 frames: no errors, no missing references reported by
  controllers (`IsSceneBuilt`), Filled Images with sprites, an authored info card on every Demo scene.
- `SurvivorDemoGameplayTests` reads the saved Survivor wiring (library components only, no game scripts) and wins a
  run with an autopilot; `-neoSurvivorFrames <dir>` writes frames for a gameplay video.
- Visual checks run in batch mode only: never open a GUI editor on the owner's desktop.
