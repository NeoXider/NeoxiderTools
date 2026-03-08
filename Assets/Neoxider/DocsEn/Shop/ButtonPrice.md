# ButtonPrice

`ButtonPrice` is a shop button view component with three visual states: `Buy`, `Select`, and `Selected`. It displays the current price, supports thousand separators, and can switch from buy mode to select mode when the price reaches zero. File: `Assets/Neoxider/Scripts/Shop/ButtonPrice.cs`, namespace: `Neo.UI`.

## Typical use

1. Add `ButtonPrice` to an item button.
2. Configure visuals for the three states in the Inspector.
3. Let `ShopItem` drive it automatically, or update it from code through `SetPrice(...)` or `SetVisual(...)`.

## Main features

- Three-state presentation: `Buy`, `Select`, `Selected`
- Optional automatic state conversion when price becomes `0`
- Separate visual groups per state
- Price formatting with a custom thousands separator

## Main API

| API | Description |
|-----|-------------|
| `SetAutoVisual(int price, ButtonType type)` | Sets price and auto-corrects the visual state if needed. |
| `SetVisual(int price, ButtonType type)` | Sets both price and explicit visual state. |
| `SetPrice(int price)` | Convenience method for price-based refresh. |
| `SetVisual(ButtonType type)` | Applies one visual state directly. |
| `TrySetVisualId(int id)` | Attempts to switch by numeric state id after validating the current price rules. |
| `SetVisualId(int id)` | Forces state by numeric id (`0 = Buy`, `1 = Select`, `2 = Selected`). |

## Events

- `OnBuy`
- `OnSelect`
- `OnSelected`

## Notes

- `ShopItem` commonly pushes both price and state into `ButtonPrice`.
- The component is primarily a presentation helper; business rules still belong in `Shop` and `ShopItem`.

## See also

- [README](./README.md)
- [Shop](./Shop.md)
- [Russian Shop docs](../../Docs/Shop/README.md)
