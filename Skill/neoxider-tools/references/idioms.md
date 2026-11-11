# NeoxiderTools — code-first idioms

Copy-pasteable canonical usage. All snippets are code-first; the no-code anti-pattern is called out so you
steer away from it. Verify exact signatures against `Assets/Neoxider/Scripts/<Module>/` before shipping.

## Audio — `AM`

One record contract for sounds and music: optional **id**, a **set of clips** (random pick, no immediate
repeat), a **volume multiplier**, an optional **pitch range**. Volume always multiplies:
`channel x entry`. Music entries are **pools**; music changes **crossfade** by default.

```csharp
using Neo.Audio;

// SFX — by id (survives reordering), by index, or a clip directly
AM.I.Play("hit");
AM.I.Play(0);
AM.I.Play("hit", 0.4f);             // entry-volume override, still x the efx channel
AM.I.Play(myClip, 0.5f);

// Music pools — id'd in the AM inspector (menu / gameplay / boss)
AM.I.PlayMusicPool("gameplay");                        // crossfade; no-op if already playing it
AM.I.PlayMusicPool("boss", MusicTransition.Instant);   // hard cut
AM.I.PlayMusicPool("boss", MusicTransition.Fade(2f));
AM.I.NextMusicTrack();                                 // another track of the current pool
AM.I.StopMusic();                                      // fades out; .Instant to cut

// Per-call overrides — one play only, never written back into the entry
AM.I.Play("ui", SoundOptions.Volume(0.6f).WithoutPitch());
AM.I.Play("step", SoundOptions.Clip(stepIndex));       // a specific clip, not a random one
AM.I.PlayMusicPool("boss", MusicOptions.Volume(0.5f).WithFade(2f));

// Channels — the player's volume sliders
AM.I.SetMusicVolume(0.6f);
AM.I.SetEfxVolume(0.9f);                              // = SetChannelVolume(SoundChannel.Sfx, ...)
AM.I.SetChannelVolume(SoundChannel.Ui, 0.5f);         // entries with Channel = Ui (clicks, hovers)
AM.I.Music.FadeOut(1.5f);           // AudioExtensions on the AudioSource

// Optional cues and positional one-shots
AM.I.TryPlay("combo_7");                               // false, silently, if missing or empty
AM.I.PlayAt("explosion", hitPoint);                    // pooled 3D voice, no allocation per call
```

Pool `Mode` is `Loop` by default (the game moves the track, via `NextMusicTrack()` or another pool) or
`Shuffle` (advances by itself at the end of the clip). Pitch randomisation defaults to on for sounds,
off for music.

`AMSettings` persists volumes **and** the Music/Efx/Master/UI mute (`PersistVolume`, `PersistMute`, on by
default) and no longer forces sound back on when it binds; a `SetMusic`/`SetEfx` call in your `Awake` is
kept. Those calls are therefore the player's saved preference — for temporary silence (pause menu, ad,
cutscene) use `AudioListener.pause` or a mixer snapshot.

Avoid: `EnableRandomMusic()` / `SetRandomMusicTracks()` — obsolete; use a `Shuffle` pool with an id.
Avoid: a `SetVolume` → `Play` → `SetVolume` dance — that is what `SoundOptions.Volume` is for.
Avoid: writing a "swap the track list from outside" component — that is what pools replaced.
Avoid: wiring a `PlayAudioBtn`/UnityEvent in the inspector when you're writing code.

## Save — `[SaveField]` + `SaveableBehaviour`
```csharp
using Neo.Save;

public class PlayerData : SaveableBehaviour
{
    [SaveField("player_coins")] private int _coins;       // auto-load on awake, auto-save on quit
    [SaveField("player_hp", autoLoadOnAwake: false)] private float _hp = 100f;

    public override void OnDataLoaded() => RefreshUI();   // called after restore
}

// manual:
SaveManager.Save();                 // all registered
SaveManager.Save(myComponent);
SaveManager.Load(myComponent);

// 64-bit values (money in cents, big counters) — no long<->string helpers:
SaveProvider.SetLong("wallet_cents", cents);
long stored = SaveProvider.GetLong("wallet_cents");   // also reads values written with SetInt
SaveProvider.Save();                // cheap with FileSaveProvider: an unchanged flush skips the disk write
```
Note: `[SaveField]` must be on a real field (not an auto-property). Avoid raw `PlayerPrefs`.
`FileSaveProvider.HasUnsavedChanges` / `FilePath` show the pending state and the file location.

## Reactive properties
```csharp
using Neo.Reactive;

[SerializeField] private ReactivePropertyInt _score = new(0);
[SerializeField] private ReactivePropertyFloat _health = new(100f);

void Start() {
    _score.AddListener(v => scoreText.text = v.ToString());     // subscribe in CODE
    _health.AddListener(v => healthBar.fillAmount = v / 100f);
}

_score.Value += 10;                 // subscribers notified
_health.OnNext(75f);                // == .Value = 75f
_health.SetValueWithoutNotify(100f);// silent (e.g. on load)
_health.ForceNotify();
ReactiveProperty<bool> flag = new ReactiveProperty<bool>(false);   // non-serialized generic
```
`OnChanged` is the inspector-facing `UnityEvent<T>`; from code use `AddListener`. Don't add a
`NoCodeBindText` to push the value — set it in code. Nested notifications do not allocate; weak
subscriptions whose Unity target was destroyed are removed silently.

## Object pooling — `SpawnUtility` / `PoolManager`
```csharp
using Neo.Tools;

GameObject bullet = SpawnUtility.Spawn(bulletPrefab, pos, Quaternion.identity);  // works without a PoolManager
GameObject b2     = PoolManager.Get(bulletPrefab, pos, Quaternion.identity, parent);
SpawnUtility.Despawn(bullet);       // or PoolManager.Release(b2), bullet.ReturnToPool(), a Despawner

public class Bullet : PoolableBehaviour {           // or implement IPoolable
    public override void OnPoolGet()     { /* reset state; the pool already activated the object */ }
    public override void OnPoolRelease() { /* stop FX */ }
}
```
Pools are created on first `Get()`. Warm get/return is O(1) and allocation-free; a double or foreign return
is ignored. Avoid `Instantiate`/`Destroy` churn for frequently spawned objects.

## Singletons & event hub — `GM` / `EM`
```csharp
using Neo.Tools;

GM.I.State = GM.GameState.Game;
bool playing = GM.I.IsPlaying;
if (GM.HasInstance) { /* safe */ }
if (GM.TryGetInstance(out GM gm)) { /* ... */ }

EM.GameStart();                                  // static shortcut
EM.I.OnWin.AddListener(ShowWinScreen);
EM.I.OnStateChange.AddListener(s => Debug.Log(s));
```

## State machine (code-first)
```csharp
using Neo.StateMachine;

public sealed class IdleState : IState
{
    public void OnEnter() { }
    public void OnUpdate() { }
    public void OnFixedUpdate() { }
    public void OnLateUpdate() { }
    public void OnExit() { }
}

StateMachine<IState> sm = new StateMachine<IState>();
StateTransition transition = new StateTransition { FromStateType = typeof(IdleState), ToStateType = typeof(AttackState) };
transition.AddPredicate(new CustomPredicate());   // attach a StatePredicate; see Docs/StateMachine for BoolPredicate/FloatComparisonPredicate/etc.
sm.RegisterTransition(transition);
sm.ChangeState<IdleState>();
sm.Update();                  // CurrentState.OnUpdate()
sm.EvaluateTransitions();     // auto-fire
sm.OnStateChanged.AddListener((prev, next) => Debug.Log($"{prev}->{next}"));
```
On a GameObject, subclass the generic wrapper (`public class EnemyBrain : StateMachineBehaviour<IState>`) —
it ticks Update/FixedUpdate/LateUpdate for the active state without allocating. Avoid the
`StateMachineData`/`StateData` ScriptableObject no-code workflow when coding. If a scene already uses it,
the wrappers accept `ChangeState(StateData)` from code; a literal `ChangeState(null)` is now ambiguous
(cast it: `ChangeState((string)null)`).

## Abilities — `Neo.Abilities` (v10 combat; see abilities.md for the full module)
```csharp
using Neo.Abilities;

// Scene: unit = AbilityUnitBehaviour (+ UnitTemplate asset), caster = AbilityCasterBehaviour.
caster.TryCast("frost_nova");                   // no-target / self
caster.TryCastAtUnit("firebolt", targetUnit);   // unit-targeted
caster.TryCastAtPoint("fireball", groundPoint); // point-targeted
CastFailureReason lastFailure = caster.LastCastResult.Failure;   // typed reason, no string parsing
cooldownImage.fillAmount = caster.GetCooldownNormalized("fireball");

unit.OnDamaged.AddListener(dmg => FlashHit(dmg));    // per-unit receipts
unit.OnDied.AddListener(HandleDeath);
float speed = unit.Unit.GetProperty("move_speed");   // aggregated base->add->mul->max
bool stunned = unit.Unit.HasState("stunned");

AbilitySystem system = AbilitySystemBehaviour.I.System;   // hub singleton, auto-creates
system.GrantAbility(unit.UnitId, "ember");           // runtime grant (upgrades)
system.SetAbilityLevel(unit.UnitId, "ember", 2);     // leveled values scale
system.Events.SubscribeAny(e => Log(e.EventId, e.Target, e.Amount)); // global receipt stream
```
Content (abilities/modifiers/units) is authored as ScriptableObjects, not code — only wire casts and
receipts in C#. Avoid `AbilityNoCodeAction`/`AbilityCooldownSource` when coding (call
`TryCast*`/`GetCooldownNormalized` directly); `AbilityAutoCaster` is real logic — reuse it for
survivor-style auto-fire instead of hand-rolling a fire-when-ready loop.

## RPG — `RpgCharacter` (LEGACY — superseded by `Neo.Abilities`; existing Rpg scenes only)
```csharp
using Neo.Core.Resources;   // RpgResourceId
using Neo.Rpg;              // RpgDamageInfo, IRpgDamageModifier
using Neo.Rpg.Components;   // RpgCharacter

RpgCharacter c = enemy.GetComponent<RpgCharacter>();   // one per enemy/player; not a singleton
c.Damage(25f);  c.Heal(10f);
c.ApplyDamage(new RpgDamageInfo(25f, source: attacker));   // typed hit with a source (attacker: IRpgCombatReceiver)
c.DamageApplied += (info, applied) => ShowHitNumber(applied);
c.AddDamageModifier(shield);    // IRpgDamageModifier.ModifyIncomingDamage(target, info, amount): shields, armor, caps
c.Spend(RpgResourceId.Mana, 20f);  c.ApplyBuffById("Regen");
c.AddXp(100f);  c.SetLevel(5);
float hp = c.GetResource(RpgResourceId.Hp);
c.OnDeathEvent.AddListener(HandleDeath);        // output event — fine from code
```
`AttackSpeedPercent` buffs feed `c.GetAttackSpeedMultiplier()`; `RpgAttackController.ScaleCooldownByAttackSpeed`
(off by default) applies it to new cooldowns. Pooled legacy hitboxes route to a receiver with
`RpgStatsDamageableBridge.Initialize(receiver)` / `ClearReceiver()`. Avoid `RpgNoCodeAction` (it just calls
these methods from a UnityEvent). Never start NEW combat on `Neo.Rpg` — use the Abilities idiom above.

## Quest / Progression (singletons)
```csharp
using System.Collections.Generic;
using Neo.Quest; using Neo.Progression;

if (!QuestManager.I.TryAcceptQuest(questConfig, out string why)) Debug.Log(why);
QuestManager.I.NotifyKill("wolf");                     // credits every matching KillCount objective, zero GC
QuestManager.I.NotifyCollect("herb", 3);               // stack pickup in one call
QuestManager.I.NotifyReachPoint("ruins");  QuestManager.I.NotifyTalk("elder");
QuestManager.I.AddObjectiveProgress(questConfig, 0, 2); // by index, clamped to RequiredCount
QuestManager.I.QuestCompleted += quest => GiveReward(quest);
QuestManager.I.GetActiveQuests(_activeBuffer);         // List<QuestState>, reused

ProgressionManager.I.AddXp(50);
ProgressionManager.I.TryBuyPerk("fast_attack", out string err);
ProgressionManager.I.NodeUnlocked += nodeId => RefreshTree();
```
An empty or null target id credits nothing (credit such objectives by index). If a progress or completion
listener throws, the credit still finishes and the completion events still fire before the exception is
rethrown. `QuestConfig.ValidateDefinition()` lists authoring problems. Avoid `QuestNoCodeAction` / `ProgressionNoCodeAction` in code — call the manager directly.

## Health / resources
```csharp
using Neo.Core.Resources;

HealthComponent health = player.GetComponent<HealthComponent>();
health.Decrease(RpgResourceId.Hp, 15f);
if (health.IsDepleted(RpgResourceId.Hp)) HandleDeath();
```
Avoid putting a `NeoCondition` "Hp <= 0 → GameOver" on the object; write the `if` in code.

## Time seams — testable timers and cooldowns
```csharp
using System;
using Neo.Tools;

ManualFrameClock frames = new ManualFrameClock(unscaledDeltaTime: 0.1f);
Timer cooldown = new Timer(1f, updateInterval: 0f) { FrameClock = frames };   // default: UnityFrameClock.Instance
cooldown.Start();                   // each frame now advances exactly 0.1 s

public sealed class DailyGate       // a rule with an injectable UTC clock
{
    private readonly INeoClock _clock;
    public DailyGate(INeoClock clock = null) { _clock = clock ?? NeoTrustedTime.AsClock; }
    public bool IsOpen(DateTime lastClaimUtc) => (_clock.UtcNow - lastClaimUtc).TotalHours >= 24d;
}
```
`TimerObject.FrameClock` / `UtcClock`, `Bonus.CooldownSchedule(cooldownSeconds, clock: ...)` and the
animators' `ManualUpdate` + `Tick(dt)` take the same seams. Guide: `Docs/Tools/Time/Clocks.md`.

## Extensions one-liners (see extensions.md for the full set)
```csharp
using Neo.Extensions;

Enemy nearest = enemies.FindClosest(transform.position);
enemies.SetActiveAll(false);
Enemy pick = enemyList.GetRandomElement();
int i = weights.GetRandomWeightedIndex();
System.Random rng = new System.Random(dailySeed);
deck.Shuffle(rng);                            // reproducible; also GetRandomElement(rng), GetRandomWeightedIndex(rng)
transform.DestroyChildren();
transform.SetPosition(y: 0f);                 // only Y
transform.LookAt2D(target.position);
children.ArrangeInCircle(center, radius: 3f);
string s = score.ToString().Bold().SetColor(Color.yellow);
string big = 1500000.ToIdleString();          // "1.5M"
string t = timeLeft.FormatTime(TimeFormat.MinutesSeconds);  // "01:45"
this.Delay(2f, SpawnEnemy);
this.WaitUntil(() => enemy.IsDead, OnEnemyDied);
```
