# CardAnimationConfig

**What it is:** a `ScriptableObject` with the timing and motion values of card animations: dealing, deck shuffle
visuals, the visual stack and the card-view presets (bounce, pulse, shake, highlight, fly-in, idle). Script:
`Scripts/Cards/Config/CardAnimationConfig.cs`, namespace `Neo.Cards`.

**How to use:**
1. Create an asset via **Create > Neoxider > Cards > Card Animation Config**.
2. Assign it to [DeckComponent](../DeckComponent.md) (**Animation Config**). With **Set Animation Config As
   Global** on (the default), the deck also publishes it as `CardSettingsRuntime.GlobalAnimationConfig` in
   `Start`, the fallback for other card components.
3. Optionally assign a different asset to a [BoardComponent](../BoardComponent.md) or a
   [CardViewUniversal](../View/CardViewUniversal.md); pass it as the `config` argument of
   `CardViewAnimationTemplates` calls in custom views.

---

## Fields

All values are read-only properties with the same name (for example `DealMoveDuration`). Durations are seconds.

| Header | Field | Default | Read by |
|--------|-------|---------|---------|
| Deal | `DealMoveDuration` | `0.3` | `DeckComponent` card move when no explicit duration is passed. |
| Deal | `DealStepDelay` | `0.12` | Not read by built-in components; available to custom deal sequences. |
| Shuffle / Shake | `ShakeDuration`, `ShakeIntensity`, `ShakeFrames` | `1`, `0.12`, `20` | `DeckComponent` shake shuffle visual (`ShakeFrames` is at least `1`). |
| Shuffle / Cut | `CutDuration` | `0.8` | `DeckComponent` cut shuffle visual. |
| Shuffle / Cut | `CutLiftHeight` | `0.5` | Not read by built-in components. |
| Shuffle / Riffle | `RiffleDuration` | `1.2` | `DeckComponent` riffle shuffle visual. |
| Shuffle / Riffle | `RiffleSpread` | `0.3` | Not read by built-in components. |
| Stack | `StackPositionJitter`, `StackRotationJitter`, `StackStepY` | `(0.02, 0, 0.02)`, `(0, 0, 3)`, `0.01` | Visual stack layout of `DeckComponent` and `BoardComponent`; overrides the components' own stack fields. |
| Card View | `CardBounceDuration`, `CardBouncePunch` | `0.25`, `0.15` | `CardViewAnimationTemplates.Bounce`. |
| Card View | `CardPulseDuration`, `CardPulseScale` | `0.4`, `0.08` | `Pulse` and `PulseLooped` (cycle = 2 x duration). |
| Card View | `CardShakeDuration`, `CardShakeStrength` | `0.3`, `8` | `Shake`. |
| Card View | `CardHighlightDuration` | `0.2` | `Highlight`, `HighlightGraphic`. |
| Card View | `CardFlyInDuration` | `0.35` | `FlyIn`. |
| Card View | `CardIdleDuration`, `CardIdleScale` | `1.2`, `0.02` | `Idle`. |

`BoardComponent` resolves its config in this order: its own field, the settings-source deck's config, then the
global config.

## Example

```csharp
Tween bounce = CardViewAnimationTemplates.Bounce(cardTransform, config: animationConfig);
```

## See also

- [DeckComponent](../DeckComponent.md), [BoardComponent](../BoardComponent.md)
- [CardViewUniversal](../View/CardViewUniversal.md) — the view that plays the presets.
- [Cards module](../README.md)
