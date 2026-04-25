# PlayerData (пример)

**Что это:** пример сохраняемого компонента: реализует `ISaveableComponent`, помечает поля `[SaveField]`, в OnDataLoaded применяет загруженные данные к объекту. Пространство имён: `Neo.Save.Examples`. Файл: `Scripts/Save/Example/PlayerData.cs`.

**Как использовать:** ориентир для своих классов: наследование от SaveableBehaviour, поля с [SaveField], логика в OnDataLoaded. См. [Save README](./README.md), [SaveableBehaviour](./SaveableBehaviour.md).

---


## Дополнительные поля

| Поле | Описание |
|------|----------|
| `IsLoad` | Is Load. |
| `_money` | Money. |
| `playerPosition` | Player Position. |
| `playerScore` | Player Score. |