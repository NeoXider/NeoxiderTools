# CursorLockController

**Назначение:** Глобальный или локальный менеджер состояния курсора мыши (видимость и блокировка `CursorLockMode`). Автоматически обрабатывает жизненный цикл (включение/отключение меню) и восстанавливает предыдущее состояние курсора с помощью системы "снимков" (snapshots). Поддерживает пресеты для типичных задач (например, геймплей или страница UI).

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Preset** | Готовые пресеты настроек: `Gameplay_Default`, `UI_Page_ShowCursorWhileActive`, `UI_MenuScene_Standalone`. |
| **Mode** | Что именно мы контролируем: `LockAndHide` (блокировать и скрыть), `OnlyHide` (только невидимость) или `OnlyLock`. |
| **Control Mode** | `AutomaticAndManual` (управляется скриптом и кодом), `AutomaticOnly` или `ManualOnly`. |
| **Lock On Start / Enable / Disable** | В каком состоянии должен быть курсор при активации или старте этого компонента. |
| **Lifecycle Snapshot Mode** | Делать ли снимок состояния курсора (SaveOnEnable/SaveOnDisable), чтобы вернуть его обратно при закрытии меню. |
| **Toggle Key** | Клавиша для ручного переключения курсора (обычно `Escape`). |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void ShowCursor()` | Делает курсор видимым и свободным. |
| `void HideCursor()` | Скрывает курсор и блокирует его в центре экрана. |
| `void SetCursorLocked(bool locked)` | Установить конкретное состояние (true = заблокирован). |
| `void ReleaseControl()` | Отказаться от управления курсором. Вернет управление предыдущему активному `CursorLockController` в стеке. |
| `bool IsLocked { get; }` | Возвращает текущее системное состояние `Cursor.lockState`. |

## Примеры

### Пример No-Code (в Inspector)
На вашей панели "Пауза" (которая включается/выключается) добавьте `CursorLockController`. Выберите пресет **`UI_Page_ShowCursorWhileActive`**. Больше ничего делать не нужно! Когда игрок откроет паузу, курсор появится. Когда закроет — курсор автоматически скроется, и игра продолжится.

### Пример (Код)
```csharp
[SerializeField] private CursorLockController _cursorManager;

public void StartMiniGame()
{
    // Включаем курсор, чтобы игрок мог кликать по UI
    _cursorManager.ShowCursor();
}
```

## См. также
- [PlayerController3DPhysics](PlayerController3DPhysics.md)
- ← [Tools/Move](../README.md)
