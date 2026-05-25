# Tools Audit

Audit date: 2026-05-25

## Summary

`Tools` is strong but very broad. Runtime code is already split into small asmdef-backed submodules, editor tools live under `Assets/Neoxider/Editor/Tools`, and deprecated compatibility code is mostly localized. The main risk is navigation and ownership: users must clearly see what is current runtime API, what is editor-only tooling, and what is legacy compatibility for old scenes.

## Zones

| Zone | Location | Status | Decision |
|---|---|---|---|
| Runtime tools | `Assets/Neoxider/Scripts/Tools/*` | OK | Keep as small asmdef-backed submodules. Add new runtime features to a concrete submodule, not the Tools root. |
| Editor tools | `Assets/Neoxider/Editor/Tools/*` | OK with navigation risk | Document as editor-only. Do not present them as scene components in runtime docs. |
| Deprecated / compatibility | `Tools/Components/AttackSystem`, `Tools/Other/AiNavigation`, selected legacy fields | Needs discipline | Keep for compatibility, but route new users to replacements. |
| Feature docs | `Docs/Tools/**` and `DocsEn/Tools/**` | OK | Keep component and submodule docs only. Do not add plans/backlog here. |
| Audit / maintenance docs | `Docs/Tools/TOOLS_AUDIT.md` | OK | Use for status, risks, and cleanup tasks. |

## Runtime asmdef Map

Runtime Tools submodules each have their own asmdef:

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

This boundary is good. New runtime components should not be added to `Scripts/Tools` without a clear asmdef owner.

## Deprecated / Compatibility

| Area | Status | Replacement |
|---|---|---|
| `Tools/Components/AttackSystem/Health` | Legacy, `[Obsolete]` | `RpgCharacter` |
| `Tools/Components/AttackSystem/Evade` | Legacy, `[Obsolete]` | `RpgEvadeController` |
| `Tools/Components/AttackSystem/AttackExecution` | Legacy, `[Obsolete]` | `RpgAttackController` + `RpgAttackDefinition` |
| `Tools/Components/AttackSystem/AdvancedAttackCollider` | Legacy, `[Obsolete]` | `RpgAttackController`, `RpgProjectile`, `RpgStatsDamageableBridge` for compatibility |
| `Tools/Components/AttackSystem/RpgStatsDamageableBridge` | Supported bridge | Keep as compatibility path from old `IDamageable/IHealable` callers into RPG |
| `Tools/Other/AiNavigation` | Deprecated, `[Obsolete]` | `Neo.NPC.NpcNavigation` and NPC modules |
| `InteractiveObject` legacy debug ray fields | Compatibility fields | Keep serialized fields, document as legacy debug behavior |
| `SpineController.legacyAddIndex` | Serialized compatibility | Keep field; new usage should use `Skin Index Offset` |

## Editor-only Tools

Current editor-only Tools area:

- `Dialogue/DialogueEditorWindow.cs`
- `Inventory/PrefabToSpriteWindow.cs`
- `Inventory/InventoryDatabaseEditor.cs`
- `Inventory/InventoryItemDataEditor.cs`
- `Physics/ParticleToRigidbody2DWindow.cs`

These should stay outside runtime asmdefs. If a runtime component needs editor support, keep editor code under `Editor/` or an editor-only asmdef.

## Documentation Findings

- `Docs/Tools/README.md` and `DocsEn/Tools/README.md` are the module entries.
- No standalone `SCRIPT_IMPROVEMENTS.md`, backlog, or old plan file remains in `Docs/Tools`.
- `AttackSystem` is correctly marked as legacy, but it is still reachable from the normal Components index. This is acceptable only while the legacy label remains explicit.
- `Other/AiNavigation` is deprecated in docs and code; keep that label visible.
- Feature docs should not contain future plans. Planning/audit content belongs in this file or the project audit, not component pages.

## Test Coverage

Relevant existing coverage:

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
- Runtime logging is still present in several Tools components; some logs are useful diagnostics, but not all are gated.
- Editor windows have limited direct editor-window tests.
- Deprecated `AiNavigation` is intentionally retained but should not receive new feature work.

## Cleanup Priorities

### P0

- Keep deprecated APIs serialized-compatible. Do not delete `AttackSystem` or `AiNavigation` without a migration pass and Unity missing-script scan.
- Keep editor code out of runtime asmdefs.

### P1

- Add explicit `Deprecated / Compatibility` sections to module indexes when adding new legacy bridges.
- Gate noisy runtime logs in `FakeLeaderboard`, `Selector`, `AiNavigation`, `SpineController`, `SetText`, `Spawner`, and move/input fallbacks when they can happen every frame or during normal user flows.
- Add focused PlayMode coverage for cursor/look lifecycle in `CursorLockController` and `FreeFlyCameraController`.

### P2

- Move legacy-heavy documentation into compatibility blocks while keeping file paths stable.
- Add editor tests for Tools editor windows where practical.
- Consider a future `Neo.Tools.Legacy` asmdef only if Unity migration tooling is ready; do not move scripts now because serialized scene references may break.

## Future Policy

- New runtime feature: add it to a concrete submodule (`Move`, `Input`, `Inventory`, etc.), add tests, add RU/EN component docs, update only the relevant README and changelog.
- New editor feature: place it under `Assets/Neoxider/Editor/Tools`, document as editor-only, and avoid runtime references.
- Deprecated feature: keep code stable, mark docs and Add Component labels clearly, point to replacement, and avoid feature expansion.
- Audit or backlog: use audit/maintenance docs, not component pages.
