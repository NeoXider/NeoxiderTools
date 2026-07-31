# NetworkReactiveSync

**What it is:** `Neo.Network.NetworkReactiveSync` — NoCode replication for `ReactivePropertyFloat/Int/Bool` (the inspector counterpart of `NetworkReactivePropertyBridge`, which needs hand-written SyncVar code). The value replicates and every local binding (`TextMoney`, UI, UnityEvents) fires on all clients.

**Usage:** put on a `NetworkIdentity` object; reference the component (e.g. `Money`) and the reactive field name (`CurrentMoney`), pick value type and direction (`ServerToClients` / `OwnerToServer`). `Sync Interval` ≥ 0.1 s. Without Mirror the component is inert — the reactive property keeps working locally.
