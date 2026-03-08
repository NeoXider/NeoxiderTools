# ProgressionNoCodeAction

**Что это:** `MonoBehaviour`-bridge из `Scripts/Progression/Bridge/ProgressionNoCodeAction.cs` для запуска действий прогрессии из `UnityEvent` без написания кода.

**Как использовать:**
1. Добавьте `ProgressionNoCodeAction` на объект сцены.
2. Назначьте `ProgressionManager` или оставьте пустым для работы через singleton.
3. Выберите `Action Type`.
4. Заполните `XP Amount`, `Perk Points Amount`, `Node Id` или `Perk Id` в зависимости от действия.
5. Вызовите `Execute()` из `Button`, `Animation Event`, `Quest`, `Condition` или другого `UnityEvent`.

**Навигация:** [← К Progression](./README.md)

---

## Поддерживаемые действия

| Action Type | Назначение |
|------------|------------|
| `AddXp` | Добавляет XP |
| `GrantPerkPoints` | Добавляет perk points |
| `UnlockNode` | Пытается открыть узел по `Node Id` |
| `BuyPerk` | Пытается купить перк по `Perk Id` |
| `ResetProgression` | Сбрасывает профиль |
| `SaveProfile` | Принудительно сохраняет профиль |
| `LoadProfile` | Принудительно загружает профиль |

## События

| Событие | Когда вызывается |
|--------|-------------------|
| `_onSuccess` | Действие выполнено успешно |
| `_onFailed(string)` | Действие не выполнено, передаётся причина |
| `_onResultMessage(string)` | Унифицированное сообщение результата |

## Типичные сценарии

- Кнопка награды: `Button.onClick -> Execute(AddXp)`.
- Узел дерева: `Button.onClick -> Execute(BuyPerk)`.
- Отладочная кнопка в сцене: `Execute(ResetProgression)`.
