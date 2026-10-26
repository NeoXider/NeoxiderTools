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
- `Face Sprite Override`
- `Back Sprite Override`

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
| `FlipDuration` | Read-only Inspector duration used by the parameterless flip. |
| `FlipAsync(float duration, CancellationToken cancellation)` | Flips with caller/destruction cancellation. |
| `MoveToAsync(...)` | Moves the card in world space with animation. |
| `MoveToLocalAsync(...)` | Moves the card in local space with animation. |
| `SetSpriteOverrides(Sprite faceSprite, Sprite backSprite = null, bool refresh = true)` | Uses explicit sprites without requiring a `DeckConfig`. |
| `ClearSpriteOverrides(bool refresh = true)` | Returns visual lookup to `DeckConfig`. |
| `UpdateOriginalTransform()` | Stores the current baseline transform used by hover/reset logic. |
| `ResetHover()` | Resets hover state and restores the card presentation. |

## Standalone custom card views

For TCG/deckbuilder/board-game cards, create custom data with `CardData.CreateCustom(...)`, assign it through `SetData(...)`, then call `SetSpriteOverrides(...)` with the current card art. This keeps the reusable card view independent from the classic 36/52/54-card `DeckConfig` sprite table.

## Animation lifetime

`MoveToAsync(Vector3, float, CancellationToken)` and `MoveToLocalAsync(Vector3, float, CancellationToken)` preserve the existing overloads while adding caller cancellation. Each move/flip owns the concrete tween it awaits. Starting another move/flip cancels the previous operation; destruction also cancels it. A cancelled operation throws `OperationCanceledException`, restores the full scale of an interrupted flip, and does not emit `OnFlip`/`OnMoveComplete` as a success. It cannot kill a newer tween or overwrite its completed transform.

```csharp
await card.FlipAsync(card.FlipDuration, cancellation);
await card.MoveToAsync(destination, 0.2f, cancellation);
```

Pass a game/turn token from your C# owner. Inspector wrappers keep their existing signatures; `DrunkardGame` supplies its per-game token internally. Cancelled visual work does not roll back card membership already committed by a hand/board.

## Notes

- The component supports both UI (`Image`) and 2D (`SpriteRenderer`) presentation.
- Hover logic uses the current transform state, not only the prefab's original scale.
- For UI interaction, an `EventSystem` must exist in the scene.
- `HandComponent` commonly updates the card baseline transform after layout changes.

## See also

- [README](./README.md)
- [CardData](./CardData.md)
- [DeckConfig](./DeckConfig.md)
- [Cards docs](./README.md)
