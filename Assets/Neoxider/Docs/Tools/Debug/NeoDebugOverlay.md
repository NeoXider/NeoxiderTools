# NeoDebugOverlay

An on-screen debug panel rendered with a runtime-built **uGUI canvas and TextMeshPro**. It shows smoothed FPS/frame time, the active scene and time scale, and existing `AM`/`SaveManager` state. Namespace: `Neo.Tools`; assembly: `Neo.Tools.Debug`. Source: `Scripts/Tools/Debug/NeoDebugOverlay.cs`.

**How to use:**

1. Add `NeoDebugOverlay` to any GameObject (**Add Component → Neoxider → Tools → Debug → NeoDebugOverlay**). It creates its own canvas and label; no prefab is required.
2. Press the toggle key (default **F3**) at runtime to show/hide the overlay.
3. Toggle individual sections (FPS, scene, manager states) in the Inspector.
4. Supply an optional TMP font or ensure a TMP default font is available in the project.

The hotkey reuses `Neo.Tools.KeyInputCompat` from the optional `Neo.Tools.Input` module through a cached delegate. This supports legacy input and Input System-only projects without a direct assembly reference. If that module is absent, legacy input is attempted; unavailable input leaves the readout updating without a hotkey.

## Lifetime and C# use

Reference `Neo.Tools.Debug` and attach the component in a scene or with `gameObject.AddComponent<NeoDebugOverlay>()`. Configure its serialized options in the Inspector. `overlay.enabled = false` hides the generated canvas; re-enabling restores the last visibility choice. A hidden overlay keeps sampling FPS but skips text rebuilding.

For an existing runtime `GameObject diagnosticsOwner` dedicated to diagnostics:

```csharp
NeoDebugOverlay overlay = diagnosticsOwner.AddComponent<NeoDebugOverlay>();
overlay.enabled = false;
overlay.enabled = true;
```

For a scene/NoCode toggle, connect a bool UnityEvent to `diagnosticsOwner.SetActive(bool)`. This controls the entire owner and lets the overlay's enable/disable callbacks hide or restore its canvas. Re-enabling preserves the previous F3 visibility choice; it does not force a previously hidden panel to appear.

Destroying the owner removes the generated canvas. Keep the owner alive explicitly if diagnostics should survive scene changes. The panel and label ignore UI raycasts. There is currently no public show/hide method; the hotkey and component lifecycle control visibility.

Manager reads use reflection without direct dependencies on the Audio/Save assemblies. Only already-resolved singletons are displayed. An absent or inactive unresolved `AM` remains absent: inspecting the panel does not invoke singleton discovery or lazy creation. The `random` value is the audio manager's legacy random-music flag.

Each enabled overlay polls input and samples FPS every frame, including when hidden. **Update Interval** throttles visible text reconstruction, not input or sampling. Optional-member discovery and the keyboard delegate are cached per overlay; multiple overlay components remain independent instances.

## Fields

| Field | Description |
|-------|-------------|
| **Toggle Key** | Key that shows/hides the overlay at runtime (default `F3`). |
| **Start Visible** | Whether the overlay is visible when Play starts. |
| **Show Fps** | Show frames-per-second and frame time. |
| **Show Scene** | Show active scene name/build index and time scale. |
| **Show Managers** | Show existing audio and save manager state; missing managers display `—`. |
| **Font Size / Font** | Text size and optional TMP font override. |
| **Background Alpha** | Opacity of the dark panel. |
| **Anchor Corner** | One of the four screen corners. |
| **Update Interval** | Seconds between text rebuilds; at least 0.05. FPS smoothing runs every frame using unscaled time. |

## See also

- [Tools/Debug README](./README.md)
- [Input compatibility](../Input/README.md)
