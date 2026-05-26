# GridGameBuilder

**Что это:** scene-конструктор для GridSystem. Компонент добавляет и поддерживает набор выбранных grid-модулей на одном GameObject: debug drawer, spawners, Match3, TicTacToe, SlidingMerge.

**Когда использовать:** когда нужно быстро собрать игровое поле через Inspector без ручного добавления каждого компонента. Для production-кода можно использовать те же компоненты напрямую.

## API

- `Features` - флаги подключаемых модулей.
- `EnsureConfigured()` - добавляет выбранные компоненты, не удаляя уже существующие.
- `Generator` - ссылка на `FieldGenerator`.

## Принцип

`GridGameBuilder` не содержит игровых правил. Он только собирает объект сцены. Правила остаются в отдельных сервисах:

- `Match3BoardService`
- `TicTacToeBoardService`
- `SlidingMergeBoardService`
- custom runtime service пользователя

## Пример

Для 2048-like игры:

1. Добавьте `GridGameBuilder`.
2. Включите `DebugDrawer` и `SlidingMerge`.
3. Установите `FieldGenerator.Config.Size = (4, 4, 1)`.
4. Подключите input/view к `SlidingMergeBoardService.Slide(...)`.

Для Match3:

1. Включите `DebugDrawer` и `Match3`.
2. Настройте размер и shape mask.
3. Подключите view к `Match3BoardService.OnBoardChanged`.
