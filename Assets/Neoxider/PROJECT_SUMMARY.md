# NeoxiderTools Project Summary

Краткая актуальная сводка по пакету `NeoxiderTools` для разработчика и мейнтейнера.

## Статус пакета

- **UPM пакет**: `Assets/Neoxider/package.json`
- **Текущая версия**: `9.1.0`
- **Unity**: `2022.1+`
- **Основной namespace**: `Neo`
- **Главный пользовательский вход**: [`README.md`](./README.md)
- **Главный индекс документации**: [`Docs/README.md`](./Docs/README.md)
- **Англоязычный вход**: [`DocsEn/README.md`](./DocsEn/README.md)
- **Тесты пакета**: `Assets/Neoxider/Tests/`

## Структура

```text
Assets/Neoxider/
  Scripts/      # Runtime modules grouped by domain
  Editor/       # Editor tools and inspectors
  Tests/        # EditMode and PlayMode tests for package runtime/editor-critical flows
  Docs/         # Russian user-facing docs
  DocsEn/       # English onboarding and mirrored docs
  Samples/      # Active development samples and smoke scenes
  Samples~/     # UPM sample source path before release packaging
  Prefabs/      # Ready-to-use prefabs
  Resources/    # Settings/assets
```

## Ключевые продуктовые слои

- **Core gameplay modules**: `Core` (Level, Resources), `Cards`, `GridSystem`, `Merge`, `Save`, `Shop`, `StateMachine`, `NPC`, `UI`, `Progression`.
- **Grid games**: `FieldGenerator` для формы/координат/cell state/pathfinding, `GridPlacementEntry` / `GridPlacementResult` / `PlaceContentFootprint` для multi-cell placement, `GridMergeResolver` для connected-group merge, `DiceBoardService` для Dice Merge, плюс Match3, TicTacToe и SlidingMerge слои.
- **Generic merge**: `Neo.Merge` - pure C# connected-group merge engine без привязки к Unity scene/Grid.
- **General-purpose tools**: `Tools/Inventory`, `Tools/Spawner`, `Tools/Move` (включая `FreeFlyCameraController`), `Tools/Dialogue`, `Tools/Input`, `Tools/Time`.
- **No-code / Inspector workflows**: `Condition`, **`Neo.NoCode`** (`NoCodeBindText`, `SetProgress` → TMP / Slider / Image), `PropertyAttribute`, UnityEvent-driven components.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional sample module**: `Samples~/NeoxiderPages`.

## Зависимости

### Через UPM

- `com.unity.textmeshpro`
- `com.unity.ai.navigation`
- `com.unity.inputsystem`

### По сценариям использования

- `UniTask` для async-heavy модулей
- `DOTween` для tween-based анимаций и UI
- `Spine Unity Runtime` только для Spine-интеграций
- `MarkdownRenderer` опционально для улучшенного просмотра `.md` в инспекторе
- `com.unity.render-pipelines.universal` только для проектов, которым нужны URP-специфичные rendering features или 2D lights; пакет больше не устанавливает URP автоматически

## Правила развития пакета

- Сначала проверяйте эту сводку и docs index: часто нужный building block уже есть, и новую механику можно собрать адаптером вместо переписывания с нуля.
- Переиспользуйте существующие модули и расширяйте их, вместо добавления дубликатов.
- При изменении публичного поведения обновляйте документацию модуля и `CHANGELOG.md`.
- Для новых пользовательских entry points сначала обновляйте `README.md` и `Docs/README.md`.
- Для multi-instance систем предпочитайте явные ссылки вместо неявного auto-find.
- Для editor-only логики держите код в `Editor/` или в отдельных `Editor`-папках под правильными `asmdef`.
- **Структура модулей:** папки (Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums), asmdef, неймспейсы и принцип «один тип — один файл» описаны в [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

- `Samples/Demo` содержит активные developer-сцены для модулей пакета.
- `Samples/NeoxiderPages` содержит активный опциональный sample-модуль навигации по страницам.
- Перед release packaging эти sample roots переводятся в UPM paths `Samples~/Demo` и `Samples~/NeoxiderPages`, как указано в `package.json.samples`.

## Тесты и качество

- В пакете подключён `com.unity.test-framework`.
- Тесты пакета лежат в `Assets/Neoxider/Tests/` (`Edit`, `Play`, `PlayMode`, `Editor`).
- На текущий момент покрыты критичные сценарии `Save`, `Level`, `Bootstrap`, `Audio`, `Parallax`, `PropertyAttribute`, `Tools/Move`, `Cards`, `GridSystem`, `Merge`, `Dice`, `Rpg`, `Settings`, `Quest`, `Progression`, `StateMachine` и часть legacy/editor-регрессий.

## Канонические документы

- Пользовательский обзор пакета: [`README.md`](./README.md)
- Пользовательская навигация по модулям: [`Docs/README.md`](./Docs/README.md)
- Англоязычный вход: [`DocsEn/README.md`](./DocsEn/README.md)
- История изменений: [`CHANGELOG.md`](./CHANGELOG.md)
- Правила структуры модулей (папки, asmdef, неймспейсы): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
