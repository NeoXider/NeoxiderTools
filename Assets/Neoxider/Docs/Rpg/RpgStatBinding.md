# RpgStatBinding

**Что это:** привязка одного стата `RpgCharacter` к UnityEvent.

Используйте для UI и NoCode, когда нужно показать или отреагировать на `Strength`, `Defense`, `FireResist` или custom stat.

## Поля

| Поле | Назначение |
|------|------------|
| `_character` | Источник `RpgCharacter`; если пусто, ищется в родителях |
| `_statId` | Preset или custom ID |
| `_onValue` | Событие с текущим значением стата |

## Пример

`_statId = Strength`, `_onValue -> SetText.SetFloat`.

Для условий используйте `RpgConditionAdapter.StatAtLeast` / `StatBelow`.
