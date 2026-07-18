# Avoiding the no-code layer (and the one case where you don't)

NeoxiderTools has a deliberate **no-code / inspector-wiring layer** for designers who assemble behavior in
the Unity Inspector with `UnityEvent`s and reflection. When you are writing C#, build on the real APIs
instead — the code is clearer, debuggable, refactor-safe, diffable, and testable. This file lists the exact
no-code surface and the code-first replacement for each.

## Why avoid it when coding
- Reflection-based bindings (`NeoCondition`, `NoCode*`) break silently on rename and can't be type-checked.
- `UnityEvent` graphs live in scene/prefab YAML — invisible to code review and hard to diff.
- A `*NoCodeAction` is just a thin wrapper that calls a real method from a UnityEvent; calling the method
  directly is shorter and clearer.

## Avoid by default → use instead

| No-code component / system | What it is | Code-first replacement |
|---|---|---|
| `NeoCondition` (`Neo.Condition`) | inspector-configured value comparison firing `OnTrue`/`OnFalse` UnityEvents | write the actual `if (...)`; subscribe to the source's own event/`ReactiveProperty` |
| `NoCodeBindText` | reflection-binds a float → text | set the text in code, or subscribe to a `ReactiveProperty.OnChanged` |
| `NoCodeFormattedText` | multi-source `String.Format` into a label | build the string in code |
| `SetProgress` | binds a float → `Slider.value`/`Image.fillAmount` | `slider.value = Mathf.InverseLerp(min, max, v)` in code |
| `NoCodeFloatBindingBehaviour` / `ComponentFloatBinding` | base/descriptor for the above | n/a — don't build on it |
| `AbilityNoCodeAction` (`Neo.Abilities`) | enum-driven `Execute()` bridge: cast/grant/revoke/level/modifier/damage/heal from UnityEvents | `caster.TryCast*(...)`, `AbilitySystemBehaviour.I.System.GrantAbility(...)` / `.SetAbilityLevel(...)`, `unit.ApplyDamage(...)` / `.ApplyHeal(...)` |
| `AbilityCooldownSource` (`Neo.Abilities`) | bindable `CooldownNormalized`/`SecondsRemaining` for `SetProgress`/`NoCodeBindText` polling | poll `caster.GetCooldownNormalized(id)` from your own view code |
| `RpgNoCodeAction` | UnityEvent bridge to `RpgCharacter` methods | `character.Damage(...)`, `.Heal(...)`, `.ApplyBuffById(...)` |
| `ProgressionNoCodeAction` | bridge to `ProgressionManager` | `ProgressionManager.I.AddXp(...)`, `.TryBuyPerk(...)` |
| `QuestNoCodeAction` | bridge to `QuestManager` | `QuestManager.I.AcceptQuest(...)`, `.CompleteObjective(...)` |
| `LevelNoCodeAction` | bridge to level component | `levelComponent.AddXp(...)`, `.SetLevel(...)` |
| `StateMachineData` / `StateData` (SO workflow) | inspector-defined states/transitions/actions | subclass `StateMachineBehaviourBase`, or use `StateMachine<T>` in code |
| `UnityLifecycleEvents` | forwards Awake/Start/Update/etc. to UnityEvents | write the corresponding `MonoBehaviour` method |

These mostly live under each module's `Bridge/` or `NoCode/` subfolder, or the `Neo.Condition` /
`Neo.NoCode` / `Neo.StateMachine.NoCode` assemblies — a useful tell when scanning source.

### Also inspector-wiring-primary (Tools & Network)
These are fine to *subscribe to* from code, but they exist to be wired in the Inspector; when writing code,
do the thing directly rather than building a graph around them:

| Component | Code-first instead |
|---|---|
| `EM` (event hub) | subscribe with `EM.I.OnWin.AddListener(...)` / fire via `GM.I.Win()` — fine; just don't treat it as your only control flow |
| `UnityLifecycleEvents` | write the real `Awake/Start/Update` method |
| `PhysicsEvents2D` / `PhysicsEvents3D` | subscribe from code (`pe.TriggerEnterOccurred += ...`) or write `OnTriggerEnter` directly |
| `AnimatorParameterDriver` | call `animator.SetTrigger/SetBool` (or the driver's methods) from code |
| `TextScore`, `StarView` | set the text / toggle stars in code from `ScoreManager.I` |
| `NeoDebugOverlay`, `CameraAspectRatioScaler` | configuration-only; fine as-is, nothing to code |
| `PlayerController2D/3DAnimatorDriver` | drive the Animator from your own controller code if you need logic |
| `NetworkActionRelay`, `NetworkContextActionRelay`, `NetworkEventDispatcher` | for code-first networking write a `NetworkBehaviour` with `[Command]`/`[ClientRpc]`; these relays are zero-code Inspector buses |
| `AbilityAutoCaster` (`Neo.Abilities`) | NOT a thin bridge — real behavior (auto-cast every ready ability, nearest-target lock-on, interval mode, failure backoff). Reusing it from code-first survivor-style games is correct per Rule 1; don't hand-roll a fire-when-ready loop |

## NOT no-code — these are normal code-first components
A component having a `UnityEvent` does not make it no-code. These expose real C# APIs; their events are
**output hooks** you may subscribe to from code with `AddListener(...)`:

`RpgCharacter`, `HealthComponent`, `LevelComponent`, `QuestManager`, `ProgressionManager`, `Money`, `AM`,
`ScoreManager`, `Counter`, `PhysicsEvents2D/3D`, `ReactiveProperty*`, `StateMachineBehaviourBase`, and most
`Tools` components. Use them freely.

## The one case where you DO embrace no-code
If the user is **already** on the no-code path, match their workflow instead of forcing a rewrite. Signs:
- Their scene/prefabs already contain `NeoCondition`, `NoCodeBindText`, `SetProgress`, or a `*NoCodeAction`.
- They explicitly ask to "wire it in the inspector", "no-code", "without scripts", or to use UnityEvents.
- They're following a `Docs/.../NoCode_*` guide.

Since v10 the Abilities quick start is genuinely no-code (`AbilityNoCodeAction` + `AbilityAutoCaster` +
`AbilityCooldownSource` — see `Docs/Abilities/`); when a user asks for a scriptless ability setup, wire
that trio rather than refusing.

In that case, configure those components correctly (read the relevant `Docs/<Module>/...NoCode...md`). This
is uncommon — when in doubt, write code and mention that a no-code option exists if they'd prefer it.
