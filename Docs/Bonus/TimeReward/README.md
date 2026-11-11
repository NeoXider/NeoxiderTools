# Bonus / TimeReward

**What it is:** time-gated rewards in `Scripts/Bonus/TimeReward/`: the plain C# rule
[CooldownSchedule](CooldownSchedule.md) (injected `INeoClock`, restart-safe, no scene needed), the scene
component [CooldownReward](CooldownReward.md), [ResourceRegen](ResourceRegen.md) (wallet regeneration) and the
legacy [TimeReward](TimeReward.md). All of them count claims with `CooldownSchedule.CountClaimable`.

**Contents:** see [Documents](#documents).

---

## Purpose

Use `CooldownSchedule` for code-first cooldowns (daily gifts on a server, tests with a fake clock), `CooldownReward`
for persistent timed grants in scenes and `ResourceRegen` for a wallet plus optional countdown.
`TimeReward` remains available for existing scenes. Assign a unique **Add Key** to every independent timer.

Call `TakeReward()` in C# or bind `Take()` to a claim button. Grant hooks run after consumption is recorded;
recursive calls are rejected. With **Save Time On Take Reward** disabled, a successful take blocks further
claims until an explicit restart/re-arm. See each component for its persistence and accumulation policy.

**Demo:** `Scenes/Bonuses/BonusRewardsDemo.unity` in the Demo Scenes sample puts a `CooldownReward` gift (10 s
cooldown, **Gift now** through `SetRewardAvailableNow`, **Restart** through `RestartTime`) next to the legacy
`TimeReward`, both on `NeoDemo.` save keys.

## Documents

- [CooldownSchedule](./CooldownSchedule.md)
- [TimeReward](./TimeReward.md)
- [CooldownReward](./CooldownReward.md)
- [ResourceRegen](./ResourceRegen.md)

