# Diagnostics

**Purpose:** shared runtime logging gate for package code. `NeoDiagnostics` lives in the foundational `Neo.Extensions` assembly so every module can use the same short API.

## Principle

Neoxider modules should not emit runtime spam through raw `Debug.Log*` calls. Package messages should go through:

```csharp
NeoDiagnostics.Log("message");
NeoDiagnostics.LogWarning("message");
NeoDiagnostics.LogError("message");
NeoDiagnostics.LogWarningThrottled("key", "message", seconds: 2f);
```

Info logs and warnings are disabled by default. Errors stay enabled so critical scene misconfiguration is still visible. Static state is reset through `SubsystemRegistration` when domain reload is disabled.

## Component-level debugging

If a component exposes its own `_debug` or `_debugLogWarnings` flag, pass it as `force`:

```csharp
NeoDiagnostics.Log("Card spawned", this, _debug);
NeoDiagnostics.LogWarning("Missing optional target", this, _debugLogWarnings);
```

This keeps public components quiet by default while still allowing scene authors to opt into diagnostics from the Inspector.

## Global diagnostics

Temporary debugging can enable channels globally:

```csharp
NeoDiagnostics.Configure(logs: true, warnings: true);
```

Do not leave global diagnostics enabled in production scenes.

**Navigation:** [Core](./README.md)
