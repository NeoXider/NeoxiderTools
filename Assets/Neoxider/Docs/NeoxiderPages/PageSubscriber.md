# PageSubscriber (NeoxiderPages)

**What it is:** Subscribes to **PM** and reacts to the Game/Win/Lose pages: executes the assigned actions when the corresponding pages open.

**How to use:** see the sections below.

---


Subscribes to **PM** and reacts to the Game/Win/Lose pages: executes the assigned actions when the corresponding pages open.

**Add:** Neoxider → Pages → PageSubscriber.

## Fields

- **PM** — reference to the Page Manager (if not set, the singleton is used).
- **Game Page Id**, **Win Page Id**, **Lose Page Id** — identifiers of the game, win, and lose pages.

Used to bind logic (sound, analytics, unlocking) to transitions between screens.
