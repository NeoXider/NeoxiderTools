# Интерфейсы IMoneySpend и IMoneyAdd

**Что это:** контракты для списания и начисления валюты (файл `Assets/Neoxider/Scripts/Shop/InterfaceMoney.cs`, глобальное пространство имён). `IMoneySpend`: метод `bool Spend(float count)`. `IMoneyAdd`: метод `void Add(float count)`. Реализует [Money](Money.md); [Shop](Shop.md) использует IMoneySpend для оплаты.

**Как использовать:** реализовать интерфейс на своём компоненте или использовать Money. В Shop в поле **Money Spend Source** указать GameObject с IMoneySpend.

---

## IMoneySpend

**Публичные свойства и поля**:
У данного интерфейса нет свойств или полей.

**Публичные методы**:
- `Spend(float count)`: Метод для попытки потратить указанное количество денег. Возвращает `bool` (`true`, если трата успешна, `false` в противном случае).

**Unity Events**:
У данного интерфейса нет `UnityEvent`.

---

## IMoneyAdd

**Публичные свойства и поля**:
У данного интерфейса нет свойств или полей.

**Публичные методы**:
- `Add(float count)`: Метод для добавления указанного количества денег.

**Unity Events**:
У данного интерфейса нет `UnityEvent`.
