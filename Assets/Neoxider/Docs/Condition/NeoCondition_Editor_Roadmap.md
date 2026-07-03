# NeoCondition — Editor/No-Code System Roadmap

**What it is:** Make `NeoCondition` a professional, extensible, and reusable system:

**How to use:** see the sections below.

---


## Goal

Make `NeoCondition` a professional, extensible, and reusable system:
- convenient for no-code users;
- resilient to code refactoring;
- suitable for reuse in other modules (`StateMachine`, `Trigger`, `Quest`, `AI Rules`).

---

## Problems with the Current Implementation

1. `NeoConditionEditor` contains too much logic in a single class (a monolithic inspector).
2. Field/property selection is based on reflection with no explicit contracts on the runtime-class side.
3. Member selection serialization (`_componentTypeName` + `_propertyName`) is fragile under renames.
4. The member selection logic is hard to reuse in other no-code systems.
5. The UI keeps growing, but there is no shared infrastructure layer for condition editor tools.

---

## Architectural Direction

### 1) Metadata via Attributes

Introduce an attribute system to explicitly describe condition variables/fields.

Proposed attributes:
- `ConditionValueAttribute` — the field/property is available to `NeoCondition`.
- `ConditionLabelAttribute(string label)` — human-readable name in the dropdown.
- `ConditionGroupAttribute(string group)` — grouping in the menu (e.g., `Stats/HP`).
- `ConditionOpsAttribute(params CompareOp[])` — allowed operators for the value.
- `ConditionOrderAttribute(int order)` — priority in the list.

Pros:
- explicit contracts;
- fewer noisy fields in the list;
- simpler UX for designers.

---

### 2) Shared Condition Member Registry (Editor)

Create an editor service, e.g. `ConditionMemberRegistry`, which:
- gathers available members per type;
- caches the result (`Type -> MemberDescriptor[]`);
- supports cache invalidation on domain reload/refresh;
- respects attributes and a fallback mode for legacy.

`MemberDescriptor` (example):
- `TypeFullName`
- `MemberName`
- `MemberKind` (`Field` / `Property`)
- `ValueType`
- `DisplayLabel`
- `Group`
- `AllowedOps`
- `Order`

Pros:
- reusability;
- unified validation logic;
- faster inspector.

---

### 3) Robust Member Selection Serialization

Move from a pair of strings to an identifier structure:
- `AssemblyQualifiedTypeName`
- `MemberName`
- `MemberKind`
- `ResolvedValueType`
- `Version` (for migrations)

Add migration:
- automatically populate the new format when loading old data;
- preserve backward compatibility with old scenes and prefabs.

---

### 4) Modular Editor UI (Drawer Pipeline)

Split the current `NeoConditionEditor` into independent blocks:
- `ConditionSourceDrawer`
- `ConditionMemberDrawer`
- `ConditionCompareDrawer`
- `ConditionRuntimePreviewDrawer`
- `ConditionListDrawer`
- `ConditionValidationDrawer`

Pros:
- easier to maintain;
- reusable in other inspectors;
- lower risk of regressions when adding new features.

---

### 5) Context Variables via a Provider Interface

Add a new source mode:
- `SourceMode.Provider`

Introduce an interface:

```csharp
public interface IConditionVariableProvider
{
    bool TryGet(string key, out int value);
    bool TryGet(string key, out float value);
    bool TryGet(string key, out bool value);
    bool TryGet(string key, out string value);
}
```

This enables:
- working without reflection where data is computed dynamically;
- connecting conditions to external systems (economy, AI, quests, server data);
- building complex no-code scenarios without fragile references to specific fields.

---

## UX Improvements (Inspector)

1. Search bar over components/properties.
2. Grouping (`Stats`, `Combat`, `UI`, `Meta`).
3. Favorite fields (pin/favorites).
4. Inline issue validation:
   - member not found;
   - type has changed;
   - operator not supported.
5. Quick actions:
   - `Ping Source Object`;
   - `Select Source Object`;
   - `Open Script` (if possible).
6. Preview mode:
   - current values in play mode;
   - highlighting of conditions that evaluate to `false`.

---

## Backward Compatibility

Mandatory rules:
- do not break existing scenes/prefabs;
- old `ConditionEntry` instances must keep working;
- all new fields must be added with safe default values;
- migration-on-load + warnings only for genuinely unresolvable conflicts.

---

## Implementation Phases

### Phase 1 — Foundation (no breaking changes)

1. Add attributes:
   - `ConditionValueAttribute`
   - `ConditionLabelAttribute`
   - `ConditionGroupAttribute`
   - `ConditionOpsAttribute`
   - `ConditionOrderAttribute`
2. Implement `ConditionMemberRegistry` and `MemberDescriptor`.
3. Hook the registry into `NeoConditionEditor` (with fallback to the current reflection).
4. Add basic unit tests for member resolution.

Expected result:
- the UI works as before, but already through the new data layer.

### Phase 2 — Data Model hardening

1. Add a new serializable member identifier.
2. Implement migration of old `ConditionEntry` data.
3. Add detailed mismatch diagnostics.

Expected result:
- resilience to component renames/moves.

### Phase 3 — Editor UX

1. Split the inspector into drawer modules.
2. Add search/groups/favorites.
3. Improve the runtime preview and debugging hints.

Expected result:
- a noticeably more convenient no-code workflow for designers.

### Phase 4 — Provider Mode

1. Add `SourceMode.Provider`.
2. Add `IConditionVariableProvider` support.
3. Implement UI for selecting key + value type + operators.

Expected result:
- universal conditions for any system without relying on reflection.

---

## Risks and Mitigations

1. **Risk:** growing inspector complexity.  
   **Mitigation:** modular drawer classes and tests.

2. **Risk:** breaking previously saved conditions.  
   **Mitigation:** migration + fallback + prefab integration tests.

3. **Risk:** excessive flexibility (hard for beginners).  
   **Mitigation:** basic UI mode + advanced foldout.

4. **Risk:** editor performance degradation.  
   **Mitigation:** registry cache + lazy updates.

---

## Definition of Done

- All existing demos and scenes work without changes.
- The new editor pipeline is covered by at least smoke tests and a manual checklist.
- Inspector open time has not noticeably worsened.
- The user can configure a condition faster thanks to search/groups.
- Documentation updated:
  - `Docs/Condition/NeoCondition.md`
  - changelog
  - provider mode usage examples (after Phase 4).

---

## Minimal MVP to Start (Recommended)

Do first:
1. The `ConditionValueAttribute` attribute.
2. `ConditionMemberRegistry`.
3. Hook the registry into the current `NeoConditionEditor` without changing the runtime model.

This provides maximum value with minimal risk and lays the foundation for all subsequent phases.
