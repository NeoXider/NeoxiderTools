using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

namespace Neo.Cards
{
    /// <summary>
    ///     Hand component: layouts (fan, line, etc.), add/remove. Inspector setup; events via UnityEvent.
    /// </summary>
    [CreateFromMenu("Neoxider/Cards/HandComponent")]
    [AddComponentMenu("Neoxider/Cards/" + nameof(HandComponent))]
    [NeoDoc("Cards/HandComponent.md")]
    public class HandComponent : MonoBehaviour
    {
        [Header("Layout")] [SerializeField] private CardLayoutType _layoutType = CardLayoutType.Fan;

        [SerializeField] private float _spacing = 60f;
        [SerializeField] private float _arcAngle = 30f;
        [SerializeField] private float _arcRadius = 400f;

        [Header("Grid Settings")] [SerializeField]
        private int _gridColumns = 5;

        [SerializeField] private float _gridRowSpacing = 80f;

        [Header("Limits")] [SerializeField] private int _maxCards = 36;

        [Header("Card Order")] [Tooltip("If true — new cards are added at bottom (sibling index 0)")] [SerializeField]
        private bool _addToBottom;

        [Header("Animation")] [SerializeField] private float _arrangeDuration = 0.3f;

        [SerializeField] private Ease _arrangeEase = Ease.OutQuad;

        [SerializeField] private UnityEvent<int> _onCardCountChanged;

        [SerializeField] private UnityEvent<CardComponent> _onCardAdded;
        [SerializeField] private UnityEvent<CardComponent> _onCardRemoved;
        [SerializeField] private UnityEvent<CardComponent> _onCardClicked;
        [SerializeField] private UnityEvent _onHandChanged;

        private readonly List<CardComponent> _cards = new();

        /// <summary>
        ///     Invoked when the card count changes; carries the new count.
        /// </summary>
        public UnityEvent<int> OnCardCountChanged => _onCardCountChanged;

        /// <summary>
        ///     Invoked when a card is added.
        /// </summary>
        public UnityEvent<CardComponent> OnCardAdded => _onCardAdded;

        /// <summary>
        ///     Invoked when a card is removed.
        /// </summary>
        public UnityEvent<CardComponent> OnCardRemoved => _onCardRemoved;

        /// <summary>
        ///     Invoked when a card is clicked.
        /// </summary>
        public UnityEvent<CardComponent> OnCardClicked => _onCardClicked;

        /// <summary>
        ///     Invoked on any hand mutation.
        /// </summary>
        public UnityEvent OnHandChanged => _onHandChanged;

        /// <summary>
        ///     Backing hand model.
        /// </summary>
        public HandModel Model { get; private set; }

        /// <summary>
        ///     Cards currently parented to this hand.
        /// </summary>
        public IReadOnlyList<CardComponent> Cards => _cards;

        /// <summary>
        ///     Card count.
        /// </summary>
        public int Count => _cards.Count;

        /// <summary>
        ///     Whether the hand has no cards.
        /// </summary>
        public bool IsEmpty => _cards.Count == 0;

        /// <summary>
        ///     Whether the hand reached <c>_maxCards</c>.
        /// </summary>
        public bool IsFull => _cards.Count >= _maxCards;

        /// <summary>
        ///     Active layout type.
        /// </summary>
        public CardLayoutType LayoutType
        {
            get => _layoutType;
            set
            {
                _layoutType = value;
                ArrangeCardsAsync().Forget();
            }
        }

        /// <summary>
        ///     Legacy layout enum for old scenes. Prefer <see cref="LayoutType" />.
        /// </summary>
        [Obsolete("Use LayoutType with CardLayoutType.")]
        public HandLayoutType LegacyLayoutType
        {
            get => (HandLayoutType)(int)_layoutType;
            set
            {
                _layoutType = (CardLayoutType)(int)value;
                ArrangeCardsAsync().Forget();
            }
        }

        private void Awake()
        {
            EnsureModelInitialized();
        }

        private void EnsureModelInitialized()
        {
            if (Model == null)
            {
                Model = new HandModel();
            }
        }

        /// <summary>
        ///     Adds a card and optionally animates layout.
        /// </summary>
        /// <param name="card">Card to add.</param>
        /// <param name="animate">Tween to slot.</param>
        public async UniTask AddCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || IsFull)
            {
                return;
            }

            EnsureModelInitialized();

            Vector3 startPosition = card.transform.position;

            card.transform.SetParent(transform, true);
            card.OnClick.AddListener(() => HandleCardClick(card));

            if (_addToBottom)
            {
                _cards.Insert(0, card);
                Model.Add(card.Data);
            }
            else
            {
                _cards.Add(card);
                Model.Add(card.Data);
            }

            if (animate)
            {
                card.transform.position = startPosition;

                List<Vector3> positions = CalculatePositions();
                List<Quaternion> rotations = CalculateRotations();
                int targetIndex = _addToBottom ? 0 : _cards.Count - 1;

                for (int i = 0; i < _cards.Count; i++)
                {
                    _cards[i].transform.SetSiblingIndex(_addToBottom ? _cards.Count - 1 - i : i);
                }

                if (targetIndex < positions.Count && targetIndex < rotations.Count)
                {
                    await AnimateCard(card, positions[targetIndex], rotations[targetIndex]);
                }

                for (int i = 0; i < _cards.Count; i++)
                {
                    if (i != targetIndex && i < positions.Count && i < rotations.Count)
                    {
                        _cards[i].transform.localPosition = positions[i];
                        _cards[i].transform.localRotation = rotations[i];
                        _cards[i].UpdateOriginalTransform();
                    }
                }

                card.UpdateOriginalTransform();
            }
            else
            {
                await ArrangeCardsAsync(false);
            }

            _onCardAdded?.Invoke(card);
            _onCardCountChanged?.Invoke(_cards.Count);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Adds a card without waiting (fires async with animate=false).
        /// </summary>
        /// <param name="card">Card.</param>
        public void AddCard(CardComponent card)
        {
            AddCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Draws the first card (War-style).
        /// </summary>
        /// <returns>First card, or null.</returns>
        public CardComponent DrawFirst()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            CardComponent card = _cards[0];
            RemoveCard(card);
            return card;
        }

        /// <summary>
        ///     Draws a random card.
        /// </summary>
        /// <returns>Random card, or null.</returns>
        public CardComponent DrawRandom()
        {
            if (_cards.Count == 0)
            {
                return null;
            }

            int index = Random.Range(0, _cards.Count);
            CardComponent card = _cards[index];
            RemoveCard(card);
            return card;
        }

        /// <summary>
        ///     Removes a card from the hand.
        /// </summary>
        /// <param name="card">Card to remove.</param>
        /// <param name="animate">Re-layout with tween.</param>
        public async UniTask RemoveCardAsync(CardComponent card, bool animate = true)
        {
            if (card == null || !_cards.Contains(card))
            {
                return;
            }

            EnsureModelInitialized();

            card.OnClick.RemoveAllListeners();
            card.ResetHover();
            card.transform.SetParent(null, true);

            _cards.Remove(card);
            Model.Remove(card.Data);

            await ArrangeCardsAsync(animate);

            _onCardRemoved?.Invoke(card);
            _onCardCountChanged?.Invoke(_cards.Count);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Removes without awaiting (animate=false).
        /// </summary>
        /// <param name="card">Card.</param>
        public void RemoveCard(CardComponent card)
        {
            RemoveCardAsync(card, false).Forget();
        }

        /// <summary>
        ///     Removes the card at <paramref name="index" />.
        /// </summary>
        /// <param name="index">Index.</param>
        /// <param name="animate">Re-layout with tween.</param>
        /// <returns>Removed card, or null.</returns>
        public async UniTask<CardComponent> RemoveAtAsync(int index, bool animate = true)
        {
            if (index < 0 || index >= _cards.Count)
            {
                return null;
            }

            CardComponent card = _cards[index];
            await RemoveCardAsync(card, animate);
            return card;
        }

        /// <summary>
        ///     Sorts by rank (inspector button).
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        [Button]
        public void SortByRank(bool ascending = true)
        {
            SortByRankAsync(ascending).Forget();
        }

        /// <summary>
        ///     Sorts by rank and re-layouts.
        /// </summary>
        public async UniTask SortByRankAsync(bool ascending = true, bool animate = true)
        {
            EnsureModelInitialized();
            Model.SortByRank(ascending);

            if (ascending)
            {
                _cards.Sort((a, b) => a.Data.Rank.CompareTo(b.Data.Rank));
            }
            else
            {
                _cards.Sort((a, b) => b.Data.Rank.CompareTo(a.Data.Rank));
            }

            await ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Sorts by suit (inspector button).
        /// </summary>
        /// <param name="ascending">Ascending if true.</param>
        [Button]
        public void SortBySuit(bool ascending = true)
        {
            SortBySuitAsync(ascending).Forget();
        }

        /// <summary>
        ///     Sorts by suit, then rank, with layout tween.
        /// </summary>
        public async UniTask SortBySuitAsync(bool ascending = true, bool animate = true)
        {
            EnsureModelInitialized();
            Model.SortBySuit(ascending);

            if (ascending)
            {
                _cards.Sort((a, b) =>
                {
                    int suitCompare = a.Data.Suit.CompareTo(b.Data.Suit);
                    return suitCompare != 0 ? suitCompare : a.Data.Rank.CompareTo(b.Data.Rank);
                });
            }
            else
            {
                _cards.Sort((a, b) =>
                {
                    int suitCompare = b.Data.Suit.CompareTo(a.Data.Suit);
                    return suitCompare != 0 ? suitCompare : b.Data.Rank.CompareTo(a.Data.Rank);
                });
            }

            await ArrangeCardsAsync(animate);
        }

        /// <summary>
        ///     Cards that can beat <paramref name="attackCard" /> under Durak rules.
        /// </summary>
        /// <param name="attackCard">Attacking card.</param>
        /// <param name="trump">Trump suit.</param>
        /// <returns>Matching components.</returns>
        public List<CardComponent> GetCardsThatCanBeat(CardData attackCard, Suit? trump)
        {
            List<CardComponent> result = new();

            foreach (CardComponent card in _cards)
            {
                if (card.Data.CanCover(attackCard, trump))
                {
                    result.Add(card);
                }
            }

            return result;
        }

        /// <summary>
        ///     Destroys all cards and clears the model.
        /// </summary>
        [Button]
        public void Clear()
        {
            foreach (CardComponent card in _cards)
            {
                if (card != null)
                {
                    card.OnClick.RemoveAllListeners();
                    Destroy(card.gameObject);
                }
            }

            _cards.Clear();

            EnsureModelInitialized();
            Model.Clear();

            _onCardCountChanged?.Invoke(0);
            _onHandChanged?.Invoke();
        }

        /// <summary>
        ///     Recomputes layout positions for every card.
        /// </summary>
        /// <param name="animate">Tween to new slots.</param>
        public async UniTask ArrangeCardsAsync(bool animate = true)
        {
            if (_cards.Count == 0)
            {
                return;
            }

            List<Vector3> positions = CalculatePositions();
            List<Quaternion> rotations = CalculateRotations();

            List<UniTask> tasks = new();

            for (int i = 0; i < _cards.Count; i++)
            {
                _cards[i].transform.SetSiblingIndex(i);

                if (animate)
                {
                    tasks.Add(AnimateCard(_cards[i], positions[i], rotations[i]));
                }
                else
                {
                    _cards[i].transform.localPosition = positions[i];
                    _cards[i].transform.localRotation = rotations[i];
                    _cards[i].UpdateOriginalTransform();
                }
            }

            if (animate && tasks.Count > 0)
            {
                await UniTask.WhenAll(tasks);
            }

            foreach (CardComponent card in _cards)
            {
                card.UpdateOriginalTransform();
            }
        }

        private void HandleCardClick(CardComponent card)
        {
            _onCardClicked?.Invoke(card);
        }

        private List<Vector3> CalculatePositions()
        {
            return CardLayoutCalculator.CalculatePositions(_layoutType, _cards.Count, BuildLayoutSettings());
        }

        private List<Quaternion> CalculateRotations()
        {
            return CardLayoutCalculator.CalculateRotations(_layoutType, _cards.Count, BuildLayoutSettings());
        }

        private CardLayoutSettings BuildLayoutSettings()
        {
            CardLayoutSettings settings = CardLayoutSettings.Default;
            settings.Spacing = _spacing;
            settings.ArcAngle = _arcAngle;
            settings.ArcRadius = _arcRadius;
            settings.GridColumns = _gridColumns;
            settings.GridRowSpacing = _gridRowSpacing;
            settings.StackStep = 2f;
            return settings;
        }

        [Button("Arrange")]
        private void ArrangeByButton()
        {
            ArrangeCardsAsync().Forget();
        }

        private async UniTask AnimateCard(CardComponent card, Vector3 position, Quaternion rotation)
        {
            TweenerCore<Vector3, Vector3, VectorOptions> moveTween =
                card.transform.DOLocalMove(position, _arrangeDuration).SetEase(_arrangeEase);
            TweenerCore<Quaternion, Quaternion, NoOptions> rotateTween =
                card.transform.DOLocalRotateQuaternion(rotation, _arrangeDuration).SetEase(_arrangeEase);

            await UniTask.WaitUntil(() => !moveTween.IsActive() && !rotateTween.IsActive());
        }
    }
}
