# Tools / Draw

`Neo.Tools.Draw` provides `Neo.Tools.Drawer`: a scene component for drawing world-space polylines, optional 2D edge colliders, smoothing, and pooled line reuse. Scripts live in `Scripts/Tools/Draw/`.

Use `BeginLine`, `AppendPoint`, and `EndLine` from C#, or enable the component's mouse input in the Inspector. The geometry helpers can also be called without creating a component. See [Drawer](./Drawer.md) for setup, API contracts, events, materials, pooling, and lifecycle details.

Reset/start callbacks may create a replacement stroke. Pooled lines capture owner/generation so external return and reuse cannot leave the former Drawer controlling the new rental. Each Drawer retires only its unique automatic source; assigned prefab pools remain reusable. See [pool cleanup](../Spawner/PooledObjectInfo.md) for the shared lifecycle contract.

Custom assemblies using this module must reference `Neo.Tools.Draw`. The component uses `Neo.Reactive`, `Neo.Extensions`, `Neo.PropertyAttribute`, `Neo.Tools.Spawner`, and `Neo.Tools.Managers`; no separate Editor assembly is required.
