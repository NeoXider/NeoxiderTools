# NeoxiderPages module

`NeoxiderPages` is a sample module for screen/page navigation on top of Unity UI.
It requires **DOTween Pro** (and DOTween) to work correctly.

## Location

After importing the sample, the module lives under:

```text
Assets/Neoxider/Samples~/NeoxiderPages/
```

## Main concepts

- `PM` manages page switching and previous/current page state. All **`UIPage`** instances must be **children (descendants)** of the GameObject that has **`PM`** — page discovery walks only that subtree (including inactive objects).
- `UIPage` marks a GameObject as a page.
- `BtnChangePage` connects UI buttons to page actions.
- `UIKit` exposes a simple static API for page changes.

## Transition behavior

- `ChangePage(pageId)` chooses the strategy from the target `UIPage`: exclusive pages go through `SetPage`, popup pages go through `ActivePage`.
- During exclusive switches, if the incoming page has a Forward animation, `PM` waits for `WaitForShowAnimation()` before hiding outgoing pages. This prevents a visible empty backdrop between pages.
- Outgoing pages are closed with `UIPage.EndActive()`, so Back animation plays when the page has `BackwardOnly` or `ForwardAndBackward`.
- Active popup pages are closed by default when an exclusive non-popup page opens. This is controlled by `PM.closePopupsOnExclusivePageChange` and defaults to `true`.
- Pages with `Ignore On Exclusive Change` are never closed by exclusive switches.
## More docs

- Russian docs: [`../../Docs/NeoxiderPages/README.md`](../../Docs/NeoxiderPages/README.md)
- [UIPage](./UIPage.md)
- UI module: [`../UI/README.md`](../UI/README.md)

