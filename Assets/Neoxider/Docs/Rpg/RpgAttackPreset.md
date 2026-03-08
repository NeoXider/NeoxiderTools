# RpgAttackPreset

**Что это:** `ScriptableObject`-preset, который связывает `RpgAttackDefinition` и правила выбора цели для AI, skills и spells.

**Навигация:** [← К RPG](./README.md)

---

## Что хранит

- Ссылку на `RpgAttackDefinition`
- Флаг `Require Target`
- Флаг `Use Selector Component When Available`
- Флаг `Aim At Target`
- `RpgTargetQuery` для автоматического поиска цели

## Когда использовать

- AI-атаки по ближайшему врагу
- Способности с фиксированной политикой выбора цели
- Заклинания и skill-кнопки, где важна не только атака, но и targeting strategy
