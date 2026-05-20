# SciFiAcademy asset layout

## For designers

Keep scene objects and reusable room pieces here:

- `Assets/_source/Prefabs/Rooms/SciFiAcademy/Modules/` - small reusable room chunks: corridors, doors, tunnel pieces, wall segments, props that are instantiated as GameObjects.
- `Assets/_source/Prefabs/Rooms/SciFiAcademy/Templates/` - complete room/template prefabs assembled from modules.

Requested target paths:

- `Assets/_source/Prefabs/Rooms/SciFiAcademy/Modules/tonel.prefab`
- `Assets/_source/Prefabs/Rooms/SciFiAcademy/Templates/Toonel.prefab`

Do not keep these prefabs under any `Resources` folder unless code loads them by exact `Resources.Load(...)` path. Prefer serialized references, Addressables, or prefab references from ScriptableObject configs.

## ScriptableObject data

Keep data/config assets here:

- `Assets/_source/Settings/Rooms/SciFiAcademy/` - ScriptableObject configs for generation rules, room metadata, spawn tables, difficulty, lighting presets, etc.

Rule of thumb:

- Prefab = GameObject hierarchy with components, meshes, colliders, lights, VFX.
- ScriptableObject = reusable data/config only; no scene hierarchy.
- Resources = only for assets that must be loaded at runtime by string path.
