# PageId

**What it is:** a `ScriptableObject` page identifier — lets you select pages without maintaining a hand-written enum. Path: `Samples~/NeoxiderPages/Runtime/Scripts/Page/PageId.cs`, namespace `Neo.Pages`. Asset creation: menu **Create → Neoxider → Pages → Page Id**.

**How to use:**
1. Create one `PageId` asset per screen, named with the recommended `Page` prefix (e.g. `PageMenu`, `PageShop`, `PageSettings`).
2. Assign the asset to the matching `UIPage`'s `PageId` field.
3. Reference the asset from `PM`/`BtnChangePage` to open/switch to that page.

---

## Fields

None — a `PageId` asset carries no configurable fields; its identity comes from the asset name.

## API

| Member | Description |
|--------|-------------|
| `Id` | Stable page key, derived from the asset name (e.g. `PageMenu`). |
| `DisplayName` | Display name with the `Page` prefix stripped (e.g. `Menu` for an asset named `PageMenu`). |

## See also

- [PM (Page Manager)](./PM.md)
- [UIPage](./UIPage.md)
- [NeoxiderPages README](./README.md)
