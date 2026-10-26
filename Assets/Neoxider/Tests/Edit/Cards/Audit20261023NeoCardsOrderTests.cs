using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Neo.Cards;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Cards
{
    public sealed class Audit20261023NeoCardsOrderTests
    {
        private readonly List<Object> _owned = new();
        private readonly List<CardPresenter> _presenters = new();

        [TearDown]
        public void TearDown()
        {
            foreach (CardPresenter presenter in _presenters)
            {
                presenter.Dispose();
            }

            _presenters.Clear();
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PresenterBuiltInSort_UsesLinearLayoutWorkAndRetainsClicksAndIndexedRemoval(bool byRank)
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            SerializedObject serialized = new(view);
            serialized.FindProperty("_layoutType").intValue = (int)CardLayoutType.Line;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            HandPresenter hand = Fill(view, 16);
            CountingCard[] cards = hand.CardPresenters.Select(card => (CountingCard)card.View).ToArray();
            int membershipEvents = 0;
            int clicks = 0;
            hand.OnCardAdded += card => membershipEvents++;
            hand.OnCardRemoved += card => membershipEvents++;
            hand.OnCardClicked += card => clicks++;
            foreach (CountingCard card in cards)
            {
                card.TransformReads = 0;
            }

            Complete(byRank ? hand.SortByRankAsync(true, false) : hand.SortBySuitAsync(false, false));

            Assert.That(cards.Sum(card => card.TransformReads), Is.LessThanOrEqualTo(cards.Length * 8),
                "Sorting built-in views must not run a complete layout for every remove/add.");
            Assert.That(membershipEvents, Is.Zero);
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.View), view.CardViews);
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.Data), hand.Model.Cards);
            for (int i = 1; i < view.Count; i++)
            {
                Assert.That(view.CardViews[i].Transform.localPosition.x,
                    Is.GreaterThan(view.CardViews[i - 1].Transform.localPosition.x));
            }

            cards[0].Click();
            Assert.That(clicks, Is.EqualTo(1));
            CardPresenter expectedRemoval = hand.CardPresenters[5];
            Assert.That(hand.RemoveAtAsync(5, false).GetAwaiter().GetResult(), Is.SameAs(expectedRemoval));
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.View), view.CardViews);
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.Data), hand.Model.Cards);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        public void SetOrder_InvalidPermutation_LeavesMembershipAndTransformsUnchanged(int invalidKind)
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            Fill(view, 3);
            ICardView[] before = view.CardViews.ToArray();
            Vector3[] positions = before.Select(card => card.Transform.localPosition).ToArray();
            ICardView[] invalid = invalidKind switch
            {
                0 => new[] { before[0], before[0], before[2] },
                1 => new[] { before[0], before[1] },
                _ => new[] { before[0], before[1], NewCard(99) }
            };

            Assert.Throws<ArgumentException>(() => view.SetOrderAsync(invalid, false).GetAwaiter().GetResult());

            CollectionAssert.AreEqual(before, view.CardViews);
            CollectionAssert.AreEqual(positions, before.Select(card => card.Transform.localPosition));
        }

        [Test]
        public void SetOrder_SelfSource_IsSnapshottedWithoutLosingMembers()
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            Fill(view, 3);
            ICardView[] before = view.CardViews.ToArray();

            Complete(view.SetOrderAsync(view.CardViews, false));

            CollectionAssert.AreEqual(before, view.CardViews);
        }

        [Test]
        public void SetOrder_PreCancelled_LeavesOrderUnchanged()
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            Fill(view, 3);
            ICardView[] before = view.CardViews.ToArray();
            using CancellationTokenSource cancellation = new();
            cancellation.Cancel();

            Assert.Throws<OperationCanceledException>(() =>
                view.SetOrderAsync(before.Reverse().ToArray(), false, cancellation.Token).GetAwaiter().GetResult());

            CollectionAssert.AreEqual(before, view.CardViews);
        }

        [Test]
        public void PresenterLegacyView_KeepsFallbackAndCorrectOrder()
        {
            LegacyView view = new();
            HandPresenter hand = Fill(view, 4);
            view.ResetCounters();

            Complete(hand.SortByRankAsync(true, false));

            Assert.That(view.AddCalls, Is.EqualTo(4));
            Assert.That(view.RemoveCalls, Is.EqualTo(4));
            Assert.That(view.ArrangeCalls, Is.EqualTo(1));
            CollectionAssert.AreEqual(hand.CardPresenters.Select(card => card.View), view.CardViews);
        }

        [Test]
        public void PresenterLegacyView_ReentrantClear_StopsStaleRepopulation()
        {
            LegacyView view = new();
            HandPresenter hand = Fill(view, 4);
            view.ResetCounters();
            view.AfterRemove = hand.Clear;

            Complete(hand.SortBySuitAsync(true, false));

            Assert.That(hand.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
            Assert.That(view.AddCalls, Is.Zero);
            Assert.That(view.RemoveCalls, Is.EqualTo(1));
        }

        [Test]
        public void PresenterModelSort_ReentrantClear_LeavesAllLayersEmpty()
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            HandPresenter hand = Fill(view, 4);
            bool clearing = false;
            hand.Model.OnHandChanged += () =>
            {
                if (!clearing)
                {
                    clearing = true;
                    hand.Clear();
                }
            };

            Complete(hand.SortByRankAsync(true, false));

            Assert.That(hand.Model.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
        }

        [Test]
        public void PresenterBuiltInLayout_ReentrantClear_DoesNotWriteOldMemberTransforms()
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            HandPresenter hand = Fill(view, 4);
            CountingCard[] cards = hand.CardPresenters.Select(card => (CountingCard)card.View).ToArray();
            Vector3[] positions = cards.Select(card => card.Transform.localPosition).ToArray();
            foreach (CountingCard card in cards)
            {
                card.BeforeTransformRead = hand.Clear;
            }

            Complete(hand.SortByRankAsync(true, false));

            Assert.That(hand.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
            foreach (CountingCard card in cards)
            {
                card.BeforeTransformRead = null;
            }

            CollectionAssert.AreEqual(positions, cards.Select(card => card.Transform.localPosition));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PresenterModelAddOrRemove_ReentrantClear_StopsStaleWorkAndAllowsOrdinaryReuse(bool adding)
        {
            HandView view = Own(new GameObject("Hand")).AddComponent<HandView>();
            HandPresenter hand = Fill(view, adding ? 0 : 1);
            CardPresenter card = adding ? NewPresenter(6) : hand.CardPresenters[0];
            int additions = 0;
            int removals = 0;
            int clicks = 0;
            hand.OnCardAdded += added => additions++;
            hand.OnCardRemoved += removed => removals++;
            hand.OnCardClicked += clicked => clicks++;
            Action<CardData> clear = null;
            clear = data =>
            {
                if (adding)
                {
                    hand.Model.OnCardAdded -= clear;
                }
                else
                {
                    hand.Model.OnCardRemoved -= clear;
                }

                hand.Clear();
            };
            if (adding)
            {
                hand.Model.OnCardAdded += clear;
            }
            else
            {
                hand.Model.OnCardRemoved += clear;
            }

            Complete(adding ? hand.AddCardAsync(card, false) : hand.RemoveCardAsync(card, false));

            Assert.That(hand.Model.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
            Assert.That(additions, Is.Zero);
            Assert.That(removals, Is.Zero);
            ((CountingCard)card.View).Click();
            Assert.That(clicks, Is.Zero, "An invalidated operation must not retain a click subscription.");

            Complete(hand.AddCardAsync(card, false));
            CollectionAssert.AreEqual(new[] { card.Data }, hand.Model.Cards);
            CollectionAssert.AreEqual(new[] { card }, hand.CardPresenters);
            CollectionAssert.AreEqual(new[] { card.View }, view.CardViews);
            ((CountingCard)card.View).Click();
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(additions, Is.EqualTo(1));
            Complete(hand.RemoveCardAsync(card, false));
            Assert.That(hand.Model.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
            Assert.That(removals, Is.EqualTo(1));
            ((CountingCard)card.View).Click();
            Assert.That(clicks, Is.EqualTo(1));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void PresenterDelayedAddOrRemove_ClearAndReplacement_SuppressOldNotifications(bool adding)
        {
            DelayedView view = new();
            HandPresenter hand = Fill(view, adding ? 0 : 1);
            CardPresenter card = adding ? NewPresenter(7) : hand.CardPresenters[0];
            int additions = 0;
            int removals = 0;
            int clicks = 0;
            hand.OnCardAdded += added => additions++;
            hand.OnCardRemoved += removed => removals++;
            hand.OnCardClicked += clicked => clicks++;
            view.DelayAdd = adding;
            view.DelayRemove = !adding;
            UniTask pending = adding ? hand.AddCardAsync(card, false) : hand.RemoveCardAsync(card, false);
            Assert.That(pending.GetAwaiter().IsCompleted, Is.False);

            hand.Clear();
            Complete(hand.AddCardAsync(card, false));
            view.CompletePending();
            Complete(pending);

            CollectionAssert.AreEqual(new[] { card.Data }, hand.Model.Cards);
            CollectionAssert.AreEqual(new[] { card }, hand.CardPresenters);
            CollectionAssert.AreEqual(new[] { card.View }, view.CardViews);
            Assert.That(additions, Is.EqualTo(1), "Only the replacement admission publishes an add.");
            Assert.That(removals, Is.Zero, "An old removal cannot announce removal of its replacement.");
            ((CountingCard)card.View).Click();
            Assert.That(clicks, Is.EqualTo(1));
            Complete(hand.RemoveCardAsync(card, false));
            Assert.That(removals, Is.EqualTo(1));
            Assert.That(hand.Model.Count, Is.Zero);
            Assert.That(hand.CardPresenters, Is.Empty);
            Assert.That(view.CardViews, Is.Empty);
            ((CountingCard)card.View).Click();
            Assert.That(clicks, Is.EqualTo(1));
        }

        private CardPresenter NewPresenter(int index)
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            CardPresenter card = new(NewCard(index), config);
            card.SetData(new CardData(Suit.Clubs, (Rank)(2 + index % 13)));
            _presenters.Add(card);
            return card;
        }

        private HandPresenter Fill(IHandView view, int count)
        {
            DeckConfig config = Own(ScriptableObject.CreateInstance<DeckConfig>());
            HandPresenter hand = new(new HandModel(), view);
            for (int i = count - 1; i >= 0; i--)
            {
                CountingCard cardView = NewCard(i);
                CardPresenter card = new(cardView, config);
                card.SetData(new CardData((Suit)(i % 4), (Rank)(2 + i % 13)));
                _presenters.Add(card);
                Complete(hand.AddCardAsync(card, false));
            }

            return hand;
        }

        private CountingCard NewCard(int index)
        {
            return new CountingCard(Own(new GameObject("Card" + index)).transform);
        }

        private T Own<T>(T value) where T : Object
        {
            _owned.Add(value);
            return value;
        }

        private static void Complete(UniTask task)
        {
            UniTask.Awaiter awaiter = task.GetAwaiter();
            Assert.That(awaiter.IsCompleted, Is.True);
            awaiter.GetResult();
        }

        private sealed class CountingCard : ICardView
        {
            private readonly Transform _transform;
            internal int TransformReads;
            internal Action BeforeTransformRead;

            internal CountingCard(Transform transform)
            {
                _transform = transform;
            }

            public CardData Data { get; private set; }
            public bool IsFaceUp { get; private set; }
            public Transform Transform
            {
                get
                {
                    TransformReads++;
                    Action callback = BeforeTransformRead;
                    BeforeTransformRead = null;
                    callback?.Invoke();
                    return _transform;
                }
            }

            public event Action<ICardView> OnClicked;
            public event Action<ICardView> OnHovered { add { } remove { } }
            public event Action<ICardView> OnUnhovered { add { } remove { } }
            public void SetData(CardData data, bool faceUp = true) { Data = data; IsFaceUp = faceUp; }
            public void Flip() { IsFaceUp = !IsFaceUp; }
            public UniTask FlipAsync(float duration = 0.3f) { Flip(); return UniTask.CompletedTask; }
            public UniTask MoveToAsync(Vector3 position, float duration = 0.2f)
            {
                _transform.position = position;
                return UniTask.CompletedTask;
            }
            public void SetInteractable(bool interactable) { }
            internal void Click() { OnClicked?.Invoke(this); }
        }

        private sealed class DelayedView : IHandView
        {
            private readonly List<ICardView> _cards = new();
            private UniTaskCompletionSource _pending;
            internal bool DelayAdd;
            internal bool DelayRemove;
            public IReadOnlyList<ICardView> CardViews => _cards;
            public int Count => _cards.Count;

            public UniTask AddCardAsync(ICardView cardView, bool animate = true)
            {
                _cards.Add(cardView);
                if (!DelayAdd)
                {
                    return UniTask.CompletedTask;
                }

                DelayAdd = false;
                _pending = new UniTaskCompletionSource();
                return _pending.Task;
            }

            public UniTask RemoveCardAsync(ICardView cardView, bool animate = true)
            {
                _cards.Remove(cardView);
                if (!DelayRemove)
                {
                    return UniTask.CompletedTask;
                }

                DelayRemove = false;
                _pending = new UniTaskCompletionSource();
                return _pending.Task;
            }

            public UniTask ArrangeCardsAsync(bool animate = true) { return UniTask.CompletedTask; }
            public void Clear() { _cards.Clear(); }

            internal void CompletePending()
            {
                UniTaskCompletionSource pending = _pending;
                _pending = null;
                Assert.That(pending, Is.Not.Null);
                pending.TrySetResult();
            }
        }

        private sealed class LegacyView : IHandView
        {
            private readonly List<ICardView> _cards = new();
            internal Action AfterRemove;
            internal int AddCalls;
            internal int RemoveCalls;
            internal int ArrangeCalls;
            public IReadOnlyList<ICardView> CardViews => _cards;
            public int Count => _cards.Count;
            public UniTask AddCardAsync(ICardView cardView, bool animate = true)
            {
                AddCalls++;
                _cards.Add(cardView);
                return UniTask.CompletedTask;
            }
            public UniTask RemoveCardAsync(ICardView cardView, bool animate = true)
            {
                RemoveCalls++;
                _cards.Remove(cardView);
                Action callback = AfterRemove;
                AfterRemove = null;
                callback?.Invoke();
                return UniTask.CompletedTask;
            }
            public UniTask ArrangeCardsAsync(bool animate = true) { ArrangeCalls++; return UniTask.CompletedTask; }
            public void Clear() { _cards.Clear(); }
            internal void ResetCounters() { AddCalls = 0; RemoveCalls = 0; ArrangeCalls = 0; }
        }
    }
}
