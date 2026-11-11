# UI Extension module

**What it is:** ready-made uGUI prefabs in `Assets/Neoxider/UI Extension/Prefabs/` - starting points for a
Canvas, layouts and scroll lists. There are no scripts in this module; the prefabs use Unity UI components and
can host the [UI module](../UI/README.md) components.

**Navigation:** [← Docs](../README.md)

**Contents:**

| Prefab | Use |
|--------|-----|
| `Canvas/Canvas LandScape.prefab`, `Canvas/Canvas Portait.prefab` | Canvas with `Scale With Screen Size` at 1920x1080 (landscape) or 1080x1920 (portrait). |
| `Layout/Horizontal Layout.prefab`, `Layout/Vertical Layout.prefab` | Layout group containers. |
| `ScrollRect.prefab` | Scroll view with a masked viewport and a `Content` container. |
| `Page.prefab` | Full-screen page root with Top / Bot / Center anchor areas, for the [UI](../UI/UI.md) page manager. |
| `Money Layout.prefab` | Horizontal icon (`Image`) + `TextMeshPro` counter, e.g. a currency display. |

---

Drag a prefab into the scene (or under a Canvas) and unpack or override it as needed.

