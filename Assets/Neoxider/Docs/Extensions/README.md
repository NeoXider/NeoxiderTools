# Extensions module

Extension methods for C# and Unity API. No Inspector components; static helpers only. Scripts in `Scripts/Extensions/`. XML docs in code are in English. Full per-class pages and examples are linked below.

## Categories (see  for links)

- **Base:** ObjectExtensions, ComponentExtensions, TransformExtensions, GameObjectArrayExtensions, PrefabPreviewExtensions
- **Collections & data:** EnumerableExtensions, DictionaryExtensions, PrimitiveExtensions, DateTimeExtensions, TimeSpanExtensions, TimeParsingExtensions, StringExtension, ColorExtension, CooldownRewardExtensions
- **Random:** RandomExtensions, RandomShapeExtensions, Shapes
- **UnityEvent:** UnityEventDelegateCache
- **System:** CoroutineExtensions, PlayerPrefsUtils, ScreenExtensions, UIUtils, AudioExtensions, DebugGizmos
- **Layout:** LayoutUtils, LayoutExtensions
- **Enums:** shared enum definitions

## Lifecycle notes

- `CoroutineExtensions` resets its global helper at `SubsystemRegistration`, so domain reload disabled does not keep stale static helper references.
- Coroutine handles are tracked by a hidden lifecycle component and are completed when the owning `GameObject` is destroyed.
- `CoroutineExtensions.Start(null)` returns an inactive handle without creating a global helper.
- `AudioExtensions.FadeIn(null, ...)` and `FadeTo(null, ...)` return `null`; active fades exit if their `AudioSource` is destroyed.

## See also

- [Animations](../Animations/README.md)
- [Tools](../Tools/README.md)
