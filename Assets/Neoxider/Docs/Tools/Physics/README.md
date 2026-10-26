# Tools / Physics

Reusable 3D physics components in `Scripts/Tools/Physics/`, namespace `Neo.Tools`, assembly `Neo.Tools.Physics`. Reference that assembly from custom asmdefs. These components use Unity's default physics scene and `Rigidbody`; they do not affect `Rigidbody2D`.

## Components

| Page | Description |
|------|-------------|
| [ExplosiveForce](./ExplosiveForce.md) | Radial force, falloff |
| [ImpulseZone](./ImpulseZone.md) | Directional impulse |
| [MagneticField](./MagneticField.md) | Attract/repel |

## C# and scene setup

Call `ExplosiveForce.Explode()`, `ImpulseZone.ApplyImpulseToObject(target)`, or configure a continuously running `MagneticField` through `SetMode`, `SetTarget`, `SetTargetPoint`, and `SetDirection`. Subscribe to their UnityEvents from C# with `AddListener`, or connect the same methods and events in the Inspector. No manager, singleton, or NoCode action chain is required.

Use 3D colliders and dynamic rigidbodies on targets, and configure **Affected Layers** on each component. **Add Rigidbody If Needed** is optional and changes static targets into physics objects. `ImpulseZone` needs a concrete collider such as `BoxCollider` on its own GameObject; `Awake` marks that collider as a trigger. Unity trigger detection requires a Rigidbody on at least one participant.

Each explosion or magnetic query applies force once per attached Rigidbody, including bodies with multiple child colliders. Explosion and magnetic emitters exclude their own attached body. Impulse one-shot history and cooldown are also shared by colliders attached to the same body. Custom falloff uses the body's world center of mass in both explosion modes. The `AddForce` explosion mode and magnetic evaluation also use the center of mass for direction; native `AddExplosionForce` delegates direction and force application to Unity's closest-surface behavior. Layer/tag filters still use the detected collider's GameObject.

`MagneticField` membership events and `ObjectsInFieldCount` track distinct collider GameObjects, so a compound body can have several membership entries while receiving one force. Disabling the field clears membership and emits exits; re-enabling discovers fresh entries. See its page for query capacity and timing limits.

## See also

- [Tools/Move](../Move/README.md)
