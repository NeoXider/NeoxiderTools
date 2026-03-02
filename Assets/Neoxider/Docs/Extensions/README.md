# Модуль Extensions

**Что это:** библиотека extension-методов и утилит для C# и Unity API (Transform, GameObject, string, коллекции, TimeSpan, UnityEvent и др.). Скрипты в `Scripts/Extensions/`.

**Навигация:** [← К Docs](../README.md) · оглавление — список ниже

### Базовые расширения
- [**ObjectExtensions**](./ObjectExtensions.md): Безопасное уничтожение и проверка объектов.
- [**ComponentExtensions**](./ComponentExtensions.md): `GetOrAdd` компонент и получение пути в иерархии.
- [**TransformExtensions**](./TransformExtensions.md): Манипуляция позицией, вращением и масштабом.
- [**GameObjectArrayExtensions**](./GameObjectArrayExtensions.md): Массовые операции над коллекциями `GameObject`'ов.
- [**PrefabPreviewExtensions**](./PrefabPreviewExtensions.md): Получение preview `Texture2D`/`Sprite` для префабов.

### Коллекции и типы данных
- [**EnumerableExtensions**](./EnumerableExtensions.md): Утилиты для `IEnumerable` и `IList` (`ForEach`, `GetSafe` и др.).
- [**PrimitiveExtensions**](./PrimitiveExtensions.md): Форматирование и конвертация для `float`, `int`, `bool`.
- [**DateTimeExtensions**](./DateTimeExtensions.md): Сериализация UTC, парсинг round-trip, `GetSecondsSinceUtc`/`GetSecondsUntilUtc`.
- [**TimeParsingExtensions**](./TimeParsingExtensions.md): Парсинг длительностей из строк (SS, MM:SS, HH:MM:SS, DD:HH:MM:SS).
- [**TimeSpanExtensions**](./TimeSpanExtensions.md): `ToCompactString`, `ToClockString` для `TimeSpan`.
- [**CooldownRewardExtensions**](./CooldownRewardExtensions.md): расчёт накопленных наград по кулдауну, `CapToMaxPerTake`, `AdvanceLastClaimTime`.
- [**StringExtension**](./StringExtension.md): Парсинг, форматирование и Rich Text для `string`.
- [**ColorExtension**](./ColorExtension.md): Манипуляция цветом (`WithAlpha`, `Darken`, `Lighten`).

### Рандомизация
- [**RandomExtensions**](./RandomExtensions.md): Получение случайных элементов, перемешивание, взвешенный шанс.
- [**RandomShapeExtensions**](./RandomShapeExtensions.md): Случайные точки внутри и на поверхности фигур.
- [**Shapes**](./Shapes.md): Определения структур `Circle` и `Sphere`.

### События (UnityEvent)
- [**UnityEventDelegateCache**](./UnityEventDelegateCache.md): кэш делегатов для корректной отписки от UnityEvent при динамических подписках по индексу (кнопки, элементы списка). Используется в Shop.

### Системные утилиты
- [**CoroutineExtensions**](./CoroutineExtensions.md): Улучшенная система для запуска и контроля корутин.
- [**PlayerPrefsUtils**](./PlayerPrefsUtils.md): Сохранение и загрузка массивов в `PlayerPrefs`.
- [**ScreenExtensions**](./ScreenExtensions.md): Проверка видимости на экране, получение координат краев.
- [**UIUtils**](./UIUtils.md): Проверка, находится ли курсор над UI.
- [**AudioExtensions**](./AudioExtensions.md): Плавное затухание и нарастание громкости для `AudioSource`.
- [**DebugGizmos**](./DebugGizmos.md): Утилиты для отрисовки отладочных гизмо.

### Размещение объектов (Layouting)
- [**LayoutUtils**](./LayoutUtils.md): Расчет позиций для размещения объектов (линия, сетка, круг).
- [**LayoutExtensions**](./LayoutExtensions.md): Применение рассчитанных позиций к `Transform`'ам.

### Перечисления
- [**Enums**](./Enums.md): Определения `enum`'ов, используемых в других расширениях.