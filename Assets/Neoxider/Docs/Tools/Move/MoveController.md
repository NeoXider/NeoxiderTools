# Компонент Move Controller

## 1. Введение

`MoveController` — это гибкий компонент для управления движением объекта, построенный на принципе разделения ответственностей. Сам `MoveController` отвечает только за то, **как** двигать объект (с какой скоростью, с какими ограничениями), но не знает, **почему** он движется. 

Инструкции для движения он получает от другого компонента, который должен находиться на том же объекте и реализовывать интерфейс `IMovementInputProvider`. Это позволяет одному и тому же `MoveController` управляться как вводом игрока, так и искусственным интеллектом или записанным сценарием — достаточно лишь поменять компонент-поставщик ввода.

---

## 2. Интерфейс IMovementInputProvider

Это "контракт", который должен выполнить любой компонент, желающий управлять `MoveController`. 

```csharp
public interface IMovementInputProvider
{
    // Должен возвращать true, если в данный момент есть активный ввод
    bool IsInputActive();

    // Должен возвращать вектор направления (для режима DirectionBased)
    Vector3 GetDirection();

    // Должен возвращать мировую позицию цели (для режима PositionBased)
    Vector3 GetTargetPosition();
}
```

---

## 3. Описание класса

### MovementController
- **Пространство имен**: Глобальное
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Move/MoveController.cs`

**Описание**
Компонент, который в `Update` запрашивает данные у своего `IMovementInputProvider` и перемещает `Transform` в соответствии с полученными инструкциями и собственными настройками.

**Ключевые поля**
- `movementType`: Режим движения.
  - `DirectionBased`: Объект движется в направлении, полученном от `GetDirection()`. Подходит для управления персонажем (WASD, джойстик).
  - `PositionBased`: Объект движется к точке, полученной от `GetTargetPosition()`. Подходит для механик "click-to-move" или следования за целью.
- `moveSpeed`: Скорость движения объекта.
- `xConstraint`, `yConstraint`, `zConstraint`: Структуры, позволяющие ограничить перемещение объекта по каждой из осей в пределах `min` и `max`.

**Unity Events**
- `OnMovementStart`: Вызывается один раз, когда `IsInputActive()` у поставщика ввода меняется с `false` на `true`.
- `OnMovementStop`: Вызывается один раз, когда `IsInputActive()` меняется с `true` на `false`.

---

## 4. Как использовать

1.  Создайте свой скрипт-поставщик ввода, например, `PlayerInput`. Заставьте его реализовать интерфейс `IMovementInputProvider`.

    ```csharp
    public class PlayerInput : MonoBehaviour, IMovementInputProvider
    {
        public bool IsInputActive() => GetDirection().sqrMagnitude > 0.1f;

        public Vector3 GetDirection()
        {
            float hor = Input.GetAxis("Horizontal");
            float ver = Input.GetAxis("Vertical");
            return new Vector3(hor, 0, ver);
        }

        public Vector3 GetTargetPosition() => Vector3.zero; // Не используется в режиме DirectionBased
    }
    ```
2.  На ваш объект-персонаж добавьте два компонента:
    - `MovementController`: Настройте его скорость и ограничения.
    - `PlayerInput`: Ваш свежесозданный скрипт.

3.  Запустите игру. `MovementController` автоматически найдет `PlayerInput` на том же объекте и начнет использовать его данные для перемещения.
