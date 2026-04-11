# InteractionRayProvider

Компонент для камеры, который управляет режимом рейкаста для **всех** `InteractiveObject` в сцене.

> **Автосоздание**: если на `Camera.main` нет `InteractionRayProvider`, он добавляется автоматически с режимом `Both`.

## Расположение

`Assets/Neoxider/Scripts/Tools/InteractableObject/InteractionRayProvider.cs`

## Основные режимы (RayMode)

| Режим | Hover от | Click от | Описание |
|-------|----------|----------|----------|
| `Mouse` | Курсор / палец | Курсор / палец | Десктоп по умолчанию |
| `ScreenCenter` | Центр экрана | Центр экрана | Мобильный прицел (crosshair) |
| `Both` | Центр экрана | Курсор / палец | Универсальный (по умолчанию) |

## Настройка

### В инспекторе
1. Выберите камеру (или `Camera.main`)
2. Добавьте компонент `InteractionRayProvider` (или дождитесь автосоздания)
3. Выберите `Ray Mode` в инспекторе

### Через код
```csharp
var provider = InteractionRayProvider.FindOnMainCamera();
provider.Mode = InteractionRayProvider.RayMode.ScreenCenter;
```

## Приоритет

1. `InteractionRayProvider` на камере — главный источник
2. `useScreenCenterRay` на `InteractiveObject` — per-object fallback
3. Если оба выключены — обычный рейкаст от мыши

## Мобильный сценарий (FPS/TPS)

```
1. Добавить InteractionRayProvider на камеру → Mode = ScreenCenter
2. Нарисовать UI-прицел в центре Canvas
3. Свайп вращает камеру (через PlayerController3D.SetLookInput)
4. Тап = взаимодействие с объектом под прицелом
```

## Public API

| Метод / Свойство | Описание |
|-----------------|----------|
| `Mode { get; set; }` | Текущий режим `Mouse`/`ScreenCenter`/`Both` |
| `Camera { get; }` | Кэшированная ссылка на камеру |
| `TryGetHoverRay(out Ray)` | Получить луч для hover-проверки |
| `TryGetClickRay(out Ray)` | Получить луч для click-подтверждения |
| `UseScreenCenterForHover` | `true` если hover от центра экрана |
| `UseScreenCenterForClick` | `true` если click от центра экрана |
| `FindOnMainCamera()` | Найти или создать провайдер на `Camera.main` |
