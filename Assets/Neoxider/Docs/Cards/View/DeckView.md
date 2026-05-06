# DeckView

**Что это:** Визуальное представление колоды карт. Реализует `IDeckView`, отображает стопку и верхнюю карту.

**Как использовать:** см. разделы ниже.

---


Визуальное представление колоды карт. Реализует `IDeckView`, отображает стопку и верхнюю карту.

- **Пространство имён:** `Neo.Cards`
- **Путь:** `Assets/Neoxider/Scripts/Cards/View/DeckView.cs`

## Основное

- **Spawn Point** — точка появления карт при сдаче.
- **Deck Image / Deck Sprite** — визуал стопки (UI Image или SpriteRenderer).
- **Top Card Image / Top Card Sprite** — отображение верхней карты.
- **Visible Card Count / Card Offset** — сколько карт «видны» в стопке и смещение.
- **Config** — DeckConfig с данными колоды.

См. также [DeckComponent](../DeckComponent.md).


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `1` | 1. |
| `SpawnPoint` | Spawn Point. |
| `VisibleCardCount` | Visible Card Count. |
| `_cardOffset` | Card Offset. |
| `_config` | Config. |
| `_deckImage` | Deck Image. |
| `_deckSprite` | Deck Sprite. |
| `_spawnPoint` | Spawn Point. |
| `_topCardImage` | Top Card Image. |
| `_topCardSprite` | Top Card Sprite. |