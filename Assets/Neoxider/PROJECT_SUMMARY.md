# NeoxiderTools Project Summary

Краткая карта пакета `NeoxiderTools` для разработчика, мейнтейнера и AI-агента. Цель: быстро понять, какие готовые runtime API, MonoBehaviour-обертки, sample-сцены и docs уже есть, прежде чем писать новую механику.

## Статус пакета

- **UPM пакет**: `Assets/Neoxider/package.json`
- **Текущая версия**: `9.7.1`
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
- **No-code / Inspector workflows**: `Condition`, **`Neo.NoCode`** (`NoCodeBindText`, `SetProgress` -> TMP / Slider / Image), `PropertyAttribute`, UnityEvent-driven components.
- **Editor support**: custom inspectors, creation menus, maintenance windows.
- **Optional sample module**: `Samples~/NeoxiderPages`.

## Reuse-first map

Перед новой реализацией проверьте эти готовые блоки:

| Если нужно | Уже есть | Где смотреть |
|------------|----------|--------------|
| Сетка, shape mask, координаты, disabled/walkable/occupied cells | `FieldGenerator`, `GridShapeMask`, `FieldDebugDrawer` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Multi-cell размещение фигур/предметов | `GridPlacementEntry`, `GridPlacementResult`, `FieldGenerator.CanPlaceContentFootprint`, `PlaceContentFootprint` | [`Docs/GridSystem/FieldGenerator.md`](./Docs/GridSystem/FieldGenerator.md) |
| Merge одинаковых связанных элементов | `Neo.Merge.MergeResolver`, `MergeRequest<TItem,TValue>`, `GridMergeRequest.Increment(...)` | [`Docs/Merge/README.md`](./Docs/Merge/README.md) |
| Dice Merge / drop-and-merge на сетке | `DicePiece`, `DicePieceGenerator`, `DiceBoardService` | [`Docs/GridSystem/Dice/README.md`](./Docs/GridSystem/Dice/README.md) |
| Лимитированные руки, лавки, draft tray, market row | `HandModel.Capacity`, `TryAdd(...)`, `RemainingCapacity`, `AddRangeUntilFull(...)` | [`Docs/Cards/README.md`](./Docs/Cards/README.md) |
| Match3, TicTacToe, 2048-like movement | `Match3BoardService`, `TicTacToeBoardService`, `SlidingMergeBoardService` | [`Docs/GridSystem/README.md`](./Docs/GridSystem/README.md) |
| Полет наград/монет между world/canvas точками | `AnimationFly.Play(AnimationFlyRequest)`, sprite/prefab visuals, reward timing, fountain/magnet/scatter motion presets | [`Docs/UI/AnimationFly.md`](./Docs/UI/AnimationFly.md) |
| Сохранение scene objects и global/profile data | `SaveManager`, `SaveProvider`, `GlobalSave`, `SaveableBehaviour` | [`Docs/Save/README.md`](./Docs/Save/README.md) |
| Деньги, магазин, мультивалюта | `Money`, `IMoneySpend`, `Shop`, `ShopItemData.CurrencyOverrideSaveKey` | [`Docs/Shop/README.md`](./Docs/Shop/README.md) |
| HP/Mana/resources, уровни и XP | `HealthComponent`, `ResourcePoolModel`, `LevelComponent`, `LevelCurveDefinition` | [`Docs/Core/README.md`](./Docs/Core/README.md) |
| RPG бой, projectiles, buffs/statuses | `RpgCharacter`, `RpgAttackController`, `RpgProjectile`, `RpgCombatMath` | [`Docs/Rpg/README.md`](./Docs/Rpg/README.md) |
| Inspector/no-code условия и действия | `NeoCondition`, `ConditionEntryPredicate`, module NoCode bridges | [`Docs/Condition/README.md`](./Docs/Condition/README.md) |
| Debug/spectator movement, pooling, timers, input helpers | `Tools/Move`, `Tools/Spawner`, `Tools/Time`, `Tools/Input` | [`Docs/Tools/README.md`](./Docs/Tools/README.md) |

## Recent stabilization notes

- `GridSystem`, `Merge` и `Dice` уже имеют reusable placement/merge APIs, configurable dice rules, cascade-limit reporting, consistent board notifications и active Dice Merge sample.
- `DicePieceGenerator` поддерживает `CreateDefaultPool()` для исходного merge-пула 1-5, `CreateD6Pool()` для классических граней 1-6 и `CreateSequentialPool(minValue, maxValue)` для кастомных numbered dice/progression-пулов.
- `Cards` поддерживает finite runtime hands через `HandModel.Capacity`; `Capacity = 0` оставляет старое unlimited-поведение, а `TryAdd(...)` / `AddRangeUntilFull(...)` дают безошибочный путь для UI overflow flows.
- `AnimationFly` уже поддерживает typed request/result, prefab или sprite visuals, world/canvas coordinate conversion, pooling/disable-on-complete, reward timing callbacks и reusable motion presets для fountain, magnet, fountain+magnet и scatter reward effects.
- `SaveManager.Save()` сохраняет shared container read-modify-write и не удаляет данные выгруженных scene objects.
- `Core`/`RPG` fixes covered edge cases around XP-backed level sync, duplicate death/resource events, regen-from-zero, target resolution, projectile hits, buff stacks and persistence.
- `Shop`/`Money` reject negative spends and avoid optimistic client-only network success before authority confirms balance.

## Зависимости

### Через UPM

- `com.unity.textmeshpro`
- `com.unity.ai.navigation`
- `com.unity.inputsystem`
- `com.unity.ugui`

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
- **Структура модулей:** папки (Interfaces, Domain, Data, Components, Bridge, Runtime, Events, Enums), asmdef, неймспейсы и принцип "один тип - один файл" описаны в [MODULE_STRUCTURE.md](./MODULE_STRUCTURE.md).

## Samples

- `Samples/Demo` содержит активные developer-сцены для модулей пакета, включая `Scenes/UI/AnimationFlyDemo.unity` для ручной проверки fly-эффекта с кнопками, motion presets и подписанными слайдерами.
- `Samples/NeoxiderPages` содержит активный опциональный sample-модуль навигации по страницам.
- Перед release packaging эти sample roots переводятся в UPM paths `Samples~/Demo` и `Samples~/NeoxiderPages`, как указано в `package.json.samples`.

## Тесты и качество

- В пакете подключен `com.unity.test-framework`.
- Тесты пакета лежат в `Assets/Neoxider/Tests/` (`Edit`, `Play`, `PlayMode`, `Editor`).
- На текущий момент покрыты критичные сценарии `Save`, `Level`, `Bootstrap`, `Audio`, `Parallax`, `PropertyAttribute`, `Tools/Move`, `Cards`, `GridSystem`, `Merge`, `Dice`, `Rpg`, `Settings`, `Quest`, `Progression`, `StateMachine` и часть legacy/editor-регрессий.

## Канонические документы

- Пользовательский обзор пакета: [`README.md`](./README.md)
- Пользовательская навигация по модулям: [`Docs/README.md`](./Docs/README.md)
- Англоязычный вход: [`DocsEn/README.md`](./DocsEn/README.md)
- История изменений: [`CHANGELOG.md`](./CHANGELOG.md)
- Правила структуры модулей (папки, asmdef, неймспейсы): [`MODULE_STRUCTURE.md`](./MODULE_STRUCTURE.md)
