# Компоненты (Components)

**Что это:** готовые компоненты игровой логики: Counter, ScoreManager, TextScore, Loot, TypewriterEffect, UnityLifecycleEvents, AnimatorParameterDriver, AttackSystem. Скрипты в `Scripts/Tools/Components/`.

**Навигация:** [← К Tools](../README.md) · оглавление — списки ниже

## Файлы

- [AnimatorParameterDriver](./AnimatorParameterDriver.md) — вызов параметров Animator (триггер, bool, float, int) из кода и UnityEvent.
- [UnityLifecycleEvents](./UnityLifecycleEvents.md) — проброс событий жизненного цикла Unity (Awake, OnEnable, OnDisable, Start, Destroy, Update/FixedUpdate/LateUpdate с deltaTime) в UnityEvent
- [Counter](./Counter.md) — универсальный счётчик (Int/Float), Add/Subtract/Set, Send по Payload или произвольное число, OnValueChanged/OnSend
- [RandomRange](./RandomRange.md) — генерация случайного числа в [Min, Max] (Int/Float), Value/ValueInt/ValueFloat для NeoCondition, Generate(), события OnGeneratedInt/OnGeneratedFloat
- [TextScore](./TextScore.md) — отображение очков (ScoreManager): текущий счёт или рекорд
- [Loot](./Loot.md) — система лута и дропа
- [ScoreManager](./ScoreManager.md) — менеджер очков с системой звезд и сохранением рекордов
- [TypewriterEffect](./TypewriterEffect.md) — эффект печатной машинки с паузами на знаках препинания

## Папки

- [AttackSystem](./AttackSystem) — система атак и урона *(Health, Evade, AttackExecution, AdvancedAttackCollider — legacy; для новых проектов используйте [RPG](../Rpg/README.md))*
- [Interface](./Interface) — интерфейсы боевой системы (IDamageable, IHealable и др.)

---

Для предложений по доработке скриптов компонентов см. [SCRIPT_IMPROVEMENTS](./SCRIPT_IMPROVEMENTS.md).
