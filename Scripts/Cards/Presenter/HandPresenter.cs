using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Bridges <see cref="HandModel" /> and <see cref="IHandView" />.
    /// </summary>
    public class HandPresenter
    {
        private readonly List<CardPresenter> _cardPresenters = new();
        private int _revision;

        /// <summary>
        ///     Creates a hand presenter.
        /// </summary>
        /// <param name="model">Hand model.</param>
        /// <param name="view">Hand view.</param>
        public HandPresenter(HandModel model, IHandView view)
        {
            Model = model ?? throw new ArgumentNullException(nameof(model));
            View = view ?? throw new ArgumentNullException(nameof(view));
        }

        /// <summary>
        ///     Hand model.
        /// </summary>
        public HandModel Model { get; }

        /// <summary>
        ///     Hand view.
        /// </summary>
        public IHandView View { get; }

        /// <summary>
        ///     Card count.
        /// </summary>
        public int Count => Model.Count;

        /// <summary>
        ///     Whether the hand is empty.
        /// </summary>
        public bool IsEmpty => Model.IsEmpty;

        /// <summary>
        ///     Active card presenters.
        /// </summary>
        public IReadOnlyList<CardPresenter> CardPresenters => _cardPresenters;

        /// <summary>
        ///     Raised when a card is added.
        /// </summary>
        public event Action<CardPresenter> OnCardAdded;

        /// <summary>
        ///     Raised when a card is removed.
        /// </summary>
        public event Action<CardPresenter> OnCardRemoved;

        /// <summary>
        ///     Raised when a card is clicked.
        /// </summary>
        public event Action<CardPresenter> OnCardClicked;

        /// <summary>
        ///     Adds a card to the hand.
        /// </summary>
        /// <param name="cardPresenter">Card presenter.</param>
        /// <param name="animate">Animate layout.</param>
        public async UniTask AddCardAsync(CardPresenter cardPresenter, bool animate = true)
        {
            if (cardPresenter == null || _cardPresenters.Contains(cardPresenter))
            {
                return;
            }

            int revision = ++_revision;
            Model.Add(cardPresenter.Data);
            if (revision != _revision)
            {
                return;
            }

            _cardPresenters.Add(cardPresenter);

            cardPresenter.OnClicked += HandleCardClicked;

            await View.AddCardAsync(cardPresenter.View, animate);

            if (revision == _revision && _cardPresenters.Contains(cardPresenter))
            {
                OnCardAdded?.Invoke(cardPresenter);
            }
        }

        /// <summary>
        ///     Removes a card from the hand.
        /// </summary>
        /// <param name="cardPresenter">Card presenter.</param>
        /// <param name="animate">Animate layout.</param>
        public async UniTask RemoveCardAsync(CardPresenter cardPresenter, bool animate = true)
        {
            if (cardPresenter == null)
            {
                return;
            }

            int index = _cardPresenters.IndexOf(cardPresenter);
            if (index < 0)
            {
                return;
            }

            int revision = ++_revision;
            Model.RemoveAt(index);
            if (revision != _revision)
            {
                return;
            }

            _cardPresenters.RemoveAt(index);

            cardPresenter.OnClicked -= HandleCardClicked;

            await View.RemoveCardAsync(cardPresenter.View, animate);

            if (revision == _revision && !_cardPresenters.Contains(cardPresenter))
            {
                OnCardRemoved?.Invoke(cardPresenter);
            }
        }

        /// <summary>
        ///     Removes the card at the index.
        /// </summary>
        /// <param name="index">Card index.</param>
        /// <param name="animate">Animate layout.</param>
        /// <returns>Removed presenter, or null.</returns>
        public async UniTask<CardPresenter> RemoveAtAsync(int index, bool animate = true)
        {
            if (index < 0 || index >= _cardPresenters.Count)
            {
                return null;
            }

            CardPresenter presenter = _cardPresenters[index];
            await RemoveCardAsync(presenter, animate);
            return presenter;
        }

        /// <summary>
        ///     Finds a presenter matching the card data.
        /// </summary>
        /// <param name="cardData">Card data.</param>
        /// <returns>Presenter or null.</returns>
        public CardPresenter FindCard(CardData cardData)
        {
            for (int i = 0; i < _cardPresenters.Count; i++)
            {
                if (_cardPresenters[i].Data.Equals(cardData))
                {
                    return _cardPresenters[i];
                }
            }

            return null;
        }

        /// <summary>
        ///     Presenters whose cards can beat the attack card.
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching presenters.</returns>
        public List<CardPresenter> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            List<CardPresenter> result = new();
            for (int i = 0; i < _cardPresenters.Count; i++)
            {
                CardPresenter presenter = _cardPresenters[i];
                if (presenter.Data.CanCover(attackCard, trump))
                {
                    result.Add(presenter);
                }
            }

            return result;
        }

        /// <summary>
        ///     Presenters matching any of the ranks.
        /// </summary>
        /// <param name="ranks">Ranks to match.</param>
        /// <returns>Matching presenters.</returns>
        public List<CardPresenter> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            HashSet<Rank> rankSet = new(ranks);
            List<CardPresenter> result = new();
            for (int i = 0; i < _cardPresenters.Count; i++)
            {
                CardPresenter presenter = _cardPresenters[i];
                if (!presenter.Data.IsJoker && rankSet.Contains(presenter.Data.Rank))
                {
                    result.Add(presenter);
                }
            }

            return result;
        }

        /// <summary>
        ///     Sorts by rank. The sort is stable: equal ranks keep their current order.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        /// <param name="animate">Animate rearrange.</param>
        public UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            return SortAsync(ascending ? CardOrdering.RankAscending : CardOrdering.RankDescending, animate);
        }

        /// <summary>
        ///     Sorts by suit, then rank. The sort is stable.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        /// <param name="animate">Animate rearrange.</param>
        public UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            return SortAsync(ascending ? CardOrdering.SuitThenRankAscending : CardOrdering.SuitThenRankDescending,
                animate);
        }

        /// <summary>
        ///     Sorts model, presenters and view with any comparer (for example <see cref="CardComparer" /> or a
        ///     comparer for custom card data). The sort is stable, so model and presenter indices stay aligned.
        /// </summary>
        /// <param name="comparer">Card comparer.</param>
        /// <param name="animate">Animate rearrange.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparer" /> is null.</exception>
        public UniTask SortAsync(IComparer<CardData> comparer, bool animate = true)
        {
            if (comparer == null)
            {
                throw new ArgumentNullException(nameof(comparer));
            }

            return SortAsync(comparer.Compare, animate);
        }

        /// <summary>
        ///     Sorts model, presenters and view with a comparison delegate. The sort is stable.
        /// </summary>
        /// <param name="comparison">Card comparison.</param>
        /// <param name="animate">Animate rearrange.</param>
        /// <exception cref="ArgumentNullException"><paramref name="comparison" /> is null.</exception>
        public UniTask SortAsync(Comparison<CardData> comparison, bool animate = true)
        {
            if (comparison == null)
            {
                throw new ArgumentNullException(nameof(comparison));
            }

            return SortCoreAsync(comparison, animate);
        }

        private async UniTask SortCoreAsync(Comparison<CardData> comparison, bool animate)
        {
            int revision = ++_revision;
            // WHY: presenters are sorted before the model so model observers already see the final order.
            CardOrdering.StableSort(_cardPresenters, (a, b) => comparison(a.Data, b.Data));
            Model.Sort(comparison);
            if (revision != _revision)
            {
                return;
            }

            await SynchronizeViewOrderAsync(animate, revision);
        }

        private async UniTask SynchronizeViewOrderAsync(bool animate, int revision)
        {
            CardPresenter[] snapshot = _cardPresenters.ToArray();
            if (View is IOrderedHandView orderedView)
            {
                ICardView[] order = new ICardView[snapshot.Length];
                for (int i = 0; i < snapshot.Length; i++)
                {
                    order[i] = snapshot[i].View;
                }

                await orderedView.SetOrderAsync(order, animate);
                return;
            }

            // WHY: older custom views keep their existing membership-based synchronization path.
            foreach (CardPresenter presenter in snapshot)
            {
                await View.RemoveCardAsync(presenter.View, false);
                if (revision != _revision)
                {
                    return;
                }
            }

            foreach (CardPresenter presenter in snapshot)
            {
                await View.AddCardAsync(presenter.View, false);
                if (revision != _revision)
                {
                    return;
                }
            }

            await View.ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Clears model, view, and presenters.
        /// </summary>
        public void Clear()
        {
            _revision++;
            foreach (CardPresenter presenter in _cardPresenters)
            {
                presenter.OnClicked -= HandleCardClicked;
            }

            _cardPresenters.Clear();
            Model.Clear();
            View.Clear();
        }

        /// <summary>
        ///     Clears the hand (same as <see cref="Clear" />).
        /// </summary>
        public void Dispose()
        {
            Clear();
        }

        private void HandleCardClicked(CardPresenter presenter)
        {
            OnCardClicked?.Invoke(presenter);
        }
    }
}
