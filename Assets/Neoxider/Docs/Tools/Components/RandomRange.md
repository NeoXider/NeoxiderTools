# RandomRange

`Neo.Tools.RandomRange` generates an Int or Float value and exposes it through a reactive property and
UnityEvents. Swapped endpoints are normalized in both modes.

```csharp
RandomRange range = GetComponent<RandomRange>();
range.ValueMode = RandomRangeValueMode.Float;
range.Min = 3f;
range.Max = -2f;
range.Generate();
float result = range.ValueFloat; // Between -2 and 3.
```

## Scene setup and output

Add **Neoxider / Tools / RandomRange**, choose **Value Mode**, and configure **Min** and **Max**.
Call `Generate()` from C# or a UnityEvent. Int mode rounds the bounds and includes both endpoints;
Float mode uses the floating-point interval. Equal bounds produce that value.

Each generation updates `Value`, then invokes both `OnGeneratedInt` and `OnGeneratedFloat`, even when
the result repeats. The reactive property only notifies when its stored value changes. `ValueInt` and
the integer event truncate fractions toward zero in Float mode. `SetMin`/`SetMax` accept int or float
arguments for UnityEvent wiring.

Storage and configuration use `float`. Use finite bounds; for exact integer output, keep integer
endpoints within -16,777,216 to 16,777,216. Extreme Int32 endpoints require a separate integer storage
contract and are not guaranteed by this component.

With Mirror installed, **Is Networked** delegates generation to the server and broadcasts the result;
the host applies it once. Command throttling uses the sender's connection. Leave networking disabled
for local randomness.

See [Components](README.md) and [Counter](Counter.md).
