using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;

namespace Neo.Cards
{
    /// <summary>
    ///     Bridges <see cref="HandModel" /> and <see cref="IHandView" />.
    /// </summary>
    public class HandPresenter
    {
        private readonly List<CardPresenter> _cardPresenters = new();

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
            if (cardPresenter == null)
            {
                return;
            }

            Model.Add(cardPresenter.Data);
            _cardPresenters.Add(cardPresenter);

            cardPresenter.OnClicked += HandleCardClicked;

            await View.AddCardAsync(cardPresenter.View, animate);

            OnCardAdded?.Invoke(cardPresenter);
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

            if (!_cardPresenters.Contains(cardPresenter))
            {
                return;
            }

            Model.Remove(cardPresenter.Data);
            _cardPresenters.Remove(cardPresenter);

            cardPresenter.OnClicked -= HandleCardClicked;

            await View.RemoveCardAsync(cardPresenter.View, animate);

            OnCardRemoved?.Invoke(cardPresenter);
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
            return _cardPresenters.FirstOrDefault(p => p.Data.Equals(cardData));
        }

        /// <summary>
        ///     Presenters whose cards can beat the attack card.
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching presenters.</returns>
        public List<CardPresenter> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            return _cardPresenters
                .Where(p => p.Data.CanCover(attackCard, trump))
                .ToList();
        }

        /// <summary>
        ///     Presenters matching any of the ranks.
        /// </summary>
        /// <param name="ranks">Ranks to match.</param>
        /// <returns>Matching presenters.</returns>
        public List<CardPresenter> GetCardsMatchingRanks(IEnumerable<Rank> ranks)
        {
            HashSet<Rank> rankSet = new(ranks);
            return _cardPresenters
                .Where(p => !p.Data.IsJoker && rankSet.Contains(p.Data.Rank))
                .ToList();
        }

        /// <summary>
        ///     Sorts by rank.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        /// <param name="animate">Animate rearrange.</param>
        public async UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            Model.SortByRank(ascending);

            if (ascending)
            {
                _cardPresenters.Sort((a, b) => a.Data.Rank.CompareTo(b.Data.Rank));
            }
            else
            {
                _cardPresenters.Sort((a, b) => b.Data.Rank.CompareTo(a.Data.Rank));
            }

            await View.ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Sorts by suit, then rank.
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        /// <param name="animate">Animate rearrange.</param>
        public async UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            Model.SortBySuit(ascending);

            if (ascending)
            {
                _cardPresenters.Sort((a, b) =>
                {
                    int suitCompare = a.Data.Suit.CompareTo(b.Data.Suit);
                    return suitCompare != 0 ? suitCompare : a.Data.Rank.CompareTo(b.Data.Rank);
                });
            }
            else
            {
                _cardPresenters.Sort((a, b) =>
                {
                    int suitCompare = b.Data.Suit.CompareTo(a.Data.Suit);
                    return suitCompare != 0 ? suitCompare : b.Data.Rank.CompareTo(a.Data.Rank);
                });
            }

            await View.ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Clears model, view, and presenters.
        /// </summary>
        public void Clear()
        {
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
