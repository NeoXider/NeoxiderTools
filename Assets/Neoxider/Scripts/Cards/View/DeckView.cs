using UnityEngine;
using UnityEngine.UI;

namespace Neo.Cards
{
    /// <summary>
    ///     Визуальное представление колоды карт
    /// </summary>
    public class DeckView : MonoBehaviour, IDeckView
    {
        [Header("References")] [SerializeField]
        private Transform _spawnPoint;

        [SerializeField] private Image _deckImage;
        [SerializeField] private SpriteRenderer _deckSprite;
        [SerializeField] private Image _topCardImage;
        [SerializeField] private SpriteRenderer _topCardSprite;

        [Header("Settings")] [SerializeField] private int _visibleCardCount = 1;

        [SerializeField] private Vector3 _cardOffset = new(2f, 2f, 0f);

        [Header("Config")] [SerializeField] private DeckConfig _config;

        private bool _showTopCard;
        private CardData _topCardData;

        /// <summary>
        ///     Конфигурация колоды
        /// </summary>
        public DeckConfig Config
        {
            get => _config;
            set
            {
                _config = value;
                UpdateVisual(_visibleCardCount);
            }
        }

        private void Start()
        {
            UpdateVisual(_visibleCardCount);
        }

        /// <inheritdoc />
        public Transform SpawnPoint => _spawnPoint != null ? _spawnPoint : transform;

        /// <inheritdoc />
        public int VisibleCardCount
        {
            get => _visibleCardCount;
            set => _visibleCardCount = Mathf.Max(0, value);
        }

        /// <inheritdoc />
        public void UpdateVisual(int remainingCount)
        {
            bool hasCards = remainingCount > 0;

            if (_deckImage != null)
            {
                _deckImage.enabled = hasCards;
                if (hasCards && _config != null)
                {
                    _deckImage.sprite = _config.BackSprite;
                }
            }

            if (_deckSprite != null)
            {
                _deckSprite.enabled = hasCards;
                if (hasCards && _config != null)
                {
                    _deckSprite.sprite = _config.BackSprite;
                }
            }

            UpdateTopCardVisual();
        }

        /// <inheritdoc />
        public void ShowTopCard(CardData card)
        {
            _showTopCard = true;
            _topCardData = card;
            UpdateTopCardVisual();
        }

        /// <inheritdoc />
        public void HideTopCard()
        {
            _showTopCard = false;
            UpdateTopCardVisual();
        }

        private void UpdateTopCardVisual()
        {
            Sprite topSprite = null;

            if (_showTopCard && _config != null)
            {
                topSprite = _config.GetSprite(_topCardData);
            }

            if (_topCardImage != null)
            {
                _topCardImage.enabled = _showTopCard && topSprite != null;
                _topCardImage.sprite = topSprite;
            }

            if (_topCardSprite != null)
            {
                _topCardSprite.enabled = _showTopCard && topSprite != null;
                _topCardSprite.sprite = topSprite;
            }
        }
    }
}