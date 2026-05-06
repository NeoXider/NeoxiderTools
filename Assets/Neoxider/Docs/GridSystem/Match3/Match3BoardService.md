# Match3BoardService

**Что это:** runtime-сервис для Match-3 на базе FieldGenerator: инициализация, проверка свапа, поиск и снос совпадений, обрушивание и добор. События по фазам и результату. Пространство имён `Neo.GridSystem.Match3`, файл `Scripts/GridSystem/Match3/Match3BoardService.cs`.

**Как использовать:** Add Component на объект с FieldGenerator; настроить минимальную длину совпадения и автогенерацию; вызывать свап и обновление из кода или UI. Подписаться на события для подсчёта очков и анимаций.

---

## См. также

- [FieldGenerator](../FieldGenerator.md)


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `32` | 32. |
| `Match3ResolvePhase` | Match3Resolve Phase. |
| `OnBoardChanged` | On Board Changed. |
| `OnBoardShuffled` | On Board Shuffled. |
| `OnMatchesResolved` | On Matches Resolved. |
| `_availableTiles` | Available Tiles. |
| `_resolveDelaySeconds` | Resolve Delay Seconds. |
| `_useResolveDelay` | Use Resolve Delay. |
| `true` | True. |