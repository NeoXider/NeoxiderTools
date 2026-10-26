# Network contracts

`Neo.Network.Contracts` contains the shared declarations in namespace `Neo.Network`. Its two types are always available, with or without Mirror. The assembly has no explicit assembly references, and these declarations use no Unity or Mirror APIs. A custom gameplay assembly can reference `Neo.Network.Contracts` without referencing the networking implementation.

Source: `Scripts/NetworkContracts/`. For runtime components and setup, start with the [Network entry point](README.md).

## Public C# API

| Type | Contract |
|------|----------|
| `INeoOptionalNetworked` | Read-only `bool IsNetworked { get; }`: whether the component is configured for networking. |
| `NetworkAuthorityMode` | Serialized policy selected by network action consumers: `None = 0`, `OwnerOnly = 1`, `ServerOnly = 2`. |

`IsNetworked` describes configuration, not whether a server or client is currently connected. Read session state through `NeoNetworkState` in the separate `Neo.Network` assembly. The interface itself does not enable objects, synchronize data, or own resources. Implementations may expose the property explicitly, so read it through an `INeoOptionalNetworked` reference.

For example, inside a `MonoBehaviour` with a [NetworkActionRelay](NetworkActionRelay.md) on the same object (this example also requires `Neo.Network`):

```csharp
using Neo.Network;

// Configure through the component's existing public API.
NetworkActionRelay relay = GetComponent<NetworkActionRelay>();
if (relay != null)
{
    relay.isNetworked = false;
    relay.AuthorityMode = NetworkAuthorityMode.OwnerOnly;

    INeoOptionalNetworked optional = relay;
    bool configuredForNetworking = optional.IsNetworked; // false, even if a session exists
}
```

Custom components can implement the interface directly, including ordinary C# objects. Scene reactivation discovers implementations on Unity components in the identity's hierarchy; a standalone C# implementation is not discovered in a scene. Keep the getter inexpensive and free of side effects because both scene-processing layers query it.

## Offline scene / Inspector workflow

Mirror disables scene `NetworkIdentity` objects during scene processing. NeoxiderTools provides an editor post-processor and a runtime safety net to restore eligible objects for local use:

1. On components that expose it, disable **Is Networked** (`isNetworked` in C#). `NeoNetworkComponent` subclasses and `Money` forward that setting through the interface; the physics player controllers always report `false`.
2. Check the whole identity hierarchy, including inactive children. It must contain at least one `INeoOptionalNetworked` implementation, and every implementation must return `false`. One component returning `true` prevents that identity's offline reactivation.
3. The editor post-processor runs after Mirror during build/Play Mode scene processing. The runtime `NeoMirrorSceneReactivator` safety net runs on scene load only when enabled and neither a server nor a client is active.
4. Reactivation targets inactive scene identities with a nonzero scene ID and skips protected hidden/non-editable objects. Runtime-spawned identities (`sceneId == 0`) are outside this recovery path. An inactive parent can still prevent an activated child from becoming active in the hierarchy.

Returning `false` is an opt-in condition, not an instruction to call `SetActive` immediately. Configure it before scene processing/loading. This assembly supplies the interface; the editor/runtime adapters perform the activation. It provides no Inspector component or NoCode action of its own.

## Authority selection

Choose the policy in a consumer's Inspector field or through its exposed typed property, such as a relay's `AuthorityMode`. With Mirror enabled, `NeoNetworkState.IsAuthorized` in `Neo.Network` applies these meanings:

| Policy | Persisted integer | Allowed caller |
|--------|-------------------|----------------|
| `None` | `0` | Any sender at the manual authority gate. |
| `OwnerOnly` | `1` | Direct server code, the host's local connection, or a remote sender owning the target identity. |
| `ServerOnly` | `2` | Direct server code or the host's local connection; remote client commands are rejected. |

The local host exception applies even when another client owns the object. Passing the authority gate does not bypass the consumer's payload validation, readiness checks, or rate limits. Commands using `requiresAuthority = false` still need the consumer's explicit authority check; selecting an enum value alone enforces nothing.

Keep all three numeric values stable: existing scenes, prefabs, and JSON settings persist the underlying integers. `default(NetworkAuthorityMode)` is `None`; individual consumers can initialize their serialized fields to a different policy. Use each component's documented default rather than assuming that all network actions accept any sender.

## 11.0.0 invalid-policy migration

Only `None`, `OwnerOnly`, and `ServerOnly` are supported policies. C# casts and deserialization can still produce unnamed integers: `(NetworkAuthorityMode)(-1)`, `(NetworkAuthorityMode)3`, or `(NetworkAuthorityMode)int.MaxValue`. The enum itself neither validates nor normalizes these values.

In 11.0.0, the Mirror-enabled `NeoNetworkState.IsAuthorized` gate rejects every unnamed policy before applying any direct-server, local-host, or ownership allowance. This also rejects an invalid policy when the target is null or the sender owns it. Earlier versions accepted unknown values through the permissive fallback. Code or imported settings that relied on that fallback must select a named policy explicitly; choose `None` only when any sender is intentionally allowed at this gate.

The named-policy table above, exact host-local-connection behavior, and persisted integers `0`, `1`, `2` remain compatible. Inspect existing consumer settings and correct unsupported numbers through the component's typed API or Inspector. This is an authorization-input change in `Neo.Network`; it adds no component, scene setup, or dependency to `Neo.Network.Contracts`. Offline action handling remains the responsibility of each consumer.

## Related workflows

- [NeoNetworkComponent](NeoNetworkComponent.md): shared network component API.
- [NetworkActionRelay](NetworkActionRelay.md) and [NetworkContextActionRelay](NetworkContextActionRelay.md): Inspector and C# action routing.
- [Multiplayer Guide](Multiplayer_Guide.md): scene and session setup.
