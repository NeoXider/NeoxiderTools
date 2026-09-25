# BillboardUniversal

**Purpose:** See Inspector fields below for configuration.

## Setup

- Add the component via the Unity menu.

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `BillboardMode` | Billboard Mode. |
| `billboardMode` | Billboard Mode. |
| `customDirection` | Custom Direction. |
| `targetCamera` | Target Camera. |

## Edit Mode

Rotation happens only in `LateUpdate` (Play Mode); there is no `OnValidate`, so loading or saving a prefab
never writes a camera-dependent rotation into it. To preview the orientation in the editor, use the
component's context menu **Face Camera Now** (recorded in Undo). Full description:
[Tools/Other/BillboardUniversal](../Other/BillboardUniversal.md).

## See Also

- [Module Root](../README.md)