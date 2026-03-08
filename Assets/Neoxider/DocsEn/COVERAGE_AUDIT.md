# DocsEn Coverage Audit

Status of English documentation relative to the Russian `Docs/` tree.

## Summary

| Status | Count | Modules |
|--------|-------|---------|
| **Fully covered** | 4 | Animations, Progression, Reactive, UI Extension |
| **Partial** | 18 | All others (README + selected deeper pages) |
| **Missing** | 0 | Every module has at least an EN entry |

## Per-module status

| Module | EN README | Key pages | Notes |
|--------|-----------|-----------|-------|
| **Animations** | ✓ | FloatAnimator, ColorAnimator, Vector3Animator, AnimationType, AnimationUtils | Full coverage |
| **Progression** | ✓ | ProgressionManager, ProgressionNoCodeAction, ProgressionConditionAdapter, Scenarios | Full coverage |
| **Reactive** | ✓ | — | Single README |
| **UI Extension** | ✓ | — | Single README |
| **Save** | ✓ | SaveManager, SaveableBehaviour, SaveProvider, ISaveIdentityProvider, SaveIdentityUtility | Core API covered |
| **Tools/Managers** | ✓ | Bootstrap, Singleton, GM, EM | Core managers covered |
| **Tools/InteractableObject** | ✓ | InteractiveObject, PhysicsEvents2D, PhysicsEvents3D, ToggleObject | All component pages |
| **Quest** | ✓ | QuestManager, QuestConfig, QuestState, QuestBridge | Core flow covered |
| **Shop** | ✓ | Shop, Money, ButtonPrice | Core flow covered |
| **Cards** | ✓ | CardData, DeckConfig, CardComponent | Core flow covered |
| **UI** | ✓ | UI, VisualToggle | Core flow covered |
| **Level** | ✓ | README | Deeper pages RU-only |
| **Condition** | ✓ | README | Deeper pages RU-only |
| **Editor** | ✓ | README | Deeper pages RU-only |
| **Extensions** | ✓ | README | Deeper pages RU-only |
| **Bonus** | ✓ | README | Deeper pages RU-only |
| **Audio** | ✓ | README | Deeper pages RU-only |
| **NPC** | ✓ | README | Deeper pages RU-only |
| **Parallax** | ✓ | README | — |
| **PropertyAttribute** | ✓ | README | Deeper pages RU-only |
| **StateMachine** | ✓ | README | Links to RU deep docs |
| **GridSystem** | ✓ (stub) | GridSystem.md | Overview + RU link |
| **NeoxiderPages** | ✓ | README | Deeper pages RU-only |

## Gaps (RU-only, high value)

- **Shop**: ShopItem, ShopItemData, TextMoney, InterfaceMoney
- **Cards**: DeckComponent, HandComponent, BoardComponent, View layer, Poker, Drunkard
- **UI**: ButtonScale, ButtonShake, AnchorMove, VariantView, PausePage, FakeLoad, etc.
- **Tools**: Many submodules (Inventory, Spawner, Dialogue, etc.) have README only
- **Editor**: CustomEditorBase, NeoCustomEditor, EditorWindows, etc.
- **Extensions**: 27 RU pages, 1 EN README
- **Bonus**: Slot, WheelFortune, Collection, TimeReward, etc.
- **Level**: LevelManager, Map, TextLevel, SceneFlowController
- **Condition**: NeoCondition deep docs
- **StateMachine**: StateMachine, StateMachineBehaviour, NoCode_StateMachine_Usage

## Strategy

- Every module has an EN entry (README or stub).
- Critical runtime modules (Save, Quest, Shop, Cards, UI, Progression, Tools/Managers, Tools/InteractableObject) have selected deeper EN pages.
- When a page is RU-only, the EN index links to the Russian documentation.
- This audit is updated when significant EN coverage is added.
