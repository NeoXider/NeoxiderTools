# EM (Event Manager)

Синглтон для событий по состояниям игры. UnityEvent на каждое состояние: Menu, Preparing, GameStart, Restart, StopGame, Win, Lose, End.

**Добавить:** GameObject → Neo → Tools → EM (или через Singleton).

## События

- **On Menu**, **On Preparing**, **On Game Start** — вход в меню, подготовку, старт игры.
- **On Restart**, **On Stop Game** — рестарт и остановка.
- **On Win**, **On Lose**, **On End** — победа, поражение, конец.

Обычно GM меняет состояние, подписчики (UI, звук, аналитика) вешаются на EM.

## См. также

- [GM](./GM.md) — Game Manager состояний.
