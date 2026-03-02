# QuestConfig

**Что это:** класс `QuestConfig` (ScriptableObject). Один ассет = один квест: Id, название, описание, список целей (Objectives), условия доступности (Start Conditions). Конфиг не хранит прогресс — только данные квеста. Файл: `Assets/Neoxider/Scripts/Quest/QuestConfig.cs`, пространство имён: `Neo.Quest`.

**Как с ним работать:**
1. Создать ассет: в Project ПКМ → **Create → Neoxider → Quest → Quest Config**.
2. Заполнить **Id** (уникальная строка без пробелов, например `MainQuest_01`), **Title**, **Description**.
3. В **Objectives** добавить цели: для каждой выбрать **Type** (KillCount, CollectCount, CustomCondition и т.д.) и при необходимости **Target Id**, **Required Count**, **Condition**.
4. При необходимости добавить **Start Conditions** (ConditionEntry) — иначе квест можно принять всегда.
5. Этот конфиг добавить в **Known Quests** у QuestManager в сцене, чтобы `AcceptQuest(questId)` находил квест по Id.

---

## Поля

### Identity
| Поле | Описание |
|------|-----------|
| **Id** | Уникальный ключ. Используется в AcceptQuest(questId), GetState(questId), в событиях. При пустом Id в редакторе подставляется из Title при сохранении ассета. |

### Display
| Поле | Описание |
|------|-----------|
| **Title** | Название для UI. |
| **Description** | Описание задания (TextArea). |

### Objectives
| Поле | Описание |
|------|-----------|
| **Objectives** | Список целей. Порядок = индекс цели (0, 1, 2, …). Этот индекс передаётся в CompleteObjective(questId, objectiveIndex) и в Quest Objective Notifier (Objective Index). |

### Start Conditions
| Поле | Описание |
|------|-----------|
| **Start Conditions** | Список ConditionEntry. При AcceptQuest менеджер вызывает для каждой записи `ConditionEntry.Evaluate(context)`; context = **Condition Context** (GameObject) у QuestManager. Все записи должны вернуть true (AND). Пустой список = квест доступен всегда. |

### Optional
| Поле | Описание |
|------|-----------|
| **Next Quest Ids** | Список Id квестов. Модуль сам по себе ничего с ними не делает — используйте в коде по событию QuestCompleted (например показ новых квестов в UI). |

---

## QuestObjectiveData (одна цель)

В каждом элементе Objectives задаётся:

| Поле | Когда заполнять |
|------|-----------------|
| **Type** | Всегда. KillCount, CollectCount, CustomCondition, ReachPoint, Talk. |
| **Target Id** | Для KillCount/CollectCount — строка, совпадающая с аргументом NotifyKill(enemyId) / NotifyCollect(itemId). |
| **Required Count** | Для KillCount/CollectCount — сколько раз нужно (например 3 гоблина). |
| **Condition** | Опционально для CustomCondition: проверка по контексту в менеджере. Если пусто — цель засчитывается только вызовом CompleteObjective или Notifier. |

Типы: **KillCount** — прогресс от NotifyKill; **CollectCount** — от NotifyCollect; **CustomCondition** / **ReachPoint** / **Talk** — один вызов CompleteObjective или Notifier.

---

## Условия старта

Формат ConditionEntry такой же, как в [NeoCondition](../Condition/NeoCondition.md): объект (или поиск по имени) → компонент/GameObject → свойство → оператор → порог. Контекст для Evaluate — GameObject из поля **Condition Context** у QuestManager (обычно игрок).

---

## Примеры

- **«Убить 3 гоблинов»:** один Objective, Type = KillCount, Target Id = `Goblin`, Required Count = 3.
- **«Убить гоблинов и принести ключ»:** цель 0 — KillCount, Goblin, 3; цель 1 — CustomCondition без Condition, зачёт через Quest Objective Notifier при подборе ключа (Objective Index = 1).
- **Квест с условием доступа:** в Start Conditions одна запись, например компонент игрока, свойство Level (int), ≥ 5.
