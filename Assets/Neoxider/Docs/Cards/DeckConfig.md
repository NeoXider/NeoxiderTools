# DeckConfig

`DeckConfig` is a `ScriptableObject` that stores deck visuals and deck-generation settings for 36-, 52-, or 54-card sets. It provides card-face sprites, back sprite, optional jokers, validation helpers, and deck generation methods. Creation menu: `Create > Neoxider > Cards > Deck Config`.

## Typical use

1. Create a `DeckConfig` asset.
2. Assign the card back sprite and per-suit face sprites.
3. Choose `DeckType` for the available sprite set.
4. Choose `GameDeckType` for the actual deck used in gameplay.
5. Assign the config to `CardComponent` and `DeckComponent`.

## Inspector fields

| Field | Description |
|------|-------------|
| `Deck Type` | Defines how many card-face sprites are available. |
| `Game Deck Type` | Defines how many cards the game should use at runtime. |
| `Back Sprite` | Card back sprite. |
| `Hearts`, `Diamonds`, `Clubs`, `Spades` | Ordered face sprites for each suit. |
| `Red Joker`, `Black Joker` | Optional joker sprites for 54-card decks. |
| `Use Custom Deck` | Makes `GenerateDeck()` use the `Custom Cards` list. |
| `Custom Cards` | Custom id, display name, sort value, group, and face sprite entries. |

## `DeckType` vs `GameDeckType`

- `DeckType` describes how many sprites exist in the asset.
- `GameDeckType` describes how many cards should be generated for gameplay.

Example:

- `DeckType = Standard52`
- `GameDeckType = Standard36`

This means the asset has enough visuals for a 52-card deck, but gameplay only uses the 36-card subset.

## Main API

| API | Description |
|-----|-------------|
| `GetSprite(CardData data)` | Returns the sprite for a specific card. |
| `GenerateDeck()` | Generates a deck based on `GameDeckType`, or `Custom Cards` when custom deck mode is enabled. |
| `GenerateCustomDeck()` | Generates a deck from custom card entries only. |
| `GenerateDeck(DeckType type)` | Generates a deck for the requested deck type. |
| `Validate(out List<string> errors)` | Validates sprite completeness and configuration rules. |

## Custom decks

Enable `Use Custom Deck` when a game is not based on classic suits and ranks. Each custom entry creates a `CardData.CreateCustom(...)` card and can optionally carry a face sprite. Missing custom face sprites are warnings, not hard errors, so external art pipelines can still provide sprites through `CardComponent.SetSpriteOverrides(...)` or a custom view.

## Notes

- `GameDeckType` cannot require cards missing from `DeckType`.
- Sprite order matters and must match the expected rank progression for the configured deck type.
- The custom editor can preview sprites and validate the asset visually.

## See also

- [README](./README.md)
- [CardData](./CardData.md)
- [Cards docs](./README.md)
