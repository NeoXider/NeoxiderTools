# NeoCharacterNetworkBinding

**What it is:** A `MonoBehaviour` (a `NetworkBehaviour` when Mirror is installed) that makes a CMF character prefab multiplayer-ready: only the local player simulates and reads input, every remote copy becomes a passive proxy driven by `NetworkTransform`. `Scripts/Tools/Move/CharacterController/NeoCharacterNetworkBinding.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add it to the character root next to `AdvancedWalkerController`. With Mirror installed it pulls in `NetworkIdentity` and `NetworkTransformUnreliable` via `[RequireComponent]`.
2. Assign `Camera Rig` — the character's camera object. Leaving it empty means every client keeps every player's camera alive.
3. Add any other local-only objects (audio listener, HUD, name-tag hiding) to `Local Only Objects`.
4. Register the prefab as the Player Prefab on your `NetworkManager` as usual.

---

## Why it is needed

CMF is a single-player controller. It has no concept of authority, so every instance runs its motor in `FixedUpdate` — each client would locally simulate *all* players and fight the incoming snapshots, producing rubber-banding on every remote character.

This component splits the prefab in two:

- **Local player:** `Controller`, `Mover` and `NeoCharacterInput` enabled, camera rig active, Rigidbody dynamic. Simulates normally and reports its transform.
- **Remote proxies:** those components disabled, camera rig inactive, Rigidbody kinematic. Position and rotation come purely from `NetworkTransform` snapshots.

Without Mirror the component compiles to a plain `MonoBehaviour` whose `HasInputAuthority` is always `true`, so the same prefab still works in a single-player project.

---

## Fields

### Local player only

| Field | Type | Purpose |
|-------|------|---------|
| `Controller` | `CMF.Controller` | Auto-found. Disabled on remote proxies. |
| `Mover` | `CMF.Mover` | Auto-found. Disabled on remote proxies. |
| `Character Input` | `NeoCharacterInput` | Auto-found. Disabled on remote proxies. |
| `Camera Rig` | `GameObject` | Deactivated on remote proxies. |
| `Local Only Objects` | `GameObject[]` | Extra objects active only for the local player. |

### Physics

| Field | Type | Purpose |
|-------|------|---------|
| `Kinematic On Remote` | `bool` | Make the Rigidbody kinematic on proxies so local physics does not fight snapshots. On by default. |
| `Rigidbody` | `Rigidbody` | Auto-found. |

## Properties

| Property | Type | Meaning |
|----------|------|---------|
| `HasInputAuthority` | `bool` | `true` for the local player, or for any instance without Mirror / outside an active network session. |

## Methods

| Method | Returns | Description |
|--------|---------|-------------|
| `ApplyAuthority()` | `void` | Re-applies the local/remote split. Called in `Start`, `OnStartClient` and `OnStartLocalPlayer`. Call it manually after enabling components at runtime (respawn, possession). |

---

## What is configured automatically

In `Awake` (before `NetworkTransform.Awake`, via `[DefaultExecutionOrder(-100)]`):

- `NetworkTransformBase.target` is set to this transform. A wrong child target in the Inspector silently breaks replication, and the character always moves its own root.
- `syncDirection` is set to `ClientToServer`.

## Authority model and its limits

Client authority: each client reports its own position. That is the standard trade-off for responsive movement, and the same model the legacy `PlayerController3DPhysics` used.

It is **not** cheat-resistant — a modified client can report any position. Validate server-side if your game needs that.

Physics interactions between two player characters are also only locally correct: each client simulates itself and sees the others as kinematic proxies, so a push resolves differently on each machine. For authoritative contact between players you need server-side simulation, which is beyond what `NetworkTransform` provides.

## Known gap: animation on remote proxies

CMF's `AnimationControl` reads velocity from the `Controller`, and the controller is disabled on proxies — so remote characters move to the right place but play their idle animation while doing it.

Two ways to close it, depending on what your game needs:

- **Derive velocity from the transform.** Track the proxy's position delta per frame and drive the `Animator` parameters from it. Cheap, no extra bandwidth, and enough for locomotion blend trees.
- **Sync the animator.** Add Mirror's `NetworkAnimator` to the character and leave the `Animator` enabled on proxies. Exact, but it sends animator state over the wire.

`AnimationControl` is left enabled on proxies either way — this component only disables the motor, input and camera.

## See also

- [CharacterController overview](./README.md)
- [Multiplayer Guide](../../../Network/Multiplayer_Guide.md)
