# NetQuantization

**What it is:** fixed-point and range packing helpers for hand-written snapshot formats (`Scripts/Network/Realtime/NetQuantization.cs`, namespace `Neo.Network.Realtime`). They keep a few hundred entities inside a few kilobytes. Pure C#, no allocation.

**How to use:** pack when writing a frame, unpack when reading it, with the same density on both sides.

```csharp
writer.WriteShort(NetQuantization.PackPosition(body.X));      // 1/32 unit, +-1023
writer.WriteUShort(NetQuantization.PackAngle(body.Heading));   // radians -> 16 bits
writer.WriteByte(NetQuantization.PackFraction(body.Health01)); // 0..1 -> 1 byte
writer.WriteVarUInt(NetQuantization.PackHitPoints(body.Hp));   // rounded UP

float x = NetQuantization.UnpackPosition(reader.ReadShort());
```

---

Every packer **saturates** instead of wrapping (a coordinate beyond the range pins to the edge instead of teleporting across the map) and rounds to nearest, so a round trip is off by at most half a step. `NaN` packs to 0.

## Presets

| Packer | Resolution | Range | Use |
|--------|------------|-------|-----|
| `PackPosition` / `UnpackPosition` | 1/32 unit (about 3 cm) | +-1023.97 | World coordinates (`short`). `FitsPosition(value)` flags a world that outgrew it. |
| `PackScale` / `UnpackScale` | 1/64 | 0..1023 | Visual scale (`ushort`). |
| `PackSpeed` / `UnpackSpeed` | 1/256 | 0..255 | Speed in units per second (`ushort`). |
| `PackSeconds` / `UnpackSeconds` | 1/32 s | 0..2047 s | Timers (`ushort`). |
| `PackFraction` / `UnpackFraction` | 1/255 | 0..1 | Health bars, progress (`byte`). |
| `PackSignedUnit` / `UnpackSignedUnit` | 1/127 | -1..1 | A stick axis (`byte`, 128 is exactly 0). |
| `PackAngle` / `UnpackAngle` | 0.0055 deg | any radians, wrapped to 0..2pi | Heading (`ushort`). |
| `PackAngleByte` / `UnpackAngleByte` | 1.4 deg | wrapped | A facing arrow (`byte`). |
| `PackHitPoints` | 1 | `uint` | Rounded **up**: a mob with 0.3 HP left still reads as alive on a client, never as a zero that looks like a corpse. |
| `PackMaxHitPoints` | 1 | `uint` | Nearest whole number, at least 1 for a real entity. |

## Explicit density

For a world with another scale:

| Member | Description |
|--------|-------------|
| `short PackShort(float value, float stepsPerUnit)` / `float UnpackShort(short, float)` | Signed 16-bit fixed point. |
| `ushort PackUShort(float value, float stepsPerUnit)` / `float UnpackUShort(ushort, float)` | Unsigned 16-bit, negatives clamp to 0. |
| `float MaxShort(float stepsPerUnit)`, `bool FitsShort(float value, float stepsPerUnit)` | The range at that density. |

A 100 x 100 arena fits `PackShort(value, 300f)` (3 mm); a 20 km map needs `PackShort(value, 1.5f)`.

## See also
- [NetFraming](./NetFraming.md)
- [Realtime IO Guide](../Realtime_IO_Guide.md)
- [Realtime README](./README.md)
