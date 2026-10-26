# Bonus / TimeReward

## Purpose

Use `CooldownReward` for persistent timed grants and `ResourceRegen` for a wallet plus optional countdown.
`TimeReward` remains available for existing scenes. Assign a unique **Add Key** to every independent timer.

Call `TakeReward()` in C# or bind `Take()` to a claim button. Grant hooks run after consumption is recorded;
recursive calls are rejected. With **Save Time On Take Reward** disabled, a successful take blocks further
claims until an explicit restart/re-arm. See each component for its persistence and accumulation policy.

## Documents

- [TimeReward](./TimeReward.md)
- [CooldownReward](./CooldownReward.md)
- [ResourceRegen](./ResourceRegen.md)

