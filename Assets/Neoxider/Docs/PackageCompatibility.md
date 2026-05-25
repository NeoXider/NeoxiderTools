# Совместимость пакета

Дата проверки: 2026-05-25

## Unity

| Источник | Версия |
|---|---|
| Пакет `Assets/Neoxider/package.json` | `version: 8.6.0`, `unity: 2022.1` |
| Локальный проект `ProjectSettings/ProjectVersion.txt` | Unity `6000.3.14f1` |

Минимальная версия UPM-пакета не повышена до Unity 6: пакет должен оставаться пригодным для проектов на Unity 2022.1+. Текущий репозиторий и проверки выполняются на Unity `6000.3.14f1`.

## Package Dependencies

| Dependency | package.json | Project manifest | Status |
|---|---|---|---|
| `com.unity.textmeshpro` | `3.0.6` | доступен через Unity UI stack | Нужен TMP/UI компонентам. |
| `com.unity.ai.navigation` | `1.1.7` | `2.0.11` | В проекте Unity 6 стоит более новая версия; пакет сохраняет нижний минимум для Unity 2022. |
| `com.unity.inputsystem` | `1.14.2` | `1.19.0` | Runtime использует optional adapters/fallback, чтобы поддерживать Legacy Input Manager и New Input System. |

## Внешние интеграции

- DOTween используется несколькими модулями (`Cards`, `UI`, `Tools/View`, `Tools/Text`, samples). Это не UPM dependency пакета; хост-проект подключает его при необходимости.
- Mirror опционален. Network-код загейтирован define `MIRROR`; проект разработки получает Mirror через scoped registry/OpenUPM.
- URP опционален. URP-specific поведение используется только когда хост-проект предоставляет нужные package/types.

## Текущее решение

В релизе `8.6.0` версия пакета повышена, но минимальная Unity-совместимость и package dependencies не повышались. Пакет остается совместимым с текущим Unity 6 проектом и сохраняет заявленное окно поддержки Unity 2022.1+.
