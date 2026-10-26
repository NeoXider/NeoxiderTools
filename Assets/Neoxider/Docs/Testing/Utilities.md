# Utility Edit Mode tests

This guide covers deterministic tests for `Neo.Extensions`, utility components, audio, UI, and their editor adapters. The tests belong to `Neo.Editor.Tests` under `Tests/Edit`; their assembly definition controls the available package references and Editor-only platform constraint.

Start with [Testing NeoxiderTools](../Testing.md) for the canonical testing entry and shared fixture ownership. See [Play Mode testing](../PlayModeTesting.md) for runtime integration, and the [gameplay](Gameplay.md) and [network Play Mode](NetworkPlayMode.md) guides for their specific coverage.

## Test the public contract

Prefer a public C# method and a concrete output: the returned collection, displayed text, event payload, selected item, or remaining time. A successful call alone does not prove the result is correct.

An extension test must call the package method. For example, a `List<T>` receiver calls its own `ForEach`, so use an `IEnumerable<T>` receiver to exercise `EnumerableExtensions.ForEach`:

```csharp
IEnumerable<int> values = new[] { 2, 4, 8 };
List<int> observed = new();
values.ForEach(observed.Add);
CollectionAssert.AreEqual(new[] { 2, 4, 8 }, observed);
```

The example requires `System.Collections.Generic`, `Neo.Extensions`, and `NUnit.Framework`. Check iterator disposal when a callback throws, as well as null inputs and negative or extreme indices. For a copying operation such as `Shuffle(false)`, compare the source with an independent snapshot and assert that the result is a different collection. Include duplicates so a set comparison cannot hide lost elements.

Use fixed UTC instants with nonzero fractional ticks for date serialization. Compare both `Ticks` and `Kind`; comparing only year through second misses precision loss and incorrect time-zone handling. Test duration parsing at the subordinate minute/hour boundaries, with malformed tokens, non-finite values, and custom separators. Failed parsing should return `false` and zero seconds.

`PlayerPrefsUtils.SetFloatArray` writes invariant decimal text. Verify reading after a culture change. The string-array writer uses comma-separated storage and rejects values containing commas; its reader also accepts the legacy JSON object with a `Value` array. Exercise that JSON branch with an actual JSON payload, including escaped quotes and commas inside elements. A rejected write should preserve the previously stored value.

## Components and scene / NoCode workflows

Exercise the same typed API that an Inspector or UnityEvent binding invokes:

| Component | C# path | Observable contract |
| --- | --- | --- |
| `SetText` | `SetPercentage`, `SetCurrency`, `Clear` | Complete TMP string and `OnTextUpdated` payload/count |
| `TimeToText` | `Set`, `TrySetFromString` | `CurrentTime`, complete TMP string, and `OnTimeChanged` |
| `UniversalRotator` | `RotateTo(point, true)` | A real change from an unaligned pose; correct resulting direction and configured axis limit |
| `TimerObject` | `StartTimer`, `Tick(delta)` | Remaining time, progress, and completion events at controlled deltas |
| `InteractiveObject` | `InteractDown`, `InteractUp`, `Click` | Event routing and the interactable gate |
| Audio entries / `AM` | Entry selection and explicit channel APIs | Selected clip, channel level/mute, source ownership, and event counts |

Create components on an inactive fixture object, assign their dependencies, and call the public method explicitly when testing a synchronous contract. Assign the TMP label instead of relying on scene search. Initialize event instances when the test attaches listeners. Preserve the complete formatted output; checking only that a string contains a currency symbol or a few digits can accept the wrong value.

For `TimeToText`, initialize a visible value before passing invalid text. Assert that rejection preserves the displayed string, `CurrentTime`, and event count. Set `Separator` before calling `TrySetFromString` to exercise the configured separator path. Repeating the same valid time should not emit another time-change event.

These synchronous checks cover the public API used by scene/NoCode bindings. They do not verify Inspector binding serialization, Unity lifecycle dispatch, physical device input, rendering, or elapsed-frame behavior. Keep those integration checks separate and use Play Mode when real lifecycle or coroutine dispatch is required. Calling a private `OnDisable` through reflection tests that method's body, not Unity's delivery of the callback.

## Restore everything the fixture owns or changes

Use `try/finally` or fixture teardown that also runs after failed assertions. Store references to created objects and destroy those references only. Do not find cleanup targets by name, tag, `Camera.main`, or a broad scene search: those can refer to objects that existed before the test.

- Destroy generated native assets separately from their GameObjects: `AudioClip`, `Texture2D`, `Sprite`, `Mesh`, materials, and render textures. An `AudioSource` does not own the clip assigned to it. Detach and release render textures before destroying them.
- Capture and restore `UnityEngine.Random.state` around seeded tests. Test invariants rather than a particular shuffle permutation or an unseeded statistical quota.
- Capture and restore culture, cursor flags, diagnostics flags, frame-rate/v-sync settings, event subscriptions, and any other global settings the fixture changes. Run shared-state fixtures sequentially in an exclusive validation host, with no concurrent fixture or editor workflow changing those globals. The runner supplies exclusive scheduling; the installed NUnit configuration does not provide a per-fixture serialization attribute.
- Before replacing `SaveProvider`, preserve its previous provider and restore it after destroying components that may save on disable. Do not reset unrelated singleton or cache state as routine cleanup.
- Use a unique PlayerPrefs key per test, for example a test prefix plus `Guid.NewGuid().ToString("N")`, and delete only that key in `finally`. Avoid `DeleteAll` and unnecessary `PlayerPrefs.Save`; deleting a fixed key can erase an existing value. For fixed EditorPrefs keys, restore both the value and whether the key originally existed.
- Keep scene inspection isolated. Prefer preview scenes or controlled additive scenes and explicitly owned roots. Saving only the active scene path cannot restore dirty edits, untitled scenes, or the complete additive scene setup after opening a sample with `OpenSceneMode.Single`.
- Editor tests must restore selection and isolate their Undo groups. Avoid advancing a shared ticker into the future or stopping every preview when the test owns only one client.

The validation host must have its own company/product and effective PlayerPrefs/persistent-data namespace. Exact cleanup and per-test keys still apply within that host.

## Capabilities and skips

State and configuration assertions should run without audio playback or a graphics device. When a test needs an unavailable capability, probe it independently of the component under test, record a specific skip reason, and release the probe's resources in `finally`.

For example, an independent `AudioSource` can establish whether native one-shot playback is observable. Skipping because `AM.Ambience.isPlaying` is false can hide a regression where `AM` failed to start playback. Keep clip assignment, loop flags, volume, and stop-state assertions outside a playback-only skip. Likewise, IMGUI tests should distinguish an unavailable graphics environment from a missing reflected repaint method or an inspector that failed to draw.

Skipped tests are not passes. Report which capabilities were unavailable and retain separate graphics/audio-enabled integration coverage where those behaviors matter.

## Samples and package layouts

Support the active layout instead of assuming a single source directory:

- Development: `Assets/Neoxider/Samples/...`.
- Release package: `Assets/Neoxider/Samples~/...`, or the corresponding hidden `Samples~` directory inside the installed UPM package.
- Imported sample: `Assets/Samples/NeoxiderTools/<version>/<sample name>/...`, such as `Assets/Samples/NeoxiderTools/11.0.0/Demo Scenes/...`.

Use the resolved package root for package prefabs and source checks; an installed package can live under `Packages/com.neoxider.tools`. Resolve imported sample versions explicitly and avoid selecting an arbitrary stale version. A hidden `Samples~` directory is not an AssetDatabase folder: validate its files as packaging data, then run imported sample integration checks in a Unity project. Report zero discovered assets as absent coverage or an explicit unsupported capability, rather than silently passing an empty loop.

When validating scene hierarchies, check every descendant, including inactive children. A missing component on a child must fail just as it would on a root object.

## Shared fixture support (proposal)

A common resource-ownership scope, independent capability probes, and an explicit package/sample resolver remain proposed test-support improvements. The primary `Neo.Editor.Tests` owner coordinates shared internals, reusing the existing small save/preview scopes and keeping dependency boundaries suitable for tests without Mirror. The supplemental fixtures do not implement these proposals. Their development should preserve existing public runtime APIs and scene/NoCode entry paths.

## Supplemental regression fixtures

The `Audit20261023TestUtilities*` fixtures add public-behavior checks for enumerable dispatch/disposal, copied versus in-place shuffle, extreme collection indices, UTC tick precision, culture-independent preferences, actual legacy JSON, duration boundaries, exact TMP output/events, rejected duration state, and nontrivial instantaneous rotation with limits. They use explicit local types, owned fixture objects, unique preference keys, and restoration of the globals they change.

Adding these fixtures does not replace central Unity compilation and execution. Run the Edit Mode assembly under supported Unity versions; run graphics/audio and Play Mode integration checks separately. A static source review or a clean `git diff --check` is not evidence that Unity tests passed.
