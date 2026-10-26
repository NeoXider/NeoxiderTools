# Tools / Managers

Managers and singleton patterns used by runtime systems. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Managers`. Scripts are located in `Scripts/Tools/Managers/`.

## Components and C# APIs

| Page | Description |
|------|-------------|
| [Bootstrap](./Bootstrap.md) | Ordered initialization and runtime registration |
| [EM](./EM.md) | Global gameplay and application event hub |
| [GM](./GM.md) | Global game state manager |
| [Singleton](./Singleton.md) | Generic singleton base class and safe access patterns |
| [SingletonById](./SingletonById.md) | Separate singleton instances keyed by a configured string Id |

## Choosing an entry point

- Use `Bootstrap.Register(IInit)` for ordered startup of plain C# services or scene components. Manual Inspector references provide explicit startup dependencies; automatic scene discovery is optional.
- Use `GM` methods to change gameplay state and pause time. Wire scene buttons to `GM.Pause()` / `GM.Resume()` when they should affect gameplay.
- Use the read-only `GM.TransitionVersion` with a captured manager reference when a C# caller owns one exact pause transition. GM also uses this version to stop obsolete broadcasts after a callback changes state; [PausePage](../../UI/PausePage.md) supplies shared scene-page ownership.
- Use `EM` to subscribe to or broadcast events. Its helpers only broadcast; calling `EM.Pause()` does not change `GM.State` or `Time.timeScale`.
- Prefer `TryGetInstance(out T)` for optional manager dependencies. `I` / `Instance` can scan inactive objects and initialize or create a manager.

Inspector actions on `GM` and `EM` are available in Play Mode. Their public C# methods remain callable directly. These components use normal typed APIs and `UnityEvent` hooks; no additional NoCode bridge is required.

When application code has its own assembly definition, reference `Neo.Tools.Managers` in addition to adding `using Neo.Tools;`.

## See also

- [Tools/Components](../Components/README.md)
