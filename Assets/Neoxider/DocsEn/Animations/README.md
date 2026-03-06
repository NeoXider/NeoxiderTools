# Animation system

The animation system provides universal tools for smooth animation of various Unity components. The main idea is to offer easy-to-use components that can animate any values: numbers, colors, vectors, light intensity, and material emission.

## Features

- **Universal**: Animate any data types (float, Color, Vector3)
- **Multiple animation types**: From simple sine waves to Perlin noise
- **Easy to use**: One component — one animation
- **Configurable**: Many parameters for fine-tuning
- **Events**: UnityEvents to react to animated value changes
- **Sync**: Can sync between components

## Components

### Core

- **[AnimationType](AnimationType.md)** — Enumeration of animation types
- **[AnimationUtils](AnimationUtils.md)** — Static utilities for computing animated values

### Universal animators

- **[FloatAnimator](FloatAnimator.md)** — Animate numeric values
- **[ColorAnimator](ColorAnimator.md)** — Animate colors
- **[Vector3Animator](Vector3Animator.md)** — Animate vectors (position, scale, rotation)

### Related

- **[Tools/View](../Tools/View/README.md)** — LightAnimator, MeshEmission and other view animators (see Russian docs for per-component pages)

## Animation types

1. **RandomFlicker** — Random flicker between values
2. **Pulsing** — Smooth sine-based pulsing
3. **SmoothTransition** — Smooth back-and-forth transition
4. **PerlinNoise** — Natural Perlin noise animation
5. **SinWave** — Sine wave
6. **Exponential** — Exponential decay
7. **BounceEase** — Bounce with decay
8. **ElasticEase** — Elastic effect
9. **CustomCurve** — Custom animation curve

## Quick usage

1. Add the desired animator component to a GameObject
2. Configure parameters in the Inspector
3. Subscribe to events or read CurrentValue / CurrentVector / CurrentColor
4. Call `Play()` to start (or enable playOnStart)

## See also

- [Tools/View](../Tools/View/README.md) — Light and view animators
- [Extensions](../Extensions/README.md) — Additional animation utilities
- Russian docs: [Animations](../../Docs/Animations/README.md)
