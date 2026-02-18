# GM (Game Manager)

Синглтон-менеджер состояний игры. Хранит текущее и предыдущее состояние (Menu, Preparing, Game, Win, Lose и т.д.), управляет паузой (Time.timeScale) и FPS.

**Добавить:** GameObject → Neoxider → Tools → GM (или через Singleton).

## Основное

- **State** / **Last State** — текущее и предыдущее состояние игры.
- **Use Time Scale Pause** — при паузе обнулять Time.timeScale.
- **Fps** — целевой FPS (для настроек качества).
- **Start On Awake** — переходить в первое состояние при старте.

## События

Через **EM** (Event Manager) подписываются события OnMenu, OnGameStart, OnWin, OnLose и др. GM задаёт состояние, EM рассылает вызовы.

## См. также

- [EM](./EM.md) — Event Manager по состояниям игры.
- [Singleton](./Singleton.md) — базовый класс синглтонов.
