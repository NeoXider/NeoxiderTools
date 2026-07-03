# Neo.NoCode

No-code UI bindings for scenes where designers should connect data to text and progress visuals without writing one-off view scripts.

## UX Discipline And C# Contract

NoCode is a data-binding layer for UI, not hidden gameplay logic in the Inspector. New behavior must expose a testable C# contract and have EditMode or PlayMode coverage before adding Inspector convenience around it.

NoCode runs on scene components. ScriptableObject assets must not store scene object references; if a setup lives in an SO, store only data, keys, or context slots, and assign concrete `GameObject` references on the scene component.

Current `ComponentFloatBinding` contract:

- data sources are readable fields and readable non-indexed properties only;
- methods addressed by string name are not valid sources and are never invoked;
- `ReactivePropertyFloat`, `ReactivePropertyInt`, and `ReactivePropertyBool` are supported source values;
- `GameObject.Find` is allowed only as an explicit `Find By Name` fallback with a retry interval, not as the primary architecture.

For business rules, keep the rule in a normal C# component or service, test it there, and expose only the final field, property, or reactive state to NoCode.

## Components

| Component | Purpose |
|-----------|---------|
| `NoCodeBindText` | Reads a numeric or string source and pushes it into `SetText`, TMP, or compatible text targets. |
| `NoCodeFormattedText` | Formats one or more resolved values into a template string before writing to UI. |
| `SetProgress` | Writes normalized progress into Slider/Image-style visual targets. |

## Source Flow

1. Select the source object.
2. Select the source component, field, or property exposed by the resolver.
3. Select the target UI component.
4. Configure formatting, clamping, or progress range when needed.

The module is intended for Inspector-driven prototypes and reusable UI screens. It intentionally does not support arbitrary method invocation by string.

## Related docs

- [Condition](../Condition/README.md)
- [Reactive](../Reactive/README.md)
- [Tools/Text](../Tools/Text/README.md)
