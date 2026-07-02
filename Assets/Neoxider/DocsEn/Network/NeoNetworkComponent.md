# NeoNetworkComponent

English API notes are not yet fully documented.

- [Russian source](../../Docs/Network/NeoNetworkComponent.md)
- [Network README](./README.md)

## RateLimitCheck note

The limit is tracked **per component instance** (one timer per object on the server), not per client.
On scene objects with `requiresAuthority = false` commands, frequent commands from one client can
drop legitimate commands from others. Per-owner objects are unaffected.
