# Tools / Debug

Runtime diagnostics in assembly `Neo.Tools.Debug`, namespace `Neo.Tools`. These components use TextMeshPro and can be added independently. Scripts live in `Scripts/Tools/Debug/`.

## Components

| Page | Description |
|------|-------------|
| [FPS](./FPS.md) | Rolling FPS counter with an assigned TMP label and explicit frame-rate controls. |
| [ErrorLogger](./ErrorLogger.md) | Filtered Unity log output to an assigned TMP label. |
| [NeoDebugOverlay](./NeoDebugOverlay.md) | Runtime-built uGUI panel for performance, scene and existing manager state. |

## C# and scene use

Reference `Neo.Tools.Debug` from a custom asmdef and use `using Neo.Tools;`. `FPS.CurrentFps`, `FPS.SetTargetFramerate(int)`, `FPS.SetVSync(bool)`, `ErrorLogger.UpdateText(string)` and `ErrorLogger.AppendText(string)` are the public C# entry points. The component pages describe Inspector setup and UnityEvent-compatible methods.

| Component | Scene / NoCode path | C# path |
|-----------|---------------------|---------|
| `FPS` | Assign a TMP label; connect an integer/bool UnityEvent to `SetTargetFramerate` / `SetVSync` when explicit global control is needed. | Read `CurrentFps`; call the same settings methods. |
| `ErrorLogger` | Assign a TMP label and select severity flags; connect a string UnityEvent to `UpdateText` / `AppendText` for direct output. | Configure the public fields and call the same text methods. Automatic capture follows the component's enabled lifetime. |
| `NeoDebugOverlay` | Add the component, configure its Inspector options and use F3. A bool UnityEvent can call `GameObject.SetActive(bool)` on its dedicated owner. | Add the component at runtime and use `overlay.enabled` to control its enabled lifetime. |

`NeoDebugOverlay` creates its own canvas; `FPS` and `ErrorLogger` need assigned text components. Install the project's TMP essentials/default font or supply an overlay font. Disabling a component stops its updates or log subscription; disabling the overlay also hides its generated canvas.

These are opt-in scene tools. The overlay reads existing managers without creating or resolving missing singletons. Frame-rate and VSync changes are global and occur only through explicit `FPS` calls or its opt-in **Unlock Framerate On Awake** setting.

ErrorLogger history and appended text remain unlimited. Replacing the label does not clear duplicate history; the component currently has no public capture/history-clear API.

## Related modules

- [Tools](../README.md)
- [Input](../Input/README.md): optional keyboard compatibility used by the overlay.
- [Audio](../../Audio/README.md) and [Save](../../Save/README.md): optional manager readouts; neither is required by the Debug assembly.
