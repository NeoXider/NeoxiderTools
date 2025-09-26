# Chance Data

`ChanceData` — ScriptableObject-обёртка над `ChanceManager`. Позволяет хранить таблицу вероятностей в проекте и переиспользовать её из разных сцен/скриптов.

---

## Основные поля
- **Manager** — встроенный `ChanceManager`. Здесь редактируются записи, веса и флаг `Locked`.
- **On Id Generated** — UnityEvent, вызываемый после `GenerateId()`.

При изменениях в инспекторе менеджер автоматически нормализуется и приводит идентификаторы к уникальным значениям.

---

## API
```csharp
[SerializeField] private ChanceData lootTable;

void Drop()
{
    int id = lootTable.GenerateId();      // Возвращает индекс записи
    float weight = lootTable.Manager.GetChanceValue(id); // Текущий вес (сырая величина)
}
```

Доступные методы:
- `int GenerateId()` — получить результат и вызвать UnityEvent.
- `int AddChance(float weight)` / `void SetChance(int index, float weight)` / `void RemoveChance(int index)` — управление списком.
- `float GetChance(int index)` — получить текущий вес.
- `void ClearChances()` — очистить все записи.

Сериализованный `ChanceManager` можно копировать в другие экземпляры через `Manager.CopyFrom(...)`.
