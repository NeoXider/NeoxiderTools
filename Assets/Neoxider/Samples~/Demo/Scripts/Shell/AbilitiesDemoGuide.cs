using Neo.Abilities;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>Instructions and feedback for the existing, scene-authored NoCode ability wiring.</summary>
    public sealed class AbilitiesDemoGuide : MonoBehaviour
    {
        [SerializeField] private Canvas _canvas;
        [SerializeField] private AbilityUnitBehaviour _player;
        [SerializeField] private AbilityUnitBehaviour _dummy;
        [SerializeField] private AbilityCasterBehaviour _caster;
        [SerializeField] private AbilityAutoCaster _autoCaster;
        private GameObject _panel;
        private TMP_Text _status;
        private TMP_Text _feedback;
        private TMP_Text _autoCaption;
        private readonly RectTransform[] _buttonRects = new RectTransform[4];
        private float _panelWidth;
        private bool _sawHeal;
        private bool _sawHit;

        /// <summary>Live normalized cooldown used by the guide's READY indicator.</summary>
        public float ZapCooldown => _caster.GetCooldownNormalized("zap");

        /// <summary>Most recent gameplay result shown to the person running this sample.</summary>
        public string LastResult => _feedback != null ? _feedback.text : string.Empty;

        private void Start()
        {
            _autoCaster.enabled = false;
            Image panel = SurvivorUI.Image("How to play", _canvas.transform, NeoDemoShell.Card);
            _panel = panel.gameObject;
            SurvivorUI.Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -350f), new Vector2(1000f, 300f));
            TMP_Text instructions = SurvivorUI.Label("Goal", panel.transform,
                "1 Hit Player 20 → Cast Heal. Retry during its 3-second cooldown.\n" +
                "2 Zap the dummy. Change level for 12 / 20 / 30 damage; Reset to repeat.",
                22f, NeoDemoShell.Text, TextAlignmentOptions.Left, FontStyles.Normal);
            SurvivorUI.Anchor(instructions.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-32f, 64f));
            _status = SurvivorUI.Label("Live status", panel.transform, "", 24f, NeoDemoShell.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            SurvivorUI.Anchor(_status.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(-32f, 60f));
            _feedback = SurvivorUI.Label("Result", panel.transform, "Start with Hit Player 20 above. Watch HP change.",
                22f, new Color(0.45f, 0.9f, 0.8f), TextAlignmentOptions.Left, FontStyles.Normal);
            SurvivorUI.Anchor(_feedback.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(-32f, 64f));
            AddButton(0, "Zap dummy", Zap);
            _autoCaption = AddButton(1, "Auto Zap: OFF", ToggleAuto);
            AddButton(2, "Next level", NextLevel);
            AddButton(3, "Reset demo", ResetDemo);
            Canvas.ForceUpdateCanvases();
            FitPanelToCanvas();
            _player.OnDamaged.AddListener(PlayerDamaged);
            _player.OnHealed.AddListener(PlayerHealed);
            _dummy.OnDamaged.AddListener(DummyDamaged);
            _caster.OnCastFailed.AddListener(CastFailed);
        }

        private TMP_Text AddButton(int index, string caption, UnityAction action)
        {
            Button button = SurvivorUI.Button(caption, _panel.transform, new Color(0.55f, 0.45f, 0.95f));
            SurvivorUI.Anchor((RectTransform)button.transform, new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 0f), new Vector2(16f + index * 244f, 16f), new Vector2(236f, 48f));
            _buttonRects[index] = (RectTransform)button.transform;
            TMP_Text label = SurvivorUI.Label("Label", button.transform, caption, 22f, NeoDemoShell.Ink,
                TextAlignmentOptions.Center, FontStyles.Bold);
            SurvivorUI.Stretch(label.rectTransform, 4f);
            button.onClick.AddListener(action);
            return label;
        }

        private void Update()
        {
            if (_status == null) return;
            FitPanelToCanvas();
            string heal = _caster.GetCooldownNormalized("heal_pulse") > 0f ? "cooling down" : "READY";
            string zap = _caster.GetCooldownNormalized("zap") > 0f ? "cooling down" : "READY";
            _status.text = "Player " + _player.CurrentHealth.ToString("0") + "/" + _player.MaxHealth.ToString("0") +
                " HP   ·   Dummy " + _dummy.CurrentHealth.ToString("0") + "/" + _dummy.MaxHealth.ToString("0") +
                " HP   ·   Level " + (_player.Unit != null ? _player.Unit.Level : 1) +
                "\nHeal: " + heal + "   ·   Zap: " + zap +
                (!_dummy.IsAlive || !_player.IsAlive ? "   ·   Unit defeated: Reset to repeat" : "");
        }

        private void FitPanelToCanvas()
        {
            if (_canvas == null || _panel == null) return;
            float width = Mathf.Min(1000f, Mathf.Max(1f, ((RectTransform)_canvas.transform).rect.width - 32f));
            if (Mathf.Approximately(width, _panelWidth)) return;
            _panelWidth = width;
            RectTransform panel = (RectTransform)_panel.transform;
            panel.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, width);
            float buttonWidth = Mathf.Max(1f, (width - 56f) / 4f);
            for (int i = 0; i < _buttonRects.Length; i++)
            {
                RectTransform button = _buttonRects[i];
                if (button == null) continue;
                button.anchoredPosition = new Vector2(16f + i * (buttonWidth + 8f), 16f);
                button.sizeDelta = new Vector2(buttonWidth, 48f);
            }
        }

        public void Zap()
        {
            if (!_dummy.IsAlive) { _feedback.text = "Dummy defeated. Reset demo to try another level."; return; }
            _caster.TryCastAtUnit("zap", _dummy);
        }

        public void ToggleAuto()
        {
            _autoCaster.enabled = !_autoCaster.enabled;
            _autoCaption.text = _autoCaster.enabled ? "Auto Zap: ON" : "Auto Zap: OFF";
            _feedback.text = _autoCaster.enabled
                ? "Auto Zap attacks the opposing dummy whenever the ability is ready."
                : "Auto Zap paused. Manual buttons let you inspect one result at a time.";
        }

        public void NextLevel()
        {
            int level = _player.Unit != null ? _player.Unit.Level % 3 + 1 : 1;
            _player.SetLevel(level);
            _feedback.text = "Player level " + level + ". The next Zap uses the same asset's level-scaled damage.";
        }

        public void ResetDemo()
        {
            _autoCaster.enabled = false;
            _caster.enabled = false;
            _player.enabled = false;
            _dummy.enabled = false;
            _player.enabled = true;
            _dummy.enabled = true;
            _caster.enabled = true;
            _player.SetLevel(1);
            _sawHeal = false;
            _sawHit = false;
            _autoCaption.text = "Auto Zap: OFF";
            _feedback.text = "Reset: both units restored, cooldowns cleared, level 1. Hit Player 20, then Cast Heal.";
        }

        private void PlayerDamaged(float amount) => _feedback.text = "Player lost " + amount.ToString("0") + " HP. Now Cast Heal.";
        private void PlayerHealed(float amount)
        {
            _sawHeal = true;
            _feedback.text = "Heal restored " + amount.ToString("0") + " HP. " +
                (_sawHit ? "Completed both healing and damage. Try level scaling or Reset." : "Next, Zap the dummy.");
        }
        private void DummyDamaged(float amount)
        {
            _sawHit = true;
            _feedback.text = "Zap dealt " + amount.ToString("0") + " damage to the dummy. " +
                (_sawHeal ? "Completed both healing and damage. Try another level." : "Try Hit Player 20, then Cast Heal.");
        }
        private void CastFailed(string reason) => _feedback.text = "Cast rejected: " + reason +
            ". Wait for READY; defeated units need Reset.";
        private void OnEnable() { if (_panel != null) _panel.SetActive(true); }
        private void OnDisable() { if (_panel != null) _panel.SetActive(false); }
        private void OnDestroy()
        {
            if (_player != null)
            {
                _player.OnDamaged.RemoveListener(PlayerDamaged);
                _player.OnHealed.RemoveListener(PlayerHealed);
            }
            if (_dummy != null) _dummy.OnDamaged.RemoveListener(DummyDamaged);
            if (_caster != null) _caster.OnCastFailed.RemoveListener(CastFailed);
            if (_panel != null) Destroy(_panel);
        }
    }
}
