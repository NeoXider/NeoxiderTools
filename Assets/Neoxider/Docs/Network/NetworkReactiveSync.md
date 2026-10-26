# NetworkReactiveSync

**What it is:** `Neo.Network.NetworkReactiveSync` — NoCode replication for `ReactivePropertyFloat/Int/Bool` (the inspector counterpart of `NetworkReactivePropertyBridge`, which needs hand-written SyncVar code). The value replicates and every local binding (`TextMoney`, UI, UnityEvents) fires on all clients.

**Usage:** put on a `NetworkIdentity` object; reference the component (e.g. `Money`) and the reactive field name (`CurrentMoney`), pick value type and direction (`ServerToClients` / `OwnerToServer`). `Sync Interval` ≥ 0.1 s. Without Mirror the component is inert — the reactive property keeps working locally.

In `OwnerToServer` mode, accepted values update the dedicated server's reactive property and notify
its listeners as well as replicating to clients. A host uses Mirror's normal hook once; the explicit
server apply covers the dedicated-server path where Mirror does not call that hook. Commands are
rejected when networking is disabled, direction is `ServerToClients`, or their payload type differs
from the configured **Value Type**. Ownership alone does not authorize changing a server-written value.

For the Inspector workflow, bind local UI/gameplay to the target reactive property's change event.
For C# code, keep a normal `ReactivePropertyFloat/Int/Bool` and call
`NetworkReactivePropertyBridge.SetFromNetwork(property, value)` from your own SyncVar hook and
authoritative server update. No reflection-based adapter is required for that path.

Metadata is validated once: the member must expose the selected reactive type through a field or a
non-indexed getter. A readonly field/get-only property containing a mutable reactive instance is valid;
the adapter changes that instance, not the containing member. Indexed or inaccessible/wrong-type members
are rejected with one gated diagnostic. A valid member whose instance is temporarily null remains
retryable: the next eligible poll or accepted update can resolve an instance created during initialization.
This does not introduce automatic replay of an earlier value without a new update or a seed-policy API.

Polling and command windows use unscaled double time. Every eligible poll is timestamped, including
unchanged values and not-yet-ready instances, so the configured read cadence survives gameplay pause.
