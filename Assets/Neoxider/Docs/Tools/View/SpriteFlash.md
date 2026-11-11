# SpriteFlash

**What it is:** a component that flashes sprites with a solid colour for a moment, the usual 2D feedback for a hit.
During the flash the sprites are drawn with one shared flash material; afterwards each gets its own material back.
Script: `Scripts/Tools/View/SpriteFlash.cs`, namespace `Neo.Tools.View`, assembly `Neo.Tools.View`.

**How to use:**
1. Add `SpriteFlash` to the object that holds the sprite, or to a parent to flash every sprite under it.
2. Set **Color** and **Duration**. The defaults are white and 0.1 s.
3. Call `Flash()` when the object is hit. In the Inspector: `AbilityUnitBehaviour > On Damaged > SpriteFlash.Flash`,
   or the damage event of your own health component.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Renderers** | Sprites to flash. Empty = every `SpriteRenderer` on this object and its children. List them to leave a shadow or a glow out. |
| **Flash Material** | Material drawn during the flash. A component added in the Editor gets the package material `Materials/Sprite Flash/SpriteFlash.mat`. Empty = a shared material created from the `Neoxider/Sprite Flash` shader. |
| **Color** | Colour of the flash. Alpha scales its strength. |
| **Duration** | Seconds one flash lasts. |
| **Fade Out** | On = the flash fades out over **Duration**. Off = full colour until it ends. |
| **Use Unscaled Time** | The flash also ends while `Time.timeScale` is 0. |
| **On Flash Started** / **On Flash Finished** | Raised when a flash starts and when the sprites have their materials back. A flash restarted while running raises **On Flash Started** once. |

## API

| Member | Description |
|--------|-------------|
| `void Flash()` | Flashes with **Color** for **Duration**. Calling it during a flash restarts the timer. |
| `void FlashFor(float seconds)` | Flashes with **Color** for the given time. |
| `void Flash(Color color, float seconds)` | Flashes with a colour of your choice. |
| `void Stop()` | Ends the flash now. |
| `void Tick(float deltaTime)` | Advances a running flash; `Update` calls it. |
| `bool IsFlashing` | True between the start and the end of a flash. |
| `Color Color`, `float Duration`, `bool FadeOut`, `bool UseUnscaledTime`, `Material FlashMaterial` | The Inspector settings. |
| `const string ShaderName` | `"Neoxider/Sprite Flash"`. |

## Behaviour

- The original material is put back when the flash ends, when the component or its object is disabled and when
  `Stop()` is called. A pooled enemy that dies in the middle of a flash does not come back white.
- A material that other code assigns during the flash is left in place.
- The flash colour and amount are written through a `MaterialPropertyBlock`, so all flashing sprites share one
  material. A sprite that had no property block gets none left behind; an existing block keeps its values.
- The shader is built on Unity's sprite include, so flip, sprite colour alpha, atlases and pixel snap behave as in
  `Sprites/Default`. It renders in the Built-in pipeline and in URP.
- A component added from code in a build has no **Flash Material**. Assign one, or add the
  `Neoxider/Sprite Flash` shader to **Always Included Shaders**; otherwise the shader is stripped, the flash does
  nothing and one warning is logged.

## Examples

**Inspector:** an enemy with `AbilityUnitBehaviour` and a `Body` child that holds the sprite. Add `SpriteFlash` to
`Body` and wire `On Damaged > SpriteFlash.Flash`.

**Code:**

```csharp
using Neo.Tools.View;
using UnityEngine;

public sealed class Health : MonoBehaviour
{
    [SerializeField] private SpriteFlash _flash;

    public void TakeDamage(float amount)
    {
        _flash.Flash();
        // A stronger hit: red and longer.
        // _flash.Flash(Color.red, 0.25f);
    }
}
```

## See also

- [View](./README.md)
- [AbilityUnitBehaviour](../../Abilities/AbilityUnitBehaviour.md) — `On Damaged`
- [TransformAnimator](../../Animations/TransformAnimator.md) — a shake for the same event

---

**По-русски:** компонент на короткое время заливает спрайты сплошным цветом — обычная для 2D вспышка при попадании.
Вызовите `Flash()` из события урона (`AbilityUnitBehaviour > On Damaged`). Цвет, длительность и затухание
настраиваются; исходный материал возвращается по окончании вспышки и при отключении объекта, поэтому враг из пула
не возвращается белым.
