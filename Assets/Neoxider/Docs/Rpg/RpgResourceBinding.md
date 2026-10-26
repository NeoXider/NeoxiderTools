# RpgResourceBinding

**What it is:** a small component that binds one `RpgCharacter` resource to UnityEvents and NoCode UI.

Callbacks belong to one exact character/resource subscription. Disable, rebind and runtime replacement
invalidate callbacks already captured in the former reactive notification. If an initial `OnCurrent`
observer disables/rebinds the binding, that former subscription emits no following `OnMax`/`OnPercent`.
Source destruction also suppresses captured output: the cached character must remain live under Unity's
null semantics before its reactive source getters are used. Rebinding still detaches the exact old states.

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

In C#, assign `Character` and `ResourceId`, then subscribe to `OnCurrent`, `OnMax`, or `OnPercent`.
These events publish initial values and subsequent changes while the binding is enabled.
The binding reconnects after `Character.RebuildRuntime()` / `ApplyTemplate(...)`, and also waits for
late character initialization when no resource exists yet. Disabling, changing source/ID, or rebuilding
detaches the exact old reactive objects so stale resources cannot update the UI.
