# ResourceRegen

**What it is:** `Neo.Bonus.ResourceRegen` — a regenerating resource (energy/lives) in one component: couples a `CooldownReward` (auto-claim forced on) with a `Money` wallet (deposits `Amount Per Claim` per cycle, capped by `Money.MaxMoney`) and an optional `TimeToText` (countdown from `RemainingTime`; shows 0 while full when `Show Zero When Full` is on).

**How to use:**
1. Put `Money` (set `Max Money`), `CooldownReward` (set `Cooldown Seconds`) and `ResourceRegen` on one object (`Scripts/Bonus/TimeReward/ResourceRegen.cs`).
2. Set `Amount Per Claim` and optionally assign a `TimeToText` countdown label.
3. Over-cap sources (bonuses, purchases) call `Money.AddOverflow` themselves.

---

For a code-only regeneration loop without components, drive a [CooldownSchedule](CooldownSchedule.md) with
`MaxClaimsPerTake = -1` and add `granted * amount` to your wallet on each `TryClaim`.
