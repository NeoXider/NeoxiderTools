# Diagnostics

**Что это:** общий runtime-gate для логов пакета. `NeoDiagnostics` находится в базовой assembly `Neo.Extensions`, чтобы любой модуль мог использовать один и тот же короткий API.

## Принцип

Модули Neoxider не должны писать runtime-spam напрямую через `Debug.Log*`. Для сообщений пакета используйте:

```csharp
NeoDiagnostics.Log("message");
NeoDiagnostics.LogWarning("message");
NeoDiagnostics.LogError("message");
NeoDiagnostics.LogWarningThrottled("key", "message", seconds: 2f);
```

По умолчанию информационные логи и warnings выключены. Ошибки включены, чтобы критичная неправильная настройка сцены не терялась. При domain reload disabled состояние сбрасывается через `SubsystemRegistration`.

## Локальная отладка компонента

Если компонент имеет свой флаг вроде `_debug` или `_debugLogWarnings`, передавайте его как `force`:

```csharp
NeoDiagnostics.Log("Card spawned", this, _debug);
NeoDiagnostics.LogWarning("Missing optional target", this, _debugLogWarnings);
```

Так публичный компонент остаётся тихим по умолчанию, но автор сцены может включить диагностику в Inspector.

## Глобальная диагностика

Для временной отладки можно включить каналы глобально:

```csharp
NeoDiagnostics.Configure(logs: true, warnings: true);
```

Не оставляйте глобальную диагностику включённой в production-сценах.

**Навигация:** [Core](./README.md)
