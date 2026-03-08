# CardComponent

`CardComponent` is the scene-facing card component used for card presentation and interaction. It handles card data, face-up state, hover effects, click events, and move/flip animations. File: `Assets/Neoxider/Scripts/Cards/Components/CardComponent.cs`.

## Typical use

1. Add `CardComponent` to a card prefab.
2. Assign a `DeckConfig`.
3. Set visual targets such as `Image` or `SpriteRenderer`.
4. Configure animation and hover settings.
5. Drive it through `SetData(...)`, `FlipAsync(...)`, and movement methods from gameplay systems.

## Main inspector groups

### Config

- `Config`
- `Suit`
- `Rank`
- `Is Joker`
- `Is Red Joker`

### State

- `Is Face Up`
- `Is Interactable`

### Visual

- `Card Image`
- `Sprite Renderer`

### Animation

- `Flip Duration`
- `Move Duration`
- `Flip Ease`
- `Move Ease`

### Hover

- `Enable Hover Effect`
- `Hover Scale`
- `Hover Y Offset`
- `Hover Duration`

## Events

- `OnClick`
- `OnFlip`
- `OnMoveComplete`
- `OnHoverEnter`
- `OnHoverExit`

## Main API

| API | Description |
|-----|-------------|
| `SetData(CardData data, bool faceUp = true)` | Assigns card data and refreshes visuals. |
| `Flip()` / `FlipAsync()` | Flips the card with or without animation. |
| `MoveToAsync(...)` | Moves the card in world space with animation. |
| `MoveToLocalAsync(...)` | Moves the card in local space with animation. |
| `UpdateOriginalTransform()` | Stores the current baseline transform used by hover/reset logic. |
| `ResetHover()` | Resets hover state and restores the card presentation. |

## Notes

- The component supports both UI (`Image`) and 2D (`SpriteRenderer`) presentation.
- Hover logic uses the current transform state, not only the prefab's original scale.
- For UI interaction, an `EventSystem` must exist in the scene.
- `HandComponent` commonly updates the card baseline transform after layout changes.

## See also

- [README](./README.md)
- [CardData](./CardData.md)
- [DeckConfig](./DeckConfig.md)
- [Russian Cards docs](../../Docs/Cards/README.md)
