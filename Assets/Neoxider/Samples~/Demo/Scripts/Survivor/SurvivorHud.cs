using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     Builds and drives the survivor HUD in code (no imported UI prefabs): XP/level bar, timer,
    ///     kills, health bar, ability cooldown pips, a fading tutorial toast, the level-up upgrade
    ///     picker and the game-over screen. Presentation only — <see cref="SurvivorGame" /> owns state.
    /// </summary>
    public sealed class SurvivorHud
    {
        private readonly List<Image> _pipFills = new List<Image>();
        private readonly List<TMP_Text> _pipLabels = new List<TMP_Text>();

        private Image _xpFill;
        private Image _hpFill;
        private TMP_Text _levelText;
        private TMP_Text _timerText;
        private TMP_Text _killsText;
        private TMP_Text _hpText;
        private RectTransform _pipRow;

        private CanvasGroup _tutorial;
        private float _tutorialTimer;

        private GameObject _upgradeOverlay;
        private RectTransform _cardRow;
        private GameObject _gameOverOverlay;
        private TMP_Text _gameOverText;
        private Button _restartButton;

        public void Build(RectTransform root)
        {
            Image xp = SurvivorUI.Bar("XPBar", root, SurvivorUI.Accent, out RectTransform xpTrack);
            SurvivorUI.Anchor(xpTrack, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f), new Vector2(0f, 10f));
            xpTrack.offsetMin = new Vector2(10f, xpTrack.offsetMin.y);
            xpTrack.offsetMax = new Vector2(-10f, xpTrack.offsetMax.y);
            _xpFill = xp;

            _levelText = SurvivorUI.Label("Level", root, "LV 1", 22f, SurvivorUI.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)_levelText.transform, new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(0f, 1f), new Vector2(16f, -20f), new Vector2(160f, 30f));

            _timerText = SurvivorUI.Label("Timer", root, "00:00", 40f, SurvivorUI.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)_timerText.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(220f, 48f));

            _killsText = SurvivorUI.Label("Kills", root, "0 kills", 20f, SurvivorUI.Muted,
                TextAlignmentOptions.Right, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)_killsText.transform, new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(1f, 1f), new Vector2(-16f, -20f), new Vector2(160f, 30f));

            BuildHealth(root);
            BuildPips(root);
            BuildTutorial(root);
            BuildUpgradeOverlay(root);
            BuildGameOverOverlay(root);
        }

        private void BuildHealth(RectTransform root)
        {
            Image hp = SurvivorUI.Bar("HPBar", root, SurvivorUI.Good, out RectTransform hpTrack);
            SurvivorUI.Anchor(hpTrack, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 28f), new Vector2(340f, 26f));
            _hpFill = hp;

            _hpText = SurvivorUI.Label("HPText", hpTrack, "100 / 100", 15f, SurvivorUI.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch((RectTransform)_hpText.transform);
        }

        private void BuildPips(RectTransform root)
        {
            _pipRow = SurvivorUI.Rect("Pips", root);
            SurvivorUI.Anchor(_pipRow, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 20f), new Vector2(400f, 46f));
        }

        /// <summary>Rebuilds the ability cooldown pips from the current ability list.</summary>
        public void SetAbilities(IReadOnlyList<string> labels)
        {
            for (int i = _pipRow.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_pipRow.GetChild(i).gameObject);
            }

            _pipFills.Clear();
            _pipLabels.Clear();

            for (int i = 0; i < labels.Count; i++)
            {
                Image cell = SurvivorUI.Image("Pip", _pipRow, SurvivorUI.Panel);
                var rt = (RectTransform)cell.transform;
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(44f, 0f);
                rt.anchoredPosition = new Vector2(i * 50f, 0f);

                Image fill = SurvivorUI.Image("Ready", cell.transform, new Color(0.49f, 0.36f, 0.94f, 0.55f));
                RectTransform frt = (RectTransform)fill.transform;
                frt.anchorMin = Vector2.zero;
                frt.anchorMax = Vector2.one;
                frt.offsetMin = new Vector2(2f, 2f);
                frt.offsetMax = new Vector2(-2f, -2f);

                TMP_Text lbl = SurvivorUI.Label("L", cell.transform, labels[i], 15f, SurvivorUI.Text,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                SurvivorUI.Stretch((RectTransform)lbl.transform);

                _pipFills.Add(fill);
                _pipLabels.Add(lbl);
            }
        }

        /// <summary>Sets one pip's cooldown state (0 = ready/full glow, 1 = just cast/empty).</summary>
        public void SetPip(int index, float cooldownNormalized)
        {
            if (index < 0 || index >= _pipFills.Count)
            {
                return;
            }

            var rt = (RectTransform)_pipFills[index].transform;
            float ready = 1f - Mathf.Clamp01(cooldownNormalized);
            rt.anchorMax = new Vector2(1f, Mathf.Lerp(0.04f, 1f, ready));
            _pipFills[index].color = ready >= 0.999f
                ? new Color(0.49f, 0.36f, 0.94f, 0.85f)
                : new Color(0.49f, 0.36f, 0.94f, 0.4f);
        }

        private void BuildTutorial(RectTransform root)
        {
            RectTransform panel = SurvivorUI.Image("Tutorial", root, SurvivorUI.Ink).rectTransform;
            SurvivorUI.Anchor(panel, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -74f), new Vector2(660f, 40f));
            _tutorial = panel.gameObject.AddComponent<CanvasGroup>();

            TMP_Text t = SurvivorUI.Label("Tip", panel, "WASD move   ·   weapons auto-fire   ·   collect XP   ·   level up to choose upgrades",
                17f, SurvivorUI.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch((RectTransform)t.transform, 8f);
            _tutorialTimer = 6f;
        }

        private void BuildUpgradeOverlay(RectTransform root)
        {
            _upgradeOverlay = SurvivorUI.Image("UpgradeOverlay", root, new Color(0.04f, 0.04f, 0.07f, 0.82f), false).gameObject;
            SurvivorUI.Stretch((RectTransform)_upgradeOverlay.transform);

            TMP_Text title = SurvivorUI.Label("Title", _upgradeOverlay.transform, "LEVEL UP", 44f,
                SurvivorUI.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -120f), new Vector2(600f, 56f));

            TMP_Text sub = SurvivorUI.Label("Sub", _upgradeOverlay.transform, "Choose one upgrade", 22f,
                SurvivorUI.Muted, TextAlignmentOptions.Center);
            SurvivorUI.Anchor((RectTransform)sub.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -172f), new Vector2(600f, 30f));

            _cardRow = SurvivorUI.Rect("Cards", _upgradeOverlay.transform);
            SurvivorUI.Anchor(_cardRow, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(830f, 300f));

            _upgradeOverlay.SetActive(false);
        }

        public void ShowUpgrades(IReadOnlyList<SurvivorUpgrade> options, Action<int> onPick)
        {
            for (int i = _cardRow.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.Destroy(_cardRow.GetChild(i).gameObject);
            }

            const float cardW = 260f;
            const float gap = 25f;
            float totalW = options.Count * cardW + (options.Count - 1) * gap;
            float startX = -totalW * 0.5f;

            for (int i = 0; i < options.Count; i++)
            {
                SurvivorUpgrade up = options[i];
                int index = i;

                Button card = SurvivorUI.Button("Card", _cardRow, SurvivorUI.Panel);
                var rt = (RectTransform)card.transform;
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0f, 0.5f);
                rt.sizeDelta = new Vector2(cardW, 300f);
                rt.anchoredPosition = new Vector2(startX + i * (cardW + gap), 0f);
                card.onClick.AddListener(() => onPick(index));

                Image strip = SurvivorUI.Image("Strip", card.transform, up.Accent);
                var srt = (RectTransform)strip.transform;
                srt.anchorMin = new Vector2(0f, 1f);
                srt.anchorMax = new Vector2(1f, 1f);
                srt.pivot = new Vector2(0.5f, 1f);
                srt.sizeDelta = new Vector2(-24f, 8f);
                srt.anchoredPosition = new Vector2(0f, -16f);

                Image dot = SurvivorUI.Image("Dot", card.transform, up.Accent, false);
                dot.sprite = SurvivorArt.Disc;
                var drt = (RectTransform)dot.transform;
                drt.anchorMin = new Vector2(0.5f, 1f);
                drt.anchorMax = new Vector2(0.5f, 1f);
                drt.pivot = new Vector2(0.5f, 1f);
                drt.sizeDelta = new Vector2(54f, 54f);
                drt.anchoredPosition = new Vector2(0f, -40f);

                TMP_Text ttl = SurvivorUI.Label("Title", card.transform, up.Title, 24f, SurvivorUI.Text,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                var trt = (RectTransform)ttl.transform;
                trt.anchorMin = new Vector2(0f, 1f);
                trt.anchorMax = new Vector2(1f, 1f);
                trt.pivot = new Vector2(0.5f, 1f);
                trt.sizeDelta = new Vector2(-24f, 34f);
                trt.anchoredPosition = new Vector2(0f, -108f);

                TMP_Text desc = SurvivorUI.Label("Desc", card.transform, up.Description, 17f, SurvivorUI.Muted,
                    TextAlignmentOptions.Top);
                var xrt = (RectTransform)desc.transform;
                xrt.anchorMin = new Vector2(0f, 0f);
                xrt.anchorMax = new Vector2(1f, 1f);
                xrt.offsetMin = new Vector2(18f, 18f);
                xrt.offsetMax = new Vector2(-18f, -150f);
            }

            _upgradeOverlay.SetActive(true);
        }

        public void HideUpgrades()
        {
            _upgradeOverlay.SetActive(false);
        }

        private void BuildGameOverOverlay(RectTransform root)
        {
            _gameOverOverlay = SurvivorUI.Image("GameOverOverlay", root, new Color(0.04f, 0.04f, 0.07f, 0.9f), false).gameObject;
            SurvivorUI.Stretch((RectTransform)_gameOverOverlay.transform);

            TMP_Text title = SurvivorUI.Label("Title", _gameOverOverlay.transform, "YOU FELL", 52f,
                SurvivorUI.Danger, TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Anchor((RectTransform)title.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(640f, 60f));

            _gameOverText = SurvivorUI.Label("Stats", _gameOverOverlay.transform, "", 26f, SurvivorUI.Text,
                TextAlignmentOptions.Center);
            SurvivorUI.Anchor((RectTransform)_gameOverText.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 90f));

            _restartButton = SurvivorUI.Button("Restart", _gameOverOverlay.transform, SurvivorUI.Accent);
            var brt = (RectTransform)_restartButton.transform;
            brt.anchorMin = new Vector2(0.5f, 0.5f);
            brt.anchorMax = new Vector2(0.5f, 0.5f);
            brt.pivot = new Vector2(0.5f, 0.5f);
            brt.sizeDelta = new Vector2(240f, 60f);
            brt.anchoredPosition = new Vector2(0f, -90f);

            TMP_Text btnLabel = SurvivorUI.Label("Label", _restartButton.transform, "PLAY AGAIN", 24f,
                Color.white, TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch((RectTransform)btnLabel.transform);

            _gameOverOverlay.SetActive(false);
        }

        public void ShowGameOver(string stats, Action onRestart)
        {
            _gameOverText.text = stats;
            _restartButton.onClick.RemoveAllListeners();
            _restartButton.onClick.AddListener(() => onRestart());
            _gameOverOverlay.SetActive(true);
        }

        public void HideGameOver()
        {
            _gameOverOverlay.SetActive(false);
        }

        public void SetTimer(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            _timerText.text = $"{s / 60:00}:{s % 60:00}";
        }

        public void SetKills(int kills)
        {
            _killsText.text = kills + " kills";
        }

        public void SetLevel(int level)
        {
            _levelText.text = "LV " + level;
        }

        public void SetXp(float current, float max)
        {
            SurvivorUI.SetFill(_xpFill, max > 0f ? current / max : 0f);
        }

        public void SetHealth(float current, float max)
        {
            float n = max > 0f ? current / max : 0f;
            SurvivorUI.SetFill(_hpFill, n);
            _hpFill.color = Color.Lerp(SurvivorUI.Danger, SurvivorUI.Good, n);
            _hpText.text = $"{Mathf.CeilToInt(Mathf.Max(0f, current))} / {Mathf.CeilToInt(max)}";
        }

        public void Tick(float unscaledDelta)
        {
            if (_tutorial == null || _tutorialTimer <= 0f)
            {
                return;
            }

            _tutorialTimer -= unscaledDelta;
            if (_tutorialTimer <= 1.2f)
            {
                _tutorial.alpha = Mathf.Clamp01(_tutorialTimer / 1.2f);
            }

            if (_tutorialTimer <= 0f)
            {
                _tutorial.gameObject.SetActive(false);
            }
        }
    }
}
