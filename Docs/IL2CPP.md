# IL2CPP and Code Stripping

**What it is:** a heads-up about IL2CPP managed code stripping and NeoxiderTools' reflection-based NoCode bindings — read this before shipping an IL2CPP build (mobile, consoles, most release builds) if you use `NeoCondition`, `ComponentFloatBinding`, `[SaveField]`, `NetworkPropertySync`, or `NetworkReactiveSync`.

**How to use:** scripts compiled into `Assembly-CSharp` are already covered by the package `link.xml`; if your
gameplay code has its own asmdef, add that assembly to a project `link.xml` as shown in
[If your scripts are in a custom asmdef](#if-your-scripts-are-in-a-custom-asmdef).

---

## The problem

Several NoCode components resolve a **member by name at runtime** via reflection, on **your** MonoBehaviour types:

| Component | Reflects over |
|-----------|----------------|
| `NeoCondition` / `ConditionValueSource` | Fields, properties, and single-argument methods on any target object |
| `ComponentFloatBinding` | A named field/property on any target component |
| `SaveManager` (`[SaveField]`) | Every field marked `[SaveField]` on a registered component |
| `NetworkPropertySync` | A named field/property to sync over the network |
| `NetworkReactiveSync` | A named `ReactiveProperty*` field to replicate |

This works flawlessly in the Editor (Mono) because nothing is stripped there. Under **IL2CPP**, Unity's linker only keeps code and members it can prove are reachable through a normal call graph. A private field that is *only* ever touched via `GetField(name, BindingFlags.NonPublic | ...)` isn't reachable by that analysis — the linker can legally remove it, and the reflection call then silently returns `null` at runtime. The feature looks broken in a release build even though it worked perfectly in the Editor.

Fields marked `[SerializeField]` (or public fields) are generally safe — Unity's own serialization system needs them, so the stripper already special-cases them. The risk is specifically **private, non-serialized fields** bound purely through one of the components above.

## The fix that ships with the package

`Assets/Neoxider/link.xml` preserves:
- The package assemblies that hold the reflection helpers: `Neo.Condition`, `Neo.NoCode`, `Neo.Save`, `Neo.Network`. The file also lists a `Neo` entry from an older layout; no current assembly has that name.
- `Assembly-CSharp` — the default compile target for scripts that aren't in a custom asmdef.

If your gameplay scripts live directly in `Assets/Scripts` (no custom `.asmdef`), this link.xml already covers you — no action needed.

## If your scripts are in a custom asmdef

Unity only auto-discovers `link.xml` files under `Assets/`, and ours only lists `Assembly-CSharp` by name. If your project's gameplay code compiles into its own assembly (e.g. `MyGame.Runtime`), add it to your own project-level `link.xml`:

```xml
<linker>
  <assembly fullname="MyGame.Runtime" preserve="all" />
</linker>
```

Any `link.xml` under `Assets/` is picked up automatically by the IL2CPP build pipeline — you don't need to reference it anywhere.

## Alternative / additional safety nets

- **Prefer `[SerializeField]`** for fields you bind via NoCode when practical — they're preserved by Unity's own stripping exemptions regardless of link.xml.
- **Lower Managed Stripping Level** (`Project Settings → Player → Optimization`) to `Minimal` if you can't use link.xml for some reason — trades a larger build for fewer surprises.
- **Test an IL2CPP build early**, not just at the end of a project — this class of bug only shows up outside the Editor.

## Optional Input System adapter

`Neo.Tools.Input` reads the Input System package without a compile-time reference: `OptionalInputSystemAdapter` finds `Keyboard`, `Mouse`, `Gamepad` and `Touchscreen` by type name and binds their `current` properties and control getters to delegates. Movement, interaction, drawing and the grid demos read input through it (`MouseInputCompat`, `KeyInputCompat`).

- The Input System package is a dependency of NeoxiderTools and the members the adapter uses are public, but the adapter's IL2CPP behaviour has not been measured yet. If a control reports "unavailable" only in a player build with **High** managed stripping, preserve the Input System assembly in your project's `link.xml`:

```xml
<linker>
  <assembly fullname="Unity.InputSystem" preserve="all" />
</linker>
```

- The allocation-free polling guarantee is measured on the Mono scripting backend only. On IL2CPP a control lookup goes through a reflected getter call and can allocate; this affects cost, not correctness.

## Verification status

No IL2CPP player build is part of the recorded package verification ([Verification 11](../Verification11.md)). Treat the guidance above as the supported setup and confirm it in your own target build.
