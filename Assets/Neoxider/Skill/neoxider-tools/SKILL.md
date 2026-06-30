---
name: neoxider-tools
description: >-
  Build Unity gameplay with the NeoxiderTools package (com.neoxider.tools, namespace `Neo`) instead of
  writing everything from scratch. Use this skill whenever you write Unity C# in a project that contains
  `Assets/Neoxider` or depends on `com.neoxider.tools` — audio, saving, object pooling, reactive values,
  singleton managers, UI fly/animation, timers, RPG/combat, slot/wheel/merge/grid mini-games,
  inventory/shop, parallax, NPC AI, or ANY common utility (random, collections, transforms, string/number
  formatting, coroutines). It has a huge library of ready modules and `Neo.Extensions` helpers — reach for
  them first. Write CODE-FIRST using the package's C# APIs and AVOID the no-code / inspector-wiring layer
  (NeoCondition, `*NoCodeAction` bridges, `NoCode*` binding components) unless the user is already using it.
  Trigger for any substantive Unity coding task in a Neo / NeoxiderTools project, even if the user never
  names the package.
metadata:
  version: 9.6.0
  author: Neoxider
  homepage: https://github.com/NeoXider/NeoxiderTools
---

# NeoxiderTools

NeoxiderTools (`com.neoxider.tools`, root namespace `Neo`, Unity 2022.1+) is a large, production-ready
gameplay/utility toolkit. It is already installed in any project that has `Assets/Neoxider/` or lists
`com.neoxider.tools` in `Packages/manifest.json`. It ships **hundreds of components, managers, and
extension methods** plus a singleton manager layer, an object pool, a reactive-property system, a save
system, and complete game systems (RPG, Cards, Slot, Merge, Grid, Shop, Quest, Progression).

The whole point of this skill: **don't reinvent what the package already does well.** Before you write a
helper, a manager, a coroutine, a random picker, a number formatter, a pool, or a save routine — check
whether NeoxiderTools already has it. It almost always does, and using it makes the code shorter, idiomatic
for this project, and consistent with the rest of the codebase.

## ⛔ STOP — mandatory check before writing ANY component or helper

The **#1 failure mode** with this package is hand-rolling something it already ships — e.g. writing a
custom timer-text `MonoBehaviour` when **`TimeToText`** already exists, or a fixed list of `Slot_0..3`
when a prefab + `HorizontalLayoutGroup` is the idiom. The SKILL.md examples below are **not** the catalog;
the catalog is in the reference files, and **you will miss things if you rely on memory or skim only this
file.**

So this is a **blocking rule, not a suggestion**: the moment you are about to create a new `MonoBehaviour`,
helper method, coroutine, formatter, binder, or "small utility", you must **first open and grep the
matching reference file** for an existing component/API. Map by what you're building:

| About to build… | grep FIRST (don't skip) |
|---|---|
| a UI/gameplay component — timer, bar, counter, list/slot view, mover, follow-cam, picker, score, dialogue, loot… | `references/tools.md` |
| a helper/one-liner — random, shuffle, weighted, format number/time, transform, collection, color, coroutine/delay… | `references/extensions.md` |
| a whole system — rpg, quest, shop, level, grid/merge, slot/wheel, npc, settings, parallax, animation/fly… | `references/game-systems.md` + `references/modules.md` |
| a manager/attribute/`.I` access, or "does Neo have a component for X" | `references/modules.md` |

Grep for the concept (`rg -i "timer\|countdown\|time" references/tools.md`), read the hit, use it. **Only
after the grep genuinely comes up empty** may you write your own — and then say so explicitly ("not in the
package, writing it"). "I didn't check tools.md" is never an acceptable reason for a hand-rolled component.

## The two rules that matter

### Rule 1 — Reach for the package first

When a task maps to something the package covers, use the package API rather than hand-rolling. A few
high-signal examples (full catalogs are in the reference files below):

- Need a sound? `AM.I.Play(...)` — not `AudioSource.PlayClipAtPoint`.
- Need to persist a value? `[SaveField("key")]` on a `SaveableBehaviour` — not raw `PlayerPrefs`.
- Spawning many objects? `PoolManager.Get(prefab, pos, rot)` — not `Instantiate`/`Destroy` churn.
- A value that drives UI when it changes? `ReactivePropertyInt`/`ReactivePropertyFloat` — not manual events.
- Random pick / weighted roll / shuffle? `list.GetRandomElement()`, `weights.GetRandomWeightedIndex()`,
  `list.Shuffle()` — not bespoke `Random.Range` loops.
- Delay / wait-until without writing a coroutine class? `this.Delay(2f, () => ...)`,
  `this.WaitUntil(() => ready, OnReady)`.
- Idle/clicker number like `1.5M`, or a `mm:ss` timer string? `value.ToIdleString()`,
  `seconds.FormatTime(TimeFormat.MinutesSeconds)`.
- A countdown/timer shown on a TMP label? The **`TimeToText`** component (`Neo.Tools`) — `Set(float)` (or
  wire a `UnityEvent<float>` → `Set`); `TimeFormat`, Clock/Compact modes, prefix/suffix, auto-grabs the
  `TMP_Text`. **Never hand-roll a timer-text MonoBehaviour.**
- Auto-wire a serialized reference? `[GetComponent]`, `[FindInScene]`, `[LoadFromResources("...")]`.
- A button in the inspector to test/cheat a method? Put `[Button]` (or `[Button("Label")]`) on the method.
  **It works on ANY MonoBehaviour — including your own scripts in the global namespace** — via the global
  fallback inspector `NeoCustomEditor` (`[CustomEditor(typeof(MonoBehaviour), true, isFallback=true)]`).
  **Do NOT write a custom `Editor` to add buttons** — a custom `[CustomEditor]` overrides the fallback and
  your `[Button]`s disappear. Just `using Neo;` + the attribute.
- A timer, camera shake, follow cam, score, loot table, dialogue, leaderboard, line drawing, mouse
  picking, swipe, weighted chance…? The `Neo.Tools` module almost certainly has it — see
  `references/tools.md` (it's a big catalog). Don't hand-roll a `Timer`/pool/`CameraShake`.
- Multiplayer / co-op / PvP? Use `NetworkSingleton<T>` + `NeoNetworkState`/`NeoNetworkSpawner` (Mirror is
  optional and the package degrades to solo-mode without it). Networking has a critical scene-object
  pitfall — read `references/network.md` before touching `NetworkIdentity` scene objects.

If you are unsure whether the package covers something, **check before writing it** (see "How to discover"
below). Surfacing "NeoxiderTools already has `X` for this" is exactly the value this skill provides.

### Rule 2 — Write code-first; avoid the no-code layer by default

The package has a parallel **no-code / inspector-wiring layer** meant for designers who assemble behavior
in the Unity Inspector via `UnityEvent`s and reflection instead of writing C#. When you are writing code,
**do not** build on that layer — write the equivalent C# directly against the real API. It is clearer,
debuggable, refactor-safe, and version-control-friendly.

**Avoid by default** (these exist mainly to be wired in the Inspector):

- `NeoCondition` (the `Neo.Condition` system) → instead write the actual `if (health.Hp <= 0) ...`.
- `*NoCodeAction` bridges: `RpgNoCodeAction`, `ProgressionNoCodeAction`, `QuestNoCodeAction`,
  `LevelNoCodeAction` → instead call the real method (`RpgCharacter.Damage(...)`,
  `ProgressionManager.I.AddXp(...)`, `QuestManager.I.AcceptQuest(...)`).
- `NoCode*` binding components: `NoCodeBindText`, `NoCodeFormattedText`, `SetProgress`,
  `NoCodeFloatBindingBehaviour` → instead set the value in code or subscribe to a `ReactiveProperty`.
- `StateMachineData` / `StateData` ScriptableObject workflow → instead subclass the C# state machine.
- `UnityLifecycleEvents` (forwards Awake/Start/Update to UnityEvents) → instead write a normal
  `MonoBehaviour` override.

**The exception:** if the user is *already* using the no-code layer (their scene has `NeoCondition` /
`NoCodeBindText` / `*NoCodeAction` components, or they explicitly ask for inspector wiring), then work with
it — that's their chosen workflow. This is rare; default to code.

> Note: a component merely *having* a `UnityEvent` (like `RpgCharacter.OnDeath` or a
> `ReactiveProperty.OnChanged`) does **not** make it no-code. Those are normal code-first components whose
> events are output hooks. Subscribe to them from code with `AddListener(...)`. The avoid-list above is
> specifically the components/systems that exist *primarily* to replace writing code.

## How to discover what's available

1. **Confirm the package is present**: look for `Assets/Neoxider/` or `com.neoxider.tools` in
   `Packages/manifest.json`. If absent, this skill doesn't apply.
2. **Read the docs for a module before using it.** Docs live at `Assets/Neoxider/Docs/<Module>/` (Russian,
   most complete) and `Assets/Neoxider/DocsEn/<Module>/` (English). Any class with a `[NeoDoc("X.md")]`
   attribute points to `Assets/Neoxider/Docs/X.md` — that's its authoritative doc.
3. **Grep the source** under `Assets/Neoxider/Scripts/<Module>/` to confirm exact method signatures before
   calling them. Namespaces are `Neo.<Module>` (e.g. `Neo.Audio`, `Neo.Save`, `Neo.Reactive`,
   `Neo.Extensions`, `Neo.Tools`). Verify the API against the real code — don't guess signatures.
4. **Add components the project's way**: package components carry `[AddComponentMenu("Neoxider/...")]` and
   `[CreateFromMenu("Neoxider/...")]`. Singleton managers are reached via `TypeName.I` (e.g. `AM.I`,
   `SaveManager.I`, `PoolManager.I`).

## Namespaces & assemblies — get the `using` right (most common error)

The single most frequent failure when using this package is a **missing `using` or assembly reference**,
not a wrong API. The root namespace is `Neo`, but most APIs live in a `Neo.<Module>` sub-namespace, and
the package is split into **assembly definitions (asmdef)** — so a type can be visible only after you both
`using` its namespace *and* reference its assembly.

Quick map of the highest-traffic types (verify others by grepping the source for `namespace`):

| API / type | `using` | Assembly (asmdef) |
|------------|---------|-------------------|
| `AM` (audio manager) | `Neo.Audio` | `Neo.Audio` |
| `EM`, `GM`, `Singleton<T>`, `PoolManager`, most of `Neo.Tools` | `Neo.Tools` | `Neo.Tools.*` (per sub-module, e.g. `Neo.Tools.Managers`, `Neo.Tools.Spawner`) |
| `SwipeController`, `SwipeData`, `SwipeDirection` | `Neo.Tools` | `Neo.Tools.Input` |
| `GetRandomElement()`, `Shuffle()`, `ToIdleString()`, all extension helpers | `Neo.Extensions` | `Neo.Extensions` |
| `ReactivePropertyInt/Float/...` | `Neo.Reactive` | `Neo.Reactive` |
| `SaveManager`, `[SaveField]`, `SaveableBehaviour` | `Neo.Save` | `Neo.Save` |
| `LevelManager` | `Neo.Level` | `Neo.Level` |
| `[Button]`, `[GetComponent]`, `[FindInScene]`, inject attributes | `Neo.` (root) | `Neo.PropertyAttribute` |
| `G`, `PM`, `UIPage` (page-navigation facade) | `Neo.Pages` | **none — Sample**, see below |

**asmdef gotcha:** code with **no asmdef** compiles into Unity's predefined `Assembly-CSharp`, which
auto-references every auto-referenced asmdef — so a bare `using Neo.Audio;` just works there. But if your
game code lives in its **own asmdef**, a `using` is not enough: add the module's assembly (e.g. `Neo.Audio`,
`Neo.Tools.Input`, `Neo.Extensions`) to that asmdef's **Assembly Definition References**, or the type stays
unresolved (`CS0246`). When you hit `CS0246`/`CS0103` on a `Neo.*` type, check the namespace first, then the
asmdef reference — not the API name.

## Samples — installed separately via Package Manager

Two pieces ship as **samples** (under `Samples~/`, not compiled until imported). The user installs them from
**Package Manager → NeoxiderTools → Samples → Import**, which copies them to
`Assets/Samples/NeoxiderTools/<version>/<SampleName>/`.

- **NeoxiderPages** — an optional page-navigation module that includes the **`G` static facade**
  (namespace `Neo.Pages`), plus `PM`, `UIPage`, `BtnChangePage`, and UIKit helpers. `G` is the convenient
  entry point many projects use for game flow/events: `G.OnStart` (→ `EM.I.OnGameStart`), `G.OnRestart`,
  `G.Win()`, `G.Lose()`, `G.Pause`, `G.GoMenu()`, etc. It forwards to `EM`/`GM` under the hood. **If you see
  `G.` in project code and it won't resolve, the fix is `using Neo.Pages;` *and* confirming the NeoxiderPages
  sample is imported** — `G` does not exist in the core package, only in this sample. It has no asmdef, so
  once imported it lands in `Assembly-CSharp` and needs no extra assembly reference.
- **Demo Scenes** — sample scenes and integration examples for Condition, Tools, GridSystem, Shop, Merge,
  TicTacToe/Match3, etc. Use these as **reference implementations** to copy idioms from; they are not meant
  to be built on directly. The demo scripts have their own asmdef (`Neo.Rpg.Demo`).

## Reference files — read the one you need

This SKILL.md is the overview. For exhaustive, verified catalogs, open the relevant file (they are dense
reference material — load the one that fits the task, don't read all of them up front):

- **`references/extensions.md`** — the full `Neo.Extensions` catalog (transforms, collections, random,
  strings, color, numbers/time formatting, coroutines, audio fades, layout, screen, pooling helpers). This
  is the highest-leverage file: each one-liner replaces 5–15 lines of hand-rolled code, which directly
  **saves both developer time and tokens** — less code to generate, and less to read back on every later
  edit or review. Default to an extension over hand-written equivalents. Skim it early so you know what
  exists.
- **`references/modules.md`** — module-by-module inventory: managers (and their `.I` access), key
  entry-point components, the property-attribute family (`[Button]`, `[GetComponent]`, inject attributes,
  `[GUIColor]`, `[RequireInterface]`), and which optional deps (Mirror/DOTween/Spine/Odin) gate what.
- **`references/tools.md`** — the `Neo.Tools` module is huge (the package's catch-all). This is the full
  component catalog grouped by category: spawning/pool, score/counters, timers, move/camera, interaction/
  physics, view/UI, text, input, debug, draw, dialogue, leaderboard, chance/loot — with key APIs and
  code-first snippets. Open this for anything gameplay-systems-y that isn't its own module.
- **`references/network.md`** — multiplayer (Mirror-optional). `NetworkSingleton<T>`, `NeoNetworkManager`,
  `NeoNetworkState`, `NeoNetworkSpawner`, the `NetworkReactivePropertyBridge`, lobby, and the critical
  scene-`NetworkIdentity` / `INeoOptionalNetworked` pitfall. Read this for any networked/co-op/PvP work.
- **`references/game-systems.md`** — the gameplay modules that don't have their own deep file: Bonus
  (Slot/Wheel of Fortune), Cards, GridSystem + Merge, NPC AI, Shop, Settings, Animations, Parallax, Level,
  UI/AnimationFly. Purpose + key API + a code-first snippet per module. Open this for slot/card/grid/merge/
  NPC/shop/level/settings work so you don't have to dig through source.
- **`references/idioms.md`** — copy-pasteable code-first snippets for the most-used systems: AM audio,
  Save, Reactive, PoolManager, Singletons/EM, StateMachine, RPG, Quest, Progression — each shown the
  correct (code) way, with the no-code anti-pattern called out.
- **`references/avoid-nocode.md`** — the precise no-code surface to avoid and the code-first equivalent for
  each, plus how to detect that a user is already on the no-code path (the one case where you embrace it).

## Default workflow for a Unity coding task in this project

1. Decide what the task needs (audio? save? spawn? UI value binding? a utility?).
2. For each need, **grep the matching reference file BEFORE writing anything** (see the ⛔ STOP table
   above) — `tools.md` for components, `extensions.md` for helpers, `game-systems.md`/`modules.md` for
   systems. Map the need to a NeoxiderTools API. Only if the grep genuinely comes up empty do you write it
   plainly — and say so, so the user knows it wasn't already in the package.
3. Write **code-first** C# against the real APIs, in the `Neo.*` idiom (singletons via `.I`, `using
   Neo.Extensions;` for helpers, attributes for wiring). Don't build no-code graphs.
4. Verify signatures against the actual source/docs before finalizing. Prefer the package's existing
   component over a new MonoBehaviour when one already does the job.
