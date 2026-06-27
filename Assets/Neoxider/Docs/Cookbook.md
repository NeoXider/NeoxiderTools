# Cookbook — рецепты и интересные примеры

Практические **кросс-модульные** связки: как из готовых компонентов NeoxiderTools собрать
типовую механику **без своего кода** (или с минимумом). В отличие от справочников по отдельным
компонентам, здесь примеры «end-to-end» — что на какой объект повесить и какие события связать.

> Документация по конкретным компонентам — в их модулях ([README](./README.md)). Здесь — **сборки из них**.

## Содержание
- [Энергия с потолком + авто-реген](#энергия-с-потолком--авто-реген)
- [Капнутый ресурс/валюта](#капнутый-ресурсвалюта)
- [Ежедневная награда (daily reward)](#ежедневная-награда-daily-reward)
- [Слот-машина → кошелёк](#слот-машина--кошелёк)
- [Магазин: купить и надеть](#магазин-купить-и-надеть)
- [Полёт награды в HUD](#полёт-награды-в-hud)

---

## Энергия с потолком + авто-реген

**Цель:** ресурс «энергия», который сам восстанавливается **+1 каждые 2 минуты**, не превышает **30**
от регена, но **может уходить выше 30** от бонусов/слотов. Только компоненты и события — без скриптов.

**Один объект `Energy`, два компонента:**

1. **`Money`** (кошелёк энергии):
   - `_moneySave = "Energy"` — свой ключ сейва, отдельно от основной валюты;
   - `_persistMoney = true`;
   - **`_maxMoney = 30`** — мягкий потолок.
2. **`CooldownReward`** (таймер регена):
   - `_cooldownSeconds = 120` (2 минуты);
   - **`_autoClaim = true`** — сам забирает награду по готовности (непрерывный реген);
   - `_startTakeReward = true` — на старте забрать накопленное **оффлайн**;
   - `_maxRewardsPerTake = -1` — выдать всё накопленное за раз (после долгого отсутствия);
   - `_addKey = "EnergyRegen"` — отдельный ключ сейва таймера.

**Связь события (в инспекторе):**
- `CooldownReward.OnRewardClaimed` → `Energy (Money).Add`, статический аргумент **`1`**.

`OnRewardClaimed` вызывается **по разу на каждую** выданную единицу, поэтому за оффлайн-накопление
прилетит N вызовов `Add(1)`, а `Money` сам обрежет баланс до `_maxMoney = 30`.

**Как работает:**
- Таймер каждые 120с делает награду доступной → `_autoClaim` сразу забирает → `OnRewardClaimed` →
  `Money.Add(1)` → клампится до 30.
- Вышли на час и вернулись → `_startTakeReward` + `_maxRewardsPerTake = -1` выдают накопленное разом,
  `Money` режет до 30.
- На полном баке `Add(1)` ничего не меняет (кламп). Потратили (`Spend`) → стало < 30 → реген снова доливает.

**Траты и бонусы (из кода/кнопок):**
- Списать за действие: `energy.Spend(1)` или `energy.TrySpend(1)`.
- Награда **сверх** лимита (слоты, покупка энергии): `energy.AddOverflow(5)` — игнорирует потолок.

**UI:**
- Значение — `TextMoney`/`SetText`, привязанный к этому `Money`.
- Таймер до следующей единицы — `CooldownReward.RemainingTime` (`ReactivePropertyFloat`) → `TimeToText.Set`.

Компоненты: [Money](./Shop/Money.md) · [CooldownReward](./Bonus/TimeReward/CooldownReward.md) · [TimeToText](./Tools/Text/TimeToText.md)

---

## Капнутый ресурс/валюта

Любой кошелёк `Money` с верхним пределом: задайте **`_maxMoney > 0`**. `Add()` и `SetMoney()` не дадут
превысить лимит. Когда конкретная награда **должна** превышать кап — зовите **`AddOverflow(float)`**
(игнорирует потолок). Так делаются «жизни», «билеты», «энергия» без отдельного скрипта.

Компоненты: [Money](./Shop/Money.md)

---

## Ежедневная награда (daily reward)

`CooldownReward` на кнопке «забрать»: `_cooldownSeconds = 86400`, `_rewardAvailableOnStart = true`,
`_maxRewardsPerTake = 1`, `_autoClaim = false`. Кнопку «Забрать» → `CooldownReward.Take()`; награду
повесьте на `OnRewardClaimed` (например `Money.Add(100)`). Кулдаун идёт по UTC и **между сессиями**.

Компоненты: [CooldownReward](./Bonus/TimeReward/CooldownReward.md) · [Money](./Shop/Money.md)

---

## Слот-машина → кошелёк

`SpinController` сам крутит барабаны; выплату подключите событием:
`SpinController.OnWin (int)` → `Money.Add`. Кнопка «Крутить» → `SpinController.StartSpin()`.
Стоимость спина — через `betsData`/цену или отдельную валюту (например энергию: списывайте
`energy.Spend(1)` перед `StartSpin`). Точный итог символов — `GetLastResult()` (`SpinResult`).

Компоненты: [SpinController](./Bonus/Slot/SpinController.md) · [Money](./Shop/Money.md)

---

## Магазин: купить и надеть

`Shop` с каталогом `ShopItemData` и `_purchaseFlow = BuyAndEquip`. Кнопки покупки авто-подписываются
(`_autoSubscribe`) или зовите `Shop.Buy(item)`. Владение — `Shop.IsOwned(item)`; цена/«sold» —
через `ButtonPrice`. Для нескольких **одновременно** надетых категорий (платье+обувь+…) ведите
выбор по категории сами (`GetItemsInCategory`, свой equip-слой), а `Shop` оставьте на покупку/владение.

Компоненты: [Shop](./Shop/README.md) · [Money](./Shop/Money.md)

---

## Полёт награды в HUD

Спрайт монеты/предмета летит из мира/кнопки в слот HUD: один `AnimationFly` в сцене (с `parentCanvas`),
вызов `AnimationFly.I.PlaySpriteWorldToCanvas(sprite, n, worldStart, uiSlot)`. Старт-позиция читается
синхронно — оригинал можно удалять сразу; счётчик обновляйте **сразу**, полёт — косметика.

Компоненты: [AnimationFly](./UI/AnimationFly.md)

---

## См. также
- [Getting Started — первая сцена за 5 минут](./GettingStarted.md)
- [Полезные компоненты](./UsefulComponents.md)
- [Примеры (Examples)](./Examples/README.md)
- [Vampire Survivors 3D guide](./VampireSurvivor_Guide.md)
