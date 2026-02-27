# Компоненты (Components)

Этот раздел содержит различные готовые компоненты, которые можно использовать для реализации игровой логики.

## Файлы

- [AnimatorParameterDriver](./AnimatorParameterDriver.md) — вызов параметров Animator (триггер, bool, float, int) из кода и UnityEvent.
- [UnityLifecycleEvents](./UnityLifecycleEvents.md) — проброс событий жизненного цикла Unity (Awake, OnEnable, OnDisable, Start, Destroy, Update/FixedUpdate/LateUpdate с deltaTime) в UnityEvent
- [Counter](./Counter.md) — универсальный счётчик (Int/Float), Add/Subtract/Set, Send по Payload или произвольное число, OnValueChanged/OnSend
- [TextScore](./TextScore.md) — отображение очков (ScoreManager): текущий счёт или рекорд, без кода
- [Loot](./Loot.md) — система лута и дропа
- [ScoreManager](./ScoreManager.md) — менеджер очков с системой звезд и сохранением рекордов
- [TypewriterEffect](./TypewriterEffect.md) — эффект печатной машинки с паузами на знаках препинания

## Папки

- [AttackSystem](./AttackSystem) — система атак и урона
- [Interface](./Interface) — интерфейсы боевой системы (IDamageable, IHealable и др.)

---

Для предложений по доработке скриптов компонентов см. [SCRIPT_IMPROVEMENTS](./SCRIPT_IMPROVEMENTS.md).
