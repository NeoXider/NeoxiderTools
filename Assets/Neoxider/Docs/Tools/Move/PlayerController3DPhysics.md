# PlayerController3DPhysics

**Назначение:** Мощный 3D контроллер от первого лица (или третьего, если камера вынесена), основанный на `Rigidbody`. Поддерживает ходьбу, бег, прыжки (с койот-таймом и буферизацией ввода), вращение камеры (Look) с учетом чувствительности мыши, а также интеграцию с `CursorLockController` и паузой. Поддерживает как Legacy Input Manager, так и New Input System.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Rigidbody** | Ссылка на `Rigidbody` персонажа. Настраивается автоматически. |
| **Camera Pivot** | Трансформ для вращения камеры по вертикали (Pitch). По умолчанию берется `Camera.main`. |
| **Walk / Run Speed** | Скорость ходьбы и бега. |
| **Jump Impulse** | Сила прыжка. Можно отключить прыжки галочкой `Can Jump`. |
| **Ground Check Radius** | Радиус сферы для проверки земли (OverlapSphere). |
| **Look Yaw Mode** | Как вращать персонажа при обзоре: `RotateCharacter`, `RotateCameraPivot` или `RotateBoth`. |
| **Use Game Settings Mouse Sensitivity** | Брать ли чувствительность мыши из глобального класса `GameSettings` (авто-обновление). |
| **Enable Cursor Control** | **По умолчанию включено.** Управляет ли этот компонент курсором: блокировка при старте, Escape, `SetCursorLocked`, авто-блок при `SetLookEnabled(true)` с *Pause Look When Cursor Visible*. Если **выключить** — курсор не трогается (удобно, когда всё делает `CursorLockController` или UI). Само движение/обзор от мыши не отключаются. |
| **Lock Cursor On Start** | В **`Start()`** (не в `Awake`) блокировать и скрывать курсор при входе в режим игры. Игнорируется, если **Enable Cursor Control** выключен или назначен активный внешний `CursorLockController`. |
| **Pause Look When Cursor Visible** | Не крутить камеру, пока курсор видим (разблокирован). |
| **Disable Look On Pause** | Отключать ли вращение камеры, когда игра ставится на паузу через `EventManager.OnPause`. |
| **Toggle Cursor On Escape** | Переключать блокировку курсора и look по Escape (см. код). Не выполняется, если **Enable Cursor Control** выключен. |

### Курсор и ранний запуск

Блокировка курсора при включённом **Lock Cursor On Start** выполняется в **`Start()`**, не в `Awake`. Если нужно полностью исключить вмешательство контроллера в курсор — снимите **Enable Cursor Control** в Inspector (или выставьте `CursorControlEnabled = false` до первого кадра).

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SetMovementEnabled(bool enabled)` | Разрешает/запрещает персонажу двигаться (ходьба, бег). |
| `void SetJumpEnabled(bool enabled)` | Разрешает/запрещает прыжки. |
| `void SetLookEnabled(bool enabled)` | Разрешает/запрещает вращение камеры мышью. |
| `void SetCursorLocked(bool locked)` | Блокирует/показывает курсор. **Ничего не делает**, если **Enable Cursor Control** снят. |
| `bool CursorControlEnabled { get; set; }` | Включить/выключить любое изменение курсора из этого компонента (по умолчанию `true`). |
| `void Teleport(Vector3 worldPosition)` | Мгновенно перемещает персонажа, сбрасывая его текущую скорость. |
| `void SetMoveInput(Vector2? input)` | Использовать кастомный ввод (например, экранный джойстик). Передайте `null`, чтобы вернуть управление с клавиатуры. |
| `bool IsGrounded { get; }` | Находится ли персонаж на земле прямо сейчас. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnJumped` / `OnLanded` | *(нет)* | Вызывается в момент прыжка или касания земли. |
| `OnMoveStart` / `OnMoveStop` | *(нет)* | Вызывается, когда персонаж начинает движение (нажаты клавиши) или останавливается. |

## Сеть (Mirror)

При установленном **Mirror** компонент учитывает владение: ввод, курсор (где применимо) и физика выполняются только у владеющего клиента; один префаб подходит и для оффлайна, и для сетевого игрока.

## Примеры

### Пример No-Code (в Inspector)
Разместите капсулу с `Rigidbody` на сцене. Добавьте `PlayerController3DPhysics`. Добавьте камеру как дочерний объект капсулы и перетащите её в `Camera Pivot`. Настройте маску `Ground Mask` на слой `Default` (или слой земли). Запустите игру — вы сразу сможете бегать (WASD), прыгать (Space) и осматриваться (Мышь).

### Пример (Код)
```csharp
[SerializeField] private PlayerController3DPhysics _player;

public void ImmobilizePlayerForCutscene()
{
    // Отбираем у игрока возможность ходить и крутить камерой
    _player.SetMovementEnabled(false);
    _player.SetLookEnabled(false);
}
```

## См. также
- [CursorLockController](CursorLockController.md)
- [KeyboardMover](KeyboardMover.md)
- ← [Tools/Move](../README.md)
