# PM (Page Manager)

Синглтон управления UI-страницами в NeoxiderPages: включение/выключение по PageId, переключение, возврат назад, интеграция с GM (Win/Lose/End).

**Добавить:** GameObject → Neo → Pages → PM.

## Основное

- Страницы регистрируются по **PageId**. **UIPage** вешается на каждый экран.
- **Open(PageId)** — открыть страницу (эксклюзивно или как popup).
- **Back()** — вернуться на предыдущую страницу.
- **GM Integration** — при смене состояния GM открывать Win/Lose/End страницы.

## См. также

- [UIPage](./UIPage.md)
- [BtnChangePage](./BtnChangePage.md)
- [GM](../../Tools/Managers/GM.md)
