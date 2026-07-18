# PageSubscriber (NeoxiderPages)

**What it is:** Listens to game-state events (Start/Restart/Win/Lose/End via `G`) and drives **PM** to open the matching page. It waits for the game managers to initialize before subscribing.

**How to use:** see the sections below.

---

**Add:** Neoxider → Pages → PageSubscriber.

## Fields

- **PM** — reference to the Page Manager (if not set, the `PM` singleton is used).
- **Game Page Id**, **Win Page Id**, **Lose Page Id**, **End Page Id** — pages opened on Start/Restart, Win, Lose, and End respectively.
- **Auto Resolve Page Ids** — when enabled, unset page ids are resolved by asset name at startup.
- **Game/Win/Lose/End Page Name** — asset names used by auto-resolve (defaults: `PageGame`, `PageWin`, `PageLose`, `PageEnd`).

Event mapping: `OnStart`/`OnRestart` → Game page, `OnWin` → Win page, `OnLose` → Lose page, `OnEnd` → End page. A page id left null is simply skipped.

Used to bind screen navigation to game flow without wiring events by hand.
