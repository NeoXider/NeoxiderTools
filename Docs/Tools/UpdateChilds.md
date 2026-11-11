# UpdateChilds

**What it is:** a NoCode `MonoBehaviour` that raises a UnityEvent whenever the direct children of its Transform change
(a child is added, removed or re-parented). Script: `Scripts/Tools/Components/UpdateChilds.cs`, namespace
`Neo.Tools`, assembly `Neo.Tools.Components`.

**How to use:**
1. Add **Neoxider / Tools / UpdateChilds** to the container whose children you watch (a list, a slot grid).
2. Wire **On Change Childs Count** to the reaction, for example a layout refresh or a "list is empty" check.

---

## Fields

| Field | Description |
|-------|-------------|
| `OnChangeChildsCount` | `UnityEvent` invoked from Unity's `OnTransformChildrenChanged` message. |

The event is fired by Unity itself (no polling), only for direct children, and only while the component's GameObject
is active. It reports that the child list changed; read `transform.childCount` in the listener for the new count.

## C# alternative

In code, implement `OnTransformChildrenChanged()` on your own component, or subscribe from code:

```csharp
UpdateChilds watcher = container.GetComponent<UpdateChilds>();
watcher.OnChangeChildsCount.AddListener(() => emptyLabel.SetActive(container.childCount == 0));
```

## See also

- [Tools](README.md)
- [Components](Components/README.md)
