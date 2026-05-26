# Evade

> Legacy-компонент. Для новых RPG-сцен используйте `Neo.Rpg.RpgEvadeController` и `RpgCharacter`.

**Что это:** универсальная механика уклонения/рывка с длительностью действия и cooldown. Подходит для dash, roll, краткого блока или любой способности с фазой действия и фазой ожидания.

Файл: `Assets/Neoxider/Scripts/Tools/Components/AttackSystem/Evade.cs`

## Настройки

| Поле | Назначение |
|------|------------|
| `evadeDuration` | Длительность действия уклонения в секундах. |
| `cooldownDuration` | Длительность cooldown. |
| `cooldownStartsWithEvade` | Если `true`, cooldown стартует вместе с уклонением; иначе после завершения. |
| `useUnscaledTimeForCooldown` | Cooldown не зависит от `Time.timeScale`. |
| `cooldownUpdateInterval` | Частота обновления reactive-полей. |

Старые serialized поля (`reloadTime`, `reloadImmediately`, `OnReloadStarted`, `OnReloadCompleted`) поддержаны через `FormerlySerializedAs`.

## Состояние

| Свойство | Описание |
|----------|----------|
| `IsEvading` | Сейчас активна фаза уклонения. |
| `IsOnCooldown` | Способность недоступна из-за cooldown. |
| `CooldownProgress` | Прогресс cooldown `0..1`. |
| `CanEvade` | Можно ли стартовать уклонение сейчас. |
| `ReloadProgressValue` | Primitive getter для NeoCondition/reflection. |
| `RemainingCooldownTimeValue` | Оставшееся время cooldown для NeoCondition/reflection. |

## События

- `OnEvadeStarted`
- `OnEvadeCompleted`
- `OnCooldownStarted`
- `OnCooldownCompleted`
- `ReloadProgress.OnChanged`
- `RemainingCooldownTime.OnChanged`

## API

```csharp
if (evade.TryStartEvade())
{
    // started
}

evade.StartEvade();              // UnityEvent-friendly wrapper
evade.ResetCooldown();
evade.SetCooldownDuration(2.5f);
evade.SetEvadeDuration(0.35f);
float left = evade.GetRemainingCooldown();
```

`ResetCooldown()` не отменяет уже активное уклонение, а только делает следующую попытку доступной после завершения текущей фазы.

## Когда использовать

Используйте `Evade`, если нужен простой legacy Tools-компонент без RPG facade. Для новых боевых RPG-сцен лучше подключать `RpgCharacter` + `RpgEvadeController`, чтобы invulnerability, stats, damage и networking оставались в одном runtime-контракте.
