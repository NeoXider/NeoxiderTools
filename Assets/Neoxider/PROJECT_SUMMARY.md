# NeoxiderTools — краткий PROJECT_SUMMARY

## Архитектура и структура

- **UPM пакет**: `Assets/Neoxider/package.json` (текущая версия: **5.8.13**)
- **Unity**: 2022.1+
- **Основной namespace**: `Neo` (далее `Neo.Tools.*`, `Neo.UI.*`, `Neo.Save.*`, `Neo.Cards.*` и т.д.)
- **Модульность**: модули изолированы через `.asmdef` (см. `Assets/Neoxider/Scripts/**/Neo.*.asmdef` и
  `Assets/Neoxider/Editor/Neo.Editor.asmdef`)
- **Документация**: `Assets/Neoxider/Docs/**`
- **Опциональные модули**: `Assets/NeoxiderPages/**` (PageManager / `Neo.Pages`, отдельные asmdef + свои `Docs/`)

Структура:

```text
Assets/Neoxider/
  Scripts/   # runtime + часть editor-скриптов в подпапках Editor
  Editor/    # editor tools (окна/утилиты/инспектор)
  Docs/      # документация по модулям/компонентам
  Demo/      # примеры
  Prefabs/   # готовые префабы

Assets/NeoxiderPages/
  Runtime/   # runtime модуль страниц (Neo.Pages)
  Editor/    # editor инструменты (Neo.Pages.Editor)
  Prefabs/   # демо префабы PageManager
  Scenes/    # демо сцены PageManager
  Docs/      # документация NeoxiderPages
```

## Правила работы (важно)

- **Сначала переиспользуй** готовые компоненты из `Assets/Neoxider/Scripts/**` (особенно `Tools/*`, `Save/*`, `UI/*`,
  `Extensions/*`).
- **Не создавай дубликаты**: если нужная функция близка — расширяй существующий компонент/модуль и обновляй доки.
- **Для UI/No‑Code**: предпочитай `MonoBehaviour` + `UnityEvent` (подписки через Inspector).
- **Для данных**: предпочитай `ScriptableObject`.
- **Для Editor‑функций**: код в `Assets/Neoxider/Editor/**` или `Scripts/**/Editor/**` + правильные asmdef ссылки.
- **После изменений**: обнови соответствующий `.md` в `Docs/` и запись в `Assets/Neoxider/CHANGELOG.md`.

## Каталог скриптов (все `.cs`)

Формат: `путь — кратко что это`.

### Animations (`Assets/Neoxider/Scripts/Animations/`)

- `Assets/Neoxider/Scripts/Animations/AnimationType.cs` — enum типов анимации значений.
- `Assets/Neoxider/Scripts/Animations/AnimationUtils.cs` — утилиты анимации/интерполяции.
- `Assets/Neoxider/Scripts/Animations/ColorAnimator.cs` — компонент анимации цвета.
- `Assets/Neoxider/Scripts/Animations/FloatAnimator.cs` — компонент анимации float.
- `Assets/Neoxider/Scripts/Animations/Vector3Animator.cs` — компонент анимации Vector3.

### Audio (`Assets/Neoxider/Scripts/Audio/`)

- `Assets/Neoxider/Scripts/Audio/AMSettings.cs` — настройки аудио-системы.
- `Assets/Neoxider/Scripts/Audio/RandomMusicController.cs` — контроллер случайной музыки.
- `Assets/Neoxider/Scripts/Audio/SettingMixer.cs` — управление AudioMixer (громкости/параметры).
- `Assets/Neoxider/Scripts/Audio/AudioSimple/AM.cs` — упрощенный AudioManager.
- `Assets/Neoxider/Scripts/Audio/AudioSimple/PlayAudio.cs` — проигрывание клипа/звука.
- `Assets/Neoxider/Scripts/Audio/AudioSimple/PlayAudioBtn.cs` — проигрывание звука по кнопке/событию.
- `Assets/Neoxider/Scripts/Audio/View/AudioControl.cs` — UI контроль аудио.

### Bonus (`Assets/Neoxider/Scripts/Bonus/`)

- `Assets/Neoxider/Scripts/Bonus/LineRoulett.cs` — линейная рулетка.

#### Bonus/Collection

- `Assets/Neoxider/Scripts/Bonus/Collection/Box.cs` — контейнер/визуал бокса.
- `Assets/Neoxider/Scripts/Bonus/Collection/Collection.cs` — система коллекций.
- `Assets/Neoxider/Scripts/Bonus/Collection/CollectionVisualManager.cs` — визуализация коллекций.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollection.cs` — элемент коллекции.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionData.cs` — данные коллекции.
- `Assets/Neoxider/Scripts/Bonus/Collection/ItemCollectionInfo.cs` — инфо/метаданные коллекции.

#### Bonus/Slot

- `Assets/Neoxider/Scripts/Bonus/Slot/CheckSpin.cs` — проверка результата/комбинаций.
- `Assets/Neoxider/Scripts/Bonus/Slot/Row.cs` — ряд слот-машины.
- `Assets/Neoxider/Scripts/Bonus/Slot/SlotElement.cs` — элемент слота.
- `Assets/Neoxider/Scripts/Bonus/Slot/SpeedControll.cs` — управление скоростью.
- `Assets/Neoxider/Scripts/Bonus/Slot/SpinController.cs` — контроллер слот-машины.
- `Assets/Neoxider/Scripts/Bonus/Slot/VisualSlotLines.cs` — визуал линий выигрыша.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/BetsData.cs` — данные ставок.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/LinesData.cs` — данные линий.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpriteMultiplayerData.cs` — данные множителей.
- `Assets/Neoxider/Scripts/Bonus/Slot/Data/SpritesData.cs` — данные спрайтов.

#### Bonus/TimeReward

- `Assets/Neoxider/Scripts/Bonus/TimeReward/TimeReward.cs` — награда по времени. GetFormattedTimeLeft, TryGetLastRewardTimeUtc, GetElapsedSinceLastReward.

#### Bonus/WheelFortune

- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelFortune.cs` — колесо фортуны.
- `Assets/Neoxider/Scripts/Bonus/WheelFortune/WheelMoneyWin.cs` — обработка выигрыша.

### Cards (`Assets/Neoxider/Scripts/Cards/`) — модуль карточных игр (MVP + компоненты)

#### Cards/Core

- `Assets/Neoxider/Scripts/Cards/Core/Data/CardData.cs` — данные карты.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/CardLocation.cs` — enum расположения карты.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/DeckType.cs` — enum типа колоды.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Rank.cs` — enum ранга.
- `Assets/Neoxider/Scripts/Cards/Core/Enums/Suit.cs` — enum масти.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardContainer.cs` — интерфейс контейнера карт.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/ICardView.cs` — интерфейс view карты.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IDeckView.cs` — интерфейс view колоды.
- `Assets/Neoxider/Scripts/Cards/Core/Interfaces/IHandView.cs` — интерфейс view руки.
- `Assets/Neoxider/Scripts/Cards/Core/Model/CardContainerModel.cs` — базовая модель контейнера.

#### Cards/Model

- `Assets/Neoxider/Scripts/Cards/Model/BoardModel.cs` — модель борда.
- `Assets/Neoxider/Scripts/Cards/Model/DeckModel.cs` — модель колоды.
- `Assets/Neoxider/Scripts/Cards/Model/HandModel.cs` — модель руки.

#### Cards/View

- `Assets/Neoxider/Scripts/Cards/View/CardView.cs` — визуал карты.
- `Assets/Neoxider/Scripts/Cards/View/DeckView.cs` — визуал колоды.
- `Assets/Neoxider/Scripts/Cards/View/HandView.cs` — визуал руки.

#### Cards/Presenter

- `Assets/Neoxider/Scripts/Cards/Presenter/CardPresenter.cs` — presenter карты.
- `Assets/Neoxider/Scripts/Cards/Presenter/DeckPresenter.cs` — presenter колоды.
- `Assets/Neoxider/Scripts/Cards/Presenter/HandPresenter.cs` — presenter руки.

#### Cards/Components

- `Assets/Neoxider/Scripts/Cards/Components/BoardComponent.cs` — компонент борда.
- `Assets/Neoxider/Scripts/Cards/Components/CardComponent.cs` — компонент карты.
- `Assets/Neoxider/Scripts/Cards/Components/DeckComponent.cs` — компонент колоды.
- `Assets/Neoxider/Scripts/Cards/Components/HandComponent.cs` — компонент руки.

#### Cards/Config

- `Assets/Neoxider/Scripts/Cards/Config/DeckConfig.cs` — конфиг колоды.
- `Assets/Neoxider/Scripts/Cards/Config/HandLayoutType.cs` — enum раскладки.

#### Cards/Poker

- `Assets/Neoxider/Scripts/Cards/Poker/PokerCombination.cs` — enum комбинаций.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandEvaluator.cs` — оценка комбинаций.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerHandResult.cs` — результат оценки.
- `Assets/Neoxider/Scripts/Cards/Poker/PokerRules.cs` — правила.

#### Cards/Utils

- `Assets/Neoxider/Scripts/Cards/Utils/CardComparer.cs` — сравнение карт.

#### Cards/Drunkard

- `Assets/Neoxider/Scripts/Cards/Drunkard/DrunkardGame.cs` — игра “Пьяница”.

#### Cards/Editor

- `Assets/Neoxider/Scripts/Cards/Editor/DeckConfigEditor.cs` — редактор/инспектор DeckConfig.

### Extensions (`Assets/Neoxider/Scripts/Extensions/`) — подробности (красивое перечисление)

#### `AudioExtensions.cs` — плавные изменения громкости `AudioSource`

- `AudioExtensions.FadeTo` — плавно меняет громкость `AudioSource` до целевого значения за время.
- `AudioExtensions.FadeOut` — плавно снижает громкость до нуля и останавливает источник.
- `AudioExtensions.FadeIn` — плавно повышает громкость и запускает источник при необходимости.

#### `ColorExtension.cs` — утилиты модификации и форматирования `Color`

- `ColorExtension.WithAlpha` — возвращает цвет с новым альфа‑каналом.
- `ColorExtension.With` — возвращает цвет с выборочной заменой RGBA‑каналов.
- `ColorExtension.WithRGB` — возвращает цвет с заменой RGB‑каналов.
- `ColorExtension.Darken` — затемняет цвет на заданную величину.
- `ColorExtension.Lighten` — осветляет цвет на заданную величину.
- `ColorExtension.ToHexString` — конвертирует `Color` в HEX‑строку.

#### `ComponentExtensions.cs` — утилиты для компонентов и иерархий

- `ComponentExtensions.GetOrAdd<T>` — возвращает компонент `T` или добавляет его к объекту.
- `ComponentExtensions.GetPath` — возвращает путь к объекту в иерархии.

#### `CoroutineExtensions.cs` — запуск действий по времени и условиям

- `CoroutineExtensions.Delay(MonoBehaviour)` — запускает действие через задержку на объекте.
- `CoroutineExtensions.WaitUntil(MonoBehaviour)` — запускает действие, когда условие станет истинным.
- `CoroutineExtensions.WaitWhile(MonoBehaviour)` — запускает действие, когда условие станет ложным.
- `CoroutineExtensions.DelayFrames(MonoBehaviour)` — запускает действие через указанное число кадров.
- `CoroutineExtensions.NextFrame(MonoBehaviour)` — запускает действие на следующем кадре.
- `CoroutineExtensions.EndOfFrame(MonoBehaviour)` — запускает действие в конце кадра.
- `CoroutineExtensions.RepeatUntil(MonoBehaviour)` — повторяет действие, пока условие не станет истинным.
- `CoroutineExtensions.Delay(GameObject)` — запускает действие через задержку на объекте.
- `CoroutineExtensions.WaitUntil(GameObject)` — запускает действие, когда условие станет истинным на объекте.
- `CoroutineExtensions.WaitWhile(GameObject)` — запускает действие, когда условие станет ложным на объекте.
- `CoroutineExtensions.DelayFrames(GameObject)` — запускает действие через число кадров на объекте.
- `CoroutineExtensions.Delay` — запускает действие через задержку без контекста объекта.
- `CoroutineExtensions.WaitUntil` — запускает действие при выполнении условия без контекста объекта.
- `CoroutineExtensions.WaitWhile` — запускает действие при прекращении условия без контекста объекта.
- `CoroutineExtensions.DelayFrames` — запускает действие через число кадров без контекста объекта.
- `CoroutineExtensions.Start` — запускает корутину и возвращает `CoroutineHandle`.

#### `DebugGizmos.cs` — отладочная отрисовка Gizmos

- `DebugGizmos.DrawBounds` — рисует гизмо‑контуры `Bounds`.
- `DebugGizmos.DrawAveragePosition` — рисует точку среднего положения.
- `DebugGizmos.DrawLineToClosest` — рисует линию к ближайшему объекту.
- `DebugGizmos.DrawConnections` — рисует линии от точки к набору целей.

#### `EnumerableExtensions.cs` — удобные операции над коллекциями

- `EnumerableExtensions.ForEach` — выполняет действие для каждого элемента последовательности.
- `EnumerableExtensions.GetSafe` — безопасно возвращает элемент по индексу или значение по умолчанию.
- `EnumerableExtensions.GetWrapped` — возвращает элемент по индексу с циклическим обходом.
- `EnumerableExtensions.IsValidIndex` — проверяет допустимость индекса для коллекции.
- `EnumerableExtensions.ToIndexedString` — формирует строку с индексами элементов.
- `EnumerableExtensions.IsNullOrEmpty` — проверяет последовательность на null или пустоту.
- `EnumerableExtensions.ToStringJoined` — склеивает элементы в строку через разделитель.
- `EnumerableExtensions.FindDuplicates` — возвращает повторяющиеся элементы.
- `EnumerableExtensions.ToDebugString` — формирует отладочную строку списка.
- `EnumerableExtensions.CountEmptyElements` — подсчитывает пустые элементы массива.

#### `GameObjectArrayExtensions.cs` — пакетные операции для GameObject/Component

- `GameObjectArrayExtensions.SetActiveAll(IEnumerable<GameObject>)` — массово включает/выключает объекты.
- `GameObjectArrayExtensions.SetActiveAll(IEnumerable<T>)` — массово включает/выключает объекты по компонентам.
- `GameObjectArrayExtensions.SetActiveRange` — включает/выключает объекты до заданного индекса.
- `GameObjectArrayExtensions.SetActiveAtIndex(IList<GameObject>)` — включает/выключает объект по индексу списка.
- `GameObjectArrayExtensions.SetActiveAtIndex(IList<T>)` — включает/выключает объект по индексу списка компонентов.
- `GameObjectArrayExtensions.SetActiveAtIndex(IEnumerable<T>)` — включает/выключает объект по индексу перечисления
  компонентов.
- `GameObjectArrayExtensions.SetActiveAtIndex(IEnumerable<GameObject>)` — включает/выключает объект по индексу
  перечисления.
- `GameObjectArrayExtensions.DestroyAll(IEnumerable<GameObject>)` — уничтожает все объекты коллекции.
- `GameObjectArrayExtensions.DestroyAll(IEnumerable<T>)` — уничтожает объекты коллекции компонентов.
- `GameObjectArrayExtensions.GetActiveObjects` — возвращает только активные объекты.
- `GameObjectArrayExtensions.GetComponentsFromAll<T>` — собирает компоненты `T` из всех объектов.
- `GameObjectArrayExtensions.GetFirstComponentFromAll<T>` — возвращает первый найденный компонент `T`.
- `GameObjectArrayExtensions.SetPositionAll` — задаёт позицию всем объектам.
- `GameObjectArrayExtensions.FindClosest(IEnumerable<GameObject>)` — находит ближайший объект к позиции.
- `GameObjectArrayExtensions.FindClosest(IEnumerable<T>)` — находит ближайший компонент к позиции.
- `GameObjectArrayExtensions.WithinDistance(IEnumerable<GameObject>)` — фильтрует объекты по дистанции.
- `GameObjectArrayExtensions.WithinDistance(IEnumerable<T>)` — фильтрует компоненты по дистанции.
- `GameObjectArrayExtensions.SetParentAll(IEnumerable<GameObject>)` — массово задаёт родителя объектам.
- `GameObjectArrayExtensions.SetParentAll(IEnumerable<T>)` — массово задаёт родителя компонентам.
- `GameObjectArrayExtensions.GetAveragePosition(IEnumerable<GameObject>)` — вычисляет среднюю позицию объектов.
- `GameObjectArrayExtensions.GetAveragePosition(IEnumerable<T>)` — вычисляет среднюю позицию компонентов.
- `GameObjectArrayExtensions.GetCombinedBounds(IEnumerable<GameObject>)` — объединяет рендер‑bounds объектов.
- `GameObjectArrayExtensions.GetCombinedBounds(IEnumerable<T>)` — объединяет рендер‑bounds объектов компонентов.

#### `LayoutExtensions.cs` — раскладки трансформов в пространстве

- `LayoutExtensions.ArrangeInCircle(Transform)` — размещает объект по окружности с индексом.
- `LayoutExtensions.ArrangeInLine` — размещает элементы по линии.
- `LayoutExtensions.ArrangeInGrid` — размещает элементы по сетке.
- `LayoutExtensions.ArrangeInCircle(IEnumerable<Transform>)` — размещает элементы по окружности.
- `LayoutExtensions.ArrangeInCircle(Transform pivot)` — размещает элементы вокруг опорного трансформа.
- `LayoutExtensions.ArrangeInGrid3D` — размещает элементы по 3D‑сетке.
- `LayoutExtensions.ArrangeInCircle3D` — размещает элементы по окружности в 3D‑плоскости.
- `LayoutExtensions.ArrangeOnSphereSurface` — размещает элементы на поверхности сферы.
- `LayoutExtensions.ArrangeInSpiral` — размещает элементы по спирали.
- `LayoutExtensions.ArrangeOnSineWave` — размещает элементы по синусоиде.

#### `LayoutUtils.cs` — генераторы наборов позиций

- `LayoutUtils.GetLine` — генерирует позиции на линии.
- `LayoutUtils.GetGrid` — генерирует позиции на 2D‑сетке.
- `LayoutUtils.GetCircle` — генерирует позиции по окружности.
- `LayoutUtils.GetGrid3D` — генерирует позиции на 3D‑сетке.
- `LayoutUtils.GetCircle3D` — генерирует позиции по окружности в 3D‑плоскости.
- `LayoutUtils.GetSphereSurface` — генерирует позиции по поверхности сферы.
- `LayoutUtils.GetSpiral` — генерирует позиции по спирали.
- `LayoutUtils.GetSineWave` — генерирует позиции по синусоиде.

#### `ObjectExtensions.cs` — безопасность и утилиты для `UnityEngine.Object`

- `ObjectExtensions.SafeDestroy` — безопасно уничтожает объект с выбором `Destroy`/`DestroyImmediate`.
- `ObjectExtensions.IsValid` — проверяет объект на null и валидность Unity‑ссылки.
- `ObjectExtensions.GetName` — возвращает имя объекта с защитой от null.
- `ObjectExtensions.SetName` — задаёт имя объекта с защитой от null.

#### `PlayerPrefsUtils.cs` — сохранение массивов в `PlayerPrefs`

- `PlayerPrefsUtils.SetIntArray` — сохраняет массив int в `PlayerPrefs`.
- `PlayerPrefsUtils.GetIntArray` — читает массив int из `PlayerPrefs`.
- `PlayerPrefsUtils.SetFloatArray` — сохраняет массив float в `PlayerPrefs`.
- `PlayerPrefsUtils.GetFloatArray` — читает массив float из `PlayerPrefs`.
- `PlayerPrefsUtils.SetStringArray` — сохраняет массив string в `PlayerPrefs`.
- `PlayerPrefsUtils.GetStringArray` — читает массив string из `PlayerPrefs`.
- `PlayerPrefsUtils.SetBoolArray` — сохраняет массив bool в `PlayerPrefs`.
- `PlayerPrefsUtils.GetBoolArray` — читает массив bool из `PlayerPrefs`.

#### `DateTimeExtensions.cs` — сериализация и расчёты с DateTime (UTC)

- `ToRoundTripUtcString` — сохранение в UTC round-trip строку.
- `TryParseUtcRoundTrip` — парсинг с fallback на legacy-форматы.
- `GetSecondsSinceUtc`, `GetSecondsUntilUtc`, `EnsureUtc`.

#### `TimeParsingExtensions.cs` — парсинг длительностей из строк

- `TryParseDuration` — парсинг SS, MM:SS, HH:MM:SS, DD:HH:MM:SS.

#### `TimeSpanExtensions.cs` — форматирование TimeSpan

- `ToCompactString` — компактный вывод (2d 3h 15m).
- `ToClockString` — вывод в формате часов.

#### `PrimitiveExtensions.cs` — форматирование и нормализация примитивов

- `PrimitiveExtensions.ToInt(bool)` — конвертирует bool в 1/0.
- `PrimitiveExtensions.RoundToDecimal` — округляет float до заданных знаков.
- `PrimitiveExtensions.FormatTime` — форматирует секунды в строку по выбранному формату (в т.ч. trimLeadingZeros).
- `PrimitiveExtensions.FormatWithSeparator(float)` — форматирует число с разделителем и точностью.
- `PrimitiveExtensions.NormalizeToUnit` — нормализует значение в диапазон [0..1] по умолчанию.
- `PrimitiveExtensions.NormalizeToRange` — нормализует значение в диапазон [-1..1] по умолчанию.
- `PrimitiveExtensions.NormalizeToRange(float,min,max)` — нормализует значение в диапазон [-1..1] по заданным границам.
- `PrimitiveExtensions.NormalizeToUnit(float,min,max)` — нормализует значение в диапазон [0..1] по заданным границам (с
  проверкой на NaN/Infinity).
- `PrimitiveExtensions.Denormalize` — переводит [0..1] в заданный диапазон (с проверкой на NaN/Infinity).
- `PrimitiveExtensions.Remap` — переносит значение из одного диапазона в другой.
- `PrimitiveExtensions.ToBool(int)` — конвертирует int в bool (0=false).
- `PrimitiveExtensions.FormatWithSeparator(int)` — форматирует int с разделителем тысяч.

#### `NumberFormatExtensions.cs` — универсальное форматирование чисел для idle/UI

- `NumberNotation` — стили: `Plain`, `Grouped`, `IdleShort`, `Scientific`.
- `NumberRoundingMode` — режимы округления: `ToEven`, `AwayFromZero`, `ToZero`, `ToPositiveInfinity`,
  `ToNegativeInfinity`.
- `NumberFormatOptions` — конфиг форматирования (нотация, точность, округление, разделители, префикс/суффикс).
- `NumberFormatExtensions.ToPrettyString(...)` — универсальное форматирование для
  `int/long/float/double/decimal/BigInteger`.
- `NumberFormatExtensions.ToIdleString(...)` — быстрый idle-вывод с суффиксами.
- `NumberFormatExtensions.FormatNumber(...)` — базовый API форматтера для `decimal` и `BigInteger`.

#### `RandomExtensions.cs` — случайности и вероятности

- `RandomExtensions.GetRandomElement` — возвращает случайный элемент списка.
- `RandomExtensions.Shuffle` — перемешивает список на месте или создаёт копию.
- `RandomExtensions.GetRandomElements` — возвращает указанное число случайных элементов.
- `RandomExtensions.GetRandomIndex` — возвращает случайный индекс коллекции.
- `RandomExtensions.Chance` — возвращает true по вероятности.
- `RandomExtensions.Random(bool)` — случайно возвращает true/false, игнорируя вход.
- `RandomExtensions.RandomBool` — возвращает случайный bool.
- `RandomExtensions.RandomColor` — возвращает случайный цвет с заданной альфой.
- `RandomExtensions.GetRandomEnumValue` — возвращает случайное значение enum (с кешированием enum-значений).
- `RandomExtensions.GetRandomWeightedIndex` — выбирает индекс по весам (с валидацией отрицательных и нулевой суммы
  весов).
- `RandomExtensions.RandomizeBetween(float)` — возвращает значение в диапазоне вокруг числа.
- `RandomExtensions.RandomizeBetween(int)` — возвращает значение в диапазоне вокруг числа.
- `RandomExtensions.RandomFromValue(float)` — возвращает случайное значение от заданного старта до исходного.
- `RandomExtensions.RandomFromValue(int)` — возвращает случайное значение от заданного старта до исходного.
- `RandomExtensions.RandomToValue(float)` — возвращает случайное значение от исходного до заданного конца.
- `RandomExtensions.RandomToValue(int)` — возвращает случайное значение от исходного до заданного конца.
- `RandomExtensions.RandomRange(Vector2)` — возвращает случайное число в диапазоне вектора.
- `RandomExtensions.RandomRange(Vector2Int)` — возвращает случайное число в диапазоне целочисленного вектора.

#### `RandomShapeExtensions.cs` — генерация случайных точек в формах

- `RandomShapeExtensions.RandomPointInBounds(Bounds)` — возвращает случайную точку внутри `Bounds`.
- `RandomShapeExtensions.RandomPointOnBounds(Bounds)` — возвращает случайную точку на границе `Bounds`.
- `RandomShapeExtensions.RandomPointInCircle` — возвращает случайную точку внутри круга.
- `RandomShapeExtensions.RandomPointOnCircle` — возвращает случайную точку на окружности.
- `RandomShapeExtensions.RandomPointInSphere` — возвращает случайную точку внутри сферы.
- `RandomShapeExtensions.RandomPointOnSphere` — возвращает случайную точку на сфере.
- `RandomShapeExtensions.RandomPointInBounds(Collider)` — возвращает случайную точку внутри 3D‑коллайдера.
- `RandomShapeExtensions.RandomPointInBounds(Collider2D)` — возвращает случайную точку внутри 2D‑коллайдера.

#### `ScreenExtensions.cs` — проверка видимости и границ экрана

- `ScreenExtensions.IsOnScreen` — проверяет, находится ли позиция в пределах экрана.
- `ScreenExtensions.IsOutOfScreen` — проверяет, находится ли позиция вне экрана.
- `ScreenExtensions.IsOutOfScreenSide` — проверяет, находится ли позиция за конкретной стороной экрана.
- `ScreenExtensions.GetClosestScreenEdgePoint` — возвращает ближайшую точку на границе экрана.
- `ScreenExtensions.GetWorldPositionAtScreenEdge` — возвращает мировую позицию на краю экрана.
- `ScreenExtensions.GetWorldScreenBounds` — возвращает мировые границы экрана на заданной дистанции.

#### `StringExtension.cs` — преобразование и стилизация строк

- `StringExtension.SplitCamelCase` — разбивает CamelCase на слова.
- `StringExtension.IsNullOrEmptyAfterTrim` — проверяет строку на пустоту после `Trim`.
- `StringExtension.ToColor` — парсит HEX‑строку в `Color`.
- `StringExtension.ToColorSafe` — безопасный парсинг HEX‑строки в `Color` с `bool` результатом.
- `StringExtension.ToCamelCase` — переводит строку в camelCase.
- `StringExtension.Truncate` — обрезает строку до длины.
- `StringExtension.IsNumeric` — проверяет строку на число.
- `StringExtension.RandomString` — генерирует случайную строку.
- `StringExtension.Reverse` — разворачивает строку.
- `StringExtension.ToBool` — парсит строку в bool.
- `StringExtension.ToInt` — парсит строку в int с значением по умолчанию.
- `StringExtension.ToFloat` — парсит строку в float с значением по умолчанию.
- `StringExtension.Bold` — оборачивает строку в тег жирного текста.
- `StringExtension.Italic` — оборачивает строку в тег курсива.
- `StringExtension.Size` — оборачивает строку в тег размера.
- `StringExtension.SetColor` — оборачивает строку в цвет.
- `StringExtension.Rainbow` — оборачивает строку в радужный градиент.
- `StringExtension.Gradient` — оборачивает строку в градиент между двумя цветами.
- `StringExtension.RandomColors` — оборачивает строку в случайные цвета.

#### `TransformExtensions.cs` — позиция, ротация и масштаб `Transform`

- `TransformExtensions.SetPosition` — задаёт мировую позицию с выборочной заменой осей.
- `TransformExtensions.AddPosition` — добавляет смещение к мировой позиции.
- `TransformExtensions.SetLocalPosition` — задаёт локальную позицию с выборочной заменой осей.
- `TransformExtensions.AddLocalPosition` — добавляет смещение к локальной позиции.
- `TransformExtensions.SetRotation` — задаёт мировую ротацию через Quaternion или euler.
- `TransformExtensions.AddRotation` — добавляет мировое вращение через Quaternion или euler.
- `TransformExtensions.SetLocalRotation` — задаёт локальную ротацию через Quaternion или euler.
- `TransformExtensions.AddLocalRotation` — добавляет локальное вращение через Quaternion или euler.
- `TransformExtensions.SetScale` — задаёт локальный масштаб с выборочной заменой осей.
- `TransformExtensions.AddScale` — добавляет смещение к локальному масштабу.
- `TransformExtensions.LookAt2D` — поворачивает объект к цели в 2D.
- `TransformExtensions.SmoothLookAtRoutine` — плавно поворачивает объект к цели в корутине.
- `TransformExtensions.GetClosest` — возвращает ближайший `Transform` из коллекции.
- `TransformExtensions.GetChildTransforms` — возвращает массив непосредственных детей.
- `TransformExtensions.ResetTransform` — сбрасывает мировую позицию/ротацию/масштаб.
- `TransformExtensions.ResetLocalTransform` — сбрасывает локальную позицию/ротацию/масштаб.
- `TransformExtensions.CopyFrom` — копирует позицию/ротацию/масштаб из источника.
- `TransformExtensions.DestroyChildren` — уничтожает всех детей.

#### `UIUtils.cs` — утилиты для UI и Canvas

- `UIUtils.GetUIElementsUnderCursor` — возвращает UI‑элементы под курсором.
- `UIUtils.IsPointerOverUI` — проверяет, находится ли курсор над UI.
- `UIUtils.WorldToCanvasPoint` — переводит мировую позицию в позицию Canvas.

#### `Shapes.cs` — базовые структуры геометрии

- Файл содержит структуры `Circle` и `Sphere` и не имеет публичных методов.

#### `Enums.cs` — общие enum’ы для разных модулей

- Файл содержит общие enum’ы и не имеет публичных методов.

### Condition (`Assets/Neoxider/Scripts/Condition/`)

- `Assets/Neoxider/Scripts/Condition/NeoCondition.cs` — No-Code система условий: проверяет поля/свойства любых
  компонентов и GameObject'ов через Inspector. AND/OR логика, события OnTrue/OnFalse/OnResult/OnInvertedResult.
  Безопасная обработка уничтоженных объектов.
- `Assets/Neoxider/Scripts/Condition/ConditionEntry.cs` — одно условие: SourceMode (Component/GameObject), опциональный
  поиск по имени (Find By Name) с Wait For Object и Prefab Preview, ссылка на GameObject → Component/GO → поле, оператор
  сравнения, порог, инверсия. Двухуровневый кеш (reflection + Find).

#### Demo Condition (`Assets/Neoxider/Demo/Scripts/Condition/`)

- `Assets/Neoxider/Demo/Scripts/Condition/ConditionDemoUI.cs` — UI контроллер демо-сцены (панели, статус, warning).
- `Assets/Neoxider/Demo/Scripts/Condition/ConditionDemoSetup.cs` — создание демо-сцены NeoCondition в Edit Mode (
  использует Health и ScoreManager).
- `Assets/Neoxider/Demo/Scripts/Condition/HealthTextDisplay.cs` — отображение HP в TMP_Text через Health.OnChange.

### GridSystem (`Assets/Neoxider/Scripts/GridSystem/`)

- `Assets/Neoxider/Scripts/GridSystem/FieldCell.cs` — ячейка сетки.
- `Assets/Neoxider/Scripts/GridSystem/FieldDebugDrawer.cs` — debug отрисовка.
- `Assets/Neoxider/Scripts/GridSystem/FieldGenerator.cs` — генерация поля.
- `Assets/Neoxider/Scripts/GridSystem/FieldGeneratorConfig.cs` — конфиг генерации.
- `Assets/Neoxider/Scripts/GridSystem/FieldObjectSpawner.cs` — спавн объектов на поле.
- `Assets/Neoxider/Scripts/GridSystem/FieldSpawner.cs` — спавнер.
- `Assets/Neoxider/Scripts/GridSystem/GridPathfinder.cs` — сервис pathfinding (`GridPathRequest`, `GridPathResult`,
  `NoPathReason`).
- `Assets/Neoxider/Scripts/GridSystem/GridShapeMask.cs` — ScriptableObject-маска формы поля.
- `Assets/Neoxider/Scripts/GridSystem/MovementRule.cs` — правила перемещения.
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3TileState.cs` — состояния тайлов Match3.
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3MatchFinder.cs` — поиск комбинаций Match3.
- `Assets/Neoxider/Scripts/GridSystem/Match3/Match3BoardService.cs` — логика board-сервиса Match3 (swap/resolve/refill).
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeCellState.cs` — состояния клетки для крестиков-ноликов.
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeWinChecker.cs` — проверка победителя TicTacToe.
- `Assets/Neoxider/Scripts/GridSystem/TicTacToe/TicTacToeBoardService.cs` — board-сервис TicTacToe (
  ходы/победа/ничья/reset).

#### Demo GridSystem (`Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/`)

- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemMatch3DemoSetup.cs` — setup demo-сцены Match3 в Edit Mode.
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemMatch3DemoUI.cs` — UI-контроллер demo Match3.
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemMatch3BoardView.cs` — runtime-визуализация интерактивного
  поля Match3.
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemTicTacToeDemoSetup.cs` — setup demo-сцены TicTacToe в Edit
  Mode.
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemTicTacToeDemoUI.cs` — UI-контроллер demo TicTacToe.
- `Assets/Neoxider/~Samples/Demo/Scripts/GridSystem/GridSystemTicTacToeBoardView.cs` — runtime-визуализация
  кликабельного поля TicTacToe.

### Level (`Assets/Neoxider/Scripts/Level/`)

- `Assets/Neoxider/Scripts/Level/LevelButton.cs` — кнопка уровня.
- `Assets/Neoxider/Scripts/Level/LevelManager.cs` — менеджер уровней.
- `Assets/Neoxider/Scripts/Level/Map.cs` — карта уровней.
- `Assets/Neoxider/Scripts/Level/TextLevel.cs` — UI вывод текущего/максимального уровня (на базе `Neo.Tools.SetText`).

### NPC (`Assets/Neoxider/Scripts/NPC/`)

- `Assets/Neoxider/Scripts/NPC/NpcNavigation.cs` — host компонент модульной навигации.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcAggroFollowCore.cs` — core агро/преследование.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcAnimationCore.cs` — core анимации.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcDestinationResolver.cs` — резолв точки назначения.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcFollowTargetCore.cs` — core следования.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcNavAgentCore.cs` — core NavMeshAgent.
- `Assets/Neoxider/Scripts/NPC/Navigation/NpcPatrolCore.cs` — core патруля.

### Parallax (`Assets/Neoxider/Scripts/Parallax/`)

- `Assets/Neoxider/Scripts/Parallax/ParallaxLayer.cs` — слой параллакса.

### Save (`Assets/Neoxider/Scripts/Save/`)

- `Assets/Neoxider/Scripts/Save/ISaveableComponent.cs` — интерфейс сохраняемого компонента.
- `Assets/Neoxider/Scripts/Save/SaveableBehaviour.cs` — базовый MonoBehaviour для сохранений.
- `Assets/Neoxider/Scripts/Save/SaveField.cs` — атрибут автосохранения полей.
- `Assets/Neoxider/Scripts/Save/SaveManager.cs` — менеджер сохранений.
- `Assets/Neoxider/Scripts/Save/SaveProvider.cs` — единый API сохранений (провайдеры).
- `Assets/Neoxider/Scripts/Save/SaveProviderExtensions.cs` — расширения SaveProvider.
- `Assets/Neoxider/Scripts/Save/Example/PlayerData.cs` — пример данных.
- `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalData.cs` — контейнер глобальных данных.
- `Assets/Neoxider/Scripts/Save/GlobalSave/GlobalSave.cs` — глобальное хранилище.
- `Assets/Neoxider/Scripts/Save/Providers/ISaveProvider.cs` — интерфейс провайдера.
- `Assets/Neoxider/Scripts/Save/Providers/PlayerPrefsSaveProvider.cs` — провайдер PlayerPrefs.
- `Assets/Neoxider/Scripts/Save/Providers/FileSaveProvider.cs` — провайдер JSON файлов.
- `Assets/Neoxider/Scripts/Save/Providers/SaveProviderType.cs` — enum типов провайдера.
- `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettings.cs` — настройки (SO).
- `Assets/Neoxider/Scripts/Save/Settings/SaveProviderSettingsComponent.cs` — обертка настроек (MB).

### Shop (`Assets/Neoxider/Scripts/Shop/`)

- `Assets/Neoxider/Scripts/Shop/ButtonPrice.cs` — UI кнопка цены.
- `Assets/Neoxider/Scripts/Shop/InterfaceMoney.cs` — интерфейс валюты.
- `Assets/Neoxider/Scripts/Shop/Money.cs` — система валюты.
- `Assets/Neoxider/Scripts/Shop/Shop.cs` — контроллер магазина.
- `Assets/Neoxider/Scripts/Shop/ShopItem.cs` — элемент магазина.
- `Assets/Neoxider/Scripts/Shop/ShopItemData.cs` — данные товара.
- `Assets/Neoxider/Scripts/Shop/TextMoney.cs` — UI отображение денег.

### StateMachine (`Assets/Neoxider/Scripts/StateMachine/`)

- `Assets/Neoxider/Scripts/StateMachine/IState.cs` — интерфейс состояния.
- `Assets/Neoxider/Scripts/StateMachine/StateCondition.cs` — базовые условия.
- `Assets/Neoxider/Scripts/StateMachine/StateMachine.cs` — core state machine.
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviour.cs` — MonoBehaviour обертка.
- `Assets/Neoxider/Scripts/StateMachine/StateMachineBehaviourBase.cs` — базовый behaviour.
- `Assets/Neoxider/Scripts/StateMachine/StatePredicate.cs` — предикаты переходов.
- `Assets/Neoxider/Scripts/StateMachine/StateTransition.cs` — переход.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateAction.cs` — no-code action.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateData.cs` — no-code state (данные).
- `Assets/Neoxider/Scripts/StateMachine/NoCode/StateMachineData.cs` — no-code machine (данные).
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditor.cs` — editor.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/StateMachineEditorRegistrar.cs` — регистрация editor.
- `Assets/Neoxider/Scripts/StateMachine/NoCode/Editor/TransitionEditorWindow.cs` — окно редактора переходов.

### Tools (`Assets/Neoxider/Scripts/Tools/`)

#### Tools/Time

- `Assets/Neoxider/Scripts/Tools/Time/Timer.cs` — таймер (класс). Play, SetRemainingTime, SetProgress.
- `Assets/Neoxider/Scripts/Tools/Time/TimerObject.cs` — MonoBehaviour таймер (события/режимы). SetDuration.

#### Tools/View

- `Assets/Neoxider/Scripts/Tools/View/BillboardUniversal.cs` — билборд.
- `Assets/Neoxider/Scripts/Tools/View/DOTweenUIImageFallback.cs` — fallback для DOTween UI.
- `Assets/Neoxider/Scripts/Tools/View/ImageFillAmountAnimator.cs` — аниматор fillAmount.
- `Assets/Neoxider/Scripts/Tools/View/LightAnimator.cs` — аниматор света.
- `Assets/Neoxider/Scripts/Tools/View/MeshEmission.cs` — emission меша.
- `Assets/Neoxider/Scripts/Tools/View/Selector.cs` — селектор объектов/индексов.
- `Assets/Neoxider/Scripts/Tools/View/StarView.cs` — звездный виджет.
- `Assets/Neoxider/Scripts/Tools/View/ZPositionAdjuster.cs` — корректировка Z.

#### Tools/Text

- `Assets/Neoxider/Scripts/Tools/Text/SetText.cs` — установка текста с анимацией и форматированием (`NumberNotation`,
  `NumberRoundingMode`, `SetBigInteger`, `SetFormatted`).
- `Assets/Neoxider/Scripts/Tools/Text/TimeToText.cs` — вывод времени в текст. TrySetFromString, AllowNegative.

#### Tools/Physics

- `Assets/Neoxider/Scripts/Tools/Physics/ExplosiveForce.cs` — взрывная сила.
- `Assets/Neoxider/Scripts/Tools/Physics/ImpulseZone.cs` — зона импульса.
- `Assets/Neoxider/Scripts/Tools/Physics/MagneticField.cs` — магнитное поле.

#### Tools/Spawner

- `Assets/Neoxider/Scripts/Tools/Spawner/IPoolable.cs` — интерфейс пула.
- `Assets/Neoxider/Scripts/Tools/Spawner/NeoObjectPool.cs` — пул объектов.
- `Assets/Neoxider/Scripts/Tools/Spawner/PoolManager.cs` — менеджер пулов.
- `Assets/Neoxider/Scripts/Tools/Spawner/PooledObjectInfo.cs` — инфо объекта пула.
- `Assets/Neoxider/Scripts/Tools/Spawner/SimpleSpawner.cs` — простой спавнер.
- `Assets/Neoxider/Scripts/Tools/Spawner/Spawner.cs` — спавнер.

#### Tools/Move

- `Assets/Neoxider/Scripts/Tools/Move/AdvancedForceApplier.cs` — apply force helper.
- `Assets/Neoxider/Scripts/Tools/Move/CameraConstraint.cs` — ограничения камеры.
- `Assets/Neoxider/Scripts/Tools/Move/CameraRotationController.cs` — вращение камеры.
- `Assets/Neoxider/Scripts/Tools/Move/DistanceChecker.cs` — проверка дистанции.
- `Assets/Neoxider/Scripts/Tools/Move/Follow.cs` — следование.
- `Assets/Neoxider/Scripts/Tools/Move/ScreenPositioner.cs` — позиционирование.
- `Assets/Neoxider/Scripts/Tools/Move/UniversalRotator.cs` — вращение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantMover.cs` — постоянное движение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/ConstantRotator.cs` — постоянное вращение.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/DirectionUtils.cs` — утилиты направлений.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/IMover.cs` — интерфейс.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/KeyboardMover.cs` — движение с клавиатуры.
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover2D.cs` — движение за мышью (2D).
- `Assets/Neoxider/Scripts/Tools/Move/MovementToolkit/MouseMover3D.cs` — движение за мышью (3D).

#### Tools/Input

- `Assets/Neoxider/Scripts/Tools/Input/MultiKeyEventTrigger.cs` — хоткеи/комбинации.
- `Assets/Neoxider/Scripts/Tools/Input/MouseEffect.cs` — эффект мыши.
- `Assets/Neoxider/Scripts/Tools/Input/MouseInputManager.cs` — ввод мыши.
- `Assets/Neoxider/Scripts/Tools/Input/SwipeController.cs` — свайпы.

#### Tools/Managers

- `Assets/Neoxider/Scripts/Tools/Managers/Bootstrap.cs` — bootstrap.
- `Assets/Neoxider/Scripts/Tools/Managers/EM.cs` — event manager.
- `Assets/Neoxider/Scripts/Tools/Managers/GM.cs` — game manager.
- `Assets/Neoxider/Scripts/Tools/Managers/Singleton.cs` — singleton base.

#### Tools/Components

- `Assets/Neoxider/Scripts/Tools/Components/Counter.cs` — универсальный счётчик (Int/Float),
  Add/Subtract/Multiply/Divide/Set, Send по Payload; события по типу; опциональное сохранение по ключу.
- `Assets/Neoxider/Scripts/Tools/Components/Loot.cs` — лут.
- `Assets/Neoxider/Scripts/Tools/Components/ScoreManager.cs` — очки/звезды.
- `Assets/Neoxider/Scripts/Tools/Components/TextScore.cs` — UI вывод текущего/лучшего счета (на базе
  `Neo.Tools.SetText`).
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffect.cs` — печать текста.
- `Assets/Neoxider/Scripts/Tools/Components/TypewriterEffectComponent.cs` — обертка печати текста.
- `Assets/Neoxider/Scripts/Tools/Components/Interface/InterfaceAttack.cs` — интерфейс атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AdvancedAttackCollider.cs` — коллайдер атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/AttackExecution.cs` — выполнение атаки.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Evade.cs` — уклонение.
- `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Health.cs` — здоровье.

#### Tools/Dialogue

- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueController.cs` — контроллер диалога.
- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueData.cs` — данные диалога.
- `Assets/Neoxider/Scripts/Tools/Dialogue/DialogueUI.cs` — UI диалога.

#### Tools/Draw

- `Assets/Neoxider/Scripts/Tools/Draw/Drawer.cs` — рисование линий.

#### Tools/FakeLeaderboard

- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/Leaderboard.cs` — лидерборд.
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardItem.cs` — элемент лидерборда.
- `Assets/Neoxider/Scripts/Tools/FakeLeaderboard/LeaderboardMove.cs` — анимация перемещения.

#### Tools/Random

- `Assets/Neoxider/Scripts/Tools/Random/ChanceManager.cs` — вероятности.
- `Assets/Neoxider/Scripts/Tools/Random/ChanceSystemBehaviour.cs` — вероятности (MB).
- `Assets/Neoxider/Scripts/Tools/Random/Data/ChanceData.cs` — данные вероятностей.

#### Tools/InteractableObject

- `Assets/Neoxider/Scripts/Tools/InteractableObject/InteractiveObject.cs` — базовое взаимодействие.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents2D.cs` — события 2D.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/PhysicsEvents3D.cs` — события 3D.
- `Assets/Neoxider/Scripts/Tools/InteractableObject/ToggleObject.cs` — toggle.

#### Tools/Other

- `Assets/Neoxider/Scripts/Tools/Other/AiNavigation.cs` — навигация ИИ (legacy).
- `Assets/Neoxider/Scripts/Tools/Other/CameraShake.cs` — тряска камеры.
- `Assets/Neoxider/Scripts/Tools/Other/RevertAmount.cs` — revert helper.
- `Assets/Neoxider/Scripts/Tools/Other/SpineController.cs` — фасад Spine (опционально).

#### Tools/Debug

- `Assets/Neoxider/Scripts/Tools/Debug/ErrorLogger.cs` — логирование ошибок.
- `Assets/Neoxider/Scripts/Tools/Debug/FPS.cs` — FPS.

#### Tools/Misc

- `Assets/Neoxider/Scripts/Tools/CameraAspectRatioScaler.cs` — масштаб под aspect.
- `Assets/Neoxider/Scripts/Tools/UpdateChilds.cs` — утилита для детей.

### UI (`Assets/Neoxider/Scripts/UI/`)

- `Assets/Neoxider/Scripts/UI/AnchorMove.cs` — движение UI.
- `Assets/Neoxider/Scripts/UI/AnimationFly.cs` — UI fly анимация.
- `Assets/Neoxider/Scripts/UI/PausePage.cs` — пауза.
- `Assets/Neoxider/Scripts/UI/UIReady.cs` — готовность UI.
- `Assets/Neoxider/Scripts/UI/Animation/ButtonScale.cs` — scale кнопки.
- `Assets/Neoxider/Scripts/UI/Animation/ButtonShake.cs` — shake кнопки.
- `Assets/Neoxider/Scripts/UI/Simple/ButtonChangePage.cs` — смена страниц.
- `Assets/Neoxider/Scripts/UI/Simple/FakeLoad.cs` — fake load.
- `Assets/Neoxider/Scripts/UI/Simple/UI.cs` — UI менеджер.
- `Assets/Neoxider/Scripts/UI/View/Points.cs` — points индикатор.
- `Assets/Neoxider/Scripts/UI/View/VariantView.cs` — варианты.
- `Assets/Neoxider/Scripts/UI/View/VisualToggle.cs` — визуальный toggle.

### PropertyAttribute (`Assets/Neoxider/Scripts/PropertyAttribute/`)

- `Assets/Neoxider/Scripts/PropertyAttribute/ButtonAttribute.cs` — атрибут кнопки.
- `Assets/Neoxider/Scripts/PropertyAttribute/ButtonAttributeDrawer.cs` — drawer кнопки (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttribute.cs` — атрибут цвета.
- `Assets/Neoxider/Scripts/PropertyAttribute/GUIColorAttributeDrawer.cs` — drawer цвета (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterface.cs` — атрибут интерфейса.
- `Assets/Neoxider/Scripts/PropertyAttribute/RequireInterfaceDrawer.cs` — drawer интерфейса (Editor).
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/FindAllInSceneAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/FindInSceneAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/GetComponentsAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadAllFromResourcesAttribute.cs` — inject.
- `Assets/Neoxider/Scripts/PropertyAttribute/InjectAttribute/LoadFromResourcesAttribute.cs` — inject.

### Editor (`Assets/Neoxider/Editor/` + `**/Editor/**`)

- `Assets/Neoxider/Editor/AutoBuildName.cs` — авто-именование билдов.
- `Assets/Neoxider/Editor/FindAndRemoveMissingScriptsWindow.cs` — окно missing scripts.
- `Assets/Neoxider/Editor/GUI/EditorWindowGUI.cs` — GUI helpers.
- `Assets/Neoxider/Editor/GUI/FindAndRemoveMissingScriptsWindowGUI.cs` — GUI окна missing scripts.
- `Assets/Neoxider/Editor/GUI/NeoxiderSettingsWindowGUI.cs` — GUI окна настроек.
- `Assets/Neoxider/Editor/GUI/SceneSaverGUI.cs` — GUI SceneSaver.
- `Assets/Neoxider/Editor/GUI/TextureMaxSizeChangerGUI.cs` — GUI TextureMaxSizeChanger.
- `Assets/Neoxider/Editor/Main/CreateSceneHierarchy.cs` — настройка иерархии сцены.
- `Assets/Neoxider/Editor/Main/NeoxiderSettings.cs` — настройки Neoxider.
- `Assets/Neoxider/Editor/Main/NeoxiderSettingsWindow.cs` — окно настроек (Main).
- `Assets/Neoxider/Editor/Scene/SceneSaver.cs` — автосохранение сцен.
- `Assets/Neoxider/Editor/TextureMaxSizeChanger.cs` — массовое изменение текстур.
- `Assets/Neoxider/Editor/SaveProjectZip.cs` — zip проекта.
- `Assets/Neoxider/Editor/Create/CreateMenuObject.cs` — Create menu helpers.
- `Assets/Neoxider/Editor/Create/SingletonCreator.cs` — создание singleton.
- `Assets/Neoxider/Editor/PropertyAttribute/ComponentDrawer.cs` — drawer компонентов.
- `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorBase.cs` — база кастом-инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/CustomEditorSettings.cs` — настройки кастом-инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/GradientButtonSettings.cs` — настройки градиентной кнопки.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoCustomEditor.cs` — кастом-инспектор система.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoCustomEditorRegistrar.cs` — регистрация кастом-инспекторов.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoEditorAsmdefFixer.cs` — фиксы asmdef.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoEditorAutoRegister.cs` — авто-регистрация.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoInspectorSettings.cs` — настройки инспектора.
- `Assets/Neoxider/Editor/PropertyAttribute/NeoUpdateChecker.cs` — проверка обновлений через GitHub API (авто 10 мин,
  ручная кнопка с кулдауном 10 сек, обработка rate limit 403, фоллбек поиска package.json).
- `Assets/Neoxider/Editor/PropertyAttribute/NeoxiderSettingsWindow.cs` — окно настроек (PropertyAttribute).
- `Assets/Neoxider/Editor/PropertyAttribute/ResourceDrawer.cs` — drawer ресурсов.
- `Assets/Neoxider/Editor/Tools/Physics/MagneticFieldEditor.cs` — scene handle для MagneticField.
- `Assets/Neoxider/UI Extension/Editor/CreateMenuObject.cs` — Create menu helpers (UI Extension).
