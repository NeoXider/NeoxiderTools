# ResourceRegen

**What it is:** `Neo.Bonus.ResourceRegen` — a regenerating resource (energy/lives) in one component: couples a `CooldownReward` (auto-claim forced on) with a `Money` wallet (deposits `Amount Per Claim` per cycle, capped by `Money.MaxMoney`) and an optional `TimeToText` (countdown from `RemainingTime`; shows 0 while full when `Show Zero When Full` is on).

**Setup:** one object: `Money` (set `Max Money`) + `CooldownReward` (set `Cooldown Seconds`) + `ResourceRegen`. Over-cap sources (bonuses, purchases) call `Money.AddOverflow` themselves.
