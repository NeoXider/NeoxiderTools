# Магазин (Shop)

**Что это:** модуль внутриигрового магазина и экономики: Shop (контроллер), ShopItem, ShopItemData, ButtonPrice, Money, TextMoney, интерфейсы IMoneySpend/IMoneyAdd. Скрипты в `Scripts/Shop/`.

**Навигация:** [← К Docs](../README.md) · оглавление — список внизу страницы

---

## Компоненты

### Логика магазина
- **Shop**: Центральный контроллер магазина.
- **ShopItem**: Визуальное представление одного товара.
- **ShopItemData**: `ScriptableObject` для хранения данных о товарах.
- **ButtonPrice**: Универсальная кнопка с поддержкой цены и состояний (купить, выбрать, выбрано).

### Система денег
- **Money**: Управление балансом (один синглтон `Money.I` или несколько экземпляров — монеты/энергия; у главного включите Set Instance On Awake).
- **TextMoney**: Отображение суммы; опционально поле **Money Source** — свой Money (например энергия), иначе `Money.I`. Инициализация в Start.
- **IMoneySpend / IMoneyAdd**: Интерфейсы для стандартизации операций с деньгами.

## Оглавление
- [Shop](./Shop.md)
- [ShopItem](./ShopItem.md)
- [ShopItemData](./ShopItemData.md)
- [ButtonPrice](./ButtonPrice.md)
- [Money](./Money.md)
- [TextMoney](./TextMoney.md)
- [Интерфейсы (IMoneySpend, IMoneyAdd)](./InterfaceMoney.md)
