# Examples

- [AnomalyGame (RU documentation)](./AnomalyGame.md)

## Demo Scenes sample

Import **Demo Scenes** from Package Manager → NeoxiderTools → Samples. The package stores it in
`Assets/Neoxider/Samples~/Demo`; Unity imports a copy under
`Assets/Samples/NeoxiderTools/<version>/Demo Scenes`. During package development the working root can be
`Assets/Neoxider/Samples/Demo`. Keep the release `Samples~` directory hidden; importing the sample makes
its `Neo.Rpg.Demo` assembly available. That assembly currently includes examples for several modules and
references Mirror as well as the RPG compatibility modules.

These scripts demonstrate composition of package APIs. Game rules remain in the corresponding runtime
modules; the sample controllers and generated views are optional. For new combat code, start with
[Abilities](../Abilities/README.md); the RPG examples maintain existing scenes.

| Example | Runtime entry points | Scene workflow |
| --- | --- | --- |
| Condition | `Health.TakeDamage`, `Health.Heal`, `ScoreManager.Add` | Wire conditions to `ConditionDemoUI` panel actions. Its setup values are copied to the UI, including custom damage, healing and score amounts. |
| Match3 | `FieldGenerator`, `Match3BoardService`, `GridSystemMatch3DemoUI` | Assign services or call the board view's `Configure`. A live view reconnects when configured with another field and rebuilds when the field is regenerated. |
| TicTacToe | `TicTacToeBoardService.TryMakeMove`, `ResetBoard` | Assign the field, board and camera to the view; use the UI to change blockers and test paths. `Rebuild` replaces the generated cells. |
| Dice Merge | `DiceBoardService`, `DiceMergeDemoController.RestartDemo`, `TryPlaceCurrentPiece` | Drag a single die or pair from the tray, tap to rotate, and restart after game over. The optional repair component resolves prefabs relative to its imported script. |
| RPG | `RpgCharacter`, `RpgCombatDemoController`, `RpgWorldHealthBars` | Assign combatants and camera explicitly. Health bars use the assigned camera, hide when disabled, and release their generated views when destroyed. |
| Survivor | `SurvivorConfig`, `SurvivorGame`, `SurvivorUpgrade` | Assign a config containing unit templates, an ability library and upgrades. The sample reuses Abilities and PoolManager. Teardown retires its exact generated pool sources; unrelated sources with the same name survive. |
| Module shell | `NeoDemoShell.Build`, `NeoDemoShell.Context.Dispose`, `ModuleDemoSceneInfo` | Audio, save, settings, level, quest, state machine, animation, parallax and network controllers provide buttons and an event log. Disabling hides the frame; removing the controller releases its generated frame and gradient. |
| Animation Fly | `AnimationFly.Play`, `AnimationFlyDemoController` | Try world, canvas and screen origins, pooling, motion presets, reward callbacks and the tuning sliders. |

## C# configuration

Use the same typed package APIs as the sample buttons. Inspector/NoCode wiring is optional. In an
imported sample, the Condition controller also exposes an amount configuration method:

```csharp
using Neo.Demo.Condition;

// References to Health, ScoreManager and panels are assigned on this scene component.
conditionUi.ConfigureAmounts(damagePerHit: 7, healPerClick: 3, pointsPerClick: 11);
conditionUi.DealDamage();
conditionUi.HealPlayer();
conditionUi.AddScore();
```

`ConfigureAmounts` clamps negative amounts to zero. Existing scenes retain the defaults of 25 damage,
10 healing and 25 points. The setup component's button labels and actions use the same configured values.

`DiceMergeDemoView.Configure` accepts optional art references. A null sprite, cell prefab, die prefab or
sprite array keeps the corresponding authored reference, so runtime repair can operate in a player where
editor asset loading is unavailable. With no die prefab, the view creates simple visible dice using
`DiceDieView` and a numeric label. Its existing three-argument `Initialize` API is retained; the overload
with `showValueLabel` can explicitly request a number when no pip sprite exists. Call `Configure` with
both services before `RestartDemo`; an unwired controller reports
`GameOver == true` and `CurrentPiece == null`. Disabling and re-enabling a view refreshes the current board
and tray. Match3 and Dice views disconnect their previous event sources when reconfigured.

`SurvivorConfig.XpForLevel(level)` returns a threshold in `[1, int.MaxValue]`, computes growth without
integer overflow, and treats levels below one as level one. Invalid zero/negative thresholds therefore
cannot trap XP processing in a loop. This is a guard for authored data; configure positive XP values for
the intended progression curve.

## Generated resources / Созданные ресурсы

In an imported sample, `NeoDemoShell.Build(title, accent, owner)` parents the frame under an explicit
owner. Keep the returned `Context`, use `SetVisible` when enabling/disabling the controller, and call
`Dispose` when removing it. Disposal is idempotent: callbacks stop immediately, and Unity destroys the
owned Canvas, gradient Sprite and Texture2D at the end of the frame in Play Mode. Shared cameras,
EventSystems and `SurvivorArt` sprites remain usable. The existing two-argument `Build` overload still
creates a root frame; its caller must also dispose the returned context.

```csharp
private Neo.Samples.NeoDemoShell.Context _frame;

private void Start()
{
    _frame = Neo.Samples.NeoDemoShell.Build("My module", UnityEngine.Color.cyan, transform);
    _frame.AddButtonRow(("Act", () => _frame.Log("Action")));
}

private void OnEnable() => _frame?.SetVisible(true);
private void OnDisable() => _frame?.SetVisible(false);
private void OnDestroy() => _frame?.Dispose();
```

The Audio demo uses `AM.PlayAt(clip, Vector3.zero, 1f, 0f)` for individually releasable 2D effects.
On destruction it unsubscribes its listeners, calls `AM.ReleaseClips` for its six generated clips,
then destroys those clips. Borrowed tracks and the audio manager survive, including when a demo pad
is the outgoing side of a crossfade. Level and StateMachine demos stop/detach their own consumers
before releasing runtime curve/state definitions. The Level demo accepts unset `LevelManager` event
fields: it subscribes only to existing events and never creates or replaces the borrowed events.
Its owned host, curve and frame are released even after partial setup. StateMachine colors are bound by a scene listener;
the ScriptableObject states contain no scene-object closures.

Both Quest examples unregister only their runtime-created configs through
`QuestManager.UnregisterQuest(expected)` before destroying them. Saved quest state, resource-loaded
configs and a replacement config registered under the same ID remain intact. Removing the demo does
not reset progress; use its explicit reset actions when replaying it. Survivor similarly calls
`SpawnUtility.RetirePool(source)` for exact source identities, returns its tracked enemies/orbs, and
allows outstanding rentals to be destroyed on their later return. It does not delete shared pool roots.

Русский: `Context` владеет только созданным им UI и градиентом. При отключении контроллера вызывайте
`SetVisible(false)`, при удалении — `Dispose()`. Общие камера, EventSystem, менеджеры и спрайты
`SurvivorArt` сохраняются. Runtime-клипы освобождаются из AM до уничтожения; конфигурации квестов
снимаются с регистрации по экземпляру без удаления сохранённого прогресса. Пулы Survivor завершаются
по точным ссылкам на созданные шаблоны, поэтому чужой пул с таким же именем не затрагивается.
Level demo допускает незаданные события `LevelManager`, не заменяет их и освобождает собственные
ресурсы при неполной настройке.

## Verification

After importing the sample, run `Audit20261023NeoRpgDemoTests` in Edit Mode and
`Audit20261023NeoRpgDemoLifecycleTests` and `Audit20261023NeoRpgDemoSessionTests` in Play Mode,
alongside the existing sample smoke tests. The session fixture covers twenty shell/controller cycles,
native gradient counts, callback teardown, borrowed art/services, audio crossfade cleanup,
runtime definitions, nullable borrowed LevelManager events, saved quest progress, same-name pool
isolation and late returns. Run the regressions against the current sample and target Unity version. The
tests discover the optional assembly at runtime and explicitly skip when it has not been imported; no
test asmdef reference to the hidden release sample is required. Exercise imported scenes separately to
check input, presentation and optional dependencies. Grid and Survivor gameplay currently use legacy
`UnityEngine.Input`; use a compatible Active Input Handling setting when trying these scenes.


