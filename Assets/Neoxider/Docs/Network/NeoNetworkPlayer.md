# NeoNetworkPlayer

Player-side network identity helper for `Neo.Network` scenes.

## Purpose

`NeoNetworkPlayer` centralizes player ownership hooks used by package-level no-code and gameplay bridges. Use it on the networked player object when scene systems need a stable player reference, owner-only initialization, or local/remote player separation.

## Typical setup

1. Add Mirror `NetworkIdentity` to the player object.
2. Add `NeoNetworkPlayer`.
3. Configure owner-only UI/input on components that should run only for the local player.
4. Use `NeoNetworkManager` scene-player template flow when the player is already placed in the scene.

## Related docs

- [NeoNetworkManager](./NeoNetworkManager.md)
- [NetworkOwnerFilter](./NetworkOwnerFilter.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
