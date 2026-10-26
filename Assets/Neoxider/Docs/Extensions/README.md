# Extensions module

Reusable helpers for C# and Unity, in assembly `Neo.Extensions`. Add `using Neo.Extensions;` and, for a custom assembly definition, reference `Neo.Extensions`. Source: `Scripts/Extensions/`. This is the canonical module entry point.

## Runtime API

- **Objects:** [ObjectExtensions](ObjectExtensions.md), [ComponentExtensions](ComponentExtensions.md), [TransformExtensions](TransformExtensions.md), [GameObjectArrayExtensions](GameObjectArrayExtensions.md), [PrefabPreviewExtensions](PrefabPreviewExtensions.md).
- **Collections:** [EnumerableExtensions](EnumerableExtensions.md), [DictionaryExtensions](DictionaryExtensions.md).
- **Numbers and time:** [PrimitiveExtensions](PrimitiveExtensions.md), [DateTimeExtensions](DateTimeExtensions.md), [TimeSpanExtensions](TimeSpanExtensions.md), [TimeParsingExtensions](TimeParsingExtensions.md), [CooldownRewardExtensions](CooldownRewardExtensions.md), and number formatting below.
- **Text and color:** [StringExtension](StringExtension.md), [ColorExtension](ColorExtension.md).
- **Random and geometry:** [RandomExtensions](RandomExtensions.md), [RandomShapeExtensions](RandomShapeExtensions.md), [Shapes](Shapes.md).
- **Events and lifecycle:** [UnityEventDelegateCache](UnityEventDelegateCache.md), [CoroutineExtensions](CoroutineExtensions.md), [AudioExtensions](AudioExtensions.md).
- **Unity utilities:** [PlayerPrefsUtils](PlayerPrefsUtils.md), [ScreenExtensions](ScreenExtensions.md), [UIUtils](UIUtils.md), [DebugGizmos](DebugGizmos.md), [LayoutUtils](LayoutUtils.md), [LayoutExtensions](LayoutExtensions.md).
- **Shared definitions:** [Enums](Enums.md), [InternalTypes](InternalTypes.md) and `NeoDiagnostics` below.

```csharp
using Neo.Extensions;

int index = (-1).Wrap(0, 5); // 4; full int bounds are supported.
string score = 1234567L.ToIdleString(); // "1.2M"
bool valid = TimeParsingExtensions.TryParseDuration("02:30", out float seconds); // 150
```

## Contract guides

- [Numeric, time and random edge cases](NumericEdgeCases.md): signed rounding, extreme numeric values, time precision/saturation, UTC reward arithmetic and weighted selection.
- [Lifecycle and ownership](LifecycleAndOwnership.md): coroutine cancellation, borrowed/generated preview assets, event subscriptions and camera/canvas coordinates.

## Number formatting

`NumberFormatExtensions` supports `int`, `long`, `float`, `double`, `decimal` and `System.Numerics.BigInteger` through `ToPrettyString(NumberFormatOptions)`. `NumberNotation` selects Plain, Grouped, IdleShort or Scientific; options select decimal places (0–12), rounding, separators and affixes. `NumberFormatOptions.Default` is grouped with no decimal places; `IdleShort` uses one decimal place and K/M/B suffixes. `ToIdleString` is the convenient short-notation entry point.

`TrimTrailingZeros` removes fractional zeros and the complete decimal separator when the fractional part is empty. Scientific exponent digits are preserved, including the double fallback for very small values and numbers beyond decimal range. See the [numeric contract guide](NumericEdgeCases.md) for directed rounding, BigInteger boundaries and the distinct float/TimeSpan time formats.

## Scene and NoCode workflow

Call the typed APIs from ordinary C# components. There is no Inspector action graph to configure. Coroutine methods attach hidden lifecycle helpers as needed; `CreateFromMenuAttribute` and `LegacyComponentAttribute` are metadata for editor authoring, in namespace `Neo`. UI button code can use `UnityEventDelegateCache.SubscribeAt` and `UnsubscribeAt` to retain matching delegates. Reusing a slot detaches its previous tracked subscription, including when changing the event source; `Clear()` only clears the cache.

Layout collection methods consume the source once and position the resulting transforms in order. `GetCombinedBounds` also supports a one-shot `IEnumerable`. `WorldToCanvasPoint` returns local canvas coordinates for every render mode; Overlay Canvas conversion uses a null UI camera.

For EventSystem picking, `UIUtils.GetUIElementsAtPosition(screenPosition, results)` accepts explicit pixels and clears/refills a caller-owned result list. For a compatible mouse in Both/New-only projects, use `Neo.Tools.UIInputCompat` from `Neo.Tools.Input`. The original UIUtils cursor methods retain legacy input semantics. List reuse still creates PointerEventData per query with an EventSystem; see [UIUtils](UIUtils.md) for exact overloads, missing-input behavior and raycaster boundaries.

## Diagnostics

`NeoDiagnostics.Configure(logs: true, warnings: true)` enables optional runtime output. Info and warning output is disabled by default; errors are enabled. `NeoDiagnostics.StableId(UnityEngine.Object)` is the shared runtime identity gateway for Unity 6.0–6.4 and 6.5+. Keep version-specific identity calls in that gateway.

## Lifecycle notes

- `CoroutineExtensions` resets its global helper at `SubsystemRegistration`, so domain reload disabled does not keep stale static helper references.
- Owner-bound coroutine handles complete when their component is destroyed or its GameObject hierarchy is deactivated. Disabling only the component keeps the operation running unless the caller cancels it.
- Keep the returned handle and call `Stop()` for tracked cancellation/disposal. Raw `StopCoroutine` / `StopAllCoroutines` cannot automatically update the handle; see [CoroutineExtensions](CoroutineExtensions.md).
- `CoroutineExtensions.Start(null)` returns an inactive handle without creating a global helper.
- `AudioExtensions.FadeIn(null, ...)` and `FadeTo(null, ...)` return `null`; active fades exit if their `AudioSource` is destroyed.
- Preview Sprites borrowed from renderers/images remain caller-borrowed. Generated Editor fallbacks belong to the preview cache, must not be stored as persistent asset data, and must be reacquired after cache clearing; see [PrefabPreviewExtensions](PrefabPreviewExtensions.md).
- Screen-edge `depth` is near-plane-relative in both projection modes; screen-bounds `distance` stays camera-relative. Review existing orthographic consumers without automatically rewriting assets; see [ScreenExtensions](ScreenExtensions.md).

## Кратко по-русски

`Neo.Extensions` — набор переиспользуемых C# API. Подключите `using Neo.Extensions;`, а в собственной asmdef добавьте ссылку на сборку. Для сцен вызывайте те же методы из компонентов; скрытые coroutine-компоненты создаются автоматически. Настройка цепочек UnityEvent для использования модуля не требуется.

`TryParseDuration` принимает секунды или 2–4 числовые части через разделитель: `MM:SS`, `HH:MM:SS`, `DD:HH:MM:SS`. NaN, бесконечность, отрицательные значения и переполнение отклоняются; результат ошибки — `false` и `0`. `Wrap` и `PingPong` поддерживают полный диапазон `int`. Раскладки и объединение bounds читают исходный `IEnumerable` один раз. Повторный `SubscribeAt` заменяет прежнюю отслеживаемую подписку; перед `Clear` снимайте оставшиеся подписки явно.

Числовые границы собраны в [NumericEdgeCases](NumericEdgeCases.md), а отмена корутин, preview-ресурсы и координаты — в [LifecycleAndOwnership](LifecycleAndOwnership.md). Для отмены используйте сохранённый handle; прямой native stop не гарантирует обновления его состояния. Generated preview нельзя сохранять как постоянную иконку. Старую camera-relative глубину ортографического screen-edge размещения переводите явно: `newDepth = oldDepth - camera.nearClipPlane`.

## See also

- [Animations](../Animations/README.md)
- [Tools](../Tools/README.md)
