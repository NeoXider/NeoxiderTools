using UnityEngine;
using UnityEngine.UI;
using Neo.Rpg.UI;
using Neo.Progression.UI;
using Neo.Progression;
using Neo.Tools;

namespace Neo.Rpg.Demo
{
    /// <summary>
    /// Displays Player HP and Level in a screen-overlay Canvas using proper universal UI components.
    /// Also attaches GameTimeController to pause the game automatically on death.
    /// </summary>
    public class DemoPlayerUI : MonoBehaviour
    {
        private GameObject _canvasObj;
        private GameObject _deathScreen;
        private GameObject _deathBg;
        private Text _damageDisplayText;

        private void Start()
        {
            CreateScreenSpaceCanvas();
        }

        private void CreateScreenSpaceCanvas()
        {
            // --- Root Canvas ---
            _canvasObj = new GameObject("Player_HUD_Canvas");
            _canvasObj.transform.SetParent(transform, false);

            var canvas = _canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;

            var scaler = _canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            _canvasObj.AddComponent<GraphicRaycaster>();

            // --- Pause On Death Setup ---
            if (RpgStatsManager.HasInstance)
            {
                var timeController = gameObject.AddComponent<GameTimeController>();
                RpgStatsManager.Instance.OnDeath.AddListener(() => 
                {
                    timeController.PauseGame();
                    ShowDeathScreen(_canvasObj.transform);
                });

                // Update damage text when level changes
                RpgStatsManager.Instance.LevelState.AddListener(_ => UpdateDamageDisplay());
            }

            // --- Progression/Level Setup ---
            var progObj = new GameObject("ProgressionText", typeof(RectTransform));
            progObj.transform.SetParent(_canvasObj.transform, false);
            var progRt = progObj.GetComponent<RectTransform>();
            progRt.anchorMin = new Vector2(0.5f, 1f);
            progRt.anchorMax = new Vector2(0.5f, 1f);
            progRt.pivot = new Vector2(0.5f, 1f);
            progRt.anchoredPosition = new Vector2(0, -20);
            progRt.sizeDelta = new Vector2(800, 50);

            var levelText = progObj.AddComponent<Text>();
            levelText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (levelText.font == null) levelText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            levelText.fontSize = 28;
            levelText.alignment = TextAnchor.MiddleCenter;
            levelText.color = Color.white;
            levelText.text = "Level: 1";

            var progBarUI = progObj.AddComponent<ProgressionBarUI>();
            progBarUI.levelText = levelText;

            // --- HP Bar Container ---
            var barContainer = new GameObject("HpBarContainer", typeof(RectTransform));
            barContainer.transform.SetParent(_canvasObj.transform, false);
            var bcRt = barContainer.GetComponent<RectTransform>();
            bcRt.anchorMin = new Vector2(0.5f, 0f);
            bcRt.anchorMax = new Vector2(0.5f, 0f);
            bcRt.pivot = new Vector2(0.5f, 0.5f);
            bcRt.anchoredPosition = new Vector2(0, 60);
            bcRt.sizeDelta = new Vector2(400, 30);

            // --- Damage Display ---
            var dmgObj = new GameObject("DamageText", typeof(RectTransform));
            dmgObj.transform.SetParent(_canvasObj.transform, false);
            var dmgRt = dmgObj.GetComponent<RectTransform>();
            dmgRt.anchorMin = new Vector2(0.5f, 0f);
            dmgRt.anchorMax = new Vector2(0.5f, 0f);
            dmgRt.pivot = new Vector2(0.5f, 0f);
            dmgRt.anchoredPosition = new Vector2(0, 100);
            dmgRt.sizeDelta = new Vector2(400, 30);

            _damageDisplayText = dmgObj.AddComponent<Text>();
            _damageDisplayText.font = levelText.font;
            _damageDisplayText.fontSize = 22;
            _damageDisplayText.alignment = TextAnchor.MiddleCenter;
            _damageDisplayText.color = new Color(1f, 0.8f, 0.4f, 1f); // Gold/Orange for stats
            UpdateDamageDisplay();

            // Background
            var bgImg = CreateImage(bcRt, "Background", Vector2.zero, Vector2.one, new Color(0.15f, 0.0f, 0.0f, 0.85f));

            // Slider
            var hpSlider = barContainer.AddComponent<Slider>();
            hpSlider.interactable = false;
            hpSlider.transition = Selectable.Transition.None;
            hpSlider.minValue = 0f;
            hpSlider.maxValue = 1f;
            hpSlider.value = 1f;

            // Fill Area & Fill
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(bcRt, false);
            var faRt = fillArea.GetComponent<RectTransform>();
            faRt.anchorMin = Vector2.zero; faRt.anchorMax = Vector2.one;
            faRt.sizeDelta = Vector2.zero; faRt.anchoredPosition = Vector2.zero;

            var fillRT = CreateImage(faRt, "Fill", Vector2.zero, Vector2.one, new Color(0.1f, 0.85f, 0.2f, 1f));
            hpSlider.fillRect = fillRT;

            // HP Text overriding the background
            var hpTextGo = new GameObject("HpText", typeof(RectTransform));
            hpTextGo.transform.SetParent(bcRt, false);
            var hpTextRt = hpTextGo.GetComponent<RectTransform>();
            hpTextRt.anchorMin = Vector2.zero; hpTextRt.anchorMax = Vector2.one;
            hpTextRt.sizeDelta = Vector2.zero; hpTextRt.anchoredPosition = Vector2.zero;

            var hpText = hpTextGo.AddComponent<Text>();
            hpText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (hpText.font == null) hpText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            hpText.fontSize = 18;
            hpText.alignment = TextAnchor.MiddleCenter;
            hpText.color = Color.white;
            hpText.text = "100% HP";

            // Universal Binding
            if (RpgStatsManager.HasInstance)
            {
                var hpBarUI = barContainer.AddComponent<RpgHpBarUI>();
                hpBarUI.hpSlider = hpSlider;
                hpBarUI.hpText = hpText;
                hpBarUI.Bind(RpgStatsManager.Instance);
            }
        }

        private RectTransform CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = Vector2.zero;
            go.AddComponent<Image>().color = color;
            return rt;
        }

        private void ShowDeathScreen(Transform parentCanvas)
        {
            if (_deathScreen != null) return;

            _deathScreen = new GameObject("DeathMessage", typeof(RectTransform));
            _deathScreen.transform.SetParent(parentCanvas, false);
            var rt = _deathScreen.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.sizeDelta = Vector2.zero;
            rt.anchoredPosition = Vector2.zero;

            var txt = _deathScreen.AddComponent<Text>();
            txt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (txt.font == null) txt.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            txt.fontSize = 120;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.color = new Color(1f, 0.2f, 0.2f, 1f);
            txt.text = "YOU LOSE!\n<size=40>Press R to Restart</size>";

            // Optional background darkness
            _deathBg = new GameObject("DeathBg", typeof(RectTransform));
            _deathBg.transform.SetParent(parentCanvas, false);
            _deathBg.transform.SetSiblingIndex(_deathScreen.transform.GetSiblingIndex()); // Put behind text
            var bgRt = _deathBg.GetComponent<RectTransform>();
            bgRt.anchorMin = Vector2.zero; bgRt.anchorMax = Vector2.one;
            bgRt.sizeDelta = Vector2.zero; bgRt.anchoredPosition = Vector2.zero;
            var img = _deathBg.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.7f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.R))
            {
                // Ensure time is unpaused before reloading to avoid getting stuck in paused state.
                Time.timeScale = 1f;

                if (RpgStatsManager.HasInstance)
                {
                    var manager = RpgStatsManager.Instance;
                    manager.ResetProfile();
                    manager.Heal(manager.MaxHp);
                }

                if (ProgressionManager.HasInstance)
                {
                    ProgressionManager.I.ResetProgression();
                }

                // Destroy death screen objects if they exist (persistent UI fix)
                if (_deathScreen != null) Destroy(_deathScreen);
                if (_deathBg != null) Destroy(_deathBg);

                UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
            }
        }

        private void UpdateDamageDisplay()
        {
            if (_damageDisplayText == null || !RpgStatsManager.HasInstance) return;

            var stats = RpgStatsManager.Instance;
            float multiplier = stats.GetOutgoingDamageMultiplier();
            int bonusPercent = Mathf.RoundToInt((multiplier - 1f) * 100f);

            // Find an active weapon to calculate current final damage
            var weapon = stats.GetComponentInChildren<Neo.Rpg.Components.Weapons.MeleeWeapon>();
            if (weapon != null)
            {
                float finalDamage = weapon.damage * multiplier;
                _damageDisplayText.text = $"Damage: <color=yellow>{finalDamage:F1}</color> (+{bonusPercent}%)";
            }
            else
            {
                _damageDisplayText.text = $"Damage Bonus: <color=yellow>+{bonusPercent}%</color>";
            }
        }
    }
}
