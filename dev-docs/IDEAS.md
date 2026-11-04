# Ideas

Ideas that can become tasks later if a mechanic starts repeating across modules or samples.

## GridSystem

- Reusable demo/input layer for grid placement: `GridDragDropView`, `GridPlacementPointerResolver`, or a similar small adapter. The Dice sample drag/drop can stay demo-only for now because reusable snap and placement live in `FieldGenerator`; extracting the input layer is worthwhile only if the same flow repeats in several games.

## Time and determinism

- Adopt the `IFrameClock` / `INeoClock` seams in more consumers once a second user needs them: the Abilities scene hub tick, `DialogueLinePlayer` (now `Tick(deltaTime)` only, to avoid a `Neo.Tools.Dialogue` → `Neo.Tools.Time` dependency) and the animators' manual clock. A shared clock interface in a leaf assembly would remove that dependency question.
- One central ticker for many small per-frame components (animators, `UnityLifecycleEvents`, light/emission animators): a single `Update` that drives registered instances instead of one Unity message each. Worth it only when a profile of a large scene shows the message overhead.

## Interaction

- Ancestor-keyed interaction index (hits × depth, built once per frame) for targets with very large hierarchies and moderate hit counts. Only if a measured case shows the current per-target hierarchy walk is the cost.

## See Also

- [TODO](TODO.md)
- [Demo scene builders](DemoSceneBuilders.md)
- [GridSystem](../Assets/Neoxider/Docs/GridSystem/README.md)
