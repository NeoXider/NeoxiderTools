# PageSubscriber (NeoxiderPages)

**What it is:** listens to game-state events (Start/Restart/Win/Lose/End via `G`) and drives **PM** to open the matching page. It subscribes once both `GM` and `EM` exist and unsubscribes when destroyed, so reloading the scene does not leave listeners on a persistent `EM`. Script `Samples~/NeoxiderPages/Runtime/Scripts/Core/PageSubscriber.cs`, namespace `Neo.Pages`.

**How to use:**
1. **Add Component → Neoxider → Pages → PageSubscriber** to any object in the UI scene.
2. Assign the Game, Win, Lose and End [PageId](./PageId.md) assets, or keep **Auto Resolve Page Ids** on and name the assets `PageGame`, `PageWin`, `PageLose`, `PageEnd`.
3. Drive the game state through `GM` (for example `GM.I.Win()`); the matching page opens.

---

## Fields

- **PM** — reference to the Page Manager (if not set, the `PM` singleton is used).
- **Game Page Id**, **Win Page Id**, **Lose Page Id**, **End Page Id** — pages opened on Start/Restart, Win, Lose, and End respectively.
- **Auto Resolve Page Ids** — when enabled, unset page ids are resolved by asset name at startup through the PM's pages (`PM.TryGetPageIdByName`); without a PM, through the scene's `UIPage` components.
- **Game/Win/Lose/End Page Name** — asset names used by auto-resolve (defaults: `PageGame`, `PageWin`, `PageLose`, `PageEnd`).

Event mapping: `OnStart`/`OnRestart` → Game page, `OnWin` → Win page, `OnLose` → Lose page, `OnEnd` → End page. A page id left null is simply skipped.

Used to bind screen navigation to game flow without wiring events by hand. `PM` with **Integrate With GM** already opens
the Win/Lose/End pages; use `PageSubscriber` when you also need the Game page on Start/Restart, or turn one of the two
off so a page does not open twice. `IsSubscribed` tells whether the listeners are attached.

## See also

- [PM](./PM.md)
- [GM](../Tools/Managers/GM.md)
