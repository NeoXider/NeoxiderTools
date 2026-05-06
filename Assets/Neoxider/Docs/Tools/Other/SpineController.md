# SpineController

**Назначение:** Универсальный контроллер для Spine-анимаций (`SkeletonAnimation`). Управляет проигрыванием анимаций по индексу или имени, переключением скинов с сохранением в `PlayerPrefs`, а также автовозвратом к дефолтной анимации.

> ⚠️ Требуется пакет **Spine Unity Runtime** (`SPINE_UNITY` define).

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Skeleton Animation** | Ссылка на `SkeletonAnimation` (авто-назначение). |
| **Auto Populate Animations / Skins** | Автоматически заполнять списки из `SkeletonDataAsset`. |
| **Default Animation Name / Index** | Анимация покоя (idle). |
| **Play Default On Enable** | Запускать дефолтную анимацию при включении компонента. |
| **Queue Default After Non Looping** | Автоматический возврат к idle после разовой анимации. |
| **Persist Skin Selection** | Сохранять выбранный скин в `PlayerPrefs`. |

## API

| Метод / Свойство | Описание |
|------------------|----------|
| `TrackEntry Play(string name, bool loop, float mix, bool queueDefault)` | Проиграть анимацию по имени. |
| `TrackEntry Play(int index, bool loop, float mix, bool queueDefault)` | Проиграть анимацию по индексу. |
| `void PlayDefault()` | Вернуться к дефолтной анимации. |
| `void Stop()` | Остановить все треки. |
| `void SetSkinByIndex(int skinIndex)` | Установить скин по индексу. |
| `void SetSkin(string skinName)` | Установить скин по имени. |
| `void NextSkin()` / `void PreviousSkin()` | Переключить скин вперёд/назад. |
| `string CurrentAnimationName { get; }` | Имя текущей анимации. |
| `int CurrentSkinIndex { get; }` | Индекс текущего скина. |

## Unity Events

| Событие | Аргументы | Описание |
|---------|-----------|----------|
| `OnSwapSkin` | *(нет)* | Скин был изменён. |

## Примеры

### Пример No-Code (в Inspector)
На объекте с `SkeletonAnimation` добавьте `SpineController`. Списки анимаций и скинов заполнятся автоматически. Выберите `Default Animation = idle`. Подключите кнопку UI к `SpineController.NextSkin()` для переключения скинов.

### Пример (Код)
```csharp
[SerializeField] private SpineController _spine;

public void PlayAttackAnimation()
{
    _spine.Play("attack", false, 0.1f, true);
}
```

## См. также
- ← [Tools/Other](README.md)
