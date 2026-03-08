# SaveIdentityUtility

## Overview
`SaveIdentityUtility` is the helper responsible for building stable save identities for scene components.

- **Namespace**: `Neo.Save`
- **Path**: `Assets/Neoxider/Scripts/Save/SaveIdentityUtility.cs`

It is mainly used internally by `SaveManager`, but it is also useful in custom save tooling or debugging workflows.

## Public API
- `string GetComponentKey(MonoBehaviour monoBehaviour)`
- `string GetStableIdentity(MonoBehaviour monoBehaviour)`

## Identity resolution rules
1. If the component implements [`ISaveIdentityProvider`](./ISaveIdentityProvider.md) and returns a non-empty `SaveIdentity`, that value is used.
2. Otherwise the utility builds a scene-based identity from:
   - scene path or scene name;
   - hierarchy path;
   - sibling index for every transform level;
   - index of the component among components of the same type on the GameObject.

`GetComponentKey()` then prefixes that identity with the component full type name.

## Why it exists
`GetInstanceID()` is not stable across sessions, scene reloads, or application restarts. A scene-based identity makes component persistence far more predictable for package users.

## Limitations
- If the object moves to another place in the hierarchy, the identity changes.
- If the order of same-type components on the GameObject changes, the index changes.
- Dynamically spawned objects usually need a custom identity via `ISaveIdentityProvider`.

## Example key
```text
MyGame.PlayerStats:Assets/Scenes/Main.unity:Root#0/Player#1:0
```

## See also
- [`ISaveIdentityProvider`](./ISaveIdentityProvider.md)
- [`SaveManager`](./SaveManager.md)
- [`README`](./README.md)
