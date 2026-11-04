# Tools — Misc Internal Types

**What it is:** an index of helper and adapter types that have no page of their own: compatibility layers, pool
helpers and the resource-pool building blocks. Use it to find where such a type lives and which module page
documents its public entry point.

**How to use:** find the type in the tables below (paths are relative to `Assets/Neoxider/Scripts`) and follow the
linked module page for its public entry point.

---

## Tools helper types

| Type | Location | Description |
|------|----------|-------------|
| `TypewriterEffectComponent` | `Tools/Components/` | Typewriter scene wrapper for `TMP_Text`; see [TypewriterEffect](Components/TypewriterEffect.md). |
| `MouseInputCompat` | `Tools/Input/` | Mouse input compatibility layer (Input Manager + Input System); see [Input](Input/README.md). |
| `OptionalInputSystemAdapter` | `Tools/Input/` | Adapter for the optional Input System package. |
| `InteractiveObjectSceneSetup` | `Tools/InteractableObject/Internal/` | Scene setup utility for interactive objects; see [InteractableObject](InteractableObject/README.md). |
| `OptionalInputSystemBridge` | `Tools/Move/` | Input System bridge for movement components; see [Move](Move/README.md). |
| `DirectionUtils` | `Tools/Move/MovementToolkit/` | Direction helpers (Vector2/Vector3 ↔ Direction). |
| `NeoObjectPool` | `Tools/Spawner/` | Flexible object pool; see [NeoObjectPool](Spawner/NeoObjectPool.md). |
| `PoolExtensions` | `Tools/Spawner/` | Pool utility extensions. |
| `DOTweenUIImageFallback` | `Tools/View/` | DOTween UI Image animation fallback. |

## Resource pool types

| Type | Location | Description |
|------|----------|-------------|
| `ResourceEntryData` | `Core/Resources/` | Resource entry data. |
| `ResourceProfileData` | `Core/Resources/` | Resource profile data. |
| `RpgResourceId` | `Core/Resources/` | Resource identifier. |
| `UnityEventFloatFloat` | `Core/Resources/` | `UnityEvent<float, float>` for resource UI. |
| `ResourceEntryInspector` | `Core/Resources/Components/` | Resource entry inspector. |
| `ResourcePoolEntry` | `Core/Resources/Domain/` | Resource pool entry. |
| `ResourcePoolModel` | `Core/Resources/Domain/` | Resource pool model. |
| `IResourcePoolProvider` | `Core/Resources/Interfaces/` | Resource pool provider interface. |

## See also

- [Tools](README.md)
- [Core](../Core/README.md)
