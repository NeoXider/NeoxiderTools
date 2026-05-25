# FreeFlyCameraController

**Что это:** компонент свободного полета в стиле Unity Scene View. Подходит для debug-камер, spectator-режима, level preview и внутренних инструментов.

По умолчанию управление активно только при зажатой ПКМ:

- ПКМ: включить обзор и полет
- `W/A/S/D`: движение вперед/влево/назад/вправо
- `E/Q`: вверх/вниз
- `Left Shift`: ускорение
- `Left Alt`: замедление
- колесо мыши: изменить базовую скорость

## Где находится

- Add Component: `Neoxider/Tools/FreeFlyCameraController`
- Create menu: `Neoxider/Tools/Movement/FreeFlyCameraController`
- Namespace: `Neo.Tools`
- Script: `Assets/Neoxider/Scripts/Tools/Move/FreeFlyCameraController.cs`

## Быстрый старт

1. Добавьте `FreeFlyCameraController` на `Camera` или любой объект, который должен летать.
2. Оставьте `Require Look Button` включенным, если нужен режим как в Unity: движение и обзор только при зажатой ПКМ.
3. Отключите `Require Look Button`, если обзор должен работать всегда.
4. Отключите `Move Only While Looking`, если клавиатурное движение должно работать без ПКМ.

## Основные настройки

| Поле | Назначение |
|---|---|
| `Controller Enabled` | Главный runtime-переключатель компонента. |
| `Require Look Button` | Требовать удержание кнопки мыши для обзора. Включено по умолчанию. |
| `Look Mouse Button` | Кнопка мыши для look-mode: `0` левая, `1` правая, `2` средняя. |
| `Move Only While Looking` | Двигаться только пока активен look-mode. Включено по умолчанию. |
| `Lock Cursor While Looking` | Прятать и блокировать курсор на время look-mode, затем восстанавливать предыдущее состояние. |
| `Input Backend` | Legacy Input Manager, New Input System или auto fallback. |
| `Log Input Fallback Warnings` | Разрешить одноразовые warning-логи при fallback между системами ввода. По умолчанию выключено, чтобы не шуметь в runtime. |
| `Movement Space` | `Local` летит по осям объекта, `World` игнорирует поворот объекта. |
| `Base Speed` | Базовая скорость в юнитах в секунду. |
| `Fast / Slow Multiplier` | Множители для `Fast Key` и `Slow Key`. |
| `Allow Mouse Wheel Speed` | Разрешить изменение `Base Speed` колесом мыши. |
| `Look Sensitivity` | Чувствительность мыши. |
| `Invert Y` | Инвертировать вертикальный обзор. |
| `Min / Max Pitch` | Ограничение вертикального угла, чтобы камера не переворачивалась. |

## API

| Метод / свойство | Описание |
|---|---|
| `SetControllerEnabled(bool)` | Включает или отключает контроллер. |
| `SetRequireLookButton(bool)` | Включает или отключает обязательную кнопку мыши. |
| `SetMoveOnlyWhileLooking(bool)` | Управляет зависимостью движения от look-mode. |
| `SetBaseSpeed(float)` | Меняет базовую скорость с учетом min/max. |
| `SetExternalMoveInput(Vector3?)` | Подменяет ввод движения, например из UI, replay или тестов. `null` возвращает встроенный ввод. |
| `SetExternalLookInput(Vector2?)` | Подменяет ввод обзора. `null` возвращает встроенный ввод. |
| `ClearExternalInput()` | Сбрасывает внешний ввод. |
| `SetRotationAngles(float yaw, float pitch)` | Устанавливает yaw/pitch и применяет поворот. |
| `Warp(Vector3, Quaternion)` | Телепортирует объект и синхронизирует внутренние углы. |
| `Tick(float)` | Ручной шаг контроллера для тестов или внешнего драйвера. |
| `IsLooking` / `IsFlying` | Текущее состояние обзора и движения. |

## События

- `On Look Start`
- `On Look Stop`
- `On Fly Start`
- `On Fly Stop`

## Рекомендации

- Для gameplay-камеры игрока используйте `PlayerController3DPhysics`; `FreeFlyCameraController` предназначен для свободной debug/spectator камеры.
- Для UI-пауз и меню можно сочетать компонент с `CursorLockController`, но если включен `Lock Cursor While Looking`, free-fly сам восстанавливает состояние курсора после отпускания ПКМ.
- В network-сценах включайте компонент только на локальной debug/spectator камере. Он не содержит Mirror-синхронизацию.

## См. также

- [CameraRotationController](./CameraRotationController.md)
- [CursorLockController](./CursorLockController.md)
- [PlayerController3DPhysics](./PlayerController3DPhysics.md)
