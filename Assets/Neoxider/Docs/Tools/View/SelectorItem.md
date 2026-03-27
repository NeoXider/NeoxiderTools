# SelectorItem

**Что это:** MonoBehaviour на каждом элементе, управляемом Selector. Хранит индекс, ссылку на родительский Selector, реагирует на команды активации/деактивации в режиме **NotifySelectorItemsOnly**. Путь: `Scripts/Tools/View/SelectorItem.cs`, пространство имён `Neo.Tools`.

**Как использовать:**
1. Добавьте **SelectorItem** на каждый дочерний объект Selector.
2. На родителе включите **Notify Selector Items Only**. Флаг **Control Game Object Active** у родительского **Selector** оставьте **включённым** (иначе вызовы к **SelectorItem** не пойдут). Это **не** означает, что Unity будет включать/выключать дочерние `GameObject`: при наличии **SelectorItem** селектор дергает только **`SelectorItem.SetActive`**, который сам по себе **не** вызывает `gameObject.SetActive`.
3. Подпишитесь на **OnActivated** / **OnDeactivated** для визуала или логики; при «исправлении» вызовите **ExcludeFromSelector()**.

---

## Поля и свойства

| Имя | Тип | Назначение |
|-----|-----|------------|
| Index | int | Индекс элемента в родительском Selector (get/set). Проставляется Selector при RefreshItemsFromChildren. |
| Active | ReactivePropertyBool | Текущее активное состояние (true = выбран). Подписка через Active.OnChanged. |
| ActiveValue / ValueBool | bool | Текущее состояние для чтения (NeoCondition, привязки). |

## События

| Событие | Когда вызывается | Параметры |
|---------|------------------|-----------|
| OnActivated | Элемент перешёл в активное состояние (выбран). | — |
| OnDeactivated | Элемент перешёл в неактивное состояние. | — |
| OnValueChangeInverse | При смене состояния; передаёт инверс нового значения (true при деактивации, false при активации). Прямое значение — через Active.OnChanged. | bool |

## Методы

| Сигнатура | Возврат | Описание |
|-----------|---------|----------|
| SetActive(bool active) | void | Устанавливает активное состояние. Вызывается из Selector в режиме NotifySelectorItemsOnly, только если у Selector включено управление активностью. Обновляет Active и вызывает события. |
| ExcludeFromSelector() | void | Исключает свой индекс из пула родительского Selector (Selector.ExcludeIndex(Index)). |
| IncludeInSelector() | void | Возвращает свой индекс в пул (Selector.IncludeIndex(Index)). |
| GetSelector() | Selector | Возвращает родительский Selector или null. |

## См. также

- [Selector](Selector.md) (раздел 3.6 «Менеджер аномалий»)
- [Пример: игра про аномалии](../../Examples/AnomalyGame.md)
