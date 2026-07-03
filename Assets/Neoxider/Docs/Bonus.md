# Bonus System Documentation

**What it is:** an overview of the Bonus module (wheel of fortune, slot machine, collections, time-based rewards). For detailed class navigation, see [Bonus/README](Bonus/README.md).

**How to use:** see the sections below or [Bonus/README](Bonus/README.md).

---


The **Bonus** module provides a set of ready-to-use bonus game mechanics that can be easily integrated into your project. It includes a Wheel of Fortune, a slot machine, an item collection system, and time-based rewards. These tools help boost player engagement and retention.

## Core Bonus Mechanics

### Wheel of Fortune (`WheelFortune`)
**Namespace:** `Neo.Bonus`
**Path:** `Scripts/Bonus/WheelFortune/WheelFortune.cs`

Implements a "Wheel of Fortune" mechanic. Lets the player spin a wheel to receive a random prize from a predefined set.

**Key features:**
- Configurable spin and deceleration speed.
- Automatic or manual placement of prizes around the wheel.
- Optional wheel alignment for precise stopping on a sector.

**Public Properties and Fields:**
- `SpinState State { get; }`: Returns the current wheel state (`Idle`, `Spinning`, `Decelerating`, `Aligning`).
- `GameObject[] Items { get; }`: Returns the array of game objects that serve as prizes.
- `bool canUse { get; set; }`: Determines whether the wheel can be used. When set to `false` in single-use mode (`singleUse`), spinning again becomes impossible.

**Public methods:**
- `Spin()`: Starts spinning the wheel.
- `Stop()`: Initiates the wheel stop.
- `GetPrize(int id)`: Returns the prize game object by its ID.

**Unity Events:**
- `OnWinIdVariant (int)`: Invoked after the wheel stops. Passes the ID of the winning item.

### Slot Machine (`SpinController`)
**Namespace:** `Neo.Bonus`
**Path:** `Scripts/Bonus/Slot/SpinController.cs`

A full-featured slot machine. Manages bets, lines, reel spinning, and win combination detection.

**Key features:**
- Flexible configuration via `ScriptableObject` (bets, lines, symbols, multipliers).
- Bet management and control over the number of active lines.
- Win probability control (`ChanceWin`).
- Optional runtime highlighting of winning lines via **LineRenderer** (`WinLineRendererPlayback` in the `SpinController` inspector); see [SpinController](./Bonus/Slot/SpinController.md) for details.

**Public Properties and Fields:**
- `float ChanceWin`: Win probability from 0 to 1 (the serialized field was previously named `chanseWin`).
- `IMoneySpend moneySpend`: Interface implementation for deducting funds per spin.
- `int[,] FinalElementIDs`: Matrix of symbol IDs in the visible window after stopping (y=0 at the bottom).

**Public methods:**
- `StartSpin()`: Starts spinning the reels.
- `IsStop()`: Returns `true` if all reels have stopped.
- `AddLine()` / `RemoveLine()`: Changes the number of active lines.
- `AddBet()` / `RemoveBet()`: Changes the current bet.
- `SetMaxBet()`: Sets the maximum bet.

**Unity Events:**
- `OnStartSpin`: Invoked when spinning starts.
- `OnEndSpin`: Invoked after all reels fully stop and results are calculated.
- `OnEnd (bool)`: Invoked at the end of a spin. Passes `true` if there was a win, otherwise `false`.
- `OnWin (int)`: Invoked on a win. Passes the total win amount.
- `OnWinLines (int[])`: Invoked on a win. Passes an array of winning line IDs.
- `OnLose`: Invoked on a loss.
- `OnChangeBet (string)`: Invoked when the total bet changes. Passes the bet amount as a string.
- `OnChangeMoneyWin (string)`: Invoked on a win. Passes the win amount as a string.

### Time Reward (`TimeReward`)
**Namespace:** `Neo.Bonus`
**Path:** `Scripts/Bonus/TimeReward/TimeReward.cs`

Implements a system where the player can claim a reward after a certain amount of time has passed.

**Key features:**
- Configurable reward wait period.
- Saves the time of the last claimed reward in `PlayerPrefs`.
- Public timer control: start/stop/pause/resume/restart.
- Two cooldown start modes: immediately on `TakeReward()` or manually via `StartTime()`.

**Public Properties and Fields:**
- `float timeLeft`: Time remaining until the reward, in seconds.
- `bool IsTimerRunning`: The timer is running.
- `bool IsTimerPaused`: The timer is paused.
- `bool IsRewardAvailable`: The reward is available right now.

**Public methods:**
- `TakeReward()`: Attempts to claim the reward. Returns `true` on success.
- `Take()`: Alternative method for calling `TakeReward()` (e.g., from Unity buttons).
- `CanTakeReward()`: Checks whether the reward is currently available. Returns `true` if the reward can be claimed.
- `GetSecondsUntilReward()`: Returns the time remaining until the next reward, in seconds.
- `static string FormatTime(int seconds)`: Formats time from seconds into an `hh:mm:ss` string.
- `StartTime() / StopTime() / PauseTime() / ResumeTime()`: Timer lifecycle control.
- `RestartTime()`: Restarts the cooldown from the current UTC time.
- `SetRewardAvailableNow()`: Makes the reward available immediately.
- `RefreshTimeState()`: Forces a recalculation of the timer state.
- `SetAdditionalKey(string addKey, bool refreshAfterChange = true)`: Changes the save key suffix.

**Unity Events:**
- `OnTimeUpdated (float)`: Invoked every second. Passes the time remaining until the reward.
- `OnRewardClaimed`: Invoked when the reward is successfully claimed.
- `OnRewardAvailable`: Invoked once when the timer reaches zero and the reward becomes available.
- `OnTimerStarted / OnTimerStopped / OnTimerPaused / OnTimerResumed`: Timer control events.

### Collection System (`Collection`)
**Namespace:** `Neo.Bonus`
**Path:** `Scripts/Bonus/Collection/Collection.cs`

Manages an item collection system. Tracks which items are already unlocked and grants new ones as prizes.

**Key features:**
- Item data stored in a `ScriptableObject` (`ItemCollectionData`).
- Unlock progress saved in `PlayerPrefs`.

**Public Properties and Fields:**
- `static Collection Instance`: Static class instance for global access.
- `ItemCollectionData[] itemCollectionDatas`: Array of data for all collection items.
- `bool[] enabledItems`: Array indicating which items are unlocked (`true`).

**Public methods:**
- `GetPrize()`: Returns the `ItemCollectionData` of a new unique item and saves progress.

**Unity Events:**
- `OnGetItem (int)`: Invoked when a new item is received. Passes the item ID.
- `OnLoadItems`: Invoked after collection data is loaded from saves.

### Line Roulette (`LineRoulett`)
**Namespace:** `Neo.Bonus`
**Path:** `Scripts/Bonus/LineRoulett.cs`

Creates a visual roulette as a horizontally moving strip of prizes. The player starts the spin, and after stopping, the prize under the arrow is determined.

**Key features:**
- Configurable speed, spin duration, and deceleration.
- Easy prize setup via a sprite array.

**Public Properties and Fields:**
- `Sprite[] sprites`: Array of sprites used to display prizes in the roulette.
- `bool updateSetting`: Flag to force a visual layout refresh of the elements in the Unity Editor when set to `true`.

**Public methods:**
- `StartRolling()`: Starts spinning the roulette.

**Unity Events:**
- `OnWin (int)`: Invoked after the roulette stops. Passes the ID of the winning sprite from the `sprites` array.
