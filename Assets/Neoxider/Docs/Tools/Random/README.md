# Случайность (Random)

Инструменты для работы со случайными событиями и шансами: луты, вероятностные исходы, рулетки. Удобны и в коде, и без кода — через настройку и подписку на события в инспекторе.

## Примеры

- **Демо-сцена:** `Samples/Demo/Scenes/Tools/ChanceSystemExample.unity` (импорт через Package Manager → Neoxider Tools → Samples → Demo Scenes).
- **No-Code:** кнопка «Крутить» → **ChanceSystemBehaviour.GenerateVoid**; в **Events By Index** по одному действию на каждый исход — без скриптов. Подробно: [ChanceSystemBehaviour — примеры](./ChanceSystemBehaviour.md#примеры).
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
