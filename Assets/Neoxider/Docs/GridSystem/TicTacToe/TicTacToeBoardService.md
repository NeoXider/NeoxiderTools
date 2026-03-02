# TicTacToeBoardService

**Что это:** runtime-сервис крестиков-ноликов на базе FieldGenerator: ходы, смена игрока, проверка победы и ничьей, сброс. События OnPlayerChanged, OnWinnerDetected, OnDrawDetected. Пространство имён `Neo.GridSystem.TicTacToe`, файл `Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs`.

**Как использовать:** Add Component на объект с FieldGenerator; при необходимости включить resetOnStart; обрабатывать ходы через API и подписываться на события для UI.

---

## События

- **OnPlayerChanged** (int) — смена активного игрока.
- **OnWinnerDetected** (int) — определён победитель.
- **OnDrawDetected** — ничья.

## См. также

- [FieldGenerator](../FieldGenerator.md)
