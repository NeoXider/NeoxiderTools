# Компонент SpineController

> **Важно:** Для работы этого компонента требуется установленный пакет **Spine Unity Runtime**. Если он отсутствует, компонент будет отключен и выведет предупреждение.

## 1. Введение

`SpineController` — это высокоуровневая обертка для `SkeletonAnimation` из пакета Spine. Он предоставляет удобный API для управления анимациями, скинами и их состоянием. Компонент автоматизирует многие рутинные задачи, такие как: 

- Получение списка всех доступных анимаций и скинов.
- Воспроизведение анимаций по имени или индексу.
- Управление анимацией по умолчанию (idle).
- Переключение скинов и сохранение выбора между сессиями.

---

## 2. Описание класса

### SpineController
- **Пространство имен**: отсутствует (глобальное)
- **Путь к файлу**: `Assets/Neoxider/Scripts/Tools/Other/SpineController.cs`

**Описание**
Компонент является центральной точкой для управления Spine-анимациями на объекте. Он требует наличия `SkeletonAnimation` и автоматически получает ссылки на него.

### Настройки в инспекторе

#### References
- `skeletonAnimation` (`SkeletonAnimation`): Ссылка на основной компонент Spine.
- `skeletonDataAsset` (`SkeletonDataAsset`): Ссылка на ассет с данными скелета.

#### Animations
- `autoPopulateAnimations` (`bool`): Если `true`, список `animationNames` будет автоматически заполнен всеми анимациями из `skeletonDataAsset`.
- `animationNames` (`List<string>`): Список имен доступных анимаций.
- `defaultAnimationName` (`string`): Имя анимации, которая будет проигрываться по умолчанию (в режиме ожидания).
- `defaultAnimationIndex` (`int`): Индекс анимации по умолчанию в списке (если имя не задано).
- `playDefaultOnEnable` (`bool`): Если `true`, анимация по умолчанию будет запущена при активации компонента.
- `queueDefaultAfterNonLooping` (`bool`): Если `true`, после завершения любой нецикличной анимации будет автоматически запущена анимация по умолчанию.

#### Skins
- `autoPopulateSkins` (`bool`): Если `true`, список `skinNames` будет автоматически заполнен всеми скинами.
- `skinNames` (`List<string>`): Список имен доступных скинов.
- `defaultSkinIndex` (`int`): Индекс скина по умолчанию.
- `persistSkinSelection` (`bool`): Если `true`, выбранный скин будет сохраняться в `PlayerPrefs` и восстанавливаться при следующем запуске.
- `skinPrefsKey` (`string`): Ключ для сохранения индекса скина в `PlayerPrefs`.
- `skinIndexOffset` (`int`): Смещение для индекса скина. Полезно, если первые скины в списке являются служебными и не должны быть доступны для выбора.

### Публичные свойства (Public Properties)
- `Animations` (`IReadOnlyList<string>`): Список только для чтения всех имен анимаций.
- `Skins` (`IReadOnlyList<string>`): Список только для чтения всех имен скинов.
- `SkeletonAnimation` (`SkeletonAnimation`): Прямой доступ к компоненту `SkeletonAnimation`.
- `CurrentAnimationName` (`string`): Имя текущей проигрываемой анимации.
- `CurrentSkinIndex` (`int`): Логический индекс текущего скина (с учетом смещения).
- `CurrentSkinName` (`string`): Имя текущего скина.

### Публичные методы (Public Methods)

#### Управление анимациями
- `Play(int animationIndex, bool loop, ...)`: Проиграть анимацию по индексу.
- `Play(string animationName, bool loop, ...)`: Проиграть анимацию по имени.
- `PlayDefault(bool forceRestart)`: Проиграть анимацию по умолчанию. Если `forceRestart` равно `true`, анимация перезапустится, даже если она уже играет.
- `SetDefaultAnimation(string animationName)`: Установить новую анимацию по умолчанию по имени.
- `SetDefaultAnimationByIndex(int animationIndex)`: Установить новую анимацию по умолчанию по индексу.
- `Stop()`: Остановить все проигрываемые анимации.

#### Управление скинами
- `SetSkin(string skinName)`: Установить скин по имени.
- `SetSkinByIndex(int skinIndex)`: Установить скин по его логическому индексу.
- `NextSkin()`: Переключиться на следующий скин в списке.
- `PreviousSkin()`: Переключиться на предыдущий скин в списке.

### Unity Events
- `OnSwapSkin`: Событие, которое вызывается каждый раз при смене скина. Полезно для обновления других элементов UI, зависящих от скина.
