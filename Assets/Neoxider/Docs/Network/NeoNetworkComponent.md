# NeoNetworkComponent

**Что это:** Абстрактный базовый класс (`NetworkBehaviour` / `MonoBehaviour`) для всех сетевых NoCode компонентов. Устраняет дублирование boilerplate-кода (rate-limiting, late-join, dispatch). Путь: `Scripts/Network/Core/NeoNetworkComponent.cs`, пространство имён `Neo.Network`.

**Как использовать:**
1. Наследуйте свой компонент от `NeoNetworkComponent` вместо `NetworkBehaviour`.
2. Объявите `[SyncVar]` для авторитетного состояния.
3. Переопределите `ApplyNetworkState()` для восстановления состояния при late-join.
4. В `[Command]` вызывайте `RateLimitCheck()` первой строкой.
5. Используйте `ShouldDispatchToServer()` / `ShouldBroadcastRpc()` вместо ручных проверок.

---

## Поля

| Поле | Тип | Описание |
|------|-----|----------|
| `isNetworked` | `bool` | Если true — состояние реплицируется по сети. Если false — работает локально |

## Методы

| Метод | Возврат | Описание |
|-------|---------|----------|
| `RateLimitCheck()` | `bool` | Возвращает `true` если команда пришла слишком рано (< `NetworkRateLimit`). Вызывать в начале каждого `[Command]` |
| `ApplyNetworkState()` | `void` | Переопределите: применяет SyncVar-значения к локальному состоянию. Вызывается из `OnStartClient` для non-server клиентов |
| `ShouldDispatchToServer()` | `bool` | Возвращает `true` если текущий узел — чистый клиент (должен отправить Cmd серверу) |
| `ShouldBroadcastRpc()` | `bool` | Возвращает `true` если текущий узел — сервер (должен разослать Rpc) |

## Свойства

| Свойство | Тип | Описание |
|----------|-----|----------|
| `NetworkRateLimit` | `float` (virtual) | Минимальный интервал между командами (по умолчанию 0.05s). Переопределите для кастомного значения |

## Примеры

```csharp
public class MyCounter : NeoNetworkComponent
{
#if MIRROR
    [SyncVar] private int _syncValue;

    protected override void ApplyNetworkState()
    {
        _localValue = _syncValue;
        UpdateUI();
    }

    [Command(requiresAuthority = false)]
    private void CmdSet(int val, NetworkConnectionToClient sender = null)
    {
        if (RateLimitCheck()) return;
        _syncValue = val;
        _localValue = val;
        RpcSet(val);
    }

    [ClientRpc]
    private void RpcSet(int val) { if (isServer) return; _localValue = val; UpdateUI(); }
#endif

    public void Set(int val)
    {
        if (ShouldDispatchToServer()) { CmdSet(val); return; }
        _localValue = val; UpdateUI();
        if (ShouldBroadcastRpc()) { _syncValue = val; RpcSet(val); }
    }
}
```

## См. также
- [NetworkSingleton](NetworkSingleton.md) — базовый класс для синглтон-менеджеров
- [NoCode Network Spec](NoCode_Network_Spec.md) — стандарты (Правило 11)
