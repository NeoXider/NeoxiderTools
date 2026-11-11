# AbilityEventListener

**What it is:** a scene component that subscribes to one event of the ability system and relays it as UnityEvents and
as a C# event. It is the entry point for everything outside the ability data: quests, achievements, analytics, a
tutorial step, a call into third-party code. `Scripts/Abilities/Components/AbilityEventListener.cs`, namespace
`Neo.Abilities`.

**How to use:**

1. Add `AbilityEventListener` to a scene object.
2. Set **Event Id** (for example `take_damage`). Optionally narrow it with **Ability Id**, **Unit** and
   **Other Unit**.
3. Wire **On Event**, **On Amount**, **On Unit Object**, **On Other Object** or **On Ability Id**, or subscribe to
   `Raised` from code.

---

## Fields (Inspector)

| Field | Description |
|-------|-------------|
| **Event Id** | The event: `ability_cast`, `take_damage`, `deal_damage`, `kill`, `death`, `heal_received`, `modifier_applied`, `modifier_removed`, `critical_hit`, `zone_spawned`, `zone_expired` and the others listed in `AbilityEvents`. |
| **Ability Id** | Only events of this ability. Empty = any. |
| **Unit** | Only events about this unit: the caster of `ability_cast`, the damaged unit of `take_damage`, the dealer of `deal_damage` and `kill`, the dead unit of `death`. Empty = any. |
| **Other Unit** | Only events whose other side is this unit: the attacker in `take_damage`, the victim in `deal_damage` and `kill`. Empty = any. |

## Events

| Event | Payload |
|-------|---------|
| **On Event** | None: the event happened. |
| **On Amount** | The amount: damage dealt, health restored, zone radius. |
| **On Unit Object** | The GameObject of the unit the event is about. |
| **On Other Object** | The GameObject of the unit on the other side. |
| **On Ability Id** | The ability id, when the event has one. |
| `event Action<AbilityEventArgs> Raised` | The full event data for code. |

## API

| Member | Description |
|--------|-------------|
| `string EventId`, `string AbilityId` | The subscription and the ability filter. Setting `EventId` re-subscribes. |
| `AbilityUnitBehaviour Unit`, `OtherUnit` | The unit filters. |
| `void Subscribe()` | Connects to the scene ability hub. Called on Start and on re-enable. |

## Examples

**Inspector:** count kills made with one weapon: **Event Id** `kill`, **Ability Id** `lightning`, **Unit** = the hero,
**On Event** -> `Counter.Add(1)`.

**Code:** the same subscription without a component:

```csharp
AbilitySystem system = AbilitySystemBehaviour.InstanceOrNull.System;
system.Events.Subscribe(AbilityEvents.Kill, args =>
{
    if (args.AbilityId == "lightning")
    {
        Analytics.Track("lightning_kill");
    }
});
```

`system.Events` also has `SubscribeTarget(unitId, handler)` for every event of one unit and `SubscribeAny(handler)`
for everything.

## See also

- [AbilitySystemBehaviour](./AbilitySystemBehaviour.md) — `EntitySpawned` for the objects abilities spawn
- [AbilityUnitBehaviour](./AbilityUnitBehaviour.md) — per-unit UnityEvents (`OnDamaged`, `OnDied`, ...)
- [TriggersAndHooks](./TriggersAndHooks.md) — reactions inside ability data

---

**По-русски:** компонент подписывается на одно событие системы способностей (каст, урон, убийство, смерть, наложение
модификатора и т. д.) и отдаёт его как UnityEvent и как C#-событие. Фильтры: способность, юнит события и юнит с
другой стороны. Это точка входа для квестов, достижений, аналитики и вызова стороннего кода.
