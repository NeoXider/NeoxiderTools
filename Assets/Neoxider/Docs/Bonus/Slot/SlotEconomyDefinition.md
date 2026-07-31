# SlotEconomyDefinition

**What it is:** `Neo.Bonus.SlotEconomyDefinition` — a slot-machine economy ScriptableObject: symbol table (drop weight, `MoneyReward`, `BonusReward`, special flag) + payline evaluation. Create via `Create → Neoxider → Bonus → Slot Economy`.

**With SpinController:** per spin — `PickWeightedId()` per reel → `ApplySpecialRule(ids)` (with `ForceLineOnSpecial`, one special converts the whole line) → feed ids to the reels → after settling, `EvaluateLine(ids)` → `LineResult` (`MoneyReward`, `BonusReward`, `SpecialTriggered`) → pay into `Money`/energy.

**Per-machine weight overrides:** assign the definition to `SpinController` (`Economy` field) and enable its local `SlotSymbolWeightOverrides` table to change drop weights for that machine only — the shared asset stays untouched. Entries match symbols by id (safe against reordering/extending the symbol list; unmatched symbols keep their definition weight), weight `0` disables a symbol, negatives clamp to `0`. `SpinController.PickEconomySymbolId()` picks through the override; the Inspector `⋮` menu **Normalize Weights** rescales all positive local weights to a total of `1`. Deterministic variants `PickWeightedId(selector, normalizedRoll)` support tests/replays/server outcomes.
