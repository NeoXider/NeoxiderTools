# NeoCondition

No-Code система условий для NeoxiderTools. Проверяет значения полей/свойств любых компонентов и GameObject'ов через Inspector, без написания кода.

См. roadmap развития Editor/архитектуры: [`NeoCondition_Editor_Roadmap.md`](./NeoCondition_Editor_Roadmap.md)

## Быстрый старт

1. Добавить `NeoCondition` на GameObject (Add Component → Neoxider → Condition → NeoCondition)
2. Нажать **+** чтобы добавить условие
3. Выбрать **Source** — режим источника данных:
   - **Component** — читать поле/свойство из компонента
   - **GameObject** — читать свойство самого игрового объекта (activeSelf, tag, layer и т.д.)
4. (Опционально) Включить **Find By Name** — для поиска объекта в сцене по имени вместо прямой ссылки
5. Выбрать **Source Object** или ввести **Object Name** (при Find By Name)
6. Выбрать **Component** / **Property** из dropdown
7. Выбрать **Compare With**:
   - **Constant** — сравнить с числом или текстом (задать порог)
   - **Other Object** — сравнить с полем/свойством другого объекта (выбрать второй объект, компонент и свойство)
8. Выбрать оператор **Compare** (==, !=, >, <, >=, <=) и при Constant — задать порог
9. Подключить события **On True** / **On False**

## Архитектура

```
NeoCondition (MonoBehaviour)
├── Logic Mode: AND / OR
├── Conditions: List<ConditionEntry>
│   ├── [0] Source=Component: Health.currentHealth <= 0
│   ├── [1] Source=GameObject: GO.activeSelf == true
│   ├── [2] FindByName("Enemy"): Health.Hp <= 0
│   └── ...
├── Check Mode: Manual / EveryFrame / Interval
├── Check On Start: bool (default: true)
├── Only On Change: bool
├── On True: UnityEvent
├── On False: UnityEvent
├── On Result: UnityEvent<bool>
└── On Inverted Result: UnityEvent<bool>
```

## ConditionEntry (одно условие)

| Поле | Описание |
|------|----------|
| **Source** | Режим источника: `Component` (поля компонента) или `GameObject` (свойства объекта) |
| **Find By Name** | Искать целевой GameObject по имени в сцене (`GameObject.Find`). Кешируется пока объект жив |
| **Object Name** | Имя объекта для поиска (отображается при включённом Find By Name) |
| **Wait For Object** | Ожидать появления объекта без Warning (для префабов, которые заспавнятся позже) |
| **Prefab Preview** | Ссылка на префаб из Project для настройки компонентов/свойств до спавна объекта (только Editor, не используется в Runtime) |
| **Source Object** | Прямая ссылка на GameObject. Пусто = текущий объект (при выключенном Find By Name) |
| **Component** | Dropdown всех компонентов на объекте (только в режиме Component) |
| **Property** | Dropdown полей/свойств (int, float, bool, string) |
| **Compare With** | **Constant** — сравнивать с числом/текстом (порог). **Other Object** — сравнивать с переменной другого объекта |
| **Compare** | == Equal, != NotEqual, > Greater, < Less, >= GreaterOrEqual, <= LessOrEqual |
| **Threshold** | Значение для сравнения (только при Compare With = Constant; тип подбирается автоматически) |
| **Other Object** (при Compare With = Other Object) | Второй источник: Source (Component/GameObject), Find By Name, объект, компонент и свойство. Если **Other Source Object** не задан — используется тот же объект, что и слева (удобно для сравнения двух полей одного объекта, например Health.Hp и Health.MaxHp). |
| **NOT** | Инвертировать результат этого условия |

## Source Mode

### Component (по умолчанию)
Читает public поля и свойства выбранного компонента: `int`, `float`, `bool`, `string`.

### GameObject
Читает свойства самого игрового объекта:

| Свойство | Тип | Описание |
|----------|-----|----------|
| `activeSelf` | bool | Объект включён |
| `activeInHierarchy` | bool | Активен в иерархии (с учётом родителей) |
| `isStatic` | bool | Статический объект |
| `tag` | string | Тег объекта |
| `name` | string | Имя объекта |
| `layer` | int | Слой объекта |

## Find By Name (поиск по имени)

Позволяет найти целевой GameObject в сцене по имени вместо прямой ссылки. Полезно для:
- Объектов, которые появляются динамически (спавн)
- Ссылок между сценами
- Объектов, на которые нельзя сделать прямую ссылку в Inspector

**Оптимизация:**
- `GameObject.Find()` вызывается один раз, результат кешируется в `_foundByNameObject`
- Пока объект жив, повторные поиски не выполняются
- Если объект уничтожен, кеш сбрасывается и поиск выполняется заново при следующей проверке
- `InvalidateCache()` сбрасывает только reflection-кеш, кеш поиска сохраняется
- `InvalidateCacheFull()` / `InvalidateAllCaches()` сбрасывает всё, включая кеш поиска

**Wait For Object (ожидание спавна):**
- Если включено — при `Find == null` условие тихо возвращает `false` **без Warning** в консоль
- Полезно для **префабов**, которые ещё не на сцене и появятся позже (спавн)
- Повторный поиск выполняется при каждой проверке, пока объект не будет найден
- Как только объект заспавнится — он автоматически подхватывается и кешируется

## Compare With: Other Object (сравнение двух переменных)

Вместо сравнения с константой можно сравнивать **одну переменную с другой** — поле/свойство одного объекта с полем/свойством другого.

- В условии выберите **Compare With** → **Other Object (variable)**.
- Укажите **второй источник**: объект (прямая ссылка или Find By Name), режим Component или GameObject, компонент и свойство.
- Оператор сравнения (==, !=, >, <, >=, <=) применяется к двум значениям: левая сторона (основной Source) и правая (Other Object).
- Типы обеих сторон должны быть сравнимыми (int, float, bool, string). При несовпадении типов используется приведение (например, int и float).

**Если Other Source Object пуст:** в качестве второго объекта используется **тот же объект, что и слева** (источник левой переменной). Так можно сравнивать два свойства одного объекта (например, `Health.Hp == Health.MaxHp` на одном GameObject), не заполняя поле «Other Source Object».

**Примеры:**
- `Health.Hp <= Health.MaxHp` — текущее HP не больше максимума (оба с одного объекта; Other Source Object можно оставить пустым).
- `Player.Score >= Enemy.Score` — счёт игрока не меньше счёта врага (указать два разных объекта).
- `ObjA.layer == ObjB.layer` — объекты на одном слое (режим GameObject для обоих).

**Prefab Preview (настройка до спавна):**
- Если объект не найден на сцене — в Inspector появляется поле **Prefab Preview**
- Перетащите туда префаб из Project, чтобы Editor показал его компоненты и свойства
- Позволяет полностью настроить условие (Component, Property, Compare, Threshold) до запуска игры
- Поле автоматически скрывается, если объект уже найден на сцене
- **Не используется в Runtime** — только для настройки в Editor

**Визуализация в Inspector:**
- Зелёная полоска слева у условий с поиском по имени
- В Edit Mode — preview найденного объекта для настройки компонентов/свойств
- В Edit Mode (объект не найден) — поле Prefab Preview + информационное сообщение
- В Play Mode — отображение найденного объекта (`Found Object`)
- Summary: `"PlayerHP".Health.Hp == 0`

## Logic Mode

- **AND** — все условия должны быть `true`
- **OR** — хотя бы одно условие должно быть `true`

## Check Mode

| Режим | Описание |
|-------|----------|
| **Manual** | Проверка только при вызове `Check()` (из UnityEvent другого компонента) |
| **EveryFrame** | Проверка каждый кадр в `Update()` |
| **Interval** | Проверка с заданным интервалом (по умолчанию 0.5 сек) |

## Параметры

| Параметр | Описание |
|----------|----------|
| **Check On Start** | Выполнить проверку при `Start()` (по умолчанию **включено**) |
| **Only On Change** | Вызывать события только когда результат изменился (не каждый тик) |

## События

| Событие | Описание |
|---------|----------|
| **On True** | Вызывается когда результат = true |
| **On False** | Вызывается когда результат = false |
| **On Result (bool)** | Вызывается при каждой проверке с текущим результатом |
| **On Inverted Result (bool)** | Вызывается при каждой проверке с инвертированным результатом (!result) |

## Защита от null / уничтоженных объектов

NeoCondition безопасно обрабатывает ситуации, когда целевой объект или компонент уничтожается в рантайме:

| Ситуация | Поведение |
|----------|----------|
| `Source Object` уничтожен | Warning в консоль (однократно), условие возвращает `false` |
| Компонент уничтожен | Кеш сбрасывается, Warning, условие = `false` |
| `Find By Name` объект уничтожен | Автоматический повторный поиск при следующей проверке |
| `Find By Name` объект ещё не создан | `false` без Warning (при `Wait For Object = ON`) или Warning + `false` |
| Ошибка reflection (поле удалено) | Warning, кеш сброшен, условие = `false` |
| Исключение в одном условии | Не ломает остальные условия, Warning с индексом |

Предупреждения выводятся **однократно** на каждое условие (не спамят в EveryFrame). Сброс через `ResetState()` или `InvalidateAllCaches()`.

## Визуализация в Inspector

Каждое условие отображается с цветовой полоской слева:
- **Голубая** — режим Component
- **Жёлтая** — режим GameObject
- **Зелёная** — поиск по имени (Find By Name)
- **Красная** — инверсия (NOT) включена

## Примеры

### Пример 1: Game Over при HP <= 0

1. На персонаже: компоненты `Health`, `NeoCondition`
2. NeoCondition:
   - Condition [0]: Source=Component, Component = Health, Property = currentHealth, Compare = LessOrEqual, Threshold = 0
   - Check Mode = EveryFrame
   - On True → GameOverPanel.SetActive(true)

### Пример 2: Разблокировать уровень при Score >= 100

1. На менеджере: `ScoreManager`, `NeoCondition`
2. NeoCondition:
   - Condition [0]: Source=Component, Component = ScoreManager, Property = Score, Compare = GreaterOrEqual, Threshold = 100
   - Check Mode = Manual
3. На `ScoreManager.OnScoreChanged` → NeoCondition.Check()
4. On True → NextLevelButton.SetInteractable(true)

### Пример 3: Проверка — объект активен

1. NeoCondition, Source=GameObject
2. Condition [0]: Property = activeSelf, Compare = Equal, Threshold = true
3. On False → ShowWarning("Object is disabled!")

### Пример 4: Найти врага по имени и проверить его HP

1. NeoCondition:
   - Condition [0]: Find By Name = true, Object Name = "Boss", Source=Component
   - Component = Health, Property = Hp, Compare = LessOrEqual, Threshold = 0
   - Check Mode = Interval (0.5 сек)
2. On True → ShowVictoryScreen()

### Пример 5: Проверка префаба, который ещё не на сцене

1. NeoCondition:
   - Find By Name = true, Object Name = "Player"
   - **Wait For Object** = true (ожидаем спавн без Warning)
   - **Prefab Preview** = перетащить префаб Player из Project (для настройки)
   - Component = Health, Property = Hp, Compare = GreaterOrEqual, Threshold = 1
2. При спавне Player'а — условие автоматически подхватит объект и начнёт работать
3. On True → UI.ShowPlayerAlive()

### Пример 6: AND — убить врага когда HP <= 0 И нет щита

1. NeoCondition, Logic = AND
2. Condition [0]: Health.currentHealth <= 0
3. Condition [1]: Shield.isActive == false (NOT)
4. On True → Enemy.Die()

### Пример 7: OR — показать предупреждение

1. NeoCondition, Logic = OR
2. Condition [0]: Health.currentHealth <= 20
3. Condition [1]: Timer.timeRemaining <= 5
4. On True → WarningUI.SetActive(true)

## Публичный API

```csharp
// Проверить условия и вызвать события
neoCondition.Check();

// Оценить без вызова событий
bool result = neoCondition.Evaluate();

// Сбросить состояние (следующий Check вызовет событие)
neoCondition.ResetState();

// Текущий результат
bool last = neoCondition.LastResult;

// Добавить/удалить условие в рантайме
neoCondition.AddCondition(entry);
neoCondition.RemoveCondition(entry);

// Сбросить кеш reflection (кеш Find By Name сохраняется)
neoCondition.InvalidateAllCaches();
```

### ConditionEntry API

```csharp
entry.Source = SourceMode.Component;       // или SourceMode.GameObject
entry.UseSceneSearch = true;               // включить поиск по имени
entry.SearchObjectName = "Player";         // имя для поиска
entry.WaitForObject = true;               // ожидать спавн без Warning
entry.PrefabPreview = prefabAsset;        // только для Editor-настройки
entry.SourceObject = someGameObject;       // прямая ссылка

entry.InvalidateCache();                   // сбросить reflection-кеш (Find-кеш остаётся)
entry.InvalidateCacheFull();               // полный сброс (включая Find-кеш)

GameObject found = entry.FoundByNameObject; // найденный объект (runtime, read-only)
```

## Производительность

- Reflection вызывается только при первом чтении, результат кешируется
- `GameObject.Find()` вызывается однократно, результат кешируется пока объект жив
- Для большого количества объектов используйте режим **Interval** (0.2-0.5 сек)
- **EveryFrame** подходит для 10-20 условий, для сотен — Interval
- **Manual** — самый быстрый, проверяет только по запросу
- Уничтоженные объекты/компоненты обрабатываются безопасно без спама в консоль

## Namespace

`Neo.Condition`

## Assembly

`Neo.Condition` (`Assets/Neoxider/Scripts/Condition/Neo.Condition.asmdef`)
