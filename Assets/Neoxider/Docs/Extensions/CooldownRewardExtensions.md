### CooldownRewardExtensions
- **Пространство имён**: `Neo.Extensions`
- **Путь**: `Assets/Neoxider/Scripts/Extensions/CooldownRewardExtensions.cs`

Статические методы для расчёта накопленных наград по кулдауну и сдвига времени последней выдачи. Используются в `TimeReward` и могут применяться в собственной логике наград.

## Методы

- **GetAccumulatedClaimCount** (extension для `DateTime`)  
  `lastClaimUtc.GetAccumulatedClaimCount(cooldownSeconds, nowUtc)`  
  Возвращает число полных циклов кулдауна с момента `lastClaimUtc` до `nowUtc` (сколько наград «накопилось»).

- **CapToMaxPerTake** (статический)  
  `CooldownRewardExtensions.CapToMaxPerTake(accumulated, maxPerTake)`  
  Ограничивает число наград за один «забор»: `maxPerTake < 0` — без ограничения (вернуть все), иначе `min(accumulated, maxPerTake)`.

- **AdvanceLastClaimTime** (extension для `DateTime`)  
  `lastClaimUtc.AdvanceLastClaimTime(claimsGiven, cooldownSeconds)`  
  Возвращает новый UTC времени последней выдачи после выдачи `claimsGiven` наград (сдвиг на `claimsGiven * cooldownSeconds`).

## Пример

```csharp
DateTime lastUtc = ...; // из сохранения
float cooldown = 60f;
DateTime now = DateTime.UtcNow;

int accumulated = lastUtc.GetAccumulatedClaimCount(cooldown, now);
int toGive = CooldownRewardExtensions.CapToMaxPerTake(accumulated, 3); // не больше 3 за раз

if (toGive > 0)
{
    // выдать toGive наград
    DateTime newLast = lastUtc.AdvanceLastClaimTime(toGive, cooldown);
    SaveLastRewardTime(newLast);
}
```
