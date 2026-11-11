# AbilityCuePlayer

**What it is:** a scene component that plays the presentation cues named in ability data. An ability names a cue in
**Cast Cue** and **Impact Cue**; the player turns the name into a pooled effect prefab and a UnityEvent. Gameplay data
stays free of prefabs, and effects and sounds need no code. `Scripts/Abilities/Components/AbilityCuePlayer.cs`,
namespace `Neo.Abilities`.

**How to use:**

1. Add `AbilityCuePlayer` to a scene object, for example next to `AbilitySystemBehaviour`.
2. Add a cue: an **Id**, an effect **Prefab** (or an inactive scene template) and, if you want, listeners in
   **On Played** (a `PlayAudio.AudioPlay`, a camera shake).
3. Write the id into **Cast Cue** or **Impact Cue** of an ability.

The effect prefab returns to the pool by itself: give it a `Despawner` with **Despawn After Seconds**.

---

## When a cue plays

| Cue field of the ability | Moment | Position |
|--------------------------|--------|----------|
| **Cast Cue** | The ability is cast (`ability_cast`). | The caster. |
| **Impact Cue** | The ability damages a unit (`take_damage`): every hop of a chain, every pulse of a zone, every hit of a projectile. | The damaged unit. |

For a projectile or a zone the damage comes from the payload ability, so put **Impact Cue** on the payload.

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Cues** | The list: **Id**, **Prefab** (optional) and **On Played** (`UnityEvent`). |
| **Max Spawns Per Frame** | Most effect prefabs spawned in one frame, so a blast that hits fifty units does not spawn fifty effects. **On Played** is still raised for every play. `0` = no limit. |

## API

| Member | Description |
|--------|-------------|
| `bool PlayAt(string cueId, Vector3 position)` | Plays a cue outside ability casts. Returns false for an unknown id. |
| `void SetCue(string id, GameObject prefab)` | Adds a cue at runtime, replacing one with the same id. |
| `void Subscribe()` | Connects to the scene ability hub. Called on Start and on re-enable. |
| `IReadOnlyList<Cue> Cues`, `int MaxSpawnsPerFrame` | The configured cues and the per-frame limit. |

## Example

A chain lightning ability with **Impact Cue** `zap`, and a cue `zap` whose prefab is a small electric burst: every unit
the chain reaches gets its own flash, although the ability data never mentions a prefab. The Survivor demo uses three
cues this way (`frost`, `zap`, `hit`); see [SurvivorDemo](./SurvivorDemo.md).

## See also

- [AbilityDefinition](./AbilityDefinition.md), [AbilitySystemBehaviour](./AbilitySystemBehaviour.md)
- [Despawner](../Tools/Spawner/Despawner.md), [PlayAudio](../Audio/PlayAudio.md)

---

**По-русски:** компонент проигрывает «подсказки» из данных способности. В способности указывается имя в **Cast Cue**
(при касте, на кастере) и **Impact Cue** (при каждом нанесении урона, на цели); здесь имени сопоставляется префаб
эффекта из пула и событие. Так эффекты и звуки добавляются без кода, а данные способности не ссылаются на префабы.
