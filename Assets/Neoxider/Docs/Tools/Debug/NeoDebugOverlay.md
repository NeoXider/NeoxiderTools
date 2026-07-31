# NeoDebugOverlay

**What it is:** a drop-in on-screen debug panel rendered with IMGUI — shows FPS, frame time, active scene, time scale, and known manager states (`AM`, `SaveManager`). Manager state is read via reflection so the overlay has no assembly dependency on the Audio/Save modules. Path: `Scripts/Tools/Debug/NeoDebugOverlay.cs`, namespace `Neo.Tools`.

**How to use:**
1. Add `NeoDebugOverlay` to any GameObject (`Add Component → Neoxider/Tools/Debug/NeoDebugOverlay`) — no scene or prefab dependencies required.
2. Press the toggle key (default **F3**) at runtime to show/hide the overlay.
3. Toggle individual sections (FPS, scene, manager states) in the Inspector.

---

## Fields

| Field | Description |
|-------|-------------|
| **Toggle Key** | Key that shows/hides the overlay at runtime (default `F3`). |
| **Start Visible** | Whether the overlay is visible when Play starts. |
| **Show Fps** | Show frames-per-second and frame time. |
| **Show Scene** | Show the active scene name. |

## See also

- [Tools/Debug README](./README.md)
