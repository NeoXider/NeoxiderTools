# Cookbook — recipes & interesting examples

Practical **cross-module** combinations: how to assemble a common mechanic from ready-made NeoxiderTools
components **without custom code** (or with minimal code). Unlike the per-component references, these
are end-to-end recipes — what to put on which object and which events to wire.

> Per-component docs live in their modules ([README](./README.md)). This page is about **combining** them.

## Contents
- [Capped energy + auto-regen](#capped-energy--auto-regen)
- [Capped resource/currency](#capped-resourcecurrency)
- [Daily reward](#daily-reward)
- [Slot machine → wallet](#slot-machine--wallet)
- [Shop: buy and equip](#shop-buy-and-equip)
- [Reward fly to HUD](#reward-fly-to-hud)

---

## Capped energy + auto-regen

**Goal:** an "energy" resource that regenerates **+1 every 2 minutes**, never exceeds **30** from regen,
but **may go above 30** from bonuses/slots. Components and events only — no scripts.

**One `Energy` object, two components:**

1. **`Money`** (energy wallet):
   - `_moneySave = "Energy"` — its own save key, separate from the main currency;
   - `_persistMoney = true`;
   - **`_maxMoney = 30`** — the soft cap.
2. **`CooldownReward`** (regen timer):
   - `_cooldownSeconds = 120` (2 minutes);
   - **`_autoClaim = true`** — auto-claims the reward when available (continuous regen);
   - `_startTakeReward = true` — claim **offline** accumulation on start;
   - `_maxRewardsPerTake = -1` — grant all accumulated at once (after a long absence);
   - `_addKey = "EnergyRegen"` — a separate timer save key.

**Event wiring (Inspector):**
- `CooldownReward.OnRewardClaimed` → `Energy (Money).Add`, static argument **`1`**.

`OnRewardClaimed` fires **once per** granted unit, so offline accumulation triggers N `Add(1)` calls,
and `Money` clamps the balance to `_maxMoney = 30` by itself.

**How it works:**
- Every 120s the timer makes the reward available → `_autoClaim` grabs it → `OnRewardClaimed` →
  `Money.Add(1)` → clamped to 30.
- Away for an hour and back → `_startTakeReward` + `_maxRewardsPerTake = -1` grant the backlog at once,
  `Money` caps it at 30.
- At full bank `Add(1)` is a no-op (clamp). After spending (`Spend`) it drops below 30 → regen tops up again.

**Spending and bonuses (code/buttons):**
- Spend per action: `energy.Spend(1)` or `energy.TrySpend(1)`.
- Reward **above** the cap (slots, energy purchase): `energy.AddOverflow(5)` — ignores the cap.

**UI:**
- Value — `TextMoney`/`SetText` bound to this `Money`.
- Time to next unit — `CooldownReward.RemainingTime` (`ReactivePropertyFloat`) → `TimeToText.Set`.

Components: [Money](./Shop/Money.md) · [CooldownReward](./Bonus/TimeReward/CooldownReward.md) · [TimeToText](./Tools/Text/TimeToText.md)

---

## Capped resource/currency

Any `Money` wallet with an upper limit: set **`_maxMoney > 0`**. `Add()` and `SetMoney()` won't exceed it.
When a specific reward **must** exceed the cap — call **`AddOverflow(float)`** (ignores the cap). This is how
"lives", "tickets", "energy" are built without a dedicated script.

Components: [Money](./Shop/Money.md)

---

## Daily reward

`CooldownReward` on a "claim" button: `_cooldownSeconds = 86400`, `_rewardAvailableOnStart = true`,
`_maxRewardsPerTake = 1`, `_autoClaim = false`. Wire the "Claim" button → `CooldownReward.Take()`; put the
reward on `OnRewardClaimed` (e.g. `Money.Add(100)`). The cooldown runs on UTC and **between sessions**.

Components: [CooldownReward](./Bonus/TimeReward/CooldownReward.md) · [Money](./Shop/Money.md)

---

## Slot machine → wallet

`SpinController` spins the reels; wire the payout via event: `SpinController.OnWin (int)` → `Money.Add`.
"Spin" button → `SpinController.StartSpin()`. Spin cost — via `betsData`/price or a separate currency
(e.g. energy: call `energy.Spend(1)` before `StartSpin`). Exact symbol outcome — `GetLastResult()` (`SpinResult`).

Components: [SpinController](./Bonus/Slot/SpinController.md) · [Money](./Shop/Money.md)

---

## Shop: buy and equip

`Shop` with a `ShopItemData` catalog and `_purchaseFlow = BuyAndEquip`. Buy buttons auto-subscribe
(`_autoSubscribe`) or call `Shop.Buy(item)`. Ownership — `Shop.IsOwned(item)`; price/"sold" — via `ButtonPrice`.
For several **simultaneously** equipped categories (dress+shoes+…), track the per-category selection yourself
(`GetItemsInCategory`, your own equip layer) and let `Shop` handle purchase/ownership.

Components: [Shop](./Shop/README.md) · [Money](./Shop/Money.md)

---

## Reward fly to HUD

A coin/item sprite flies from the world/button into a HUD slot: one `AnimationFly` in the scene (with
`parentCanvas`), call `AnimationFly.I.PlaySpriteWorldToCanvas(sprite, n, worldStart, uiSlot)`. The start
position is read synchronously — the original can be destroyed immediately; update the counter **right away**,
the flight is cosmetic.

Components: [AnimationFly](./UI/AnimationFly.md)

---

## See also
- [Getting Started — first scene in 5 minutes](./GettingStarted.md)
- [Samples](./Samples.md)
