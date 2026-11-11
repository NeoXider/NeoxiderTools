# Bonus System Documentation

**What it is:** an overview of the Bonus module (`Scripts/Bonus/`, namespace `Neo.Bonus`): wheel of fortune, slot machine, collections with progress boxes, and time-based rewards. Every mechanic has a plain C# core for code-first games and tests, and scene components that wrap it with Inspector fields and UnityEvents. For class-by-class navigation see [Bonus/README](Bonus/README.md).

**How to use:**
1. Pick the mechanic below and open its page.
2. In scenes, add the component and wire its UnityEvents; from code, use the same component API or the plain C# core directly.
3. Assign wallets and collections explicitly where a component offers a field; the global singletons (`Money.I`, `Collection.I`) are only the fallback.

---

## Wheel of Fortune (`WheelFortune`)

`Scripts/Bonus/WheelFortune/WheelFortune.cs`. Spins a UI wheel and reports the sector under the arrow through
`OnWinIdVariant(int)`. `Spin()` / `Stop()` drive it; `State` is `Idle`, `Spinning`, `Decelerating` or
`Aligning`; `canUse` gates a **Single Use** wheel. `WheelFortune.ResolveSectorIndex(wheelZ, arrowZ, offsetZ, count)`
is the component-free sector math. [WheelMoneyWin](Bonus/WheelFortune/WheelMoneyWin.md) turns a sector id into
a money prize. See [WheelFortune](Bonus/WheelFortune/WheelFortune.md).

## Slot machine (`SpinController`)

`Scripts/Bonus/Slot/SpinController.cs`. Charges the bet, spins `Row` reels, evaluates paylines with `CheckSpin`,
pays the wallet (**Auto Payout**) and raises `OnStartSpin`, `OnEndSpin`, `OnEnd(bool)`, `OnWin(int)`,
`OnWinLines(int[])`, `OnLose`, `OnChangeBet(string)` and `OnChangeMoneyWin(string)`. Main calls: `StartSpin()`,
`AddLine()` / `RemoveLine()`, `AddBet()` / `RemoveBet()` / `SetMaxBet()`, `ForceNextOutcome(int[,])`,
`StartEconomySpin()`, `GetLastResult()`. `ChanceWin` (0..1) shapes random plans. Plain C# pieces:
`CheckSpin` (payline rules) and `SlotSpinMath` (price and payout). See [Slot](Bonus/Slot/README.md).

## Collections (`CollectionSet`, `Collection`, `BoxProgress`, `Box`)

`Scripts/Bonus/Collection/`. [CollectionSet](Bonus/Collection/CollectionSet.md) is the plain C# unlock set:
independent instances are claimed, saved (`<prefix>Item_<index>`) and reset separately, with C# events and an
injectable random source. [Collection](Bonus/Collection/Collection.md) is the scene component (`Collection.I`)
over one set, exposed read-only as `Set` (`IReadOnlyCollectionSet`); change it through the component: `GetPrize()`, `TryAddItem(int)`, `RemoveItem(int)`, `ClearCollection()`, completion queries and
UnityEvents (`OnGetItem`, `OnItemAdded`, `OnItemRemoved`, `OnLoadItems`, `OnCompletionChanged`).
[BoxProgress](Bonus/Collection/BoxProgress.md) is an integer piggy bank with idempotent claim receipts;
[Box](Bonus/Collection/Box.md) is the float progress UI box that buys a locked item from a collection. Progress
and unlocks persist through `SaveProvider`.

## Time rewards (`CooldownSchedule`, `CooldownReward`, `ResourceRegen`)

`Scripts/Bonus/TimeReward/`. [CooldownSchedule](Bonus/TimeReward/CooldownSchedule.md) is the plain C# cooldown
rule on an injected `INeoClock` (tamper-resistant `NeoTrustedTime` by default) with save/restore.
[CooldownReward](Bonus/TimeReward/CooldownReward.md) is the scene component built on `TimerObject`: `TakeReward()`,
`CanTakeReward()`, `GetSecondsUntilReward()`, **Auto Claim**, `RemainingTime` and `OnRewardClaimed`.
[ResourceRegen](Bonus/TimeReward/ResourceRegen.md) regenerates a `Money` wallet. The legacy
[TimeReward](Bonus/TimeReward/TimeReward.md) is `[Obsolete]` and kept for existing scenes.

## Line Roulette (`LineRoulett`)

`Scripts/Bonus/LineRoulett.cs`. A horizontal strip of prize images; `StartRolling()` scrolls it and `OnWin(int)`
reports the index (in `sprites`) of the image closest to the arrow; **Use Unscaled Time** lets it finish during a
pause. See [LineRoulett](Bonus/LineRoulett.md).

## Example

```csharp
// Daily chest: a cooldown gates a box claim that grants a collectible.
CooldownSchedule daily = new(24f * 60f * 60f, availableInitially: true);
daily.TryLoadFrom("DailyChest");

CollectionSet stickers = new(24, "Stickers_");
stickers.Load();

// Check first that a sticker is left, so a complete set never burns the daily cooldown.
if (!stickers.IsComplete && daily.TryClaim(out int chests))
{
    daily.SaveTo("DailyChest"); // Persist the consumed cooldown before granting.
    for (int i = 0; i < chests && stickers.TryClaimNext(out int sticker); i++)
    {
        ShowSticker(sticker);
    }
}
```
