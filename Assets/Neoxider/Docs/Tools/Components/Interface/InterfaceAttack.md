# InterfaceAttack

`Scripts/Tools/Components/Interface/InterfaceAttack.cs` declares four independent interfaces in the
global namespace. The filename is historical; there is no `InterfaceAttack` type.

| Interface | Public contract |
|---|---|
| `IAttackable` | `void Attack(int damage)` |
| `IDamageable` | `void TakeDamage(int amount)` |
| `IHealable` | `void Heal(int amount)` |
| `IRestorable` | `void Restore()` |

Implement only the capabilities a gameplay object provides. These interfaces do not prescribe health
storage, scene lookup, events, or damage rules. C# consumers call the typed methods directly; scene
UnityEvents can target the corresponding public methods of a component implementing the interface.

## See Also

- ← [Tools/Components](../README.md)
