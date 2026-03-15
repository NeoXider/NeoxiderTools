# Level Curve Definition

**Что это:** ScriptableObject, определяющий зависимость уровня от накопленного XP. Реализует `ILevelCurveDefinition`. Три режима: **Formula** (формула по типу), **Curve** (график AnimationCurve), **Custom** (ручная таблица уровень → XP). Используется в `LevelComponent` и через `LevelModel.SetCurveDefinition()`. Путь: `Scripts/Core/Level/Data/LevelCurveDefinition.cs`, пространство имён `Neo.Core.Level`. Создание ассета: меню **Create → Neoxider → Core → Level Curve Definition**.

**Как использовать:**
1. Создать ассет через меню выше.
2. Выбрать **Режим**: Formula, Curve или Custom.
3. В зависимости от режима задать параметры формулы, ключи кривой или список записей (уровень, требуемый XP).
4. Назначить ассет в поле **Level Curve** у `LevelComponent`; уровень и XP до следующего уровня будут считаться по этому определению.

---

## Режимы (LevelCurveMode)

| Режим | Описание |
|-------|----------|
| **Formula** | Уровень по формуле. Выбирается **тип формулы** (LevelFormulaType) и параметры; кумулятивный XP для уровня L задаётся выражением. |
| **Curve** | Уровень по графику. **AnimationCurve**: по оси X — номер уровня (1, 2, 3…), по Y — кумулятивный XP до этого уровня. Удобно для произвольной кривой прогрессии. |
| **Custom** | Ручная таблица. Список записей **LevelCurveEntry** (уровень, требуемый XP). Полный контроль над порогами. |

---

## Типы формул (LevelFormulaType)

Используются при режиме **Formula**. Во всех случаях: **RequiredXp(level)** — кумулятивный XP для достижения уровня; текущий уровень по totalXp находится как максимальный level, для которого RequiredXp(level) ≤ totalXp.

| Тип | Формула RequiredXp(level) | Параметры в инспекторе |
|-----|----------------------------|-------------------------|
| **Linear** | (level − 1) × xpPerLevel | Xp Per Level |
| **LinearWithOffset** | constantOffset + (level − 1) × xpPerLevel | Constant Offset, Xp Per Level |
| **Quadratic** | quadraticBase × (level − 1)² | Quadratic Base |
| **Exponential** | expBase × expFactor^(level − 1) | Exp Base, Exp Factor |
| **Power** | powerBase × (level − 1)^powerExponent | Power Base, Power Exponent |
| **PolynomialSingle** | то же, что Power | Power Base, Power Exponent |

Формулы можно вынести в отдельный переиспользуемый модуль (магазин, улучшения); доменная логика в `LevelCurveEvaluator` не зависит от Unity.

---

## Поля (Inspector)

### Режим

| Поле | Тип | Назначение |
|------|-----|------------|
| Mode | LevelCurveMode | Formula / Curve / Custom. |
| Formula Type | LevelFormulaType | Тип формулы (при Mode = Formula). |

### Параметры формулы (Mode = Formula)

| Поле | Тип | Назначение |
|------|-----|------------|
| Xp Per Level | int | XP за уровень (Linear, LinearWithOffset). |
| Constant Offset | float | Сдвиг (только LinearWithOffset). |
| Quadratic Base | float | База для level² (Quadratic). |
| Exp Base, Exp Factor | float | База и множитель для экспоненты (Exponential). |
| Power Base, Power Exponent | float | База и степень для Power / PolynomialSingle. |

### Кривая (Mode = Curve)

| Поле | Тип | Назначение |
|------|-----|------------|
| Animation Curve | AnimationCurve | X = уровень (1, 2, 3…), Y = кумулятивный XP до уровня. |

### Ручная таблица (Mode = Custom)

| Поле | Тип | Назначение |
|------|-----|------------|
| Custom Entries | List&lt;LevelCurveEntry&gt; | Список пар (уровень, требуемый XP). |

---

## API (ILevelCurveDefinition)

| Метод / свойство | Возврат | Описание |
|------------------|---------|----------|
| EvaluateLevel(int totalXp, int maxLevel = 0) | int | Вычисляет уровень по накопленному XP; maxLevel = 0 — без ограничения. |
| GetXpToNextLevel(int totalXp, int maxLevel = 0) | int | XP до следующего уровня; 0 если на макс. уровне. |
| Mode | LevelCurveMode | Текущий режим. |
| FormulaType | LevelFormulaType | Тип формулы (при Formula). |
| XpPerLevel | int | Параметр формулы (get/set). |
| SetLinear(int xpPerLevel) | void | Задать режим Formula, тип Linear и XP за уровень (для тестов/рантайма). |
| TryGetDefinition(int level, out LevelCurveEntry entry) | bool | Получить запись по уровню (имеет смысл для Custom). |

---

## Примеры

**No-Code:** создать ассет Level Curve Definition → выбрать Formula, Linear, Xp Per Level = 100 → назначить в LevelComponent. Уровень будет считаться как 1 + totalXp / 100.

**Код:**

```csharp
// Назначение в компоненте
LevelComponent levelComponent = go.GetComponent<LevelComponent>();
levelComponent.LevelCurveDefinition = myCurveAsset;

// Прямой расчёт по определению
ILevelCurveDefinition curve = myCurveAsset;
int level = curve.EvaluateLevel(totalXp, maxLevel: 50);
int xpToNext = curve.GetXpToNextLevel(totalXp, maxLevel: 50);

// Рантайм: линейная кривая для теста
var def = ScriptableObject.CreateInstance<LevelCurveDefinition>();
def.SetLinear(100);
```

---

## См. также

- [Level.md](./Level.md) — LevelComponent, ILevelProvider, использование кривой.
- [HealthComponent.md](./HealthComponent.md) — пулы HP/Mana.
- [Progression/README.md](../Progression/README.md) — награды за уровень, перки (Progression использует свой LevelCurveDefinition для дерева прогресса).
