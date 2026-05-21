# RpgResourceBinding

**What it is:** a small component that binds one `RpgCharacter` resource to UnityEvents and NoCode UI.

Add it to a UI object, assign `RpgCharacter`, choose a resource through preset or `Custom`, then wire events:

| Event | Value |
|-------|-------|
| `OnCurrent` | Current resource value |
| `OnMax` | Resource max |
| `OnPercent` | Percent 0-1 |

## Examples

- Stamina bar: `_resourceId = Stamina`, `OnPercent -> Slider.value`.
- Dark mana text: `_resourceId = Custom/DarkMana`, `OnCurrent -> SetText`.
- Shield UI: `_resourceId = Shield`, `OnPercent -> Image.fillAmount`.

For conditions without a binding component, use `RpgConditionAdapter.ResourceAtLeast` / `ResourcePercentBelow`.
