# ParallaxLayer

**Что это:** компонент бесшовного 2D-параллакса. Он раскладывает тайлы вокруг камеры, двигает слой с заданным множителем, переиспользует тайлы при выходе из видимости и поддерживает автоскролл, варианты спрайтов и предпросмотр в редакторе.

Файл: `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs`

## Принцип модуля

`ParallaxLayer` должен работать как самостоятельный сценовый wrapper: игровая сцена явно передает камеру через `targetCamera` или `SetTargetCamera(Camera)`, а `Camera.main` используется только как опциональный fallback. Это делает компонент пригодным для нескольких камер, split-screen, runtime-spawned камер и тестов.

## Основные поля

| Поле | Назначение |
|------|------------|
| `targetCamera` | Камера, относительно которой считается параллакс. Рекомендуемый путь для production-сцен. |
| `useMainCameraFallback` | Если включено, при пустом `targetCamera` компонент попробует взять `Camera.main`. |
| `logMissingCamera` | Разрешает предупреждение через `NeoDiagnostics` при отсутствии камеры. Логи остаются под глобальным diagnostics gate. |
| `parallaxMultiplier` | Множитель реакции слоя на движение камеры: `0` почти приклеен к камере, `1` двигается в противоположную сторону на ту же величину. |
| `scrollSpeed` | Постоянный world-space скролл слоя. |
| `generateInEditor` | Создает preview tiles в редакторе. |
| `tileSpacing` | Дополнительный отступ между тайлами. |
| `tileHorizontally`, `tileVertically` | Оси бесшовного тайлинга. |
| `paddingTiles` | Запасные тайлы за пределами экрана. |
| `templateRenderer` | `SpriteRenderer`-шаблон, из которого копируются sprite/material/sorting настройки. |
| `spriteVariants` | Альтернативные спрайты для init/recycle. |
| `randomiseOnInit`, `randomiseOnRecycle` | Рандомизация вариантов при создании и переиспользовании. |
| `fitToMaxSpriteSize` | Подгоняет тайлы под самый крупный спрайт, чтобы не было щелей. |

## C# API

```csharp
parallaxLayer.SetTargetCamera(camera);
Camera activeCamera = parallaxLayer.TargetCamera;
```

`SetTargetCamera` сбрасывает missing-camera state и переинициализирует слой, если компонент активен.

## Настройка

1. Добавьте `ParallaxLayer` на объект со `SpriteRenderer`.
2. Назначьте `targetCamera` явно. Оставляйте `useMainCameraFallback` включенным только для простых демо-сцен.
3. Настройте `parallaxMultiplier` и `scrollSpeed`.
4. При необходимости включите вертикальный тайлинг, `paddingTiles`, `tileSpacing` и `spriteVariants`.
5. Для редакторского предпросмотра оставьте `generateInEditor` включенным.

## Поведение

- При реинициализации слой удаляет старые tile objects и строит новый пул.
- В Edit Mode удаление выполняется через `DestroyImmediate`, в Play Mode через `Destroy`.
- Если камеры нет, слой не создает тайлы и не спамит консоль: warning возможен только при включенном `logMissingCamera` и глобальном `NeoDiagnostics`.
- `templateRenderer` восстанавливается после очистки, поэтому исходный объект не остается скрытым после остановки/отключения.
