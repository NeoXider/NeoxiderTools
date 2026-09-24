# NeoxiderPages module

`NeoxiderPages` is a sample module for screen/page navigation on top of Unity UI.
It imports and runs without **DOTween** or **DOTween Pro**.

## Location

In the package the sample source lives in `Assets/Neoxider/Samples~/NeoxiderPages/` (or
`Assets/Neoxider/Samples/NeoxiderPages/` while samples are being developed). Importing it through
**Package Manager → NeoxiderTools → Samples → Import** copies it into the game project:

```text
Assets/Samples/NeoxiderTools/<version at import>/NeoxiderPages/
```

### Updating an imported copy

The imported copy is part of the game, not of the package: bumping the package version does **not**
update it. When a release fixes something in NeoxiderPages (for example 10.16.1, where the `PM` editor
page preview stopped switching pages during Play Mode), re-import the sample or copy the changed files.
The folder keeps the version it was first imported with, so to find out which fixes a project has,
compare the files with the package's `Samples~/NeoxiderPages` (ignore BOM and line endings) instead of
trusting the folder name.

## Main concepts

- `PM` manages page switching and previous/current page state. All **`UIPage`** instances must be **children (descendants)** of the GameObject that has **`PM`** — page discovery walks only that subtree (including inactive objects).
- `UIPage` marks a GameObject as a page.
- `BtnChangePage` connects UI buttons to page actions.
- `UIKit` exposes a simple static API for page changes.

## Transition behavior

- `ChangePage(pageId)` chooses the strategy from the target `UIPage`: exclusive pages go through `SetPage`, popup pages go through `ActivePage`.
- During exclusive switches, `PM` enables the incoming page and closes outgoing pages through `UIPage.EndActive()`.
- Active popup pages are closed by default when an exclusive non-popup page opens. This is controlled by `PM.closePopupsOnExclusivePageChange` and defaults to `true`.
- Pages with `Ignore On Exclusive Change` are never closed by exclusive switches.

## Android Back

`PM` does not handle the Back button itself; one game-level handler maps
`PM.I.currentUiPage.PageId` to the action of that page's on-screen back/close button (popups close,
sub-pages return to their parent, result pages ignore Back, the root page minimises the app with
`moveTaskToBack`). With the Input System, read Escape from **every** `Keyboard` device
(`Keyboard.current` can be null on a handset) and, on Android, also treat `Application.wantsToQuit` as
a Back press (return `false`), guarded so one press is handled once per frame.
## More docs

- [UIPage](./UIPage.md)
- UI module: [`../UI/README.md`](../UI/README.md)

