# Случайность (Random)

**Что это:** инструменты для случайных событий и шансов: луты, вероятностные исходы, рулетки. Компонент ChanceSystemBehaviour, ChanceData, ChanceManager. Настройка и подписка на события в инспекторе или из кода.

**Оглавление:** ChanceSystemBehaviour, ChanceManager, ChanceData (Data/) — см. таблицу и ссылки ниже.

---

## Примеры

- **Демо-сцена:** `Samples/Demo/Scenes/Tools/ChanceSystemExample.unity` (импорт через Package Manager → Neoxider Tools → Samples → Demo Scenes).
- **Инспектор:** кнопка «Крутить» → **ChanceSystemBehaviour.GenerateVoid**; в **Events By Index** по одному действию на каждый исход. Подробно: [ChanceSystemBehaviour — примеры](./ChanceSystemBehaviour.md#примеры).
- **Код:** `GenerateId()` / `EvaluateAndNotify()`, `LastSelectedIndex`, `LastSelectedEntry`, подписка на `OnIndexSelected` / `OnRollComplete`. Сниппеты: [ChanceSystemBehaviour — примеры](./ChanceSystemBehaviour.md#примеры).

## Компоненты и данные

| Элемент | Назначение |
|--------|------------|
| **ChanceSystemBehaviour** | Компонент на GameObject: настройка шансов в инспекторе, события по индексу (On Id Generated, Events By Index, On Roll Complete, On Index And Weight Selected), вызов `GenerateId()` / `GenerateVoid()` из кнопок или кода. |
| **ChanceData** | ScriptableObject с конфигурацией шансов; можно переиспользовать в сценах и подставлять в ChanceSystemBehaviour. |
| **ChanceManager** | Класс весов и нормализации; используется внутри ChanceData и ChanceSystemBehaviour, доступен и для чистого C#. |

## Документация

- [ChanceManager](./ChanceManager.md) — API весов, нормализация, TryEvaluate.
- [ChanceSystemBehaviour](./ChanceSystemBehaviour.md) — события No-Code, **примеры (No-Code и код)**, Events By Index, быстрый старт.
- [Data](./Data) — ChanceData.
