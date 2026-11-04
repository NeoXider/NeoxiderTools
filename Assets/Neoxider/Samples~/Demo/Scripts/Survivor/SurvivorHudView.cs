using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     Presentation of the authored survivor HUD: XP bar, level, timer, kills, health bar, ability cooldown pips,
    ///     the fading tutorial toast, the level-up card picker and the game-over screen. Every element is a saved
    ///     scene object (authored by the SurvivorDemoBuilder) referenced here; the upgrade cards and the restart
    ///     button call <see cref="SurvivorGame" /> through persistent UnityEvents. Labels are rewritten only when the
    ///     shown value changes, so a running HUD does not allocate per frame. <see cref="SurvivorGame" /> owns state.
    /// </summary>
    [AddComponentMenu("Neoxider/Survivor Demo/Survivor HUD View")]
    public sealed class SurvivorHudView : MonoBehaviour
    {
        [Header("Top bar")]
        [Tooltip("Fill of the XP bar; its anchorMax.x follows the XP progress.")]
        [SerializeField] private RectTransform _xpFill;

        [SerializeField] private TMP_Text _levelText;
        [SerializeField] private TMP_Text _timerText;
        [SerializeField] private TMP_Text _killsText;

        [Header("Health")]
        [Tooltip("Fill of the health bar; width and color follow the health fraction.")]
        [SerializeField] private Image _hpFill;

        [SerializeField] private TMP_Text _hpText;
        [SerializeField] private Color _healthFull = new(0.26f, 0.85f, 0.64f);
        [SerializeField] private Color _healthEmpty = new(1f, 0.36f, 0.48f);

        [Header("Ability pips (one per granted ability, extra pips stay hidden)")]
        [SerializeField] private GameObject[] _pips = Array.Empty<GameObject>();

        [SerializeField] private Image[] _pipFills = Array.Empty<Image>();
        [SerializeField] private TMP_Text[] _pipLabels = Array.Empty<TMP_Text>();
        [SerializeField] private Color _pipReady = new(0.49f, 0.36f, 0.94f, 0.85f);
        [SerializeField] private Color _pipCooling = new(0.49f, 0.36f, 0.94f, 0.4f);

        [Header("Tutorial toast")]
        [SerializeField] private CanvasGroup _tutorial;

        [SerializeField] [Min(0f)] private float _tutorialSeconds = 6f;
        [SerializeField] [Min(0.01f)] private float _tutorialFadeSeconds = 1.2f;

        [Header("Level-up picker")]
        [SerializeField] private GameObject _upgradeOverlay;

        [Tooltip("Card buttons; each calls SurvivorGame.PickUpgrade(index) through its persistent OnClick.")]
        [SerializeField] private GameObject[] _cards = Array.Empty<GameObject>();

        [SerializeField] private TMP_Text[] _cardTitles = Array.Empty<TMP_Text>();
        [SerializeField] private TMP_Text[] _cardDescriptions = Array.Empty<TMP_Text>();
        [SerializeField] private Image[] _cardStrips = Array.Empty<Image>();
        [SerializeField] private Image[] _cardDots = Array.Empty<Image>();

        [Header("Game over")]
        [SerializeField] private GameObject _gameOverOverlay;

        [SerializeField] private TMP_Text _gameOverStats;

        private float _tutorialTimer;
        private int _shownSeconds = -1;
        private int _shownKills = -1;
        private int _shownLevel = -1;
        private int _shownHealth = int.MinValue;
        private int _shownMaxHealth = int.MinValue;
        private int _visiblePips;

        /// <summary>Number of authored upgrade cards (the picker offers at most this many).</summary>
        public int CardCount => _cards != null ? _cards.Length : 0;

        /// <summary>Pips currently shown (one per granted ability, capped by the authored pip count).</summary>
        public int VisiblePipCount => _visiblePips;

        public bool IsUpgradePickerOpen => _upgradeOverlay != null && _upgradeOverlay.activeSelf;
        public bool IsGameOverShown => _gameOverOverlay != null && _gameOverOverlay.activeSelf;

        /// <summary>True when the builder assigned the references the game loop writes every frame.</summary>
        public bool IsBuilt => _xpFill != null && _hpFill != null && _levelText != null && _timerText != null &&
                               _killsText != null && _upgradeOverlay != null && _gameOverOverlay != null;

        private void Start()
        {
            _tutorialTimer = _tutorialSeconds;
            if (_tutorial != null)
            {
                _tutorial.alpha = 1f;
                _tutorial.gameObject.SetActive(_tutorialSeconds > 0f);
            }
        }

        private void Update()
        {
            if (_tutorial == null || _tutorialTimer <= 0f)
            {
                return;
            }

            _tutorialTimer -= Time.unscaledDeltaTime;
            if (_tutorialTimer <= _tutorialFadeSeconds)
            {
                _tutorial.alpha = Mathf.Clamp01(_tutorialTimer / _tutorialFadeSeconds);
            }

            if (_tutorialTimer <= 0f)
            {
                _tutorial.gameObject.SetActive(false);
            }
        }

        /// <summary>Shows one card per option (extra authored cards are hidden) and opens the picker.</summary>
        public void ShowUpgrades(IReadOnlyList<SurvivorUpgrade> options)
        {
            int count = options != null ? options.Count : 0;
            for (int i = 0; i < CardCount; i++)
            {
                SurvivorUpgrade upgrade = i < count ? options[i] : null;
                GameObject card = _cards[i];
                if (card == null)
                {
                    continue;
                }

                card.SetActive(upgrade != null);
                if (upgrade == null)
                {
                    continue;
                }

                SetText(_cardTitles, i, upgrade.Title);
                SetText(_cardDescriptions, i, upgrade.Description);
                SetColor(_cardStrips, i, upgrade.Accent);
                SetColor(_cardDots, i, upgrade.Accent);
            }

            SetActive(_upgradeOverlay, true);
        }

        public void HideUpgrades()
        {
            SetActive(_upgradeOverlay, false);
        }

        public void ShowGameOver(string stats)
        {
            if (_gameOverStats != null)
            {
                _gameOverStats.text = stats ?? string.Empty;
            }

            SetActive(_gameOverOverlay, true);
        }

        public void HideGameOver()
        {
            SetActive(_gameOverOverlay, false);
        }

        public void SetTimer(float seconds)
        {
            int whole = Mathf.Max(0, Mathf.FloorToInt(seconds));
            if (whole == _shownSeconds || _timerText == null)
            {
                return;
            }

            _shownSeconds = whole;
            _timerText.text = (whole / 60).ToString("00") + ":" + (whole % 60).ToString("00");
        }

        public void SetKills(int kills)
        {
            if (kills == _shownKills || _killsText == null)
            {
                return;
            }

            _shownKills = kills;
            _killsText.text = kills + " kills";
        }

        public void SetLevel(int level)
        {
            if (level == _shownLevel || _levelText == null)
            {
                return;
            }

            _shownLevel = level;
            _levelText.text = "LV " + level;
        }

        public void SetXp(float current, float max)
        {
            SetFill(_xpFill, max > 0f ? current / max : 0f);
        }

        public void SetHealth(float current, float max)
        {
            float fraction = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (_hpFill != null)
            {
                SetFill(_hpFill.rectTransform, fraction);
                _hpFill.color = Color.Lerp(_healthEmpty, _healthFull, fraction);
            }

            int shown = Mathf.CeilToInt(Mathf.Max(0f, current));
            int shownMax = Mathf.CeilToInt(max);
            if (_hpText == null || shown == _shownHealth && shownMax == _shownMaxHealth)
            {
                return;
            }

            _shownHealth = shown;
            _shownMaxHealth = shownMax;
            _hpText.text = shown + " / " + shownMax;
        }

        /// <summary>Shows one pip per ability label (first letters) and hides the rest of the authored pips.</summary>
        public void SetAbilities(IReadOnlyList<string> labels)
        {
            int count = labels != null ? labels.Count : 0;
            int pipCount = _pips != null ? _pips.Length : 0;
            _visiblePips = Mathf.Min(count, pipCount);
            for (int i = 0; i < pipCount; i++)
            {
                bool visible = i < count;
                SetActive(_pips[i], visible);
                if (visible)
                {
                    SetText(_pipLabels, i, labels[i]);
                    SetPip(i, 0f);
                }
            }
        }

        /// <summary>Sets one pip's cooldown state (0 = ready, full glow; 1 = just cast, empty).</summary>
        public void SetPip(int index, float cooldownNormalized)
        {
            if (_pipFills == null || index < 0 || index >= _pipFills.Length || index >= _visiblePips ||
                _pipFills[index] == null)
            {
                return;
            }

            Image fill = _pipFills[index];
            float ready = 1f - Mathf.Clamp01(cooldownNormalized);
            fill.rectTransform.anchorMax = new Vector2(1f, Mathf.Lerp(0.04f, 1f, ready));
            fill.color = ready >= 0.999f ? _pipReady : _pipCooling;
        }

        private static void SetFill(RectTransform fill, float amount)
        {
            if (fill != null)
            {
                fill.anchorMax = new Vector2(Mathf.Clamp01(amount), fill.anchorMax.y);
            }
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetText(TMP_Text[] labels, int index, string text)
        {
            if (labels != null && index < labels.Length && labels[index] != null)
            {
                labels[index].text = text ?? string.Empty;
            }
        }

        private static void SetColor(Image[] images, int index, Color color)
        {
            if (images != null && index < images.Length && images[index] != null)
            {
                images[index].color = color;
            }
        }
    }
}
