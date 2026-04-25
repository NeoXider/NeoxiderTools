# StatePredicate

**Назначение:** Сериализуемый предикат для No-Code условий переходов в `StateMachine`. Позволяет задать условие на основе значения поля компонента (`int`, `float`, `bool`, `string`) без написания кода.

## Поля (Inspector)

| Поле | Описание |
|------|----------|
| **Source Object** | Компонент, с которого считывается значение. |
| **Property Name** | Имя свойства/поля для проверки. |
| **Comparison** | Оператор сравнения (`==`, `!=`, `<`, `>`, `<=`, `>=`). |
| **Target Value** | Целевое значение для сравнения. |

## См. также
- [StateCondition](StateCondition.md)
- [StateMachine](StateMachine.md)
- ← [StateMachine](README.md)
