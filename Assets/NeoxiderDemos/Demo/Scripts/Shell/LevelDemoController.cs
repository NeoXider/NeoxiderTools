using System.Globalization;
using Neo.Core.Level;
using Neo.Level;
using Neo.Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Controller of the authored level demo scene (LevelFlowDemo.unity). The left
    ///     card drives a <b>Neo.Core.Level</b> <see cref="LevelComponent" /> (linear curve asset, 100 XP per level):
    ///     the level and XP-to-next labels are bound to its reactive states in the scene (NoCode), the NoCode buttons
    ///     call <see cref="LevelNoCodeAction" /> components, and the C# buttons call <see cref="AddXp" /> and
    ///     <see cref="LevelUp" /> here. The right card drives a <b>Neo.Level</b> <see cref="LevelManager" /> with a
    ///     finite and an infinite map, a level-select grid of <see cref="LevelButton" />s and a completion badge from
    ///     <see cref="Map.IsComplete" />. Module events reach this component through persistent listeners authored in
    ///     the scene; it builds no UI at runtime.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Level Demo")]
    public sealed class LevelDemoController : MonoBehaviour
    {
        private static readonly string GoodHex = "#" + ColorUtility.ToHtmlStringRGB(DemoPalette.Good);

        [Header("Neo.Core.Level · XP")]
        [SerializeField] private LevelComponent _level;
        [SerializeField] private TMP_Text _totalXpValue;
        [SerializeField] private Image _xpFill;

        [Header("Neo.Level · maps")]
        [SerializeField] private LevelManager _levelManager;

        [Tooltip("Must match the LevelManager save key; used only by ResetMapProgress.")]
        [SerializeField] private string _mapSaveKey = "LevelManager";

        [SerializeField] private string[] _mapNames = { "World 1", "Endless" };
        [SerializeField] private TMP_Text _mapValue;
        [SerializeField] private TMP_Text _completeValue;

        [Tooltip("Shown instead of the level grid while the selected map is infinite.")]
        [SerializeField] private GameObject _infiniteMapHint;

        [Header("Feed")]
        [SerializeField] private DemoLogFeed _log;

        private bool _warnedUnbuilt;

        /// <summary>True when the authored scene references are assigned.</summary>
        public bool IsSceneBuilt => _level != null && _levelManager != null && _totalXpValue != null && _xpFill != null &&
                                    _mapValue != null && _completeValue != null && _log != null;

        public LevelComponent Level => _level;
        public LevelManager Manager => _levelManager;

        private void Start()
        {
            if (!IsSceneBuilt)
            {
                WarnUnbuilt();
            }

            RefreshXp();
            RefreshMap();
            if (_level != null)
            {
                Log("LevelComponent ready · Level " + _level.Level + " · " + _level.TotalXp + " XP (profile is saved)");
            }
        }

        /// <summary>Button target (C# path): <see cref="LevelComponent.AddXp" />.</summary>
        public void AddXp(int amount)
        {
            if (!HasLevel())
            {
                return;
            }

            Log("LevelComponent.AddXp(" + amount + ")");
            _level.AddXp(amount);
            RefreshXp();
        }

        /// <summary>Button target (C# path): adds exactly the XP missing to the next level.</summary>
        public void LevelUp()
        {
            if (!HasLevel())
            {
                return;
            }

            int toNext = _level.XpToNextLevel;
            if (toNext > 0)
            {
                Log("LevelComponent.AddXp(XpToNextLevel = " + toNext + ")");
                _level.AddXp(toNext);
            }
            else
            {
                Log("LevelComponent.SetLevel(" + (_level.Level + 1) + ")");
                _level.SetLevel(_level.Level + 1);
            }

            RefreshXp();
        }

        /// <summary>Persistent listener of <see cref="LevelComponent.XpState" />.OnChanged.</summary>
        public void OnXpChanged(int totalXp)
        {
            RefreshXp();
        }

        /// <summary>Persistent listener of <see cref="LevelComponent.OnLevelUp" />.</summary>
        public void OnLevelUp(int level)
        {
            Log("event LevelComponent.OnLevelUp -> Level " + level);
            RefreshXp();
        }

        /// <summary>Persistent listener of <see cref="LevelManager.OnChangeLevel" /> (zero-based level).</summary>
        public void OnMapLevelChanged(int level)
        {
            Log("event LevelManager.OnChangeLevel -> level " + (level + 1));
            RefreshMap();
        }

        /// <summary>Persistent listener of <see cref="LevelManager.OnChangeMaxLevel" /> (levels cleared).</summary>
        public void OnMapProgressChanged(int cleared)
        {
            Log("event LevelManager.OnChangeMaxLevel -> " + cleared + " cleared");
            RefreshMap();
        }

        /// <summary>Persistent listener of <see cref="LevelManager.OnChangeMap" />.</summary>
        public void OnMapChanged(int mapId)
        {
            Log("event LevelManager.OnChangeMap -> " + MapName(mapId));
            RefreshMap();
        }

        /// <summary>Button target (C# path): forgets the saved progress of the selected map and selects level 1.</summary>
        public void ResetMapProgress()
        {
            Map map = _levelManager != null ? _levelManager.Map : null;
            if (map == null)
            {
                Log("ResetMapProgress: no LevelManager map assigned");
                return;
            }

            int mapId = _levelManager.MapId;
            // WHY: LevelManager has no reset API; the documented key format is Map_<saveKey>_<mapId>__level.
            SaveProvider.DeleteKey("Map_" + _mapSaveKey + "_" + mapId + "__level");
            map.Load(mapId, _mapSaveKey);
            Log("SaveProvider.DeleteKey(Map_" + _mapSaveKey + "_" + mapId + "__level) + Map.Load");
            _levelManager.SetMapId(mapId);
            _levelManager.SetLevel(0);
        }

        private bool HasLevel()
        {
            if (_level != null)
            {
                return true;
            }

            WarnUnbuilt();
            return false;
        }

        private void RefreshXp()
        {
            if (_level == null)
            {
                return;
            }

            int level = _level.Level;
            int totalXp = _level.TotalXp;
            if (_totalXpValue != null)
            {
                _totalXpValue.text = totalXp.ToString(CultureInfo.InvariantCulture);
            }

            if (_xpFill == null)
            {
                return;
            }

            LevelCurveDefinition curve = _level.LevelCurveDefinition;
            float progress = 1f;
            if (curve != null)
            {
                int current = curve.GetRequiredXpForLevel(level);
                int next = curve.GetRequiredXpForLevel(level + 1);
                int span = next - current;
                progress = span > 0 ? (totalXp - current) / (float)span : 1f;
            }

            RectTransform fill = _xpFill.rectTransform;
            fill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
        }

        private void RefreshMap()
        {
            Map map = _levelManager != null ? _levelManager.Map : null;
            if (map == null)
            {
                return;
            }

            if (_mapValue != null)
            {
                string name = MapName(_levelManager.MapId);
                _mapValue.text = map.isInfinity ? name + " · infinite" : name + " · " + map.countLevels + " levels";
            }

            if (_completeValue != null)
            {
                _completeValue.text = map.isInfinity
                    ? "never (infinite)"
                    : map.IsComplete
                        ? "<color=" + GoodHex + ">YES</color> · " + map.countLevels + " of " + map.countLevels
                        : "no · " + map.level + " of " + map.countLevels;
            }

            if (_infiniteMapHint != null && _infiniteMapHint.activeSelf != map.isInfinity)
            {
                _infiniteMapHint.SetActive(map.isInfinity);
            }
        }

        private string MapName(int mapId)
        {
            return _mapNames != null && mapId >= 0 && mapId < _mapNames.Length && !string.IsNullOrEmpty(_mapNames[mapId])
                ? _mapNames[mapId]
                : "Map " + mapId;
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
                "[LevelDemoController] The scene UI is not authored. Re-import the scene from the Demo download.", this);
        }
    }
}
