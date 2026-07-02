# SlotEconomyDefinition

**What it is:** `Neo.Bonus.SlotEconomyDefinition` — a slot-machine economy ScriptableObject: symbol table (drop weight, `moneyReward`, `bonusReward`, special flag) + payline evaluation. Create via `Create → Neoxider → Bonus → Slot Economy`.

**With SpinController:** per spin — `PickWeightedId()` per reel → `ApplySpecialRule(ids)` (with `ForceLineOnSpecial`, one special converts the whole line) → feed ids to the reels → after settling, `EvaluateLine(ids)` → `LineResult` (`MoneyReward`, `BonusReward`, `SpecialTriggered`) → pay into `Money`/energy.
