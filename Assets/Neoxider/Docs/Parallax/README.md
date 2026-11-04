# Parallax module

<!-- module-art:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/modules/parallax.jpg" alt="Concept art for the Parallax module" width="100%" /></p>
<!-- module-art:end -->

**What it is:** parallax layers for 2D scrolling (`Neo.Parallax`, scripts in `Scripts/Parallax/`). Use this page as the English module entry.

<!-- demo-shots:begin -->
<p align="center"><img src="https://raw.githubusercontent.com/NeoXider/NeoxiderTools/main/Images/demos/ParallaxDemo.jpg" alt="ParallaxDemo: seven camera-bound parallax bands with auto-scrolling clouds" width="100%" /></p>

*Demo scene: `Scenes/Parallax/ParallaxDemo.unity`, in the **Demo Scenes** sample (Package Manager > NeoxiderTools > Samples). The scenes are saved with their UI and wiring, so you can inspect them without entering Play Mode.*
<!-- demo-shots:end -->

**Contents:** [ParallaxLayer](./ParallaxLayer.md) - the one component of the module.

---

Demo: `Samples~/Demo/Scenes/Parallax/ParallaxDemo.unity`, controller `Samples~/Demo/Scripts/Shell/ParallaxDemoController.cs`. The developer layout uses `Samples/` instead of `Samples~/`; importing **Demo Scenes** through Package Manager copies it under `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/`.

The scene is authored by the editor builder `Samples~/Demo/Editor/Builders/Parallax/ParallaxDemoBuilder.cs`, so every layer is visible and configurable without Play Mode. `[Demo] World` holds seven bands, each a `ParallaxLayer` bound to the scene camera through `targetCamera`:

| Band | `parallaxMultiplier.x` | Shows |
|---|---|---|
| Sun | -1.00 | follows the camera (infinitely far), no tiling |
| Clouds | -0.85 | plus `scrollSpeed` -0.35: drifts even when the camera stops |
| Far mountains / Mid hills / Near hills | -0.75 / -0.50 / -0.25 | deeper bands follow the camera more |
| Ground | 0.00 | stays in the world |
| Front bushes | +0.40 | races past in front, with `tileSpacing` gaps |

The card's slider, toggle and buttons only move the camera (`ParallaxDemoController.SetPanSpeed`, `SetAutoPan`, `Reverse`, `Jump`, `ResetCamera`); the 25-unit jumps show that tiles recycle in one step.

## docs

- [ParallaxLayer](./ParallaxLayer.md) — C# camera binding, Inspector/UnityEvent setup, source ownership, count/local-geometry validation and recovery, preview, gaps and randomization.

## See also

- [Animations](../Animations/README.md)
