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
| `GenerateDeck()` | Generates a deck based on `GameDeckType`. |
| `GenerateDeck(DeckType type)` | Generates a deck for the requested deck type. |
| `Validate(out List<string> errors)` | Validates sprite completeness and configuration rules. |

## Notes

- `GameDeckType` cannot require cards missing from `DeckType`.
- Sprite order matters and must match the expected rank progression for the configured deck type.
- The custom editor can preview sprites and validate the asset visually.

## See also

- [README](./README.md)
- [CardData](./CardData.md)
- [Russian Cards docs](../../Docs/Cards/README.md)
