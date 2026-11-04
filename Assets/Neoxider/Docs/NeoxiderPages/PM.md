# PM (Page Manager)

**What it is:** singleton that manages UI pages in NeoxiderPages: enabling/disabling by PageId, switching, returning to the previous page, and integration with GM (Win/Lose/End).

**How to use:**
1. Add **Add Component → Neoxider → Pages → PM** to the root of your UI (or create it from the **Create Neoxider Object** window, **Pages → PM**).
2. Put every page under it as a child with a [UIPage](./UIPage.md) and a [PageId](./PageId.md) asset.
3. Open pages with `ChangePage(pageId)` from code or with [BtnChangePage](./BtnChangePage.md) buttons.

Script: `Samples~/NeoxiderPages/Runtime/Scripts/Page/PM.cs`, namespace `Neo.Pages`.

---

**Hierarchy:** every **`UIPage`** managed by this **PM** must live on a GameObject that is a **child** of the PM object (including inactive objects in that subtree). Auto-discovery of pages (`FindAllScenePages` / resetting the list in the editor) only scans that subtree — pages outside the PM hierarchy are not registered.

## Basics

- Pages are registered by **PageId**. **UIPage** goes on each screen.
- **ChangePage(PageId)** — opens a page (exclusively or as a popup, chosen from the target `UIPage`).
- **ChangePageByName(string)** / **ChangePage(KnownPage)** — the same by PageId asset name (`"PageEnd"`), falling back to the page GameObject name (`"End Page"`).
- **TryGetPageIdByName(string, out PageId)** — the lookup behind `ChangePageByName`; the hierarchy is rescanned only when the known pages do not match, so pages added at runtime are found too.
- **ActivePage(PageId)** — activates a page without deactivating others (popup-style).
- **SetPage(PageId)** — activates a page and deactivates the others (exclusive).
- **SwitchToPreviousPage()** — returns to the previous page (no-op when there is no history).
- **CloseCurrentPage()** — closes the current page via `EndActive()`. When it was a popup over the previous page, that page becomes current again and `OnPageChanged` fires.
- **ActivatePages(PageId, ...)** — low-level show/hide by PageId with an ignore list; does not change `currentUiPage`.
- **GM Integration** — opens Win/Lose/End pages when GM's state changes.

## State and history

- `currentUiPage` / `previousUiPage` hold one level of history for **Back**. Reopening the page that is already current keeps the history, so Back still returns to the page before it.
- A switch to a PageId that no page under this PM uses changes nothing: the current and previous pages stay, and `OnPageChanged` does not fire.
- `SetPage` and `ChangePage` raise `OnPageChanged` once per switch with the new page.
- The PageId lookup cache is checked on every hit: a page that was destroyed or got another PageId is found again by a rescan.
- Name lookups (`ChangePageByName`, `TryGetPageIdByName`, the GM Win/Lose/End switches) use a name index built from the page list, so a switch does not read `Object.name` of every page. The index is rebuilt with every rescan of the page list, and a name it does not know triggers that rescan, so pages added or renamed at runtime are still found.

## Inspector

| Section | Fields |
|---------|--------|
| Singleton | Dont Destroy On Load, Set Instance On Awake, **Select Instance** (resolved on click) |
| Runtime | Integrate With GM, **Close Popups On Exclusive Change**, Startup Page (dropdown or asset; defaults to `PageMenu` when empty) |
| Ignored Pages | Pages whose active state `PM` never changes |
| Debug | **Debug Logs**, current/previous page, all pages, `OnPageChanged` |
| Editor Preview | Refresh Pages In Editor, Auto Select Editor Page, Editor Active Page (buttons, dropdown or asset) |

**Debug Logs** (`PM.DebugLogs`) logs page switches and setup warnings (unknown, duplicate or blocked pages) of this
PM; otherwise they follow `NeoDiagnostics` (off by default). Errors for pages that cannot be found log unless
`NeoDiagnostics` errors are turned off. `ClosePopupsOnExclusivePageChange` is also a public property.

The **Editor Preview** buttons and the deferred preview after an Inspector change go through
`PreviewPageInEditor(PageId)` (editor only): it shows the page and hides the others without raising `OnPageChanged`
and without touching `currentUiPage`/`previousUiPage`, and does nothing in Play Mode.

## Exclusive transitions, popups, and animations

- `ChangePage(pageId)` picks a strategy based on the target `UIPage`: a regular page opens via `SetPage`, a popup page opens via `ActivePage`.
- `BtnChangePage` does not close pages itself. It only calls `PM` and, if needed, changes `GameState`.
- On an exclusive transition, if the incoming page reports a show animation (`UIPage.HasShowAnimation`, overridden by an animated page subclass), `PM` shows it first and waits for `WaitForShowAnimation()` before closing the outgoing pages. This avoids an empty background flash between pages. Outside Play Mode or on an inactive PM the other pages close at once.
- Outgoing pages close via `UIPage.EndActive()`. The base `UIPage` switches instantly in every animation mode; an animated page subclass plays its hide animation in its `EndActive()` override.
- Active popup pages close by default when a regular non-popup page opens. This is controlled by `closePopupsOnExclusivePageChange = true` on `PM`.
- If a popup should stay on top through any exclusive transition, disable `closePopupsOnExclusivePageChange` or set `Ignore On Exclusive Change` on that specific page.

## See also

- [UIPage](./UIPage.md)
- [BtnChangePage](./BtnChangePage.md)
- [GM](../Tools/Managers/GM.md)
