# Counter

`Neo.Tools.Counter` stores an integer-style or floating-point value, exposes reactive output and typed
UnityEvents, and optionally saves through `SaveProvider` or replicates through Mirror.

## C# API

```csharp
Counter counter = GetComponent<Counter>();
counter.Set(1000000);
counter.Add(1);                  // 1000001; the increment is preserved.
counter.Divide(2);
float current = counter.ValueFloat;
counter.Send(current);           // Sends a payload without changing the counter.
```

`Add`, `Subtract`, `Multiply`, `Divide`, `Set`, and `Send` accept `int` and `float` arguments. Int mode
rounds arithmetic results; Float mode preserves fractions. `ValueInt` rounds in Int mode and truncates
toward zero in Float mode. Division by zero is ignored; Float division also ignores approximately zero
divisors. Equal stored values do not emit change events. Distinct representable values do.

Use these methods for mutations. Assigning `Value.Value` directly only updates the reactive property;
it bypasses counter persistence, typed events, and network dispatch. Storage is `float`, including Int
mode, so consecutive integers beyond 16,777,216 cannot all be represented exactly.

## Inspector and UnityEvents

1. Add **Neoxider / Tools / Counter**, select **Value Mode**, and enter the initial **Value**.
2. Connect `OnValueChangedInt` or `OnValueChangedFloat` to a UI or gameplay consumer.
3. Configure **Send Payload** as Counter, Score, or Money. Calling parameterless `Send()` reads that
   source; an explicit argument bypasses source selection. `OnSend` always emits the float payload,
   followed by the mode-specific send event.
4. Enable repeat events only when repeated callbacks are intended: the repeat count is the current
   counter value rounded and clamped to zero, including when a different send payload is selected.

**Send On Start** is optional. Send events are local notifications and do not mutate the counter.
The repeat loop is synchronous; use a bounded gameplay value rather than a large currency balance.

## Saving and lookup

Enable **Save**, choose a unique **Save Key**, and optionally keep **Load On Start** enabled.
`LoadFromSave()` refreshes the current value explicitly. A load fires `OnLoadedInt/Float` and, when
**Invoke Events On Load** is enabled, the reactive and typed change notifications.

Enabled counters are indexed by save key in `Counter.Registry` for
[ModifyCounterByKey](ModifyCounterByKey.md). Disabling removes the registration captured at enable
time, even if the Inspector key was subsequently edited, and removes empty key buckets. Disable and
re-enable after editing a live key to register under its new name. Treat the public registry as a lookup,
and let counter lifecycle methods maintain its entries.

With Mirror installed, **Is Networked** enables server replication. The host applies each received
value once; its own RPC must not repeat typed or repeat events. Persistence keys remain a local save
configuration, so choose them deliberately for replicated scene objects. Server startup captures the
configured value for initial replication, and server-side save loads update the replicated snapshot.
Network command throttling is per sender rather than shared between independent remote clients.

Remote `Add`/`Subtract` and `Multiply`/`Divide` send an operation and operand. The server evaluates each
accepted operation against its current balance, in receive order: two independent clients adding one
to ten produce twelve. Subtraction uses a negative delta; division retains the overload's existing
zero check and sends its reciprocal as a multiplication. Int-mode rounding still happens on the
server after each operation. The client waits for the authoritative result rather than predicting it.
`Set` remains an absolute, last-writer-wins request, even when the client's displayed value already
equals the requested value. Commands rejected by throttling have no acknowledgement or retry.

`Is Networked` off rejects queued mutation Commands. The existing shared-counter
`requiresAuthority = false` policy is retained; games must control who can request shared mutations.
This change does not add a wallet permission policy or exact-integer backend. Use the same 11.0.0
build on every peer, because arithmetic now uses an additional private Mirror Command.

### Remote numeric admission

A received absolute `Set` requires a finite input. A finite `Set` can repair an existing NaN or
infinite stored balance. Received arithmetic requires both a finite operand and a finite raw float
result against the current server balance. NaN, positive/negative infinity and finite-input float
overflow are rejected before Int-mode rounding, which could otherwise hide an invalid result.

Rejected numeric requests leave the balance, reactive/typed/repeat notifications, save value, initial
network state and outgoing value RPCs unchanged. They do not consume the sender's command window,
so a valid request from the same peer can still be accepted immediately. There is no acknowledgement
or automatic retry. These checks apply to received Commands; direct public calls on the server/host
and offline calls retain their existing float behavior. No clamp, new numeric backend or permission
policy is introduced. In Inspector/UnityEvent workflows, the same mutation methods retain their
ordinary rounding and division rules.

## Callback ordering and re-entry

An ordinary change notifies reactive listeners, then the mode-specific value event, then optional
repeat callbacks, then saves and replicates the result. A callback may call a Counter mutation method.
That nested mutation supersedes the outer change: after the current callback batch returns, the
outer call stops its remaining typed/repeat/save/replication work. A repeat loop checks after each
invocation. An already-running UnityEvent batch finishes with its captured argument; notifications
already delivered are not rolled back. For example, a reactive listener replacing one with two emits
the typed value two once and replicates two; a typed listener replacing one with two observes one,
then two, and only the nested value continues to repeat/save/replicate.

See [Components](README.md), [ModifyCounterByKey](ModifyCounterByKey.md), and [Save](../../Save/README.md).
