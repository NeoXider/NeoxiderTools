using Neo.Core.Level;
using Neo.Samples.Survivor;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Demo UI for the LevelComponent + LevelCurveDefinition system, built at runtime with uGUI
    ///     through <see cref="NeoDemoShell" /> (no prefabs, no IMGUI). Shows level, XP, XP-to-next,
    ///     an XP progress bar, and provides buttons for adding XP and resetting progress.
    /// </summary>
    public class ProgressionDemoUI : MonoBehaviour
    {
        private static readonly Color Accent = new Color(0.26f, 0.85f, 0.64f);
        private static readonly Color LevelYellow = new Color(1f, 0.84f, 0.35f);

        [Header("References")] [SerializeField]
        private LevelComponent _levelComponent;

        [Header("Settings")] [SerializeField] private int _xpPerClick = 25;

        private NeoDemoShell.Context _shell;
        private TMP_Text _levelBig;
        private TMP_Text _totalXpValue;
        private TMP_Text _toNextValue;
        private TMP_Text _currentLevelXpValue;
        private Image _xpFill;
        private GameObject _currentLevelXpRow;
        private GameObject _xpBarRow;

        private void Start()
        {
            if (_levelComponent == null)
            {
                _levelComponent = FindFirstObjectByType<LevelComponent>();
            }

            // WHY: the shell header is single-line; a long title wraps onto the overline text.
            _shell = NeoDemoShell.Build("Neo.Level Progression", Accent);
            NeoDemoShell.ShowInfoCardOnce(
                "Progression (Level System)",
                "LevelComponent turns XP into levels via a LevelCurveDefinition curve.",
                "XP buttons call LevelComponent.AddXp(amount)",
                "OnLevelUp / OnXpGained events drive the readout and log",
                "Reset Progress calls LevelComponent.Reset()");

            if (_levelComponent == null)
            {
                TMP_Text status = _shell.AddValueLabel("Status");
                status.text = "No LevelComponent found in scene!";
                status.color = SurvivorUI.Danger;
                Log("<color=red>No LevelComponent found in scene!</color>");
                return;
            }

            _levelComponent.OnLevelUp.AddListener(OnLevelUp);
            _levelComponent.OnXpGained.AddListener(OnXpGained);

            BuildStatsRows();
            BuildButtonRows();
            RefreshStats();

            Log("Progression Demo started. Use buttons below to add XP and level up!");
        }

        private void OnDestroy()
        {
            if (_levelComponent != null)
            {
                _levelComponent.OnLevelUp.RemoveListener(OnLevelUp);
                _levelComponent.OnXpGained.RemoveListener(OnXpGained);
            }
        }

        private void BuildStatsRows()
        {
            _levelBig = _shell.AddBigLabel("Level 1");
            _levelBig.color = LevelYellow;

            _totalXpValue = _shell.AddValueLabel("Total XP");
            _toNextValue = _shell.AddValueLabel("XP To Next Level");

            _currentLevelXpValue = _shell.AddValueLabel("Current Level XP");
            _currentLevelXpRow = _currentLevelXpValue.transform.parent.gameObject;

            _xpFill = _shell.AddBar("XP Progress", Accent);
            // WHY: AddBar nests fill → track → row; the row is what layout shows/hides.
            _xpBarRow = _xpFill.transform.parent.parent.gameObject;
        }

        private void BuildButtonRows()
        {
            _shell.AddButtonRow(($"+ {_xpPerClick} XP", AddSmallXp));
            _shell.AddButtonRow(($"+ {_xpPerClick * 5} XP (Medium)", AddMediumXp));
            _shell.AddButtonRow(($"+ {_xpPerClick * 20} XP (Large)", AddLargeXp));

            Button[] reset = _shell.AddButtonRow(("Reset Progress", ResetLevel));
            ((Image)reset[0].targetGraphic).color = SurvivorUI.Danger;
        }

        private void AddSmallXp()
        {
            if (_levelComponent == null)
            {
                return;
            }

            _levelComponent.AddXp(_xpPerClick);
            Log($"+{_xpPerClick} XP added.");
        }

        private void AddMediumXp()
        {
            if (_levelComponent == null)
            {
                return;
            }

            int amount = _xpPerClick * 5;
            _levelComponent.AddXp(amount);
            Log($"+{amount} XP added.");
        }

        private void AddLargeXp()
        {
            if (_levelComponent == null)
            {
                return;
            }

            int amount = _xpPerClick * 20;
            _levelComponent.AddXp(amount);
            Log($"+{amount} XP added.");
        }

        private void ResetLevel()
        {
            if (_levelComponent == null)
            {
                return;
            }

            _levelComponent.Reset();
            Log("<color=red>Progress reset to Start values.</color>");
            RefreshStats();
        }

        private void OnLevelUp(int newLevel)
        {
            Log($"<color=yellow>LEVEL UP! You are now Level {newLevel}</color>");
            RefreshStats();
        }

        private void OnXpGained()
        {
            RefreshStats();
        }

        private void RefreshStats()
        {
            if (_levelComponent == null || _shell == null)
            {
                return;
            }

            _levelBig.text = "Level " + _levelComponent.Level;
            _totalXpValue.text = _levelComponent.TotalXp.ToString();
            _toNextValue.text = _levelComponent.XpToNextLevel.ToString();

            bool hasCurve = _levelComponent.LevelCurveDefinition != null;
            _currentLevelXpRow.SetActive(hasCurve);
            _xpBarRow.SetActive(hasCurve);
            if (hasCurve)
            {
                XpProgressSnapshot progressSnapshot = GetXpProgressSnapshot(_levelComponent);
                _currentLevelXpValue.text =
                    $"{progressSnapshot.CurrentLevelXp} / {progressSnapshot.RequiredForCurrentLevel}";
                SurvivorUI.SetFill(_xpFill, progressSnapshot.Progress01);
            }
        }

        private void Log(string msg)
        {
            _shell?.Log($"[{Time.time:F1}] {msg}");
        }

        private static XpProgressSnapshot GetXpProgressSnapshot(LevelComponent levelComponent)
        {
            LevelCurveDefinition curve = levelComponent.LevelCurveDefinition;
            int currentLevelRequired = curve.GetRequiredXpForLevel(levelComponent.Level);
            int nextLevelRequired = curve.GetRequiredXpForLevel(levelComponent.Level + 1);
            int currentLevelXp = Mathf.Max(0, levelComponent.TotalXp - currentLevelRequired);
            int requiredForCurrentLevel = nextLevelRequired > currentLevelRequired
                ? nextLevelRequired - currentLevelRequired
                : currentLevelXp + Mathf.Max(0, levelComponent.XpToNextLevel);

            if (requiredForCurrentLevel <= 0)
            {
                return new XpProgressSnapshot(1f, currentLevelXp, 0);
            }

            int clampedCurrentLevelXp = Mathf.Clamp(currentLevelXp, 0, requiredForCurrentLevel);
            float progress = Mathf.Clamp01(clampedCurrentLevelXp / (float)requiredForCurrentLevel);
            return new XpProgressSnapshot(progress, clampedCurrentLevelXp, requiredForCurrentLevel);
        }

        private readonly struct XpProgressSnapshot
        {
            public XpProgressSnapshot(float progress01, int currentLevelXp, int requiredForCurrentLevel)
            {
                Progress01 = progress01;
                CurrentLevelXp = currentLevelXp;
                RequiredForCurrentLevel = requiredForCurrentLevel;
            }

            public float Progress01 { get; }
            public int CurrentLevelXp { get; }
            public int RequiredForCurrentLevel { get; }
        }
    }
}
