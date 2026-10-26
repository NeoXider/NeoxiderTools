# SelectorItem

**Purpose:** Receive logical selection from a parent `Selector` without requiring GameObject activation.
Namespace: `Neo.Tools`; assembly: `Neo.Tools.View`.

## Setup

- Attach to each item and enable the parent's `NotifySelectorItemsOnly`. The selector assigns `Index`.
- Wire `OnActivated` / `OnDeactivated` to visuals or gameplay adapters. `SelectorItem.SetActive` changes
  logical state; it does not activate/deactivate its own GameObject.
- The first parent synchronization can force matching-state callbacks once. Later identical values
  do not notify again unless `SetActive(value, forceNotify: true)` is requested.

## C# API

`SetActive(bool)`, `Activate()` and `Deactivate()` update state and the `Active` reactive property.
`ValueBool` / `ActiveValue` read that property. `OnValueChangeInverse` passes the inverse state.
`ExcludeFromSelector()` / `IncludeInSelector()` affect the parent's random pool, and `GetSelector()`
returns the cached parent (or null). Reparenting clears that cache for the next lookup. For rule-only
selection outside a scene, use [SelectorModel](./SelectorModel.md).

## Key Fields (Inspector)

| Field | Description |
|-------|-------------|
| `Active` | Active. |
| `ActiveValue` | Active Value. |
| `Index` | Index. |
| `OnActivated` | On Activated. |
| `OnDeactivated` | On Deactivated. |
| `OnValueChangeInverse` | On Value Change Inverse. |
| `ValueBool` | Value Bool. |
| `_index` | Index. |
| `_isActive` | Is Active. |

## See Also

- [Module Root](../README.md)
