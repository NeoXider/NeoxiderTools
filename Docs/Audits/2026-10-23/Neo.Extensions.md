# Neo.Extensions: audit

Status: first source pass complete; centralized Unity compilation and test execution pending.

- Base revision: `ca198556` (package 10.19.0).
- Model: `gpt-6.1-sol`, reasoning `max`.
- Assembly: `Assets/Neoxider/Scripts/Extensions/Neo.Extensions.asmdef`.
- Scope: 31 C# files; nested assemblies excluded.

## Scope

- `Assets/Neoxider/Scripts/Extensions/AudioExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/ColorExtension.cs`
- `Assets/Neoxider/Scripts/Extensions/ComponentExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CooldownRewardExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CoroutineExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/CreateFromMenuAttribute.cs`
- `Assets/Neoxider/Scripts/Extensions/DateTimeExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/DebugGizmos.cs`
- `Assets/Neoxider/Scripts/Extensions/DictionaryExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/EnumerableExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/Enums.cs`
- `Assets/Neoxider/Scripts/Extensions/GameObjectArrayExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/LayoutExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/LayoutUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/LegacyComponentAttribute.cs`
- `Assets/Neoxider/Scripts/Extensions/NeoDiagnostics.cs`
- `Assets/Neoxider/Scripts/Extensions/NumberFormatExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/ObjectExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/PlayerPrefsUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/PrefabPreviewExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/PrimitiveExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/RandomExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/RandomShapeExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/ScreenExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/Shapes.cs`
- `Assets/Neoxider/Scripts/Extensions/StringExtension.cs`
- `Assets/Neoxider/Scripts/Extensions/TimeParsingExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/TimeSpanExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/TransformExtensions.cs`
- `Assets/Neoxider/Scripts/Extensions/UIUtils.cs`
- `Assets/Neoxider/Scripts/Extensions/UnityEventDelegateCache.cs`

## Findings and fixes

All 31 files listed above were read, including the complete formatter and coroutine implementations. Existing EditMode extension/lifecycle tests and Shop subscription callers were inspected as context. Findings below describe source-proven paths, with new regression cases written but not executed by this assembly owner.

| ID / severity | Reproduction and previous effect | Implemented correction / regression |
| --- | --- | --- |
| EXT-01 / P2 | Format `1.23e100` as Scientific with two decimals and `TrimTrailingZeros=true`. The double fallback trimmed exponent zeros, displaying `1.23e+1`. A multi-character decimal separator could also remain after trimming a zero fraction. | `NumberFormatExtensions.NormalizeDecimalPart` trims the fractional mantissa only and removes the whole decimal separator. `Scientific_TrimZeros_PreservesExponent` and `Plain_TrimZeros_RemovesWholeMultiCharacterSeparator`. |
| EXT-02 / P2 | `TryParseDuration("NaN", out seconds)` or `"NaN:00"` previously succeeded with a NaN result because comparison checks do not reject NaN. | Reject non-finite parsed parts and totals, preserving `false` / zero failure semantics. `Duration_NonFiniteValue_ReturnsFalseAndZero`, six inputs. |
| EXT-03 / P2 | `0.Wrap(int.MinValue, int.MaxValue)` overflowed the range and returned `int.MinValue`; other valid bounds could overflow `value - min`. | Use `long` for range/offset arithmetic, casting only the final in-range result. `Wrap_FullIntegerRange_DoesNotOverflow`. |
| EXT-04 / P2 | `int.MaxValue.PingPong(int.MaxValue)` overflowed `length * 2` and returned 1 instead of the endpoint. | Widen cycle and remainder arithmetic to `long`. `PingPong_LargeLength_DoesNotOverflow`. |
| EXT-05 / P2 | More than `int.MaxValue` accumulated cooldown cycles were cast to `int` outside its range, violating the documented non-negative count. NaN cooldown also passed the positive-duration check. | Saturate at `int.MaxValue` and return zero for non-finite cooldowns. `Cooldown_Accumulation_SaturatesInsteadOfReturningNegativeCount`. |
| EXT-06 / P2 | `GetBoolArray(key, fallback)` with persisted `"invalid"`, `"1,invalid"`, or an empty string returned an empty array after `GetIntArray` swallowed the parse failure, discarding the caller's fallback. | Treat an empty parsed integer array as invalid boolean data. `BoolArray_InvalidStoredData_PreservesCallerDefault`, including existing invalid-number behavior. The persisted format is unchanged. |
| EXT-07 / P2 | `"A".Gradient(Color.red, Color.blue)` computed `0 / 0` for its interpolation parameter, producing a non-finite color instead of the start color. | Use progress zero for a singleton string. `Gradient_SingleCharacter_UsesStartColor` checks singleton and two-character endpoints. |
| EXT-08 / P2 | Repeated `SubscribeAt(0, evt, ...)` overwrote the cached delegate while leaving the previous listener attached; moving the slot to another event leaked its previous subscription too. | Track the event for each slot and remove the previous listener before replacing it. Wrong-event unsubscription retains the tracked slot. `DelegateCache_ReplacingSlot_DetachesPreviousListener`, `DelegateCache_MovingSlot_DetachesPreviousEvent`, and a manual Add/Clear compatibility test. `Clear()` still does not unsubscribe. |
| EXT-09 / P2 | Layout methods counted and then enumerated an arbitrary `IEnumerable<Transform>` again. One-shot sources could throw, position nothing, or create a second set of objects. | Snapshot once, then derive both count and placement from that list. `Layout_OneShotSource_IsEnumeratedOnce` covers all eight collection arrangement paths. Null entries retain their positions. |
| EXT-10 / P2 | `GetCombinedBounds` enumerated the source through `Any`, `First`, and `Skip`; a one-shot source could throw or lose its first renderer. | Initialize and expand bounds in one traversal. `CombinedBounds_OneShotSource_IncludesEveryRenderer` checks exact bounds for two cubes and a null entry. |
| EXT-11 / P2 | `GetWorldScreenBounds` on a camera rotated 45 degrees around Z used only lower-left and upper-right corners. Its AABB could have zero width and exclude the other viewport corners. | Encapsulate all four corners. `ScreenBounds_RotatedCamera_ContainsAllViewportCorners`. |
| EXT-12 / P2 | `WorldToCanvasPoint` returned screen pixels for ScreenSpaceOverlay despite its canvas-local return contract. A world point at the canvas pivot therefore returned the pixel center rather than `(0, 0)`. | Convert every render mode through the canvas RectTransform and use a null UI camera for Overlay. Missing canvas returns zero. `WorldToCanvas_Overlay_ReturnsLocalCoordinates`. |
| EXT-13 / P3 | Eleven named local variables in eight owned files used `var`, violating the repository's explicit-type rule. This is a maintenance issue, not a runtime defect. | Replace those declarations with their exact types; no broad reformatting or API changes. |

### Architecture and compatibility review

- The assembly remains a general utility layer: stateless C# collection/text/format helpers, Unity object adapters, and optional coroutine hosts. No new scene dependency or singleton manager was introduced.
- `Neo.Extensions.asmdef` references `Neo.PropertyAttribute` and retains its existing all-platform runtime setup. Assembly definitions were read, not modified.
- `CreateFromMenuAttribute`, `LegacyComponentAttribute`, `Enums`, `DictionaryExtensions`, `EnumerableExtensions`, `ColorExtension`, `DateTimeExtensions`, `DebugGizmos`, `ObjectExtensions`, `ComponentExtensions`, `Shapes`, `RandomShapeExtensions`, `AudioExtensions` and `NeoDiagnostics` were reviewed without a new behavior change in this pass. Other files have the narrow corrections or explicit-type edits above.
- `NeoDiagnostics.StableId(Object)` remains the only owned version switch for Unity identity APIs. Both Unity compiler branches still require the coordinator's corresponding compiler run; static inspection is not compilation evidence.
- Public method signatures and serialized fields are preserved. `UnityEventDelegateCache` adds private event bookkeeping; replacing a slot now has replacement semantics. PlayerPrefs CSV/JSON read compatibility remains in place. No sample, scene, prefab, package configuration or settings change was made.

## Documentation

- Updated the canonical `Assets/Neoxider/Docs/Extensions/README.md` with real per-class links, assembly setup, typed C# examples, number-format entry points, scene/NoCode usage, diagnostics gates, and a Russian usage summary.
- Corrected `TimeParsingExtensions.md`: the implementation parses numeric parts and does not support the previously claimed `"1h 30m"` syntax. Documented finite-duration validation, separators, culture and failure output in English and Russian.
- Expanded the UnityEvent guide with replacement, event-source tracking, manual Add and Clear semantics in English and Russian.
- Updated existing Primitive, PlayerPrefs, Layout, Screen and UI guides to describe the corrected contracts. Removed unsupported canvas scaling/safe-area claims from the UI guide.
- Added regression source `Assets/Neoxider/Tests/Edit/Extensions/Audit20261023ExtensionsTests.cs` with 32 NUnit cases and unique meta GUID `ed255f0aa8ed43e7bd6c34d1cef7c756`.

## Substantial improvement proposal

**Versioned, storage-independent string-array codec (proposal only).**

**Problem:** legacy CSV cannot represent commas, distinguish null from empty strings, or reliably round-trip a singleton empty element. Heuristic detection of the historical JSON `Value` wrapper also overlaps with ordinary string contents. These are persistence-format limitations; silently rewriting existing stored values during an audit would create a migration risk.

**Proposed API/design:** introduce a pure C# `StringArrayCodec` with `string Encode(IReadOnlyList<string> values)` and `bool TryDecode(string raw, out string[] values)`, backed by a versioned envelope with explicit element lengths/null markers. Add `PlayerPrefsUtils.SetStringArrayV2` and `TryGetStringArray` adapters that use it. Keep codec logic independent of Unity and PlayerPrefs so Save, network payloads and tests can reuse the same contract. Decoding must impose explicit size/count limits before allocating; invalid data returns false without partial results.

**Compatibility:** keep existing setters and their CSV format unchanged. The new reader accepts a recognized version prefix, historical JSON, and CSV through explicit format branches. Migration is opt-in: decode legacy data successfully, then write V2 to the same or a new key only when requested. Do not auto-normalize unrelated text that merely resembles JSON. Document the unavoidable ambiguity of legacy singleton/JSON-shaped values.

**Acceptance:** deterministic round trips for commas, quotes, newlines, Unicode, null elements, empty strings, singleton empty strings and empty arrays; malformed lengths, truncation, unknown versions and allocation-limit tests; fixtures for historical CSV/JSON; proof that old setters still write identical values; Unity EditMode smoke test of the PlayerPrefs adapter. Programmer documentation must show the codec and migration path; scene adapters should call these same typed APIs.

## Verification and remaining risks

- **Executed:** scoped `git diff --check` for tracked owned sources/docs, exit code 0; scoped search found no `var` in owned runtime sources or the new test; the new GUID search found exactly its own meta file. A standalone static Python check decoded all 31 runtime sources and 29 module docs as UTF-8, found no broken local documentation links, and counted 32 new NUnit cases. New test source/meta and this report were checked directly for UTF-8 and trailing whitespace.
- **Not executed by this owner:** Unity compilation, EditMode/PlayMode tests, builds, shared repository test/validation scripts, Unity/MCP actions, or runtime performance profiling. The 32 added cases are pending coordinator execution.
- No files outside the assigned extension/doc/test/report scope were edited. Existing untracked `Assets/Neoxider/DocsEn.meta` was left untouched. No commits or pushes were made.
- Second-pass numeric review remains important for tiny-double conversion to decimal, directed rounding of negative/very large values, grouped trimming, extreme float time formatting, TimeSpan extremes and non-finite/overflowing weights. Coroutine review should verify synchronous completion and externally stopped/disabled-owner handle states. These areas are not claimed fixed or verified by the first-pass regression cases.
- Preview sprite cache ownership across editor resets and UI cursor-allocation costs remain unmeasured; no speculative native-object lifecycle or performance rewrite was made.
