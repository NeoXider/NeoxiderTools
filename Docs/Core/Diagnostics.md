# Diagnostics

**What it is:** `NeoDiagnostics`, the shared runtime logging gate for package code (static class in the global
namespace, file `Scripts/Extensions/NeoDiagnostics.cs`). It lives in the foundational `Neo.Extensions` assembly so
every module can use the same short API.

**How to use:**
1. Log through `NeoDiagnostics.Log` / `LogWarning` / `LogWarningThrottled` / `LogError` instead of `Debug.Log*`.
2. Pass a component's debug flag as `force` to opt one object into output.
3. Call `NeoDiagnostics.Configure(logs: true, warnings: true)` temporarily to see every channel.

---

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

## Per-object throttle keys

A throttled message needs a key that is unique per component instance, otherwise the first object to
report a problem silences every other one. Build it with `NeoDiagnostics.StableId`:

```csharp
NeoDiagnostics.LogWarningThrottled(
    $"{nameof(MouseEffect)}.{NeoDiagnostics.StableId(this)}.MissingCamera",
    "Target Camera is not assigned and no fallback camera is available.",
    this,
    5f);
```

The throttle gives at most `NeoDiagnostics.MaxThrottleKeys` (1024) keys their own window, so per-object keys
cannot grow the table for the whole session. A running window is never forgotten. A new key arriving at the
bound takes the slot of a window that has already elapsed; while all 1024 windows are still running, every
further key shares one overflow window: the first of them logs, the others are counted, and the count is
appended to the next throttled warning that logs. A burst that cycles through more than 1024 keys therefore
logs each tracked key once plus one overflow line per window, however many passes it makes. The search for
elapsed slots only runs once the earliest window can have ended, so a sustained overflow costs a lookup and a
time comparison per call, and a throttled or suppressed call does not allocate. When a warning message is expensive to
build (string interpolation, method descriptions), check `NeoDiagnostics.RuntimeWarningsEnabled` first so
disabled warnings allocate nothing.

Use `StableId` instead of `GetInstanceID()` directly: Unity 6.5 made `Object.GetInstanceID` obsolete as an
error in favour of `GetEntityId`, so a direct call breaks the build for anyone on 6.5+. `StableId` holds
that switch in one place for the whole package and returns `"null"` for a null target rather than throwing -
diagnostics get called exactly where something is missing.

## Global diagnostics

Temporary debugging can enable channels globally:

```csharp
NeoDiagnostics.Configure(logs: true, warnings: true);
```

Do not leave global diagnostics enabled in production scenes.

**Navigation:** [Core](./README.md)
