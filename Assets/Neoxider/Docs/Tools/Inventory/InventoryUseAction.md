# InventoryUseAction

Optional scene/Inspector adapter over [exact item selection](InventorySelection.md).
Namespace `Neo.Tools`, assembly `Neo.Tools.Inventory`. The storage and consumption APIs also work
directly from C# without this component.

1. Assign **Hand**. Keep **Listen To Hand** enabled to subscribe while this adapter is enabled.
2. Set **Item Id** to a specific type, or `-1` to accept every type.
3. Set **Consume Amount**: `1` spends one selected unit; `0` is reusable.
4. Connect **On Used** to the gameplay effect. Its dynamic `InventoryItemRecord` contains the exact UID
   and current payload. **On Rejected** can display an invalid/stale selection or insufficient quantity.
5. A button may call `UseEquipped()`. When wiring `OnUseSelectionRequested` manually to `UseSelection`,
   turn **Listen To Hand** off to avoid subscribing twice.

If a legacy `OnUseItemRequested` listener already spends items, migrate that spending to the exact
selection path before adding this adapter. Binding both paths to consume can spend twice.

`TryUse(InventoryComponent inventory, InventorySelection selection)` is the explicit C# entry point.
Filtered, stale, foreign or insufficient requests return false and do not invoke the effect. Positive
consumption commits before `OnUsed`; inventory notifications/save are then attempted even if the effect
listener throws. Listener faults propagate and do not roll back consumption. Later listeners in a
throwing UnityEvent are not replayed. A reusable action reads current payload and does not mutate storage.
Recursive use of the same adapter while its effect is executing is rejected.

For effects with application-specific preconditions (full HP, occupied podium, cooldown), check them
before calling use. Placement that must preserve ownership should transfer the exact instance to the
destination inventory rather than consume it and recreate a different item.

**По-русски:** назначьте руку, тип предмета и расход. В `OnUsed` приходит именно выбранная запись с UID
и состоянием. Расход `0` позволяет повторное использование; расход `1` удаляет ровно одну единицу
выбранного стека/экземпляра. Проверяйте игровые условия до вызова: полное HP или занятый подиум не должны
зря тратить предмет. Для размещения уникальной вещи используйте `TransferSelection`, сохраняя UID.

Sample: `Demo/Scenes/Inventory/InventoryUseDemo.unity` uses this adapter for CELL consumption/healing.
The sample view owns the healing effect and goal feedback; library runtime contains no specimen or
game-specific healing rules. See [Inventory entry point](README.md).
