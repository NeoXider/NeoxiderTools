# Legacy combat compatibility (`Neo.Rpg.Combat`)

This optional assembly preserves `Neo.Tools.Health`, `AttackExecution`, `Evade`,
`AdvancedAttackCollider`, and `RpgStatsDamageableBridge` for existing scenes and C# callers.
Reference `Neo.Rpg.Combat` from your own assembly. For new combat, see
[Abilities](../../Abilities/README.md). Existing RPG actors can use the
[RPG module](../README.md) and the bridge described below.

The older [AttackSystem pages](../../Tools/Components/AttackSystem/README.md) remain available
for existing links. This page is the compatibility assembly's canonical contract and Inspector help target.

## C# API

### Health

`Health` implements `IDamageable`, `IHealable`, and `IRestorable`.

```csharp
using Neo.Tools;

// Existing serialized component references; these types remain available for compatibility.
Health health = actor.GetComponent<Health>();
health.SetMaxHp(100, restore: true);
health.TakeDamage(25);
health.Heal(10);
float fraction = health.HpPercentValue; // 0.85
health.Hp.OnChanged.AddListener(value => UpdateHealthLabel(value));
```

- Use `SetHp`, `SetMaxHp`, `TakeDamage`, `Heal`, and `Restore` to mutate health.
  Nonpositive damage/healing is ignored; large integer amounts saturate safely.
  `SetMaxHp` normalizes negative maxima to zero and clamps current HP immediately.
- `HpValue`, `HpPercentValue`, and serialized `hp` reflect the same state after API calls.
  When `restoreOnAwake` is false, authored `hp` is preserved and clamped during initialization.
  Treat the public reactive properties as observation endpoints; directly assigning their values
  bypasses the health API and its serialization/event guarantees.
- A per-hit limit of `-1` is unlimited; zero blocks that operation. `OnDamage` and `OnHeal`
  retain their legacy capped **requested** amount, which can exceed the HP actually changed.
  Damage to an already dead actor is ignored. `OnDeath` is raised for a damage-caused transition
  to zero; `SetHp`/`SetMaxHp` do not synthesize a death event.
- Explicit `Heal` can revive an actor. `ignoreIsAlive` controls automatic healing eligibility.
  `SetHeal(amount, delay)` configures automatic healing; zero/negative amounts stop its timer.
  Enabling starts configured auto-heal, disabling stops it, and destruction disposes the timer.

**Integration status:** the shared `Neo.Tools.Time` source now delivers completion events for
looping timers and honors cancellation from start listeners. Regression source covers auto-heal
through disable/re-enable and cooldown-start reset/disable. Runtime integration remains unverified
until the central Unity execution; these source corrections are not a claim that those tests passed.

### AttackExecution

Call `Attack()` to request a cycle; `false` means the component is disabled, globally blocked,
or already busy. A successful request enters `Attacking`, invokes `OnStartAttack`, waits for
`delayTimeAttack`, invokes `OnAttack`, and enters `Cooldown`. The full interval is
`max(wind-up, 1 / (AttackSpeed * multiplayAttackSpeed))` for finite positive speed settings.
Negative wind-up is treated as zero. Speed/multiplier settings are refreshed for each request.

`ResetAttack()` cancels the cycle, restores `Ready`, and invokes `OnEndAttack`.
Disabling cancels without a completion event. Reset or disable from `OnStartAttack` prevents
the hit; reset from `OnAttack` prevents the old cycle from restoring cooldown. A listener on
`OnEndAttack` may start a new cancellable cycle. `SetCanAttack(false)` blocks future requests;
it does not cancel an accepted cycle. `Attack()` can return true even if its start listener
immediately cancels that accepted request. Timing uses scaled Unity time.

### AdvancedAttackCollider

`ActivateTrigger()` starts a swing with `AttackDamage`; `ActivateTrigger(damage)` overrides it
and `-1` selects the default. Each activation clears the optional repeat-hit cache.

With `autoManageColliders` enabled, activation enables the assigned local 2D/3D collider and
schedules its deactivation after `triggerDuration` in scaled time. Reactivation restarts that
window. Disable clears cached hits, disables managed colliders, and cancels pending completion.
With automatic management disabled, the scene owns collider enable/disable state.

`use2D`/`use3D`, `useTrigger`/`useCollision`, `hittableLayers`, and `ignoreObjects` filter callbacks
before damage or event delivery. Ignored objects do not consume the repeat-hit allowance.
The ignore list uses exact GameObjects; it does not implicitly ignore their children.
Repeat-hit suppression is per collider, rather than per actor.

The hit object must carry `IDamageable` or `IHealable` directly. Put a bridge on a child hitbox
when its actor component lives on a parent. Accepted contacts can still invoke `OnHit`, forces,
and effects even when the object has no damage/heal interface. Trigger-specific events fire
only after accepted trigger contacts; collision callbacks do not emit trigger events.

Knockback uses the target's `AdvancedForceApplier` when available and enabled by configuration;
otherwise it falls back to a local `Rigidbody`/`Rigidbody2D`. `forceDuration` remains reserved.
The effect prefab owns its own lifetime. `destroyTargetOnHit` targets the hitbox GameObject;
`destroySelfOnHit` targets the attacking GameObject.

### Evade

`TryStartEvade()` accepts a request while active, outside an evade, and outside cooldown.
`StartEvade()` is its void UnityEvent wrapper. The component controls timing and output events;
movement, animation, and invulnerability are supplied by scene listeners.

- Cooldown starts with the action or immediately before `OnEvadeCompleted`, according to
  `cooldownStartsWithEvade`. Completion listeners therefore observe an already active deferred
  cooldown and cannot bypass it by immediately requesting another evade.
- `SetCooldownDuration(seconds)` affects the next cooldown, preserving a running cooldown.
  `SetEvadeDuration(seconds)` affects future actions. The next cooldown also reads the public
  interval and unscaled-time settings.
- `ResetCooldown()` clears cooldown without cancelling an active evade. Disable interrupts
  the action and resets cooldown without completion events. External invulnerability/animation
  owners must release their own state when disabled; interruption does not emit `OnEvadeCompleted`.
- An `OnCooldownStarted` listener may call `ResetCooldown()` or disable the actor. That cooldown
  is cancelled without `OnCooldownCompleted` or delayed progress updates. Reset preserves the
  active action; disable interrupts it. Both use the same methods from C# and UnityEvent wiring.
- `ReloadProgressValue`, `CooldownProgress`, and `ReloadProgress` use **1 = ready**;
  remaining cooldown is zero when ready. Subscribe to the reactive values for UI.
- The action duration uses scaled Unity time. `useUnscaledTimeForCooldown` applies only to cooldown.

### RpgStatsDamageableBridge

Assign `_character` for an explicit `RpgCharacter` dependency, or put the bridge on the actor
or a child hitbox. The fallback resolves and caches a parent character when first used.
`TakeDamage(int)` and `Heal(int)` ignore nonpositive amounts, multiply positive requests by
`DamageMultiplier`/`HealMultiplier`, and forward them to `RpgCharacter.Damage`/`Heal`.
Multiplier setters clamp negative values to zero. Missing characters are ignored safely.

The bridge does not add network authority. Networked RPG actors require their existing
`Neo.Rpg.Network` adapter. Serialized references, legacy component menus, and interfaces are retained.

## Scene and UnityEvent setup

1. Keep existing legacy components on their actors. Configure maximum/authored HP, hit limits,
   optional healing, attack speed/wind-up, and evade timings in the Inspector.
2. For a weapon hitbox, add a 2D or 3D collider and configure normal Unity physics requirements
   for collision/trigger delivery. Assign the collider explicitly, or let `Awake` find a local one.
   Set the layer mask, ignore list, and optional repeat-hit suppression.
3. Wire `AttackExecution.OnAttack` to the hitbox's parameterless `ActivateTrigger`. If automatic
   collider management is enabled, its duration controls the attack window.
4. Wire `Health.OnDeath` to existing death handling; subscribe to `Hp`/`HpPercent` for UI.
   Wire evade start/completion to movement, animation, or invulnerability, with interruption
   cleanup owned by those listeners. Call all runtime action methods in Play Mode.
5. When the target already uses `RpgCharacter`, put `RpgStatsDamageableBridge` on each legacy
   hitbox and assign the character explicitly if it is outside the parent chain.

These UnityEvent routes call the same public methods as C#; they do not require NoCode actions,
scene searches, or scene references stored in ScriptableObjects.
