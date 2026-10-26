# LayoutExtensions

**Purpose:** Extension methods providing `LayoutUtils` grid/arrangement helpers directly on `Transform` collections.

Collection arrangement methods materialize the source once, then use that same ordered list for both count and placement. One-shot enumerables are supported. Null entries retain their layout slots and are skipped during placement; a null collection is a no-op.

## See Also
- [LayoutUtils](LayoutUtils.md)
- ← [Extensions](README.md)
