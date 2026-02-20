# WheelFortuneImproved

Улучшенный компонент рулетки: состояния Idle/Spinning/Decelerating/Aligning, null-безопасность, события, настраиваемые шансы (inline-веса, ChanceData, ChanceSystemBehaviour) и гарантированный результат через `SpinToResult(id)`.

**Рекомендуется для новых сцен.** Старый [WheelFortune](./WheelFortune.md) помечен устаревшим и сохранён для обратной совместимости.

---

## 1. Публичные члены

| Член | Тип | Описание |
|------|-----|----------|
| `State` | `SpinState` | Текущее состояние (Idle, Spinning, Decelerating, Aligning). |
| `Items` | `GameObject[]` | Массив призов (секторов). |
| `canUse` | `bool` | Можно ли крутить колесо; при присвоении выставляет `CanvasGroup.interactable`, если ссылка задана. |
| **Spin()** | `void` | Запуск вращения. Результат выбирается в начале по весам или равный случай. |
| **SpinToResult(int id)** | `void` | Гарантированно выигрывает сектор с индексом `id` (0..Items.Length-1). |
| **Stop()** | `void` | Переход в замедление (если не Idle). |
| **GetPrize(int id)** | `GameObject` | Приз по индексу; при неверном `id` — `null`. |

**Unity Events:**

| Событие | Тип | Когда вызывается |
|---------|-----|-------------------|
| `OnWinIdVariant` | `UnityEvent<int>` | В момент полной остановки; передаётся ID выигрышного сектора. Совместим с WheelMoneyWin. |
| `OnSpinStarted` | `UnityEvent` | При переходе в Spinning. |
| `OnDecelerationStarted` | `UnityEvent` | При переходе в Decelerating. |
| `OnAlignmentStarted` | `UnityEvent` | При переходе в Aligning (плавное доворачивание к сектору). |
| `OnStopped` | `UnityEvent` | Когда колесо полностью остановилось (после выравнивания или замедления). Удобно для закрытия UI, включения кнопок. |
| `OnSpinBlocked` | `UnityEvent` | Когда вызвали Spin() или SpinToResult(), но вращение не стартовало (уже крутится или single-use исчерпан). Для подсказок в UI или звука «занято». |

---

## 2. Шансы и веса

Результат определяется **в начале вращения** (в `Spin()` или `SpinToResult`). При вызове **Spin()** источник результата (по приоритету):

1. **SpinToResult(id)** — если перед этим вызван `SpinToResult(id)`, используется переданный id.
2. **ChanceSystemBehaviour** — если в инспекторе задана ссылка, вызывается `GetId()`.
3. **ChanceData** — если задан ScriptableObject, используется `Manager.TryEvaluate`; количество записей должно соответствовать количеству секторов.
4. **Inline-веса** (`Sector Weights`) — массив весов по одному на сектор; пустой или нулевая сумма = равные шансы.
5. **Равные шансы** — случайный индекс 0..Items.Length-1.

При **включённом выравнивании** колесо доворачивается до выбранного сектора. При **выключенном** результат в `OnWinIdVariant` берётся по текущему углу остановки. Для весов и `SpinToResult` рекомендуется включать выравнивание.

---

## 3. No-Code: настройка в инспекторе

### 3.1 Базовая настройка

1. Добавьте на объект с колесом компонент **WheelFortuneImproved**.
2. **Wheel Transform** — RectTransform вращающегося колеса.
3. **Arrow** — RectTransform стрелки (неподвижной).
4. **Items** — массив дочерних объектов секторов (или нажмите **Set Prizes** в секции Prize Items, чтобы заполнить из дочерних элементов Wheel Transform).
5. **Canvas Group** (опционально) — для блокировки кнопки «Крутить» во время вращения. Компонент выставляет `interactable` по `canUse` при старте вращения и восстанавливает при полной остановке (в т.ч. при отключённом выравнивании или сбое инициализации выравнивания).

### 3.2 Кнопка «Крутить» без кода

- Создайте кнопку (Button).
- В **On Click ()** добавьте вызов: объект с **WheelFortuneImproved** → метод **Spin()** (без параметров).
- Колесо будет крутиться по настройкам компонента (равные шансы или веса из полей).

### 3.3 Выдача приза без кода (WheelMoneyWin)

1. Добавьте на тот же объект (или на другой) компонент **WheelMoneyWin**.
2. В **WheelMoneyWin** заполните массив **Wins** (размер = количеству секторов): сумма выигрыша по индексу (например `[10, 25, 50, 100, 0, 25, 50, 10]`).
3. В **WheelFortuneImproved** в событии **On Win Id Variant** добавьте вызов: объект с **WheelMoneyWin** → метод **Win** (Dynamic int) — Unity подставит ID сектора как аргумент.
4. Опционально: в **Prize** укажите TMP_Text для отображения выигрыша (WheelMoneyWin сам выведет туда число).

### 3.4 Звук и анимация без кода

- **On Spin Started** — включить звук вращения, показать затемнение/оверлей.
- **On Deceleration Started** — сменить звук на «замедление».
- **On Alignment Started** — начать анимацию «фиксации» сектора.
- **On Win Id Variant** — помимо WheelMoneyWin: воспроизвести звук победы, показать частицы, открыть панель приза.
- **On Stopped** — закрыть оверлей, включить кнопки, финальная анимация (вызывается вместе с On Win Id Variant).
- **On Spin Blocked** — показать подсказку «нельзя крутить» или воспроизвести звук «занято».

### 3.5 Шансы без кода (только инспектор)

- **Sector Weights** — массив float (один элемент на сектор). Например `[1, 1, 2, 1, 0.5f]` — третий сектор выпадает чаще, пятый реже. Пустой массив или нулевая сумма = равные шансы.
- **Chance Data** — перетащите ScriptableObject Chance Data (Neoxider/Tools/Random); количество записей должно совпадать с количеством секторов.
- **Chance System Behaviour** — перетащите компонент ChanceSystemBehaviour со сцены, если веса настроены на нём.

### 3.6 Гарантированный приз (тест) без кода

- Для одной кнопки «Тест: выиграть сектор 5»: в **On Click ()** вызвать **SpinToResult** (Dynamic int) и передать **5**. Колесо гарантированно остановится на секторе с индексом 5.

### 3.7 Тестовые кнопки в инспекторе (атрибут [Button])

В инспекторе доступны кнопки для отладки: **Spin**, **Stop**, **SpinToResult** (с полем для ID сектора), **AllowSpinAgain** (сброс single-use), **LogState** (текущее состояние и результат по углу в консоль), **ResetWheelAngle**, **ArrangePrizesFromEditor**. Требуется поддержка атрибута Button (например, Odin Inspector).

---

## 4. Примеры кода

### 4.1 Запуск вращения и подписка на результат

```csharp
using Neo.Bonus;
using UnityEngine;

public class WheelDemo : MonoBehaviour
{
    [SerializeField] private WheelFortuneImproved _wheel;

    private void OnEnable()
    {
        if (_wheel != null)
            _wheel.OnWinIdVariant.AddListener(OnWheelStopped);
    }

    private void OnDisable()
    {
        if (_wheel != null)
            _wheel.OnWinIdVariant.RemoveListener(OnWheelStopped);
    }

    private void OnWheelStopped(int sectorId)
    {
        Debug.Log($"Выпал сектор: {sectorId}");
        GameObject prize = _wheel.GetPrize(sectorId);
        if (prize != null)
            Debug.Log($"Приз: {prize.name}");
    }

    public void OnSpinButtonClicked()
    {
        _wheel?.Spin();
    }
}
```

### 4.2 Гарантированный результат (сценарий, туториал)

```csharp
using Neo.Bonus;
using UnityEngine;

public class TutorialWheel : MonoBehaviour
{
    [SerializeField] private WheelFortuneImproved _wheel;

    // Вызвать из кнопки или по условию: дать игроку приз за сектор 2
    public void GivePrizeSector2()
    {
        if (_wheel != null && _wheel.Items != null && 2 < _wheel.Items.Length)
            _wheel.SpinToResult(2);
    }
}
```

### 4.3 Разрешить крутить снова после награды

```csharp
[SerializeField] private WheelFortuneImproved _wheel;

void OnRewardGiven()
{
    _wheel.canUse = true; // или через свойство, если нужна блокировка кнопки через CanvasGroup
}
```

### 4.4 Проверка состояния и события старта/замедления

```csharp
_wheel.OnSpinStarted.AddListener(() => Debug.Log("Колесо крутится"));
_wheel.OnDecelerationStarted.AddListener(() => Debug.Log("Замедление"));
_wheel.OnAlignmentStarted.AddListener(() => Debug.Log("Выравнивание"));

if (_wheel.State == WheelFortuneImproved.SpinState.Idle)
    _wheel.Spin();
```

### 4.5 Использование GetPrize и индекса из события

```csharp
_wheel.OnWinIdVariant.AddListener(id =>
{
    GameObject prizeObj = _wheel.GetPrize(id);
    if (prizeObj == null) return;
    // Настроить UI, выдать награду по id или по данным на prizeObj
});
```

---

## 5. Настройка полей (кратко)

| Группа | Поле | Назначение |
|--------|------|------------|
| Usage | Single Use | Одно вращение до следующего разрешения (canUse). |
| | Can Use | Разрешить крутить; связано с Canvas Group. |
| Stop Timing | Auto Stop Time | Через сколько секунд вызвать Stop (0 = только вручную). |
| | Extra Spin Time | Случайная добавка к Auto Stop Time. |
| Transforms | Offset Z / Wheel Offset Z | Смещение колеса и стрелки (градусы). |
| Spin Settings | Rotate Left, Initial Angular Velocity, Angular Deceleration | Направление и «физика» вращения. |
| Alignment | Enable Alignment | Включить доворачивание до сектора (нужно для весов и SpinToResult). |
| | Alignment Duration | Длительность доворачивания (сек). |
| Prize Items | Items, Prize Distance, Auto Arrange Prizes, Set Prizes | Секторы и их расстановка по кругу. |
| Chances | Sector Weights, Chance Data, Chance System Behaviour | Источники вероятностей (см. раздел 2). |

---

## 6. Совместимость с WheelMoneyWin

**WheelMoneyWin** подписывается на **OnWinIdVariant** и по индексу выдаёт сумму из массива **Wins**. Работает и со старым WheelFortune, и с WheelFortuneImproved (один и тот же контракт `UnityEvent<int>`). Подключение в инспекторе: On Win Id Variant → WheelMoneyWin.Win (Dynamic int).
