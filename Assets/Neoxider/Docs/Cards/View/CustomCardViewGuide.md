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
        var tween = transform.DOMove(position, duration).SetEase(Ease.OutQuad);
        await UniTask.WaitUntil(() => !tween.IsActive());
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
    Tween t = CardViewAnimationTemplates.PlayOneShot(transform, type, duration ?? 0.3f);
    if (t != null)
        await UniTask.WaitUntil(() => !t.IsActive(), cancellationToken: cancellation);
}
```

Looped animations: store the active tweens in a dictionary keyed by type and kill them on StopLooped.

---

## Step 4. Hooking Into the System

- **HandView** works with any **ICardView**: adding/removing cards, layout (Fan, Line, Grid).
- **CardPresenter** takes (ICardView view, DeckConfig config) and binds the data to the view.
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
