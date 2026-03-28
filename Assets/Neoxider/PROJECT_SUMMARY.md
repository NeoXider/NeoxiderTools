# NeoxiderTools Project Summary

Краткая актуальная сводка по пакету `NeoxiderTools` для разработчика и мейнтейнера.

## Статус пакета

- **UPM пакет**: `Assets/Neoxider/package.json`
- **Текущая версия**: `7.7.25`
- **Unity**: `2022.1+`
- **Основной namespace**: `Neo`
- **Главный пользовательский вход**: [`README.md`](./README.md)
- **Главный индекс документации**: [`Docs/README.md`](./Docs/README.md)
- **Англоязычный вход**: [`DocsEn/README.md`](./DocsEn/README.md)
- **Базовые EditMode тесты**: `Assets/Neoxider/Editor/Tests/`

## Структура

```text
Assets/Neoxider/
  Scripts/      # Runtime modules grouped by domain
  Editor/       # Editor tools and inspectors
    Tests/      # EditMode tests for runtime/editor-critical flows
  Docs/         # Russian user-facing docs
  DocsEn/       # English onboarding and mirrored docs
  Samples~/     # UPM samples
  Prefabs/      # Ready-to-use prefabs
  Resources/    # Settings/assets
```

## Ключевые продуктовые слои

- **Core gameplay modules**: `Core` (Level, Resources), `Cards`, `GridSystem`, `Save`, `Shop`, `StateMachine`, `NPC`, `UI`, `Progression`.
- **General-purpose tools**: `Tools/Inventory`, `Tools/Spawner`, `Tools/Move`, `Tools/Dialogue`, `Tools/Input`, `Tools/Time`.
- **No-code / Inspector workflows**: `Condition`, `PropertyAttribute`, UnityEvent-driven components.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional sample module**: `Samples~/NeoxiderPages`.

## Зависимости

### Через UPM

- `com.unity.textmeshpro`
- `com.unity.ai.navigation`

### По сценариям использования

- `UniTask` для async-heavy модулей
- `DOTween` для tween-based анимаций и UI
- `Spine Unity Runtime` только для Spine-интеграций
- `MarkdownRenderer` опционально для улучшенного просмотра `.md` в инспекторе

## Правила развития пакета

- Переиспользуйте существующие модули и расширяйте их, вместо добавления дубликатов.
- При изменении публичного поведения обновляйте документацию модуля и `CHANGELOG.md`.
- Для новых пользовательских entry points сначала обновляйте `README.md` и `Docs/README.md`.
- Для multi-instance систем предпочитайте явные ссылки вместо неявного auto-find.
- Для editor-only логики держите код в `Editor/` или в отдельных `Editor`-папках под правильными `asmdef`.
- **Структура модулей:** папки (Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums), asmdef, неймспейсы и принцип «один тип — один файл» описаны в [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

- `Samples~/Demo` содержит демонстрационные сцены для модулей пакета.
- `Samples~/NeoxiderPages` содержит опциональный sample-модуль навигации по страницам.

## Тесты и качество

- В пакете подключён `com.unity.test-framework`.
- Базовые `EditMode` тесты лежат в `Editor/Tests/`.
- На текущий момент покрыты критичные сценарии `Save`, `Level`, `Bootstrap` и часть legacy/editor-регрессий.

## Канонические документы

- Пользовательский обзор пакета: [`README.md`](./README.md)
- Пользовательская навигация по модулям: [`Docs/README.md`](./Docs/README.md)
- Англоязычный вход: [`DocsEn/README.md`](./DocsEn/README.md)
- История изменений: [`CHANGELOG.md`](./CHANGELOG.md)
- Правила структуры модулей (папки, asmdef, неймспейсы): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
