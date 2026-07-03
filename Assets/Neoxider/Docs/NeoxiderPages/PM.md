# PM (Page Manager)

**What it is:** singleton that manages UI pages in NeoxiderPages: enabling/disabling by PageId, switching, returning to the previous page, and integration with GM (Win/Lose/End).

**How to use:** see the sections below.

---

**Add:** GameObject → Neoxider → Pages → PM.

**Hierarchy:** every **`UIPage`** managed by this **PM** must live on a GameObject that is a **child** of the PM object (including inactive objects in that subtree). Auto-discovery of pages (`FindAllScenePages` / resetting the list in the editor) only scans that subtree — pages outside the PM hierarchy are not registered.

## Basics

- Pages are registered by **PageId**. **UIPage** goes on each screen.
- **Open(PageId)** — opens a page (exclusively or as a popup).
- **SwitchToPreviousPage()** — returns to the previous page.
- **GM Integration** — opens Win/Lose/End pages when GM's state changes.

## Exclusive transitions, popups, and animations

- `ChangePage(pageId)` picks a strategy based on the target `UIPage`: a regular page opens via `SetPage`, a popup page opens via `ActivePage`.
- `BtnChangePage` does not close pages itself. It only calls `PM` and, if needed, changes `GameState`.
- On an exclusive transition, if the incoming page has a Forward animation, `PM` plays it first and waits for `WaitForShowAnimation()` before closing the outgoing pages. This avoids an empty background flash between pages.
- Outgoing pages close via `UIPage.EndActive()`, so the Back animation plays in `BackwardOnly` and `ForwardAndBackward` modes.
- Active popup pages close by default when a regular non-popup page opens. This is controlled by `closePopupsOnExclusivePageChange = true` on `PM`.
- If a popup should stay on top through any exclusive transition, disable `closePopupsOnExclusivePageChange` or set `Ignore On Exclusive Change` on that specific page.

## See also

- [UIPage](./UIPage.md)
- [BtnChangePage](./BtnChangePage.md)
- [GM](../Tools/Managers/GM.md)
