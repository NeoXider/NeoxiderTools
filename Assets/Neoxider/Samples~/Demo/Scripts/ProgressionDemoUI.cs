using System.Collections.Generic;
using System.Text;
using Neo.Core.Level;
using Neo.Progression;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored <b>Neo.Progression</b> demo scene (Progression_Demo.unity). The whole UI, the
    ///     ProgressionNoCodeAction objects, the NoCodeBindText readouts and every persistent listener are saved in
    ///     the scene by its DemoSceneKit builder (ProgressionDemoBuilder); this component builds nothing at runtime.
    ///     XP buttons call <see cref="AddXp" /> (the C# path: <see cref="LevelComponent.AddXp" />), perk/unlock/premium
    ///     buttons call ProgressionNoCodeAction.Execute directly (the NoCode path), and the LevelComponent and
    ///     ProgressionManager UnityEvents call the Handle* methods below to refresh the authored labels and log.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Progression Demo")]
    public class ProgressionDemoUI : MonoBehaviour
    {
        private const string OwnedColor = "#43D9A3";
        private const string AvailableColor = "#EDEFFA";
        private const string LockedColor = "#7C7F92";

        [Header("References")]
        [Tooltip("Core XP/level source. Falls back to the first LevelComponent in the scene when empty.")]
        [SerializeField]
        private LevelComponent _levelComponent;

        [Tooltip("Meta progression (perk points, unlock tree, perk tree, premium). Falls back to the singleton.")]
        [SerializeField]
        private ProgressionManager _progression;

        [Header("Settings")]
        [Tooltip("XP step used by AddSmallXp / AddMediumXp (x5) / AddLargeXp (x20).")]
        [SerializeField]
        private int _xpPerClick = 25;

        [Header("Authored UI")]
        [SerializeField] private TMP_Text _levelLabel;
        [SerializeField] private TMP_Text _currentLevelXpValue;
        [SerializeField] private Image _xpFill;
        [SerializeField] private TMP_Text _premiumValue;
        [SerializeField] private TMP_Text _unlockTreeValue;
        [SerializeField] private TMP_Text _perkTreeValue;
        [SerializeField] private DemoLogFeed _log;

        private readonly StringBuilder _text = new();
        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned (the scene builder ran).</summary>
        public bool IsSceneBuilt => _log != null && _levelLabel != null && _xpFill != null && _unlockTreeValue != null &&
                                    _perkTreeValue != null;

        public LevelComponent LevelComponent => _levelComponent;
        public ProgressionManager Progression => _progression;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            if (_levelComponent == null)
            {
                // WHY: scene search is only the fallback for an unbuilt scene; the builder assigns the reference.
                _levelComponent = FindFirstObjectByType<LevelComponent>();
            }

            if (_progression == null)
            {
                _progression = ProgressionManager.I;
            }

            if (_levelComponent == null)
            {
                Log("<color=#FF5C7A>No LevelComponent in the scene - run Neoxider/Demo/Rebuild Active Demo Scene.</color>");
                return;
            }

            RefreshView();
            Log("Progression demo ready: XP buttons call LevelComponent.AddXp, the right column runs NoCode actions");
        }

        /// <summary>Button target (C# path): adds XP through <see cref="LevelComponent.AddXp" />.</summary>
        public void AddXp(int amount)
        {
            if (_levelComponent == null || amount <= 0)
            {
                return;
            }

            // WHY: logged before the call so the level-up line it may trigger appears above it (newest first).
            Log("LevelComponent.AddXp(" + amount + ")");
            _levelComponent.AddXp(amount);
            RefreshView();
        }

        /// <summary>ProgressionNoCodeAction.OnResultMessage listener: shows the NoCode result in the log.</summary>
        public void LogNoCodeResult(string message)
        {
            Log("NoCode -> " + message);
        }

        /// <summary>Adds <c>XP Per Click</c>.</summary>
        public void AddSmallXp()
        {
            AddXp(_xpPerClick);
        }

        /// <summary>Adds five times <c>XP Per Click</c>.</summary>
        public void AddMediumXp()
        {
            AddXp(_xpPerClick * 5);
        }

        /// <summary>Adds twenty times <c>XP Per Click</c>.</summary>
        public void AddLargeXp()
        {
            AddXp(_xpPerClick * 20);
        }

        /// <summary>LevelComponent.OnLevelUp listener.</summary>
        public void HandleLevelUp(int newLevel)
        {
            Log("<color=#FFD659>LevelComponent.OnLevelUp -> level " + newLevel +
                "</color> · ProgressionManager pays the Reward Track entry");
            RefreshView();
        }

        /// <summary>ProgressionManager.OnNodeUnlocked listener.</summary>
        public void HandleNodeUnlocked(string nodeId)
        {
            Log("ProgressionManager.OnNodeUnlocked -> " + nodeId);
            RefreshView();
        }

        /// <summary>ProgressionManager.OnPerkPurchased listener.</summary>
        public void HandlePerkPurchased(string perkId)
        {
            Log("ProgressionManager.OnPerkPurchased -> " + perkId);
            RefreshView();
        }

        /// <summary>ProgressionManager.OnPremiumActivated listener.</summary>
        public void HandlePremiumActivated()
        {
            Log("ProgressionManager.OnPremiumActivated -> premium rewards of reached levels granted retroactively");
            RefreshView();
        }

        /// <summary>ProgressionManager.OnProfileReset listener.</summary>
        public void HandleProgressionReset()
        {
            Log("<color=#FF5C7A>ProgressionManager.OnProfileReset -> level, XP, perks and unlocks cleared</color>");
            RefreshView();
        }

        /// <summary>Re-reads the level provider and the progression profile into the authored labels.</summary>
        public void RefreshView()
        {
            if (_levelComponent != null)
            {
                if (_levelLabel != null)
                {
                    _levelLabel.text = "Level " + _levelComponent.Level;
                }

                RefreshXpBar();
            }

            if (_progression == null)
            {
                return;
            }

            if (_premiumValue != null)
            {
                _premiumValue.text = _progression.HasPremium ? "<color=#FFD659>ACTIVE</color>" : "off";
            }

            if (_unlockTreeValue != null)
            {
                _unlockTreeValue.text = DescribeNodes();
            }

            if (_perkTreeValue != null)
            {
                _perkTreeValue.text = DescribePerks();
            }
        }

        private void RefreshXpBar()
        {
            Neo.Core.Level.LevelCurveDefinition curve = _levelComponent.LevelCurveDefinition;
            if (curve == null)
            {
                if (_currentLevelXpValue != null)
                {
                    _currentLevelXpValue.text = "no curve";
                }

                return;
            }

            int level = _levelComponent.Level;
            int currentLevelRequired = curve.GetRequiredXpForLevel(level);
            int nextLevelRequired = curve.GetRequiredXpForLevel(level + 1);
            int currentLevelXp = Mathf.Max(0, _levelComponent.TotalXp - currentLevelRequired);
            int requiredForLevel = nextLevelRequired > currentLevelRequired
                ? nextLevelRequired - currentLevelRequired
                : currentLevelXp + Mathf.Max(0, _levelComponent.XpToNextLevel);
            float progress = requiredForLevel > 0 ? Mathf.Clamp01(currentLevelXp / (float)requiredForLevel) : 1f;

            if (_currentLevelXpValue != null)
            {
                _currentLevelXpValue.text = Mathf.Min(currentLevelXp, requiredForLevel) + " / " + requiredForLevel;
            }

            if (_xpFill != null)
            {
                RectTransform fill = _xpFill.rectTransform;
                fill.anchorMax = new Vector2(progress, 1f);
            }
        }

        private string DescribeNodes()
        {
            UnlockTreeDefinition tree = _progression.UnlockTree;
            if (tree == null || tree.Nodes.Count == 0)
            {
                return "no unlock tree assigned";
            }

            _text.Clear();
            IReadOnlyList<UnlockNodeDefinition> nodes = tree.Nodes;
            for (int i = 0; i < nodes.Count; i++)
            {
                UnlockNodeDefinition node = nodes[i];
                if (node == null)
                {
                    continue;
                }

                bool owned = _progression.HasUnlockedNode(node.Id);
                bool available = !owned && _progression.CanUnlockNode(node.Id, out _);
                AppendEntry(node.DisplayName, owned, available, "Lv " + node.RequiredLevel);
            }

            return _text.ToString();
        }

        private string DescribePerks()
        {
            PerkTreeDefinition tree = _progression.PerkTree;
            if (tree == null || tree.Perks.Count == 0)
            {
                return "no perk tree assigned";
            }

            _text.Clear();
            IReadOnlyList<PerkDefinition> perks = tree.Perks;
            for (int i = 0; i < perks.Count; i++)
            {
                PerkDefinition perk = perks[i];
                if (perk == null)
                {
                    continue;
                }

                bool owned = _progression.HasPurchasedPerk(perk.Id);
                bool available = !owned && _progression.CanBuyPerk(perk.Id, out _);
                AppendEntry(perk.DisplayName, owned, available, perk.Cost + " PP");
            }

            return _text.ToString();
        }

        private void AppendEntry(string displayName, bool owned, bool available, string requirement)
        {
            if (_text.Length > 0)
            {
                _text.Append("   ");
            }

            string color = owned ? OwnedColor : available ? AvailableColor : LockedColor;
            _text.Append("<color=").Append(color).Append('>').Append(displayName);
            if (!owned)
            {
                _text.Append(" <size=80%>(").Append(requirement).Append(")</size>");
            }

            _text.Append("</color>");
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private void WarnUnbuilt()
        {
            if (_warnedUnbuilt)
            {
                return;
            }

            _warnedUnbuilt = true;
            global::Neo.Demo.SampleDiagnostics.Warn(
                "[ProgressionDemoUI] The scene UI is not authored. Run Neoxider/Demo/Rebuild Active Demo Scene.", this);
        }
    }
}
