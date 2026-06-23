# NeoxiderTools — code-first idioms

Copy-pasteable canonical usage. All snippets are code-first; the no-code anti-pattern is called out so you
steer away from it. Verify exact signatures against `Assets/Neoxider/Scripts/<Module>/` before shipping.

## Audio — `AM`
```csharp
using Neo.Audio;

AM.I.Play(0);                       // SFX by index in AM's _sounds[]
AM.I.Play(myClip, 0.5f);            // direct clip + volume
AM.I.Play(myClip);                  // direct clip, default volume
AM.I.PlayMusic(0);                  // music by index in _musicClips[]
AM.I.PlayMusicByClip(myTrack, 0.8f);
AM.I.SetMusicVolume(0.6f);
AM.I.SetEfxVolume(0.9f);
AM.I.EnableRandomMusic();  AM.I.DisableRandomMusic();
AM.I.Music.FadeOut(1.5f);           // AudioExtensions on the AudioSource
```
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
```
Note: `[SaveField]` must be on a real field (not an auto-property). Avoid raw `PlayerPrefs`.

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
var flag = new ReactiveProperty<bool>(false);   // non-serialized generic
```
`OnChanged` is the inspector-facing `UnityEvent<T>`; from code use `AddListener`. Don't add a
`NoCodeBindText` to push the value — set it in code.

## Object pooling — `PoolManager`
```csharp
using Neo.Tools;

GameObject bullet = PoolManager.Get(bulletPrefab, pos, Quaternion.identity);
GameObject b2     = PoolManager.Get(bulletPrefab, pos, Quaternion.identity, parent);
// return via Despawner component, or PooledObjectInfo.OwnerPool.Release(instance)

public class Bullet : PoolableBehaviour {           // or implement IPoolable
    public override void OnPoolGet()     => gameObject.SetActive(true);
    public override void OnPoolRelease() { /* stop FX */ }
}
```
Pools are created on first `Get()`. Avoid `Instantiate`/`Destroy` churn for frequently spawned objects.

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

var sm = new StateMachine<IState>();
var transition = new StateTransition { FromStateType = typeof(IdleState), ToStateType = typeof(AttackState) };
transition.AddPredicate(new CustomPredicate());   // attach a StatePredicate; see Docs/StateMachine for BoolPredicate/FloatComparisonPredicate/etc.
sm.RegisterTransition(transition);
sm.ChangeState<IdleState>();
sm.Update();                  // CurrentState.OnUpdate()
sm.EvaluateTransitions();     // auto-fire
sm.OnStateChanged.AddListener((prev, next) => Debug.Log($"{prev}->{next}"));
```
Avoid the `StateMachineData`/`StateData` ScriptableObject no-code workflow when coding; subclass
`StateMachineBehaviourBase` (override `OnEnter`/`OnUpdate`/`OnExit`/`CheckTransition`) or use the generic
`StateMachine<T>` above.

## RPG — `RpgCharacter` (one per enemy/player; not a singleton)
```csharp
using Neo.Rpg;

var c = enemy.GetComponent<RpgCharacter>();
c.Damage(25f);  c.Heal(10f);
c.Spend("Mana", 20f);  c.ApplyBuffById("Regen");
c.AddXp(100f);  c.SetLevel(5);
float hp = c.GetResource("Hp");
c.OnDeathEvent.AddListener(HandleDeath);        // output event — fine from code
```
Avoid `RpgNoCodeAction` (it just calls these methods from a UnityEvent).

## Quest / Progression (singletons)
```csharp
using Neo.Quest; using Neo.Progression;

QuestManager.I.AcceptQuest(questConfig);
QuestManager.I.CompleteObjective(questConfig, objectiveIndex: 0);
QuestManager.I.OnQuestCompleted.AddListener(id => Debug.Log($"done {id}"));

ProgressionManager.I.AddXp(50);
ProgressionManager.I.TryBuyPerk("fast_attack", out string err);
```
Avoid `QuestNoCodeAction` / `ProgressionNoCodeAction` in code — call the manager directly.

## Health / resources
```csharp
using Neo.Core.Resources;

var health = player.GetComponent<HealthComponent>();
health.Decrease(RpgResourceId.Hp, 15f);
if (health.IsDepleted(RpgResourceId.Hp)) HandleDeath();
```
Avoid putting a `NeoCondition` "Hp <= 0 → GameOver" on the object; write the `if` in code.

## Extensions one-liners (see extensions.md for the full set)
```csharp
using Neo.Extensions;

Enemy nearest = enemies.FindClosest(transform.position);
enemies.SetActiveAll(false);
var pick = list.GetRandomElement();
int i = weights.GetRandomWeightedIndex();
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
