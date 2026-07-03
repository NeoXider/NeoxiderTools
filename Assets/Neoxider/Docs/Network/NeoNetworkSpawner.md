# NeoNetworkSpawner

Network-aware spawn helper for multiplayer scenes that use Mirror.

## Purpose

`NeoNetworkSpawner` wraps common spawn flows so gameplay or no-code triggers can request a networked object without duplicating Mirror spawn boilerplate in every scene.

## Typical setup

1. Register the prefab with the active Mirror network manager.
2. Add `NeoNetworkSpawner` to a scene object that owns the spawn point or trigger.
3. Assign the prefab and spawn transform.
4. Invoke the spawn action from UI, no-code logic, or gameplay code on the authoritative side.

## Notes

- Use it for simple replicated spawns.
- Keep durable game state in dedicated networked components after the object is spawned.
- Validate authority before exposing player-controlled spawn actions.

## Related docs

- [NeoNetworkManager](./NeoNetworkManager.md)
- [NetworkActionRelay](./NetworkActionRelay.md)
- [Multiplayer Guide](./Multiplayer_Guide.md)
