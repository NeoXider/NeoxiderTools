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
| **Lock Cursor On Start** | Автоматически блокировать и скрывать курсор при старте игры. |
| **Disable Look On Pause** | Отключать ли вращение камеры, когда игра ставится на паузу через `EventManager.OnPause`. |
| **Toggle Cursor On Escape** | Позволять ли игроку освобождать курсор клавишей Escape. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `void SetMovementEnabled(bool enabled)` | Разрешает/запрещает персонажу двигаться (ходьба, бег). |
| `void SetJumpEnabled(bool enabled)` | Разрешает/запрещает прыжки. |
| `void SetLookEnabled(bool enabled)` | Разрешает/запрещает вращение камеры мышью. |
| `void Teleport(Vector3 worldPosition)` | Мгновенно перемещает персонажа, сбрасывая его текущую скорость. |
| `void SetMoveInput(Vector2? input)` | Использовать кастомный ввод (например, экранный джойстик). Передайте `null`, чтобы вернуть управление с клавиатуры. |
| `bool IsGrounded { get; }` | Находится ли персонаж на земле прямо сейчас. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnJumped` / `OnLanded` | *(нет)* | Вызывается в момент прыжка или касания земли. |
| `OnMoveStart` / `OnMoveStop` | *(нет)* | Вызывается, когда персонаж начинает движение (нажаты клавиши) или останавливается. |

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
