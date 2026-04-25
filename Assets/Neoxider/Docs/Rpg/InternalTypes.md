# RPG Module — Internal Types

## Enums
| Тип | Описание |
|-----|----------|
| `BuffStatType` | Тип бафф-стата (Health, Attack, Defense, и т.д.). |
| `RpgAttackDeliveryType` | Способ доставки урона (Melee, Projectile, Aura). |
| `RpgHitMode` | Режим попадания (Single, AOE, Piercing). |
| `RpgInputTriggerType` | Тип триггера ввода (Press, Hold, Release). |
| `RpgMouseButton` | Кнопка мыши (Left, Right, Middle). |
| `RpgTargetSelectionMode` | Режим выбора цели (Nearest, Manual, Auto). |
| `RpgConditionEvaluationMode` | Режим оценки условий (All, Any). |
| `RpgNoCodeActionType` | Тип No-Code действия для RPG. |

## Data / Config
| Тип | Описание |
|-----|----------|
| `AuraWeapon` | Конфигурация аурного оружия (радиус, урон, тики). |
| `BuffStatModifier` | Модификатор бафф-стата (тип, значение, длительность). |
| `RpgAttackEffectRefs` | Ссылки на эффекты атаки (VFX, SFX). |
| `RpgButtonBinding` | Привязка кнопки к RPG-действию. |
| `RpgStatGrowthDefinition` | Определение роста статов (формула, коэффициенты). |
| `RpgTargetQuery` | Запрос для поиска целей. |
| `RpgProfileData` | Профильные данные RPG-персонажа. |

## Events
| Тип | Описание |
|-----|----------|
| `RpgAttackEvent` | UnityEvent для атаки. |
| `RpgGameObjectEvent` | UnityEvent<GameObject> для RPG. |
| `RpgStringEvent` | UnityEvent<string> для RPG. |

## Helpers / Runtime
| Тип | Описание |
|-----|----------|
| `RpgProgressionHelper` | Хелпер для интеграции RPG с Progression. |
| `IRpgCombatReceiver` | Интерфейс получателя урона. |
| `RpgCombatMath` | Утилита расчёта урона. |
| `RpgTargetingUtility` | Утилита поиска целей. |

## См. также
- ← [Rpg](README.md)
