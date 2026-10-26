# Custom Card View Implementation

**What it is:** A step-by-step guide: how to build your own card view and hook it into the system (HandView, animations, layouts).

**How to use:** see the sections below.

---


A step-by-step guide: how to build your own card view and hook it into the system (HandView, animations, layouts).

---

## Step 1. Minimal ICardView

Implement the [ICardView](../Interfaces.md) interface: Data, IsFaceUp, Transform, SetData, Flip, FlipAsync, MoveToAsync, SetInteractable, and the OnClicked/OnHovered/OnUnhovered events.

Skeleton example:

```csharp
public class MyCardView : MonoBehaviour, ICardView
{
    public CardData Data { get; private set; }
    public bool IsFaceUp { get; private set; }
    public Transform Transform => transform;
    public event Action<ICardView> OnClicked;
    public event Action<ICardView> OnHovered;
    public event Action<ICardView> OnUnhovered;

    public void SetData(CardData data, bool faceUp = true)
    {
        Data = data;
        IsFaceUp = faceUp;
        // update the visuals (sprite, text, etc.)
    }

    public void Flip()
    {
        IsFaceUp = !IsFaceUp;
        // update the visuals
    }

    public async UniTask FlipAsync(float duration = 0.3f)
    {
        // flip animation (e.g. scaleX 0 -> 1 and sprite swap)
        IsFaceUp = !IsFaceUp;
        await UniTask.CompletedTask;
    }

    public async UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
    {
        Tween tween = transform.DOMove(position, duration).SetEase(Ease.OutQuad).SetLink(gameObject);
        try
        {
            await UniTask.WaitUntil(() => !tween.IsActive(),
                cancellationToken: this.GetCancellationTokenOnDestroy());
        }
        finally
        {
            tween.Kill();
        }
    }

    public void SetInteractable(bool interactable) { /* raycast, button */ }
}
```

Hooking up: pass the instance to **HandView.AddCardAsync(myCardView)** or to **CardPresenter**.

---

## Step 2. Optional: ICardDisplayMode

If the card is always face up or always face down — implement **ICardDisplayMode** and return **AlwaysFaceUp** or **AlwaysFaceDown**. Then Flip/FlipAsync can be left empty or as no-ops.

---

## Step 3. Optional: ICardViewAnimations

Implement **ICardViewAnimations**: PlayOneShotAsync, PlayLooped, StopLooped, StopAllLooped. Inside, call [CardViewAnimationTemplates](CardViewUniversal.md#переиспользование-шаблонов) for your `transform`:

```csharp
public async UniTask PlayOneShotAsync(CardViewAnimationType type, float? duration = null, CancellationToken cancellation = default)
{
    cancellation.ThrowIfCancellationRequested();
    using CancellationTokenSource lifetime = CancellationTokenSource.CreateLinkedTokenSource(
        cancellation, this.GetCancellationTokenOnDestroy());
    Tween t = CardViewAnimationTemplates.PlayOneShot(transform, type, duration ?? 0.3f);
    if (t != null)
    {
        using CancellationTokenRegistration registration = lifetime.Token.Register(() => t.Kill());
        try
        {
            await UniTask.WaitUntil(() => !t.IsActive(), cancellationToken: lifetime.Token);
        }
        finally
        {
            t.Kill();
        }
    }
}
```

Looped animations: store the active tweens in a dictionary keyed by type and kill them on StopLooped.

---

## Step 4. Hooking Into the System

- **HandView** works with any **ICardView**: adding/removing cards, layout (Fan, Line, Grid).
- **CardPresenter** takes (ICardView view, DeckConfig config) and binds the data to the view.
- **CardPresenter.IsFaceUp** reads the view's actual state. Fixed modes must keep `IsFaceUp` consistent with their rendered side when receiving `SetData`, `Flip`, or `FlipAsync`.
- A custom **IHandView** owns `CardViews` order. Existing implementations retain nonanimated remove/add sorting before final arrangement. Removing a view must keep its object usable for reuse/transfer; the gameplay owner handles destruction.
- Optionally implement **IOrderedHandView.SetOrderAsync(IReadOnlyList<ICardView>, bool, CancellationToken)** to apply an exact current-member permutation in one layout without membership callbacks or reparenting. Validate the complete permutation before mutation; cancellation stops motion while keeping committed order. Built-in **HandView** provides this path. Callback-driven presenter changes stop an obsolete legacy synchronization pass.
- The minimal card skeleton above shows destruction cleanup. For overlapping move/flip calls, also cancel the prior operation and guard post-await state. Built-in **CardView** and **CardViewUniversal** already provide concrete cancellation overloads and exact tween ownership; custom **ICardView** implementations need no new mandatory members.
- For drop/drag: handle clicks or **IDragHandler** on your view and call **MoveToAsync** or move cards between zones in the game logic.

---

## Step 5. When to Use CardComponent vs Your Own ICardView

| Option | When to Use |
|---------|--------------------|
| **CardComponent** | No-code, inspector setup, classic cards (36/52/54-card deck), existing examples (War/Drunkard). |
| **Custom ICardView / CardViewUniversal** | Code-driven MVP, arbitrary cards, custom visuals and animations, "always face up" modes, etc. |

---

## When the Card Is Not CardData (CCG, roguelike)

If you have your own card model (id, name, cost, art):

- Create **your own view interface** (e.g. IMyGameCardView) with **SetData(MyCardData)** and your own events if needed.
- Implement visuals and zone logic yourself; for animations and layouts **reuse**:
  - [CardViewAnimationTemplates](CardViewUniversal.md#переиспользование-шаблонов) — Bounce, Pulse, Shake, Highlight, FlyIn, Idle;
  - **CardLayoutCalculator** and **CardLayoutSettings** (Config, Utils) for card positions in a hand/zone.

This way you are not tied to CardData, but still reuse the common animations and layouts.

---

## See Also

- [Interfaces](../Interfaces.md)
- [CardViewUniversal](CardViewUniversal.md)
- [README Cards](../README.md) — two branches (classic cards vs arbitrary ones)
