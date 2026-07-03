# Camera Aspect Ratio Scaler Tool

**What it is:** The component supports several scaling modes, which lets you choose the optimal strategy for your project, whether it is a 2D or 3D game.

**How to use:** see the sections below.

---


## 1. Introduction

`CameraAspectRatioScaler` is a tool for stable camera adaptation to different aspect ratios. The component works correctly with both orthographic and perspective cameras: for 3D it uses a mathematically correct horizontal/vertical FOV recalculation, and for `FitBoth` a letterbox/pillarbox mode via `Camera.rect` is available.

The component supports several scaling modes, which lets you choose the optimal strategy for your project, whether it is a 2D or 3D game.

---

## 2. Class Description

### CameraAspectRatioScaler
- **Namespace**: `Neo`
- **File path**: `Assets/Neoxider/Scripts/Tools/CameraAspectRatioScaler.cs`

**Description**
A component that is attached to a `Camera` and automatically adjusts its parameters to adapt to different screen resolutions.

**Key features**
- **Versatility**: Works with both orthographic and perspective cameras.
- **Scaling modes**:
  - `FitWidth`: Preserves the view width, adjusting the height.
  - `FitHeight`: Preserves the view height, adjusting the width.
  - `FitBoth`: Shows the entire target area (without cropping). You can use:
    - camera scaling;
    - `Camera.rect` for letterbox/pillarbox (`useViewportRectInFitBoth`).
- **Editor and runtime operation**: There are separate update flags for Play Mode and Edit Mode.
- **Update optimization**: Recalculation is performed only when parameters or the screen size change.

**Public methods**
- The core logic works automatically.
- The inspector provides an `Apply Camera Scale` button for a manual forced update.


## Additional Fields

| Field | Description |
|------|----------|
| `1f` | 1f. |
| `20f` | 20f. |
| `5f` | 5f. |
| `ScaleMode` | Scale Mode. |
| `scaleMode` | Scale Mode. |
| `targetResolution` | Target Resolution. |
| `true` | True. |
