# Evade (legacy)

**What it is:** a legacy `MonoBehaviour` that times an evade action and its cooldown and raises events; movement, animation and invulnerability come from scene listeners. Kept for existing scenes and C# callers; source: `Scripts/Rpg/Combat/Evade.cs`, optional assembly `Neo.Rpg.Combat`.

**How to use:**
1. Add **Neoxider / Tools / Components / Evade (Legacy)** to an existing actor.
2. Configure the evade duration and cooldown.
3. Call `TryStartEvade()` (C#) or `StartEvade()` (UnityEvent); show `CooldownProgress` (1 = ready) in UI.

---

The complete contract (C# API, events, limits, scene setup) is the canonical
[RPG / Combat compatibility page](../../../Rpg/Combat/README.md#evade), which is also the component's
Inspector help target. For new projects use [RpgEvadeController](../../../Rpg/RpgEvadeController.md).

## See also

- [AttackSystem](./README.md)
- [RPG / Combat compatibility](../../../Rpg/Combat/README.md)
