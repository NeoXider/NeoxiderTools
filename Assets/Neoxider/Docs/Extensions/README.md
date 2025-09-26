# Модуль Extensions

Модуль **Extensions** — это огромная библиотека методов-расширений и утилит, предназначенная для значительного упрощения и ускорения написания кода в Unity. Он добавляет новый функционал к существующим классам, таким как `Transform`, `GameObject`, `string`, `AudioSource`, а также к различным коллекциям и примитивным типам.

## Документация по скриптам

### Базовые расширения
- [**ObjectExtensions**](./ObjectExtensions.md): Безопасное уничтожение и проверка объектов.
- [**ComponentExtensions**](./ComponentExtensions.md): `GetOrAdd` компонент и получение пути в иерархии.
- [**TransformExtensions**](./TransformExtensions.md): Манипуляция позицией, вращением и масштабом.
- [**GameObjectArrayExtensions**](./GameObjectArrayExtensions.md): Массовые операции над коллекциями `GameObject`'ов.

### Коллекции и типы данных
- [**EnumerableExtensions**](./EnumerableExtensions.md): Утилиты для `IEnumerable` и `IList` (`ForEach`, `GetSafe` и др.).
- [**PrimitiveExtensions**](./PrimitiveExtensions.md): Форматирование и конвертация для `float`, `int`, `bool`.
- [**StringExtension**](./StringExtension.md): Парсинг, форматирование и Rich Text для `string`.
- [**ColorExtension**](./ColorExtension.md): Манипуляция цветом (`WithAlpha`, `Darken`, `Lighten`).

### Рандомизация
- [**RandomExtensions**](./RandomExtensions.md): Получение случайных элементов, перемешивание, взвешенный шанс.
- [**RandomShapeExtensions**](./RandomShapeExtensions.md): Случайные точки внутри и на поверхности фигур.
- [**Shapes**](./Shapes.md): Определения структур `Circle` и `Sphere`.

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