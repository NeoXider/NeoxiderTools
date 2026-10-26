# Tools / View

`Neo.Tools.View` contains reusable scene views for selection, progress, stars, facing, light and emission.
Scripts live in `Scripts/Tools/View/`. Selection rules also have a plain C# entry point: `SelectorModel`.

## Choose a component

| Page | Description |
|------|-------------|
| [ImageFillAmountAnimator](./ImageFillAmountAnimator.md), [BillboardUniversal](./BillboardUniversal.md) | UI/3D |
| [Selector](./Selector.md), [SelectorModel](./SelectorModel.md), [SelectorItem](./SelectorItem.md) | Scene selection, pure selection rules and item callbacks |
| [StarView](./StarView.md) | ScoreManager star display |
| [LightAnimator](./LightAnimator.md), [MeshEmission](./MeshEmission.md) | Light/emission |
| [ZPositionAdjuster](./ZPositionAdjuster.md) | Z-order |

## C# and Inspector workflows

Use `Neo.Tools` for selection, fill, billboard, stars and Z ordering; use `Neo.Tools.View` for
`LightAnimator` and `MeshEmission`. A custom assembly consuming these types must reference `Neo.Tools.View`.
Use `SelectorModel` directly when no scene objects are needed; `Selector.CreateModelSnapshot()` gives an
independent copy. Scene components expose typed methods and optional UnityEvents for Inspector wiring.

Assign dependencies before activating runtime-created objects. In particular, `StarView.scoreManager`
is explicit, `MeshEmission` needs an assigned material on its `MeshRenderer`, and camera-facing billboard
modes need either an assigned camera or the optional `Camera.main` fallback. Custom-direction billboards
do not need a camera. DOTween is used by the fill animator; Light2D support is discovered only when URP
is installed. `MeshEmission` restores a surviving renderer's source material when its component is removed.

Selection persistence initializes in `Start` before runtime writes are allowed. For networking, the
component delegates authority to the existing Neo network layer; the pure model has no network or save
dependencies. See each component page for lifecycle details and callable APIs.

In 11.0, selection bounds are checked instead of silently overflowing, deferred index replacements
preserve their previous set on enumeration failure, and received selection events see complete snapshots.
Network admission uses per-caller budgets; see [Selector](./Selector.md) for authority and matching-peer
version requirements, and [SelectorModel](./SelectorModel.md) for the exact integer/input contracts.

## See also

- [Animations](../../Animations/README.md)
