# ResourceRegen

**Что это:** `Neo.Bonus.ResourceRegen` — регенерирующий ресурс (энергия/жизни) одним компонентом: сцепляет `CooldownReward` (авто-клейм включается принудительно) с кошельком `Money` (депозит `Amount Per Claim` за цикл, кап — `Money.MaxMoney`) и опциональным `TimeToText` (отсчёт из `RemainingTime`; при полном кошельке показывает 0, если включено `Show Zero When Full`).

**Сборка:** один объект: `Money` (задать `Max Money`) + `CooldownReward` (задать `Cooldown Seconds`) + `ResourceRegen`. Источники сверх капа (бонусы, покупки) вызывают `Money.AddOverflow` сами.
