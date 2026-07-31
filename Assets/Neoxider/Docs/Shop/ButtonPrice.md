# ButtonPrice

**Purpose:** A smart shop button that automatically switches its visual state and text based on its logical state: "Buy", "Select", "Selected", or "Unaffordable" (a priced item the player cannot pay for right now).

## Setup

1. Add the `Add Component > Neoxider > UI > ButtonPrice` component to a button object.
2. Create and assign GameObjects for each visual state (e.g., different button backgrounds for Buy and Selected) in the `_visual` field.
3. Configure the text fields for the price and button name.
4. Set up the `OnBuy`, `OnSelect`, `OnSelected`, and `OnUnaffordable` events.

> The `Unaffordable` state is normally driven by [ShopPurchaseButtonView](./ShopPurchaseButtonView.md), which watches the wallet balance and calls `SetVisual(price, ButtonType.Unaffordable)` / toggles `Button.interactable`. Auto-type keeps `Unaffordable` for priced items and degrades it to `Select` for free ones.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_textPrice` | Array of `TMP_Text` references where the formatted price will be displayed. |
| `_textButton` | Array of `TMP_Text` references where the button text ("Buy", "Select") will be displayed. |
| `_visual` | A group of objects (`GameObject[]`) that are enabled/disabled depending on the button's state (Buy / Select / Selected / Unaffordable). The `unaffordable` group is optional — when empty, the Unaffordable state keeps showing the Buy visuals. |
| `_price` | The current price. If the price is 0, the state can automatically switch to "Select" (if configured). |
| `_textPrice_0` | Whether to show the price if it equals zero (e.g., "0" or "Free"). If `false`, the price text is hidden. |
| `_textButtonAndPrice` | If `true`, the price and text are written into the same text field. |
| `_type` | The current button type: `Buy`, `Select`, `Selected`, `Unaffordable` (read back via `CurrentType`). |
| `_textBuy` | String for the button in the "Buy" state (default "Buy"). |
| `_textSelect` | String for the button in the "Select" state (default "Select"). |
| `_textSelected` | String for the button in the "Selected" state (default "Selected"). |
| `_textUnaffordable` | String for the "Unaffordable" state (defaults to the Buy text so old prefabs look unchanged). |
| `_customSeparator` | Separator for thousands in the price (e.g., `.`). |
| `_editorView` | Enables visualization of states directly in the Unity Editor. |

## See Also

- [ShopItem](ShopItem.md) - The component that often controls this button.
- [Module Root](../README.md)
