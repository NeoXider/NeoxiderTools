# Tools Audit

Дата аудита: 2026-05-25

## Итог

`Tools` остается сильным, но слишком широким зонтичным модулем. Runtime-код уже в основном разделен по asmdef-подмодулям, editor tools лежат отдельно в `Assets/Neoxider/Editor/Tools`, а deprecated/legacy зона в основном локализована. Главная проблема не в архитектуре кода, а в навигации и ownership: пользователю нужно явно видеть, где текущий runtime API, где editor-only инструменты, а где совместимость для старых сцен.

## Разделение зон

| Зона | Где находится | Статус | Решение |
|---|---|---|---|
| Runtime tools | `Assets/Neoxider/Scripts/Tools/*` | OK | Держать как набор малых asmdef-подмодулей. Новые runtime-фичи добавлять в конкретный подмодуль, а не в корень `Tools`. |
| Editor tools | `Assets/Neoxider/Editor/Tools/*` | OK с риском навигации | Документировать отдельно как editor-only. Не ссылаться на них из runtime README как на scene components. |
| Deprecated / compatibility | `Tools/Components/AttackSystem`, `Tools/Other/AiNavigation`, часть legacy-полей в `InteractiveObject`, `SpineController`, inventory snapshots | Требует дисциплины | Оставлять для обратной совместимости, но в README показывать как legacy/bridge, не как рекомендуемый путь. |
| Feature docs | `Docs/Tools/**` и `DocsEn/Tools/**` | OK | Хранить только пользовательские страницы компонентов и подмодулей. Не добавлять сюда планы/backlog. |
| Audit / maintenance docs | `Docs/Tools/TOOLS_AUDIT.md` | OK | Использовать для статуса, рисков и cleanup-задач. |

## Runtime asmdef карта

Каждый runtime-подмодуль имеет отдельный asmdef:

- `Neo.Tools.Components`
- `Neo.Tools.Debug`
- `Neo.Tools.Dialogue`
- `Neo.Tools.Draw`
- `Neo.Tools.FakeLeaderboard`
- `Neo.Tools.Input`
- `Neo.Tools.InteractableObject`
- `Neo.Tools.Inventory`
- `Neo.Tools.Managers`
- `Neo.Tools.Move`
- `Neo.Tools.Other`
- `Neo.Tools.Physics`
- `Neo.Tools.Random`
- `Neo.Tools.Spawner`
- `Neo.Tools.Text`
- `Neo.Tools.Time`
- `Neo.Tools.View`

Это хорошая граница. Не стоит добавлять новые runtime-компоненты в `Scripts/Tools` без asmdef-владельца.

## Deprecated / legacy inventory

| Область | Статус | Замена |
|---|---|---|
| `Tools/Components/AttackSystem/Health` | Legacy, `[Obsolete]` | `RpgCharacter` |
| `Tools/Components/AttackSystem/Evade` | Legacy, `[Obsolete]` | `RpgEvadeController` |
| `Tools/Components/AttackSystem/AttackExecution` | Legacy, `[Obsolete]` | `RpgAttackController` + `RpgAttackDefinition` |
| `Tools/Components/AttackSystem/AdvancedAttackCollider` | Legacy, `[Obsolete]` | `RpgAttackController`, `RpgProjectile`, `RpgStatsDamageableBridge` for compatibility |
| `Tools/Components/AttackSystem/RpgStatsDamageableBridge` | Supported bridge | Keep as compatibility path from old `IDamageable/IHealable` callers into RPG |
| `Tools/Other/AiNavigation` | Deprecated, `[Obsolete]` | `Neo.NPC.NpcNavigation` and NPC modules |
| `InteractiveObject` legacy debug ray fields | Compatibility fields | Keep serialized fields, document as legacy debug behavior |
| `SpineController.legacyAddIndex` | Serialized compatibility | Keep field; new usage should use `Skin Index Offset` |

## Editor-only tools

Current editor-only area under `Assets/Neoxider/Editor/Tools`:

- `Dialogue/DialogueEditorWindow.cs`
- `Inventory/PrefabToSpriteWindow.cs`
- `Inventory/InventoryDatabaseEditor.cs`
- `Inventory/InventoryItemDataEditor.cs`
- `Physics/ParticleToRigidbody2DWindow.cs`

These should stay out of runtime asmdefs and should be documented only as editor tools/windows. If a runtime component needs editor support, keep the editor code under `Editor/` or an editor-only asmdef.

## Documentation findings

- `Docs/Tools/README.md` and `DocsEn/Tools/README.md` now act as module entries.
- No standalone `SCRIPT_IMPROVEMENTS.md`, backlog, or old plan file remains in `Docs/Tools`.
- `AttackSystem` is correctly described as legacy, but it is still reachable from the normal Components index. This is acceptable only if the label remains explicit.
- `Other/AiNavigation` is deprecated in docs and code, but `Other` README should continue to mark it as deprecated.
- Feature docs should not contain future plans. New planning/audit content belongs in this file or a top-level project audit, not in component pages.

## Test coverage

Existing coverage relevant to `Tools`:

- `ToolsComponentsTests`
- `InteractiveObjectTests` and `InteractiveObjectPlayTests`
- `PhysicsToolsTests`
- `SpawnerTests`
- `TimerTests`
- `SelectorTests`
- `ExtensionsLifecycleTests`
- `FreeFlyCameraControllerTests`
- Inventory tests under `RPG/InventorySystemTests` and PlayMode inventory/shop integration tests
- Network-related Tools tests under `Tests/Play/Network`

Gaps:

- No broad PlayMode coverage for `Move` camera/cursor flows (`CursorLockController`, `PlayerController2D/3DPhysics`, `FreeFlyCameraController` real cursor behavior).
- Runtime logging is still present in several Tools components; many are diagnostics, but not all are gated.
- Editor windows have limited direct editor-window tests.
- Deprecated `AiNavigation` is intentionally retained but should not receive new feature work.

## Cleanup priorities

### P0

- Keep deprecated APIs serialized-compatible. Do not delete `AttackSystem` or `AiNavigation` without a migration pass and missing-script scan through Unity.
- Keep editor code out of runtime asmdefs.

### P1

- Add explicit `Deprecated / Compatibility` section to module indexes when adding new legacy bridges.
- Gate noisy runtime logs in `FakeLeaderboard`, `Selector`, `AiNavigation`, `SpineController`, `SetText`, `Spawner`, and move/input fallbacks when they can happen every frame or during normal user flows.
- Add focused PlayMode coverage for cursor/look lifecycle in `CursorLockController` and `FreeFlyCameraController`.

### P2

- Move legacy-heavy documentation into compatibility blocks while keeping file paths stable.
- Add editor tests for Tools editor windows where practical.
- Consider a future `Neo.Tools.Legacy` asmdef only if Unity migration tooling is ready; do not move scripts now because it may break serialized scene references.

## Policy for future changes

- New runtime feature: add it to a concrete submodule (`Move`, `Input`, `Inventory`, etc.), add tests, add RU/EN component docs, update only the relevant README and changelog.
- New editor feature: place it under `Assets/Neoxider/Editor/Tools`, document as editor-only, and avoid runtime references.
- Deprecated feature: keep code stable, mark docs and Add Component labels clearly, point to replacement, and avoid feature expansion.
- Audit or backlog: use audit/maintenance docs, not component pages.
