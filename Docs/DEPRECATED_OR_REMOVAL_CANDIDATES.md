# Deprecated Components and Removal Candidates

**What it is:** the list of package types and members marked `[Obsolete]` in code, with their replacements. Obsolete members produce compiler warnings, never errors, and keep working until a major release removes them; removals are announced in the [changelog](../CHANGELOG.md) and the upgrade guide of that release.

**How to use:** when the compiler reports an obsolete warning, find the member below and switch to the replacement. Components marked **(Legacy)** in the Add Component menu are listed here too.

**Navigation:** [← Docs](./README.md) · [Upgrading to 12](./Migration12.md)

---

## Components and types

| Obsolete | Replacement | Note |
|----------|-------------|------|
| `TimeReward` (Bonus) | `CooldownReward`, or the plain C# `CooldownSchedule` | See [Bonus/TimeReward](./Bonus/TimeReward/README.md). Still functional. |
| `AiNavigation` (Tools/Other) | `Neo.NPC.NpcNavigation` | See [NPC](./NPC/README.md); the AiMoveExample demo shows the migrated agents. |
| `Health` (`Neo.Tools`, Rpg.Combat) | `Neo.Rpg.Components.RpgCharacter` | Persistent/local RPG actor. |
| `AttackExecution` | `Neo.Rpg.RpgAttackController` + `RpgAttackDefinition` | Melee/ranged/area attacks. |
| `Evade` | `Neo.Rpg.RpgEvadeController` | Evasion, cooldown and invulnerability locks. |
| `AdvancedAttackCollider` | `Neo.Rpg.RpgAttackController` + `Neo.Rpg.RpgProjectile` | For legacy `IDamageable`, `RpgStatsDamageableBridge` is the bridge. |
| `HandLayoutType` (enum, Cards) | `CardLayoutType` | `Cards/Config/HandLayoutType.cs`. |
| `Neo.UI.CreateMenuObject` (`Editor/Create/LegacyUiCreateMenuObject.cs`) | `Neo.CreateMenuObject` | Editor only; compatibility facade of the old UI Extension create API. |
| `Neo.Editor.NeoEditorAutoRegister` | Nothing | Empty since 12.0; `NeoCustomEditor` is discovered by Unity directly. |

## Members

| Obsolete | Replacement | Since |
|----------|-------------|-------|
| `VariantView.currentStateIndex` | `VariantView.CurrentStateIndex` (+ `StateChanged` / `OnStateChanged`) | 12.0 |
| `Map.GetCopmplete()` (Level) | `Map.IsComplete` | 12.0 |
| `ChanceData.OnIdGenerated` (Tools/Random) | `ChanceSystemBehaviour.OnIdGenerated` / `OnIndexSelected`, or the index returned by `ChanceData.GenerateId()` | 12.0 |
| `HandComponent.LegacyLayoutType` | `HandComponent.LayoutType` (`CardLayoutType`) | earlier |
| `AM.startVolumeEfx` / `AM.startVolumeMusic` | `AM.StartVolumeEfx` / `AM.StartVolumeMusic` | earlier |
| `AM.SetRandomMusicTracks(...)`, `AM.EnableRandomMusic()` | A music entry (pool) with `MusicPoolMode.Shuffle` and `PlayMusicPool(id)` | earlier |
| `SpinController.chanseWin` | `SpinController.ChanceWin` | earlier |
| `Shop.Buy()`, `Shop.Buy(int)`, `Shop.ShowPreview(int)`, `Shop.Id`, `Shop.PreviewId`, `Shop.Prices` | `Buy(string)` / `TryBuy(...)`, `ShowPreview(string)`, `EquippedId`, `PreviewIdString`, `GetPrice(string)` | earlier |
| `ShopItem.LegacyId`, `ShopItem.Visual(ShopItemData, int, int)`, `ShopItem.Visual(ShopBundleData, int, int)` | `BoundItemId` / `BoundItemData` / `BoundBundleData`, `Visual(ShopItemData, int)`, `Visual(ShopBundleData, int)` | earlier |
| `NetworkEventDispatcher.requiresAuthority` | `NetworkEventDispatcher.AuthorityMode` | earlier |

## Demo sample (not part of the runtime package)

| Obsolete | Replacement |
|----------|-------------|
| `NeoDemoShell.Build(...)`, `NeoDemoShell.ShowInfoCardOnce(...)` | A frame authored in the scene as uGUI objects ([Samples](./Samples.md#how-demo-scenes-are-made)) |
| `ModuleDemoSceneInfo.Show(...)` | An info card authored in the scene |
| `QuestDemoView` | `QuestDemoController` in the QuestDemo scene |

`SurvivorHud`, obsolete earlier in the 12.0 cycle, was deleted together with the unused `RpgDemoUI`,
`ConditionDemoSetup`, `HealthTextDisplay` and `Network/TestStart`. In 12.3 the Survivor demo scripts
(`SurvivorGame`, `SurvivorEnemy`, `SurvivorPlayerController`, `SurvivorXpOrb`, `SurvivorHudView`) were removed too:
the demo is now a saved scene wired from library components. The editor scene builders (`Neo.Demo.Editor`,
`Neo.Demo.Editor.Tests`), the `DemoGeneratedRoot` marker, `SurvivorConfig` and `SurvivorUpgrade` left the demo in the
same release: demo scenes are plain saved scenes.

## Planned removal

Removal is decided release by release; no removal of the types above is scheduled yet. Several messages in code still mention "removed in v9" for the integer-indexed Shop API; that plan was not carried out, and the members stay until a major release announces their removal. The `Rpg` module as a whole is superseded by [Abilities](./Abilities/README.md) for new combat code but is not obsolete.
