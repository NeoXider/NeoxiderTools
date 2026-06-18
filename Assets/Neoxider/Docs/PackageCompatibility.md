# Совместимость Пакета

Дата проверки: 2026-06-04

## Unity

| Источник | Версия |
| --- | --- |
| Пакет `Assets/Neoxider/package.json` | `version: 9.2.3`, `unity: 2022.1` |
| Локальный проект `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

Минимальная версия UPM-пакета остается Unity 2022.1. Проект разработки сейчас открыт и проверяется на Unity `6000.3.14f1`, но это не должно автоматически поднимать минимальную версию пакета.

## Package Dependencies

| Dependency | `package.json` | Project manifest | Статус |
| --- | --- | --- | --- |
| `com.unity.textmeshpro` | `3.0.6` | доступен через Unity UI stack | Нужен TMP/UI-компонентам. |
| `com.unity.ai.navigation` | `1.1.7` | `2.0.11` | В Unity 6 проекте стоит более новая версия; пакет сохраняет нижний минимум для Unity 2022. |
| `com.unity.inputsystem` | `1.14.2` | `1.19.0` | Runtime использует optional adapters/fallback, чтобы поддерживать Legacy Input Manager и New Input System. |

## Внешние интеграции

- DOTween используется модулями `Cards`, `UI`, `Tools/View`, `Tools/Text` и некоторыми проектными UI workflow. Это не UPM dependency пакета; host-проект подключает его при необходимости. `NeoxiderPages` sample больше не требует DOTween/DOTween Pro для импорта.
- Mirror опционален и нужен для `Neo.Network` / multiplayer flows. В проекте разработки Mirror доступен через scoped registry/OpenUPM.
- URP опционален. URP-specific поведение используется только когда host-проект предоставляет нужные package/types.

## Samples

- Во время активной разработки sample-сцены находятся в `Assets/Neoxider/Samples`.
- Перед UPM-релизом папка возвращается в `Assets/Neoxider/Samples~`.
- После импорта через Unity Package Manager sample копируется в `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`.
- `package.json.samples[].path` должен оставаться release-facing и указывать на `Samples~/...`.
- Validation-тесты поддерживают dev root `Samples`, package source root `Samples~`, imported root `Assets/Samples/NeoxiderTools` и legacy imported root `Assets/Samples/Neoxider Tools`.

## Политика

- Не поднимать `unity` в `package.json` только из-за того, что текущий проект разработки использует Unity 6.
- Optional third-party интеграции держать guarded/fallback-friendly, чтобы package-only проекты не ломались без необязательных зависимостей.
- Обновлять эту страницу при изменениях `package.json`, `Packages/manifest.json`, sample layout или install requirements.
