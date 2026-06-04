# Sample-сцены

Текущий рабочий путь sample-сцен во время разработки: `Assets/Neoxider/Samples/Demo/`.

Перед релизом/UPM-упаковкой папка возвращается в `Assets/Neoxider/Samples~/Demo/`, а `Assets/Neoxider/package.json` должен ссылаться на `Samples~/...`.

После импорта через Unity Package Manager sample попадает в проект не в пакетную папку, а в `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`. Для текущей версии ожидаемый путь: `Assets/Samples/NeoxiderTools/9.2.0/Demo Scenes/...`.

Эти сцены служат smoke-покрытием и точками входа для ручной проверки модулей. Они не заменяют тесты: публичные C# контракты проверяются EditMode/PlayMode тестами, а сцены показывают минимальную сборку через MonoBehaviour wrappers.

## Обязательные smoke-сцены

| Модуль | Сцена | Что проверяет |
|--------|-------|---------------|
| Audio | `Scenes/Audio/AudioDemo.unity` | `AM`, `AudioSource`, базовый scene entry |
| Level | `Scenes/Level/LevelFlowDemo.unity` | `LevelManager` как сценовая обертка |
| Network | `Scenes/Network/NetworkDemo.unity` | `NeoNetworkManager` + Mirror transport |
| NoCode | `Scenes/NoCode/NoCodeBindingDemo.unity` | scene-only binding wrapper |
| Parallax | `Scenes/Parallax/ParallaxDemo.unity` | `ParallaxLayer` + visual template |
| Save | `Scenes/Save/SaveDemo.unity` | `SaveProviderSettingsComponent` + `SaveManager` |
| Settings | `Scenes/Settings/SettingsDemo.unity` | `GameSettingsComponent` defaults |
| StateMachine | `Scenes/StateMachine/StateMachineDemo.unity` | `StateMachineBehaviourBase` lifecycle entry |

## Игровые и интеграционные сцены

| Область | Сцены | Ручная проверка |
|---------|-------|-----------------|
| GridSystem | `Scenes/GridSystem/GridSystemMatch3Demo.unity`, `Scenes/GridSystem/GridSystemTicTacToeDemo.unity` | поле, правила, view binding |
| Progression | `Scenes/Progression_Demo.unity` | XP, рост уровня, прогресс-бар |
| Quest | `Scenes/Quests/QuestDemo.unity` | quest flow и scene reload |
| RPG | `Scenes/RpgCharacterQuickDemo.unity`, `Scenes/RpgCombatNpcDemo.unity` | `RpgCombatNpcDemo` должен показывать HUD, автобой, урон игроку, смерть NPC и авто-восстановление волны |
| Bonus | `Scenes/Bonuses/SlotExample.unity`, `Scenes/Bonuses/WheelFortuneExample.unity` | slot/wheel runtime loop |
| Shop | `Scenes/Shop/Shop.unity` | typed shop API |
| Tools | `Scenes/Tools/**` | отдельные runtime/editor wrappers |
| Example games | `Scenes/_ExampleGame/**`, `Scenes/VampireSurvivorMCP.unity` | `VampireSurvivorMCP` должен показывать HUD, spawner counters, урон по игроку, death overlay и reset |

## Автоматическая проверка

`SampleAssetValidationTests` проверяет sample prefabs/scenes на missing scripts, missing MonoScript GUID, `NetworkBehaviour` без `NetworkIdentity` и битые `TerrainData.treePrototypes`. Тест ищет активный root в `Assets/Neoxider/Samples`, `Assets/Neoxider/Samples~`, imported root `Assets/Samples/NeoxiderTools` и legacy root `Assets/Samples/Neoxider Tools`.

`SampleSceneCoverageTests` поддерживает все три формата sample root. Для активных development/imported samples он открывает обязательные smoke-сцены и проверяет, что они не пустые, содержат `ModuleDemoSceneInfo` и не имеют missing scripts. Для скрытого `Samples~` Unity не компилирует sample-only scripts, поэтому тест валидирует `.unity` YAML: проверяет наличие сцен, `ModuleDemoSceneInfo`, RPG demo controllers, `RpgCharacter`, `Spawner` и резолв всех referenced MonoScript GUID через assets/package metadata.
