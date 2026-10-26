# Animation system

The animation system provides universal tools for smooth animation of various Unity components. The main idea is to offer easy-to-use components that can animate any values: numbers, colors, vectors, light intensity, and material emission.

## Features

- **Universal**: Animate any data types (float, Color, Vector3)
- **Multiple animation types**: From simple sine waves to Perlin noise
- **Easy to use**: One component — one animation
- **Configurable**: Many parameters for fine-tuning
- **Events**: UnityEvents to react to animated value changes
- **Caller-owned time**: Evaluate several values with the same supplied time through `AnimationUtils`

## Components

### Core

- **[AnimationType](AnimationType.md)** — Enumeration of animation types
- **[AnimationUtils](AnimationUtils.md)** — Static utilities for computing animated values

### Universal animators

- **[FloatAnimator](FloatAnimator.md)** — Animate numeric values
- **[ColorAnimator](ColorAnimator.md)** — Animate colors
- **[Vector3Animator](Vector3Animator.md)** — Animate vectors (position, scale, rotation)
- **[TransformAnimator](TransformAnimator.md)** — Combinable transform channels: rotate, float, scale pulse, shake, impulse

### Related

- **[Tools/View](../Tools/View/README.md)** — LightAnimator, MeshEmission and other view animators (see per-component pages)

### Demo

Import the optional **Demo Scenes** sample through Unity Package Manager first. Its package source is
`Assets/Neoxider/Samples~/Demo` in the release layout, or `Assets/Neoxider/Samples/Demo` during sample
development. Unity imports it into `Assets/Samples/NeoxiderTools/<version>/Demo Scenes/...`.

Add `AnimatorsDemoController` (component menu **Neoxider/Demos/Animators Demo**) to an empty scene. At runtime it builds a `NeoDemoShell` UI plus a single disc driven at once by `Vector3Animator` (scale), `ColorAnimator` (colour) and `FloatAnimator` (spin), with buttons to cycle the shared `AnimationType` and Play/Pause/Stop, and a speed slider.

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
4. Call `Play()` to start (or enable `playOnStart`; `PlayOnEnable` for `TransformAnimator`)

## Runtime API and scene wiring

Use `AnimationUtils.GetAnimatedFloat`, `GetAnimatedColor`, or `GetAnimatedVector3` directly when the
caller owns the clock. Transform channels also have a scene-free API:
`TransformAnimationEvaluator.Evaluate` returns a `TransformAnimationState` from settings, a base pose,
time and a seed. See [TransformAnimator](TransformAnimator.md) for the settings and wrapper API.
Each component owns its clock; there is no shared clock or public manual-tick mode. To synchronize
samples explicitly, pass the same time to the static utilities, as in [AnimationUtils](AnimationUtils.md).

For scene authoring, configure the animator in the Inspector and connect `Value.OnChanged` (float),
`OnColorChanged`, or `OnVectorChanged` to the receiving component. Code can subscribe to
`FloatAnimator.Value.AddListener` or the color/vector UnityEvents. These value animators publish data;
the receiving code or event target applies it to a light, material, UI element or transform.

`FloatAnimator` writes each evaluated value to its reactive property, including changes below 0.001;
the property suppresses equal-value notifications. Color/vector current-value properties update each
playing, unpaused frame, while their change events require distance greater than 0.001 from the last
notified value (RGBA distance for color). Their initial start value is not emitted as a change event;
apply an initial visual value explicitly when binding. See the component pages for typed subscriptions
and listener cleanup.

## Playback and pooling contracts

| Operation | Float / Color / Vector3 | Transform |
|-----------|-------------------------|-----------|
| Startup | `Start` randomizes the clock and noise offset even when `playOnStart` is false | Captures the base on enable; optional `RandomizeStartTime`; `PlayOnEnable` starts at first startup and later re-enables |
| Time | Scaled `Time.deltaTime` while playing and unpaused | Scaled or unscaled time while playing and unpaused |
| `Play()` | Retains time and emits started on every call | Retains time; an already playing, unpaused call emits no duplicate started event |
| `Pause()` / `Resume()` | Pause emits while playing, including repeated calls; resume emits started only if paused | Repeated pause/resume calls are guarded; resume emits started |
| `Stop()` | Freezes the current output, retains time and emits stopped | Restores the captured local pose, clears impulse, retains main time and emits stopped |
| Disable / re-enable | Unity stops/continues Update; flags and output persist; `playOnStart` does not run again | Disable restores the base and clears playback/impulse without a stopped event; re-enable captures again and optionally plays |

`ResetTime()` and `RandomizeTime()` change the clock without immediately sampling. For the value
animators, a reset before `Start` is overwritten by startup randomization; the next active Update
advances time before evaluating. Non-positive speed selects the initial/minimum value without stopping
playback. Use the evaluators when an exact supplied phase is required.

По-русски: `AnimationUtils` и `TransformAnimationEvaluator` позволяют считать значения без сценового
компонента. Float передаёт и малые изменения через reactive property; Color/Vector3 сохраняют порог
событий. Для сценовой настройки подключите получателя через UnityEvent; жизненный цикл и подписки
описаны выше и на страницах компонентов.

## See also

- [Tools/View](../Tools/View/README.md) — Light and view animators
- [Extensions](../Extensions/README.md) — Additional animation utilities
