# Движение (Move)

**Что это:** Этот раздел содержит разнообразные инструменты для управления движением и позиционированием объектов, от простого следования до сложных контроллеров движения и работы с камерой.

**Оглавление:** см. список ссылок ниже.

---


Этот раздел содержит разнообразные инструменты для управления движением и позиционированием объектов, от простого следования до сложных контроллеров движения и работы с камерой.

В версии 6.0.5 модуль доработан: универсальные API (SetTarget, SetSpeed, Teleport, события OnMoveStart/OnMoveStop, OnTargetLost, OnApplyFailed и др.), опции ввода (New Input System, выбор кнопки мыши, осей), гистерезис и пороги в DistanceChecker, режимы курсора в CursorLockController, реализация IMover в MouseMover3D. Подробности — в CHANGELOG и в описаниях компонентов ниже.

## Файлы

- [AdvancedForceApplier](./AdvancedForceApplier.md)
- [CameraConstraint](./CameraConstraint.md)
- [CameraRotationController](./CameraRotationController.md)
- [CursorLockController](./CursorLockController.md)
- [DistanceChecker](./DistanceChecker.md)
- [Follow](./Follow.md)
- [PlayerController2DAnimatorDriver](./PlayerController2DAnimatorDriver.md)
- [PlayerController2DPhysics](./PlayerController2DPhysics.md)
- [PlayerController3DAnimatorDriver](./PlayerController3DAnimatorDriver.md)
- [PlayerController3DPhysics](./PlayerController3DPhysics.md)
- [ScreenPositioner](./ScreenPositioner.md)
- [UniversalRotator](./UniversalRotator.md)

## Частый сценарий: gameplay + UI страница

Для FPS/TPS-сцен часто удобно держать `PlayerController3DPhysics` на игроке, а `CursorLockController` не на нём, а на объекте страницы меню/паузы.

Рекомендуемая схема:

- страница включается -> `CursorLockController` показывает курсор
- `CursorLockController` страницы назначен в `PlayerController3DPhysics.External Cursor Lock Controller`
- в событии открытия страницы вызывается `PlayerController3DPhysics.SetLookEnabled(false)`
- страница выключается -> `CursorLockController` снова прячет/блокирует курсор
- в событии закрытия страницы вызывается `PlayerController3DPhysics.SetLookEnabled(true)`

Дополнительно можно использовать отдельный gameplay-shortcut, например `Z`, через `CursorLockController.Cursor Access Key`:

- `HoldToShowCursor` — курсор виден, пока удерживается клавиша
- `ToggleShowCursor` — отдельный mini-toggle поверх обычного режима

Подробно это описано в:

- [CursorLockController](./CursorLockController.md)
- [PlayerController3DPhysics](./PlayerController3DPhysics.md)

## Папки

- [MovementToolkit](./MovementToolkit)
