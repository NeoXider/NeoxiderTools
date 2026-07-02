# NetworkPropertySync

No separate English document is maintained currently.
See [Network README](./README.md).

## Interval constraints (9.6.2)

`Sync Interval` now has a **0.1 s minimum** (`[Min]`): an interval below the server-side rate limit
(0.05 s) caused silent Cmd drops — the owner considered the value sent while all clients stayed stuck
on the stale value until the next change.

`OwnerToServer` caveat: the SyncVar hook overwrites the owner's local value with the server value;
continuously changing local values may rubber-band.
