# NeoCondition

`NeoCondition` - компонент для проверки условий по значениям полей, свойств и простых методов компонентов или самого `GameObject`. Модуль предназначен для NoCode-сценариев, но опирается на обычные C# контракты и кэширует reflection-резолв, чтобы не выполнять дорогой поиск каждый кадр.

## Быстрый старт

1. Добавьте `NeoCondition` на объект через `Add Component -> Neoxider -> Condition -> NeoCondition`.
2. Добавьте условие в список `Conditions`.
3. Выберите источник:
   - `Component` - читать поле, свойство или поддерживаемый метод компонента;
   - `GameObject` - читать свойства объекта (`activeSelf`, `activeInHierarchy`, `tag`, `name`, `layer`, `isStatic`).
4. Укажите `Source Object` или включите `Find By Name`, если объект появляется в сцене позже.
5. Выберите `Component`, `Property`, оператор сравнения и порог.
6. Подключите `On True`, `On False` или `On Result`.

## Что Можно Проверять

`NeoCondition` поддерживает значения `int`, `float`, `bool` и `string`.

Для компонента можно выбрать:

- public поле;
- public свойство;
- public метод без аргументов;
- public метод с одним аргументом типа `int`, `float` или `string`.

Если выбран метод с аргументом, в инспекторе появляется поле `Argument`. Значение аргумента читается при каждой проверке, поэтому его можно менять в Play Mode.

Пример проверки денег:

```text
Source Object: объект с Money
Component: Money
Property: CanSpend (float) -> bool [method]
Argument (float): 100
Compare: == true
```

Для bool-значений используйте только `==` и `!=`.

## ConditionEntry

| Поле | Назначение |
| --- | --- |
| `Source` | Источник данных: `Component` или `GameObject`. |
| `Find By Name` | Искать объект в сцене через `GameObject.Find`. Поиск кэшируется и повторяется с интервалом. |
| `Object Name` | Имя объекта для `Find By Name`. |
| `Wait For Object` | Не логировать warning, пока объект еще не появился. Полезно для prefab/spawn-сценариев. |
| `Find Retry Interval` | Интервал между повторными `GameObject.Find`, если объект не найден. `0` - проверять каждый вызов. |
| `Prefab Preview` | Editor-only prefab для выбора компонентов и свойств, если runtime-объекта еще нет в сцене. |
| `Source Object` | Прямая ссылка на объект. Если пусто и `Find By Name` выключен, используется объект с `NeoCondition`. |
| `Component` | Компонент, из которого читается значение. |
| `Property` | Поле, свойство или поддерживаемый метод. |
| `Compare With` | Сравнение с константой или со значением другого объекта. |
| `Compare` | `==`, `!=`, `>`, `<`, `>=`, `<=`. |
| `Threshold` | Значение для сравнения с константой. |
| `Other Object` | Второй источник значения при `Compare With = Other Object`. |
| `NOT` | Инвертировать результат конкретного условия. |

## Режимы Проверки

| Режим | Поведение |
| --- | --- |
| `Manual` | Проверка только при вызове `Check()`. |
| `EveryFrame` | Проверка в `Update()`. Используйте аккуратно и предпочитайте кэшированные источники. |
| `Interval` | Проверка с заданным интервалом. |

`Check On Start` выполняет первую проверку при старте. `Only On Change` вызывает события только при изменении результата.

## Logic Mode

- `AND` - все условия должны вернуть `true`;
- `OR` - достаточно одного условия `true`.

Каждое условие может быть инвертировано через `NOT`.

## События

| Событие | Когда вызывается |
| --- | --- |
| `On True` | Итоговый результат `true`. |
| `On False` | Итоговый результат `false`. |
| `On Result(bool)` | При каждой проверке с текущим результатом. |
| `On Inverted Result(bool)` | При каждой проверке с инвертированным результатом. |

## Find By Name

`Find By Name` нужен для объектов, которые нельзя безопасно сохранить прямой ссылкой: поздний spawn, сцены с runtime-сборкой, объекты из другой сцены.

Правила:

- успешный результат кэшируется, пока объект жив;
- если объект не найден, повторный поиск выполняется не чаще `Find Retry Interval`;
- при `Wait For Object = true` отсутствие объекта не логируется;
- при уничтожении найденного объекта кэш сбрасывается на следующем resolve;
- `InvalidateCache()` сбрасывает reflection-кэш, а `InvalidateCacheFull()` / `InvalidateAllCaches()` сбрасывают все кэши, включая поиск по имени.

Та же логика поиска используется в `Neo.NoCode` через `BindingSourceGameObjectResolver`, чтобы NoCode-привязки и условия работали одинаково.

## Compare With: Other Object

Вместо сравнения с константой можно сравнить два runtime-значения.

Примеры:

- `Health.Hp <= Health.MaxHp` - оба значения с одного объекта;
- `Player.Score >= Enemy.Score` - сравнение двух объектов;
- `ObjA.layer == ObjB.layer` - сравнение свойств `GameObject`.

Если `Other Source Object` не задан, используется тот же объект, что и у основного условия.

## Защита От Ошибок

`NeoCondition` не должен ломать сцену при частично настроенном условии:

- отсутствующий объект возвращает `false`;
- отсутствующий компонент возвращает `false`;
- удаленный объект сбрасывает кэш;
- ошибка reflection логируется через `NeoDiagnostics` и не останавливает остальные условия;
- предупреждения логируются без спама.

## Примеры

### Game Over При HP <= 0

```text
Condition [0]:
  Source = Component
  Component = Health
  Property = currentHealth
  Compare = LessOrEqual
  Threshold = 0
Check Mode = EveryFrame
On True -> GameOverPanel.SetActive(true)
```

### Разблокировка Уровня При Score >= 100

```text
Condition [0]:
  Source = Component
  Component = ScoreManager
  Property = Score
  Compare = GreaterOrEqual
  Threshold = 100
Check Mode = Manual
ScoreManager.OnScoreChanged -> NeoCondition.Check()
On True -> NextLevelButton.SetInteractable(true)
```

### Ожидание Spawn-Объекта

```text
Find By Name = true
Object Name = Player
Wait For Object = true
Prefab Preview = Player prefab
Component = Health
Property = Hp
Compare = GreaterOrEqual
Threshold = 1
On True -> UI.ShowPlayerAlive()
```

## Смежные Разделы

- [Condition_Reuse.md](./Condition_Reuse.md) - переиспользование условий в State Machine, триггерах и своих системах.
- [NoCode/README.md](../NoCode/README.md) - NoCode-привязки, которые используют тот же resolver объектов.
