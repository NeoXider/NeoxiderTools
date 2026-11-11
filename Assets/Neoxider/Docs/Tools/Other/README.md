# Tools / Other

**What it is:** small scene adapters in assembly `Neo.Tools.Other`, under `Scripts/Tools/Other/`: camera shake, value
inversion, legacy NavMesh AI and the optional Spine controller. Each component exposes a C# API and can also be
called from Inspector UnityEvents.

**Navigation:** [← Tools](../README.md)

**Contents:** the per-component table below.

---

## Docs (per-component)

| Page | Description |
|------|-------------|
| [CameraShake](./CameraShake.md), [RevertAmount](./RevertAmount.md) | Common |
| [AiNavigation](./AiNavigation.md), [SpineController](./SpineController.md) | Integrations (`AiNavigation` is deprecated; use `NpcNavigation`) |
| [BillboardUniversal](../View/BillboardUniversal.md) | Lives in the View module (script in `Scripts/Tools/View/`, assembly `Neo.Tools.View`); the old page here only redirects |

`CameraShake` requires DOTween. `AiNavigation` requires a `NavMeshAgent` on a baked NavMesh; new navigation code should use `Neo.NPC.NpcNavigation`. `SpineController` is a global-namespace optional adapter: its animation/skin API is compiled only with `SPINE_UNITY` and the Spine Unity Runtime available. The fallback component disables itself when Spine is absent. `RevertAmount` only forwards `1 - amount` and has no tween or navigation dependency in its own behavior.

For existing scenes, [AiNavigation](./AiNavigation.md) documents its single follow-path scheduler, accepted-destination blockage/arrival events, and synchronous callback stop/replacement behavior. Settled status is observed before automatic refresh, including when every frame exceeds the configured interval; callbacks can cancel the remaining frame work. No scene migration or additional navigation manager is required.

See each component page for code examples, events, lifecycle behavior, and scene setup. Projects using their own asmdefs must reference `Neo.Tools.Other`; integrations also need their installed dependency references.
