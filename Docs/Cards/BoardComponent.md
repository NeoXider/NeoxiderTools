# BoardComponent

**What it is:** `BoardComponent` is a reusable card zone for table cards, discard piles, market rows, lanes, and other board-style layouts. It stores placed `CardComponent` instances, applies layout, and can force newly placed cards face up or face down. Script: `Scripts/Cards/Components/BoardComponent.cs`.

**How to use:**
- Add the component (menu `Neoxider/Cards/BoardComponent`).
- Set `Max Cards` for the zone size.
- Use slot transforms for fixed positions, or layout settings for dynamic arrangements.
- Call `SetCapacity(...)` and `SetFaceUp(...)` from custom game setup when the same prefab is reused for different zones.

---

## Main API

| API | Description |
|-----|-------------|
| `Cards` / `Count` | Current cards on the board. |
| `MaxCards` | Current board capacity. |
| `FaceUp` | Default face state applied on placement when enabled. |
| `CanPlaceCard(CardComponent card)` | Checks null, capacity, and whether this exact scene card is already present. |
| `SetCapacity(int maxCards, bool regenerateSlots = true)` | Changes capacity for custom boards/zones. |
| `SetFaceUp(bool faceUp, bool applyToExisting = false)` | Changes default face state and optionally updates current cards. |
| `PlaceCardAsync(card, animate, overrideFaceUp)` | Places a card with optional animation; completes after layout. |
| `PlaceCard(card)` / `PlaceCardsAsync(cards, ...)` | Immediate single placement / sequential placement with a delay. |
| `PlaceCardsImmediate(cards, overrideFaceUp = true)` | Places a list of cards with **one** immediate layout pass and returns how many were placed (see Batch placement). |
| `RemoveCard(card)` / `RemoveAt(index)` | Detaches one card and re-arranges the rest immediately. A world card goes to the scene root, a UI card to its root Canvas. |
| `RemoveCards(cards)` | Detaches every listed member with **one** layout pass for the rest and returns how many were removed (see Batch placement). |
| `Clear()` / `ClearAndReturn()` | Removes cards by destroying or detaching them (detached cards follow the same rule as `RemoveCard`). |
| `ArrangeCards()` / `FlipAll()` | Re-runs the animated layout / flips every card. |
| `GetAllRanks()` / `GetAllCardData()` | Query current card data; missing or destroyed entries are skipped. |
| `RestoreAllSourcesToBoard()` / `RestoreAllSourcesToBoardAsync()` | Pulls cards back from the configured hands, boards and extra roots (restart helper); places them in one batch. |
| `RegisterInitialCard(card)` | Records a card in the `_initialSpawnedCards` debug view. |

Repeated placement of the same `CardComponent` is ignored and does not raise `OnCardPlaced` again. Separate scene cards with equal `CardData` can occupy different slots. Remove a card from its previous zone before placement; parenting alone does not update that zone's list.

Every layout works from a fixed membership snapshot. Add/remove/clear, capacity changes and a newer arrange pass cancel obsolete motion before a detached card can be moved by its former board. Remaining members are arranged using their current order, including slot indices after removal. `OnCardPlaced`/`OnBoardFull` are published after arrangement only while that exact admission still belongs to the board. `ClearAndReturn()` cancels board-owned motion before handing objects back to the caller.

Membership is committed before Unity invokes hierarchy callbacks during parenting. If `OnTransformParentChanged` or `OnTransformChildrenChanged` clears or releases the board, the interrupted placement cannot add the card back, change its face state, move it or publish placement/full events. A released card retains any new parent and pose assigned by that callback.

If such a callback (or a face-change side effect) moves the incoming card to another parent without using the board API, or destroys it with `DestroyImmediate`, the board drops that admission instead of keeping a phantom member: the card is not listed, does not count toward capacity, keeps its new parent and pose, and the remaining members are re-arranged immediately. No placement/full event is published for it. The same rule covers the whole `PlaceCardAsync` call: a card reparented or destroyed by other code while an animated placement is still arranging is dropped when that arrange finishes. A deferred `Object.Destroy` issued during an immediate placement lands after the placement completed, so that card stays a member until `RemoveCard` or `Clear`.

Immediate placements and removals arrange synchronously and do not create a cancellation source; only an animated arrange links one to the board's destroy token. Every pass reuses the board's member, position and rotation buffers, so a warm immediate placement or removal does not allocate. Each pass still touches every member (sibling index, pose), so placing n cards one by one costs O(n²) transform writes; use batch placement for stacks and restarts.

### Batch placement

`PlaceCardsImmediate(cards, overrideFaceUp)` admits the cards in list order until the board is full (null entries, duplicates and current members are skipped), runs a single immediate layout pass and then raises `OnCardPlaced` once per placed card, in admission order, followed by one `OnBoardFull` when the board is full. Listeners therefore already see the whole batch on the board. The hierarchy-callback rules above apply to every card of the batch: a card that a callback moves elsewhere is dropped and its slot goes to the next listed card, and the members are still arranged once at the end. A clear issued from a hierarchy callback stops the batch without a layout pass; a clear from a placement listener stops the remaining events, including the batch's `OnBoardFull`, so a listener that clears and refills the board sees only the refill's full event. A warm batch does not allocate. `DeckComponent` builds and re-orders its visual stack this way, `DrunkardGame` fills its initial board this way, and `RestoreAllSourcesToBoard()` places everything it collected this way, growing capacity once (with `_autoExpandCapacity`) so `OnBoardFull` is raised at most once and auto slots are regenerated once.

`RemoveCards(cards)` is the removal counterpart: every listed card that is on the board is released exactly as `RemoveCard` releases it (motion cancelled, detached by the Canvas rule below, no event), null entries and non-members are skipped, and the remaining members are arranged once. The list is copied first, so `board.RemoveCards(board.Cards)` empties the board without raising `OnBoardCleared` (unlike `ClearAndReturn()`). A clear issued from a hierarchy callback stops the batch and skips the final layout. A warm call does not allocate. `DrunkardGame` releases its initial board this way before dealing.

```csharp
int placed = board.PlaceCardsImmediate(dealtCards, overrideFaceUp: true);
int removed = board.RemoveCards(discarded);
```

### Released cards and Canvas

A released card keeps its world pose. A world card is unparented to the scene root, as before. A UI card (`RectTransform`) is moved under the root Canvas of the zone it left instead, so it keeps rendering and its canvas scale while you animate it to the next zone (a UI object at the scene root is not drawn). If the zone itself is that root Canvas, the card goes to the zone's parent. `HandComponent` uses the same rule.

### Layout stability and deck stacks

Scattered (`Beat` mode) placement and stack jitter come from `CardLayoutCalculator`, which is deterministic per card index: adding a card does not move the cards already on the board, and layout never consumes `UnityEngine.Random`. When a `DeckComponent` uses a board as its visual stack, the deck owns that board's contents: `Reset()`/`Initialize()` clear it and `BuildVisualStackAsync()` rebuilds it, so do not place unrelated cards there.

`PlaceCardsImmediate(cards)` размещает список карт одним проходом раскладки (вместо прохода на каждую карту) и после него вызывает `OnCardPlaced` для каждой размещённой карты по порядку и один `OnBoardFull`; так строятся стопка колоды, начальная доска «Пьяницы» и `RestoreAllSourcesToBoard()`. Правила колбэков иерархии действуют для каждой карты пакета: карта, которую колбэк унёс к другому родителю, выбывает, и её место получает следующая карта списка. Очистка из колбэка иерархии останавливает пакет без раскладки, а очистка из слушателя `OnCardPlaced` отменяет оставшиеся события пакета, включая его `OnBoardFull`. Обратная операция `RemoveCards(cards)` снимает перечисленные карты так же, как `RemoveCard`, но выравнивает оставшиеся один раз и не вызывает событий (в отличие от `ClearAndReturn()` нет `OnBoardCleared`); список копируется, поэтому можно передать `board.Cards`. Снятая с доски или из руки UI-карта (`RectTransform`) остаётся под корневым Canvas зоны, а не уходит в корень сцены, поэтому видна во время полёта к следующей зоне; мировая карта по-прежнему уходит в корень сцены.

Если колбэк иерархии во время размещения переносит карту к другому родителю в обход API доски или уничтожает её через `DestroyImmediate`, доска отменяет это размещение: карта не попадает в список и не занимает место, сохраняет новый parent и позу, остальные карты сразу выравниваются, события размещения не публикуются. То же действует до конца анимированного размещения: карту, которую другой код перенёс или уничтожил во время раскладки, доска убирает после её завершения. Отложенный `Object.Destroy` во время мгновенного размещения срабатывает уже после него, поэтому такую карту нужно убрать через `RemoveCard` или `Clear`. `GetAllRanks()` и `GetAllCardData()` пропускают пустые и уничтоженные элементы.

In Table/Slots mode, a serialized empty slot element remains optional. Placement still commits the member and parent and raises `OnCardPlaced`, but leaves its world position/rotation unchanged for both animated and immediate calls. Assigned slots retain their normal placement/animation. An empty slot is distinct from the existing board-root fallback used when the array is absent or an index is outside it; no asset rewrite is required.

For scene-independent logic, use `BoardModel(maxCards)` through `ICardContainer`. Its `CanAdd(...)` and `Add(...)` enforce the same finite limit; an overflowing add throws without changing state or emitting `OnChanged`. A zero-capacity model rejects all cards, and a negative constructor limit throws `ArgumentOutOfRangeException`.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `_cardSlots` | Optional slot transforms for Table/Slots mode. An empty element leaves the card's world pose unchanged. |
| `_slotSpacing` | Spacing for generated slots and the default line layout. |
| `_autoGenerateSlots` | Generate slot children on Awake when none are assigned, and regenerate them when capacity changes. |
| `_mode` | `Table` (layout/slots) or `Beat` (scattered discard pile). |
| `_layoutType` | Layout used in Table mode (`Slots`, `Line`, `Fan`, `Grid`, `Stack`, `Scattered`). |
| `_layoutSettings` | Layout parameters; an all-zero value uses defaults with `_slotSpacing`. |
| `_stackZSorting` | Sibling order of placed cards (`TopCardFirst` or `BottomCardFirst`). |
| `_animationConfig` | Local animation config; falls back to `_settingsSourceDeck`, then the global config. |
| `_settingsSourceDeck` | Optional deck whose animation config is used when the local one is empty. |
| `_maxCards` | Capacity; `IsFull` and `CanPlaceCard` use it. |
| `_faceUp` | Face state applied when a placement overrides face state. |
| `_autoExpandCapacity` | The restore helper grows capacity (once, to fit every collected card) instead of dropping cards. |
| `_handSources` / `_boardSources` / `_extraRoots` | Sources collected by `RestoreAllSourcesToBoard()`. |
| `_placeDuration` | Seconds per card for animated layout. |
| `_cards` | Current members (serialized for inspection). |
| `_lastCollectedCards` / `_initialSpawnedCards` | Debug views of the last restore and initial spawn; destroyed entries leave the initial-spawn list on the next registration, so it does not grow across restarts. |
| `OnCardPlaced` / `OnBoardFull` / `OnBoardCleared` | Placement, capacity and clear/restore events. |

## See Also

- [Cards README](./README.md)
