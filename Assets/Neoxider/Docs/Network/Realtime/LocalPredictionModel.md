# LocalPredictionModel

**What it is:** client-side prediction and reconciliation of the local player's body (`Scripts/Network/Realtime/LocalPredictionModel.cs`, namespace `Neo.Network.Realtime`), so it answers input on the very next frame instead of one round trip plus the interpolation delay later. Pure C#: no Unity, no Mirror, no allocation after construction. 2D (x, y); use it per axis pair or ignore the second axis for 1D.

**How to use:**
1. Create `new LocalPredictionModel(LocalPredictionSettings.ForTickRate(50f))`, set `Bounds` (the playable rectangle, same as the authority's) and optionally a `Constraint` for obstacles.
2. Every fixed tick call `Step(tick, dirX, dirY, speed)` with the same input the player sends to the authority, stamped with a growing tick number.
3. When a snapshot brings your authoritative position and the **last input tick the authority applied** (the *ack tick*), call `Reconcile(serverX, serverY, ackTick, speed)`.
4. Every rendered frame call `Relax(deltaTime)` and draw at `RenderXAt(alpha)` / `RenderYAt(alpha)`.
5. `Teleport(x, y)` for a respawn or forced move; `Reset()` on disconnect.

---

## How it works

Each tick the model integrates the input exactly as the authority integrates the same input (`position += direction * speed * tick`, clamped to the bounds, shaped by the constraint) and remembers it under its tick. A snapshot says "my position is P after applying inputs up to tick N". The model discards inputs up to N (already in P), replays the rest on top of P, and that is where the body should be *now*. If it is somewhere else, the difference is not applied as a jump: the drawn position is `simulation + renderOffset`, the reconcile moves the error into the offset, and the offset decays with `SmoothingSeconds`. A large error (above `SnapDistance`: a teleport, a dash, a respawn) snaps.

The server stays authoritative. Nothing the model computes is ever sent anywhere; it only chooses where to draw the local body. A cheating client that predicts garbage is corrected on the next snapshot, and the authority never reads a position from a client.

## LocalPredictionSettings

| Field | Default | Meaning |
|-------|---------|---------|
| `TickSeconds` | 0.02 | The fixed step both peers integrate with. `ForTickRate(hz)` derives it. |
| `SmoothingSeconds` | 0.12 | Time constant of the correction offset. |
| `SnapDistance` | 3 | Error above this many world units snaps. |
| `SettleDistance` | 0.01 | An offset below this is zeroed: no endless sub-pixel drift. |
| `HistoryCapacity` | 128 | Ticks of input kept for replay (about 2.5 s at 50 Hz). |

## Members

| Member | Description |
|--------|-------------|
| `void Step(uint tick, float dirX, float dirY, float speed)` | Integrates one tick and remembers it. `speed` 0 stands still. Ignored before the first server state. |
| `PredictionReport Reconcile(float serverX, float serverY, uint ackTick, float speed, int fallbackReplayTicks = 0)` | Applies an authoritative sample. `ackTick == 0` means "no ack": the last `fallbackReplayTicks` inputs are replayed instead. The first call adopts the server state (`Initialised`). A non-finite position is ignored. |
| `void Relax(float deltaSeconds)` | Decays the render offset by one frame. |
| `float X, Y` | The corrected simulation position. |
| `float OffsetX, OffsetY` | Remaining correction. |
| `float RenderX, RenderY`, `RenderXAt(alpha)`, `RenderYAt(alpha)` | What to draw; the `At` forms interpolate between the previous and the current fixed step (0..1). |
| `bool HasState`, `int PendingInputCount` | State. |
| `PredictionBounds Bounds`, `IPredictionMovementConstraint Constraint` | Clamp rectangle and optional obstacle model. |
| `void Teleport(float x, float y)`, `void Reset()` | Jump without smoothing / forget everything. |

`PredictionReport` carries `Outcome` (`Initialised`, `Corrected`, `Snapped`), `ErrorDistance` and `ReplayedTicks`: log them to see how well the client and the authority agree.

`IPredictionMovementConstraint.Constrain(fromX, fromY, ref toX, ref toY)` lets the host environment veto part of a step (walls, props) so predicted and authoritative movement slide along the same obstacles instead of fighting at each one. It is applied in the step **and** in the replay.

## What the authority has to provide

1. Apply each client's input in tick order with the same integration (same tick length, same speed, same bounds).
2. Write the last applied tick (`AckTick`) and the effective speed into that player's entry of the snapshot.
3. Do not route the input stream through `NeoNetworkComponent` commands (0.05 s rate limit per component): use your own Mirror message and your own flood guard.

## Example

```csharp
private readonly LocalPredictionModel _model = new LocalPredictionModel(LocalPredictionSettings.ForTickRate(50f))
{
    Bounds = new PredictionBounds(-100f, 100f, -100f, 100f)
};
private uint _tick;

void FixedUpdate()
{
    Vector2 move = ReadInput();
    _model.Step(++_tick, move.x, move.y, _speed);
    NetworkClient.Send(new MoveInput { Tick = _tick, X = move.x, Y = move.y });
}

void OnSnapshot(float x, float y, uint ackTick, float speed)
{
    _speed = speed;
    PredictionReport report = _model.Reconcile(x, y, ackTick, speed);
    if (report.Outcome == PredictionOutcome.Snapped) PlaySnapEffect();
}

void LateUpdate()
{
    _model.Relax(Time.deltaTime);
    float alpha = (Time.time - Time.fixedTime) / Time.fixedDeltaTime;
    transform.position = new Vector3(_model.RenderXAt(alpha), _model.RenderYAt(alpha), 0f);
}
```

## See also
- [Realtime IO Guide](../Realtime_IO_Guide.md)
- [Snapshot interpolation](./SnapshotInterpolation.md)
- [Realtime README](./README.md)
