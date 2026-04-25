# StateMachineEvaluationContext

**Назначение:** Контекст вычисления для `StateMachine`. Содержит ссылки на текущий `GameObject`, `Transform` и кешированные компоненты для оптимизации предикатов при оценке условий перехода.

## API

| Свойство | Описание |
|----------|----------|
| `GameObject GameObject { get; }` | GameObject машины состояний. |
| `Transform Transform { get; }` | Transform машины состояний. |

## См. также
- [StateMachine](StateMachine.md)
- [StatePredicate](StatePredicate.md)
- ← [StateMachine](README.md)
