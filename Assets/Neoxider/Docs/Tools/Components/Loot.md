# Loot

`Neo.Tools.Loot` instantiates a random number of prefab drops around its transform.

## Scene setup

Add **Neoxider / Tools / Loot**, assign `lootItems`, choose `dropChance` in 0–1, and set nonnegative,
ordered `minCount`/`maxCount`. The count includes both endpoints. `dropRadius` offsets drops inside a
sphere projected onto the owner's horizontal plane; each instance keeps the owner's Y coordinate.
Null prefab entries are skipped, so they can reduce the number of actual spawned objects.

**Spawn On Destroy** calls `DropLoot()` only while the application is playing. Editor cleanup must not
create scene drops. Destruction during runtime scene teardown can still invoke the drop path; turn off
Spawn On Destroy and invoke the method from the gameplay death event when teardown must never drop loot.

## C# and UnityEvents

```csharp
Loot loot = GetComponent<Loot>();
loot.spawnOnDestroy = false;
loot.DropLoot(); // Explicit drop, for example after a confirmed enemy death.
```

`DropLoot()` is public and can also be wired to a UnityEvent. It uses `Instantiate`; spawned objects
are independent scene objects, so their cleanup belongs to the consuming game. Empty/null prefab arrays
produce no drops. Explicit calls remain available outside Play Mode for deliberate authoring code.

See [Components](README.md).
