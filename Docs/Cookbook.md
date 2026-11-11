# Cookbook — recipes & interesting examples

**What it is:** practical **cross-module** combinations: how to assemble a common mechanic from ready-made
NeoxiderTools components **without custom code** (or with minimal code). Unlike the per-component references,
these are end-to-end recipes — what to put on which object and which events to wire.

**How to use:** pick a recipe from the contents, add the listed components and wire the events as described;
each recipe ends with links to the component pages.

> Per-component docs live in their modules ([README](./README.md)). This page is about **combining** them.

## Contents
- [Capped energy + auto-regen](#capped-energy--auto-regen)
- [Capped resource/currency](#capped-resourcecurrency)
- [Daily reward](#daily-reward)
- [Slot machine → wallet](#slot-machine--wallet)
- [Shop: buy and equip](#shop-buy-and-equip)
- [Reward fly to HUD](#reward-fly-to-hud)
- [Reward earned on a popup: fly on the next screen](#reward-earned-on-a-popup-fly-on-the-next-screen)
- [Mobile audio: music, ambience, background pause](#mobile-audio-music-ambience-background-pause)
- [Sound-off that survives restarts](#sound-off-that-survives-restarts)
- [Jackpot with exact odds](#jackpot-with-exact-odds)
- [Money in cents](#money-in-cents)
- [NPC barks that do not repeat](#npc-barks-that-do-not-repeat)
- [Quest credit from enemy prefabs](#quest-credit-from-enemy-prefabs)
- [Android Back over PM pages](#android-back-over-pm-pages)

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

Assign the wallet (`Money`) to the `SpinController` money field. With **Auto Payout** on (the default) the
controller charges the spin price from that wallet and credits winnings itself, so use
`SpinController.OnWin (int)` for feedback only (sound, `AnimationFly`, a win label). Wire `OnWin` → `Money.Add`
only after turning **Auto Payout** off, otherwise the player is paid twice.
"Spin" button → `SpinController.StartSpin()`. Spin cost — via `betsData`/price or a separate currency
(e.g. energy: call `energy.Spend(1)` before `StartSpin`). Exact symbol outcome — `GetLastResult()` (`SpinResult`).
Servers and tests can compute the same price and payout with the plain C# `SlotSpinMath`.

Components: [SpinController](./Bonus/Slot/SpinController.md) · [Money](./Shop/Money.md)

---

## Shop: buy and equip

`Shop` with a `ShopItemData` catalog and `_purchaseFlow = BuyAndEquip`. Buy buttons auto-subscribe
(`_autoSubscribe`) or call `Shop.Buy(item)`. From C#, prefer `Shop.TryBuy(item)`: it returns a
`ShopPurchaseStatus` (`Purchased`, `AlreadyOwned`, `InsufficientFunds`, ...) so you can show the right message
without listening to UnityEvents. Ownership — `Shop.IsOwned(item)`; price/"sold" — via `ButtonPrice`.
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

## Reward earned on a popup: fly on the next screen

**Goal:** the Win or Gift popup grants coins, then closes. Flying the coins into a counter that is being
hidden looks like they vanished, so the flight plays on the screen the player lands on.

1. When the reward is granted, write the new balance to the save **immediately** and queue
   `{ amount, balanceAfter, source }` instead of flying.
2. When the destination page opens, wait for its entrance (~0.35 s), set its counter to
   `balanceAfter - amount`, and play one flight into that page's counter icon:
   `AnimationFly.I.Play(new AnimationFly.AnimationFlyRequest { Sprite, Count = min(amount, 12),
   StartTransform, EndTransform, …, OnItemArrived, OnAllArrived })`. Set every field before `Play`:
   the request is copied, so later edits do not reach the running flight.
3. `OnItemArrived` raises the counter by `amount * arrived / count` and punches the icon;
   `OnAllArrived` sets the exact balance. Use `MotionPreset = FountainMagnet` and
   `EndScaleMultiplier ≈ 0.3` so arrivals melt into the icon.

Rewards collected on the screen that already shows the counter (a map pin, a shop pack) fly at once
from the collected object.

Components: [AnimationFly](./UI/AnimationFly.md)

---

## Mobile audio: music, ambience, background pause

- Menu and gameplay music through `AM` with crossfade; a quiet bed under both with
  `AM.I.PlayAmbience(clip)` and `AmbienceVolume` (it follows the music volume and mute).
- One tap sound for every button: at start, hook every `Button` under the `PM` root
  (`GetComponentsInChildren<Button>(true)` also finds inactive pages).
- Pause everything in the background and during rewarded ads with one owner of
  `AudioListener.pause` — see [AM → Pausing in the background](./Audio/AM.md#pausing-in-the-background-and-during-ads).
- Pair key sounds with [Haptics](./Haptics/Haptics.md): `Success` on a win, `Selection` on a grab,
  `Warning` on a locked tap, with `Haptics.EnabledProvider` bound to the vibration setting.

Components: [AM](./Audio/AM.md), [Haptics](./Haptics/Haptics.md)

---

## Sound-off that survives restarts

Put `AMSettings` next to `AM` and leave **Persist Volume** and **Persist Mute** on (the defaults). Wire the
music and sound toggles to `AMSettings.SetMusic(bool)` / `SetEfx(bool)` (or `ToggleMusic()` / `ToggleEfx()`).
The choice is saved and restored on the next launch, and binding to `AM` no longer turns sound back on.
Do not use these methods for temporary silence: for a pause menu, an ad or a cutscene use
`AudioListener.pause`, a mixer snapshot or `AudioFocusMute`, so the player's saved preference stays intact.

Components: [AMSettings](./Audio/AMSettings.md), [AudioFocusMute](./Audio/AudioFocusMute.md)

---

## Jackpot with exact odds

Float weights cannot express "1 in a million" exactly. Keep a serialized `IntegerChanceTable` on your
reward component and author the rows in the Inspector (Jackpot `1`, Big win `4999`, Nothing `995000`);
call `Roll()` for the outcome index. The probability of each row is exactly `weight / TotalWeight`, and
assigning `RandomBelow` lets tests and servers inject their own seeded source.

```csharp
using Neo.Tools;
using UnityEngine;

public sealed class DailyChest : MonoBehaviour
{
    [SerializeField] private IntegerChanceTable _odds = new IntegerChanceTable();

    public int Open()
    {
        return _odds.Roll();
    }
}
```

Components: [IntegerChanceTable](./Tools/Random/IntegerChanceTable.md), [ChanceSystemBehaviour](./Tools/Random/ChanceSystemBehaviour.md) for float weights with UnityEvents

---

## Money in cents

Store large or fractional currencies as a 64-bit integer number of cents instead of a float:
`SaveProvider.SetLong("Wallet.Cents", cents)` and `SaveProvider.GetLong("Wallet.Cents", 0L)`. Values above
`int.MaxValue` round-trip on every provider, and keys written earlier with `SetInt` read back unchanged.

Components: [SaveProvider](./Save/SaveProvider.md)

---

## NPC barks that do not repeat

Add **Neoxider > Tools > Dialogue > DialogueLinePlayer** to an NPC, fill **Lines** with a tag per situation
(`idle`, `hit`, `greet`) and assign a TMP label. Idle lines play on **Idle Interval**; wire gameplay
UnityEvents to `RequestContext("hit")` to interrupt idle chatter. The pool never repeats the previous line
of a tag back to back, and `FirstOnly` lines play only for the first pick (a greeting). From C#, use the
plain `DialogueLinePool` without a component.

Components: [DialogueLinePool](./Tools/Dialogue/DialogueLinePool.md)

---

## Quest credit from enemy prefabs

Give each enemy prefab a `QuestNoCodeAction` with the **NotifyKill** action and a **Target Id** (`wolf`),
and call its `Execute()` from the enemy's death event. Pickups use **NotifyCollect**, trigger zones
**NotifyReachPoint**, and NPC dialogs **NotifyTalk**. No scene reference to the quest manager is needed;
from C#, call `QuestManager.NotifyKill("wolf")` or `Notify(type, targetId, amount)`. An empty Target Id
credits nothing.

Components: [Quest](./Quest/README.md)

---

## Android Back over PM pages

One handler in the game controller: on Back, look at `PM.I.currentUiPage.PageId` and run the same
action as that page's on-screen back/close button. Popups close, sub-pages go to their parent, result
pages (Win, Gift, Time's Up) ignore Back, and the root menu minimises the app:

```csharp
#if UNITY_ANDROID && !UNITY_EDITOR
using (AndroidJavaClass player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
    activity.Call<bool>("moveTaskToBack", true);
#endif
```

With the Input System, read Escape from every `Keyboard` in `InputSystem.devices` and, on Android, also
treat `Application.wantsToQuit` as Back (return `false`); handle one press once per frame.

Components: [NeoxiderPages](./NeoxiderPages/README.md)

---

## See also
- [Getting Started — first scene in 5 minutes](./GettingStarted.md)
- [Samples](./Samples.md)
