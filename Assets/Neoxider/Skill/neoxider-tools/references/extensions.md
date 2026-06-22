# Neo.Extensions — full catalog

`using Neo.Extensions;` (assembly `Neo.Extensions`). These are static extension methods — prefer them over
hand-rolled helpers. Signatures below were read from source, but always re-verify against the actual file
under `Assets/Neoxider/Scripts/Extensions/` before relying on an exact overload.

## Table of contents
- Transforms · Collections · Random · Strings & rich text · Color · Primitives (number/time)
- Number formatting (idle/clicker) · Coroutines · Audio fades · Components · Objects
- GameObject arrays · Layout · Screen · TimeSpan · PlayerPrefs · DateTime · misc

## TransformExtensions
```
SetPosition(transform, Vector3? position=null, float? x=null, float? y=null, float? z=null)  // partial-axis world pos
AddPosition(transform, Vector3? delta=null, float? x, float? y, float? z)
SetLocalPosition / AddLocalPosition
SetRotation / AddRotation            // Quaternion or euler
SetLocalRotation / AddLocalRotation
SetScale / AddScale                  // localScale, partial component override
LookAt2D(transform, Vector3 target, float offset=0f)        // rotate Z to face target in XY
SmoothLookAtRoutine(transform, Vector3 target, float speed) : IEnumerator
GetClosest(transform, IEnumerable<Transform> others) : Transform
GetChildTransforms(transform) : Transform[]
ResetTransform / ResetLocalTransform
CopyFrom(transform, Transform source)
DestroyChildren(transform)           // play: Destroy, editor: DestroyImmediate
```

## EnumerableExtensions
```
ForEach<T>(IEnumerable<T>, Action<T>)            // null-safe
GetSafe<T>(IList<T>, int index, T defaultValue)  // bounds-checked
GetWrapped<T>(IList<T>, int index)               // modulo wrap
IsValidIndex<T>(ICollection<T>, int index) : bool
IsNullOrEmpty<T>(IEnumerable<T>) : bool
ToIndexedString<T> / ToStringJoined<T>(sep=", ") / ToDebugString<T>
FindDuplicates<T>(IEnumerable<T>) : IEnumerable<T>
CountEmptyElements<T>(T[] array) : int           // counts nulls
```

## RandomExtensions
```
GetRandomElement<T>(IList<T>) : T                // throws on empty
GetRandomElements<T>(IList<T>, int count) : IEnumerable<T>
GetRandomIndex<T>(ICollection<T>) : int
Shuffle<T>(IList<T>, bool inplace=true) : IList<T>
Chance(this float probability) : bool            // 0..1
Random(this bool) : bool ; RandomBool() : bool   // 50/50
RandomColor(float alpha=1f) : Color
GetRandomEnumValue<T>() where T : Enum
GetRandomWeightedIndex(IList<float> weights) : int
RandomizeBetween(this float/int)                 // [-v, v]
RandomFromValue(this float/int, start) ; RandomToValue(this float/int, end)
RandomRange(this Vector2) : float ; RandomRange(this Vector2Int) : int
```

## StringExtension
```
SplitCamelCase / ToCamelCase
IsNullOrEmptyAfterTrim(string) : bool ; IsNumeric(string) : bool
ToColor(hex) : Color ; ToColorSafe(hex, out Color) : bool
Truncate(string, int maxLength) : string         // adds "..."
RandomString(int length, string chars=…) ; Reverse(string)
ToBool / ToInt(default=0) / ToFloat(default=0)
// rich text:
Bold / Italic / Size(int) / SetColor(Color) / Rainbow / Gradient(start,end) / RandomColors
```

## ColorExtension
```
WithAlpha(Color, float) ; With(Color, r?,g?,b?,a?) ; WithRGB(Color, r,g,b)
Darken(Color, amount) ; Lighten(Color, amount)   // amount 0..1
ToHexString(Color) : string                       // "#RRGGBBAA"
```

## PrimitiveExtensions
```
ToInt(this bool) ; ToBool(this int)
RoundToDecimal(this float, int places)
FormatTime(this float seconds, TimeFormat format, string sep=":", bool trimLeadingZeros=false) : string
FormatWithSeparator(this float/int, string sep, int decimalPlaces=2)
NormalizeToUnit(min,max) [0,1] ; NormalizeToRange(min,max) [-1,1] ; Denormalize(min,max)
Remap(this float, fromMin, fromMax, toMin, toMax)
// TimeFormat enum: Seconds, MinutesSeconds, HoursMinutesSeconds, DaysHoursMinutesSeconds, Milliseconds, ...
```

## NumberFormatExtensions  (idle/clicker formatting)
```
ToPrettyString(this int/long/float/double/decimal/BigInteger, NumberFormatOptions) : string
ToIdleString(this int/long/float/double/BigInteger, int decimals=1, ...) : string   // 1234567 -> "1.2M"
// NumberFormatOptions.Default (grouped, 0 decimals) ; NumberFormatOptions.IdleShort (K/M/B, 1 decimal)
// NumberNotation enum: Plain, Grouped, IdleShort, Scientific
```

## CoroutineExtensions  (delays/waits without writing a coroutine class)
```
// on MonoBehaviour or GameObject:
Delay(seconds, Action, bool useUnscaledTime=false) : CoroutineHandle
WaitUntil(Func<bool> predicate, Action) ; WaitWhile(Func<bool>, Action)
DelayFrames(int frames, Action, bool useFixedUpdate=false) ; NextFrame(Action) ; EndOfFrame(Action)
RepeatUntil(Func<bool> condition, Action)
// static global (no owner):
CoroutineExtensions.Delay(seconds, Action) ; .WaitUntil(Func<bool>, Action) ; .Start(IEnumerator) : CoroutineHandle
// CoroutineHandle: handle.Stop() ; handle.IsRunning
```

## AudioExtensions
```
FadeTo(this AudioSource, float targetVolume, float duration) : CoroutineHandle
FadeOut(this AudioSource, float duration) ; FadeIn(this AudioSource, float duration, float targetVolume=1f)
```

## ComponentExtensions / ObjectExtensions
```
GetOrAdd<T>(this Component) : T                  // GetComponent or AddComponent
GetPath(this Component) : string                 // "Parent/Child/Name"
SafeDestroy(this Object, bool immediate=false)   // play: Destroy ; edit: DestroyImmediate
IsValid(this Object) : bool                      // not null and not destroyed
GetName / SetName(this Object)                   // null-safe
```

## GameObjectArrayExtensions  (operate on many objects at once)
```
SetActiveAll(IEnumerable<GameObject>, bool) ; SetActiveAll<T>(…) where T : Component
SetActiveRange(IList<GameObject>, int upTo, bool) ; SetActiveAtIndex(IList, int, bool=true)
DestroyAll(IEnumerable<GameObject>) ; DestroyAll<T>(…)
GetActiveObjects(…) ; GetComponentsFromAll<T>(…) ; GetFirstComponentFromAll<T>(…)
SetPositionAll(…, Vector3) ; SetParentAll(…, parent, worldPositionStays=true)
FindClosest(…, Vector3) ; FindClosest<T>(…, Vector3) ; WithinDistance(…, Vector3, float)
GetAveragePosition(…) : Vector3 ; GetCombinedBounds(…) : Bounds
```

## LayoutExtensions  (arrange transforms procedurally)
```
ArrangeInCircle(Transform, center, radius, index, totalCount, rotationOffset=0)
ArrangeInCircle(IEnumerable<Transform>, center, radius, rotationOffset=0)
ArrangeInLine(IEnumerable<Transform>, origin, direction, spacing)
ArrangeInGrid(IEnumerable<Transform>, origin, columns, spacingX, spacingY)
ArrangeInGrid3D / ArrangeInCircle3D / ArrangeOnSphereSurface / ArrangeInSpiral / ArrangeOnSineWave
```

## ScreenExtensions
```
IsOnScreen(this Vector3, Camera=null) ; IsOutOfScreen(this Vector3, Camera=null)
IsOutOfScreenSide(this Vector3, ScreenEdge side, Camera=null)
GetClosestScreenEdgePoint(this Vector3, Camera=null) : Vector3
GetWorldPositionAtScreenEdge(this Camera, ScreenEdge, Vector2 offset=default, float depth=0)
GetWorldScreenBounds(this Camera, float distance) : Bounds
// ScreenEdge enum: Left,Right,Top,Bottom,Front,Back,TopLeft,TopRight,BottomLeft,BottomRight,Center
```

## TimeSpanExtensions
```
ToCompactString(this TimeSpan, bool includeSeconds=false, int maxParts=3)  // "2d 03h 15m"
ToCompactString(this TimeSpan, TimeFormat format)
ToClockString(this TimeSpan, bool includeDays=false, string sep=":")        // "03:15:27"
```

## Static utilities (call as ClassName.Method, not extensions)
```
PlayerPrefsUtils.SetIntArray(key, int[]) / GetIntArray(key, default=null) ; also float[]/string[]/DateTime
DateTimeExtensions.ToRoundTripUtcString(this DateTime) ; TryParseUtcRoundTrip(this string, out DateTime)
NeoDiagnostics.Configure(logs:true, warnings:true, errors:true)   // logging is gated/silent by default
DebugGizmos.*        // runtime gizmo drawing
Shapes.* / RandomShapeExtensions.*   // shape geometry / random point sampling
// Enums.cs holds TimeFormat, ScreenEdge used across these extensions
```
