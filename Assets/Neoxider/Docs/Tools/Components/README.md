# Tools / Components

Reusable components: counters, Animator driver, score, typewriter, loot, lifecycle events, and Interface.
Scripts live in `Scripts/Tools/Components/`. Legacy AttackSystem documentation remains indexed here for
discoverability, but its compatibility sources now live in `Scripts/Rpg/Combat/` and compile in the
optional `Neo.Rpg.Combat` assembly.

## Main components (docs)

| Component | Description |
|-----------|-------------|
| [Counter](./Counter.md) | Universal counter (int/float), events, optional save |
| [ScoreManager](./ScoreManager.md) | Score source for UI |
| [TextScore](./TextScore.md) | Bind a formatted label to current or best score |
| [RandomRange](./RandomRange.md) | Int/float random values with reactive output and events |
| [ModifyCounterByKey](./ModifyCounterByKey.md) | Apply an Inspector-configured operation by save key |
| [AnimatorParameterDriver](./AnimatorParameterDriver.md) | Drive Animator params from code/UnityEvent |
| [TypewriterEffect](./TypewriterEffect.md) | Typewriter text effect |
| [Loot](./Loot.md) | Loot/drop logic |
| [UnityLifecycleEvents](./UnityLifecycleEvents.md) | Lifecycle as UnityEvents |

## Submodules

- [AttackSystem](AttackSystem/README.md) — legacy Health, Evade, AttackExecution, and
  AdvancedAttackCollider; sources in `Scripts/Rpg/Combat/`. Its canonical C#/scene contract is
  [RPG / Combat compatibility](../../Rpg/Combat/README.md).
- [Interface](Interface/README.md) — InterfaceAttack and related.

## Runtime API and scene setup

Use `Counter.Set/Add/Subtract/Multiply/Divide` for counter changes, `ScoreManager.Set/Add` for score,
and `TypewriterEffect.PlayAsync` for reusable text playback. Subscribe to reactive properties with
`Subscribe` and dispose the returned handle when the consumer is disabled or destroyed. Directly
writing a component's public reactive output bypasses its arithmetic, persistence, and typed event path.

Scene users can configure these components in the Inspector and connect their UnityEvents to typed
methods. `TextScore` accepts an explicit score source and otherwise resolves the singleton; disabled
views release both subscriptions and pending source searches. The typewriter scene wrapper starts
automatic playback in `Start`, preserving authored text until playback begins.

`CameraAspectRatioScaler` and `UpdateChilds` also compile in `Neo.Tools.Components`; their existing
guides are [CameraAspectRatioScaler](../CameraAspectRatioScaler.md) and [UpdateChilds](../UpdateChilds.md).
For camera render textures, the scaler uses the texture dimensions and `FitBoth` expands the projection
to keep the reference view visible. [Unity ignores camera viewport rectangles for render textures](https://docs.unity3d.com/6000.0/Documentation/ScriptReference/Camera-targetTexture.html),
so this path does not create viewport bars. Changing texture dimensions or projection mode invalidates
the runtime cache. Disable **Update In Editor** to suppress automatic edit-mode previews, including
`Start` and disable cleanup; the explicit **Apply Camera Scale** button remains available.

## See also

- [Condition](../../Condition/README.md)
- [Tools/Time](../Time/README.md)
- [Save](../../Save/README.md)
