using Neo.Core.Level;
using Neo.Level;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Samples
{
    /// <summary>
    ///     Bright, self-contained demo for the level modules. The primary section drives a real
    ///     <b>Neo.Core.Level</b> <see cref="LevelComponent" /> fed by a <see cref="LevelCurveDefinition" />
    ///     created at runtime (linear, 100 XP/level): buttons add XP, a bar shows XP-to-next, a hero label
    ///     shows the level, and the component's OnLevelUp event is logged. The secondary section drives the
    ///     scene-flow <b>Neo.Level</b> <see cref="LevelManager" />, which runs headless here (its default
    ///     infinite Map persists the level number through SaveProvider — no build scenes required).
    ///     Robust in an empty scene.
    /// </summary>
    [AddComponentMenu("Neoxider/Demos/Level Demo")]
    public sealed class LevelDemoController : MonoBehaviour
    {
        private NeoDemoShell.Context _shell;

        private LevelComponent _level;
        private LevelCurveDefinition _curve;
        private TMP_Text _levelBig;
        private TMP_Text _xpValue;
        private TMP_Text _toNextValue;
        private Image _xpBar;

        private LevelManager _levelManager;
        private TMP_Text _mapValue;

        private void Start()
        {
            _shell = NeoDemoShell.Build("Neo.Level", new Color(0.49f, 0.36f, 0.94f));

            NeoDemoShell.ShowInfoCardOnce(
                "Neo.Core.Level + Neo.Level",
                "LevelComponent turns XP into levels via a curve. LevelManager tracks scene/level flow.",
                "+10 / +50 XP call LevelComponent.AddXp(n)",
                "Level up fills the remaining Xp-to-next; OnLevelUp is logged",
                "LevelManager runs headless — no scene assets needed");

            BuildLevelComponent();

            _levelBig = _shell.AddBigLabel("Level 1");
            _xpBar = _shell.AddBar("XP to next level", _shell.Accent);
            _xpValue = _shell.AddValueLabel("Total XP");
            _toNextValue = _shell.AddValueLabel("XP to next");

            _shell.AddButtonRow(
                ("+10 XP", () => AddXp(10)),
                ("+50 XP", () => AddXp(50)),
                ("Level up", LevelUp));

            BuildLevelManager();

            RefreshLevel();
            _shell.Log("LevelComponent ready (linear curve, 100 XP/level)");
        }

        private void OnDestroy()
        {
            if (_level != null)
            {
                _level.OnLevelUp.RemoveListener(HandleLevelUp);
            }

            if (_levelManager != null)
            {
                _levelManager.OnChangeLevel.RemoveListener(HandleMapLevel);
                _levelManager.OnChangeMaxLevel.RemoveListener(HandleMapMax);
            }
        }

        private void BuildLevelComponent()
        {
            // WHY: runtime curve — no ScriptableObject asset on disk.
            _curve = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            _curve.SetLinear(100);

            var host = new GameObject("LevelComponent");
            host.transform.SetParent(transform, false);
            _level = host.AddComponent<LevelComponent>(); // Awake → EnsureInitialized builds the model
            _level.LevelCurveDefinition = _curve; // model already exists, so this re-applies the curve
            _level.SetLevel(1); // clean, predictable start (XP 0, level 1) before we listen
            _level.OnLevelUp.AddListener(HandleLevelUp);
        }

        private void BuildLevelManager()
        {
            if (!LevelManager.HasInstance)
            {
                LevelManager.CreateInstance = true;
            }

            _levelManager = LevelManager.I; // Init loads its default infinite Map from SaveProvider
            _levelManager.OnChangeLevel.AddListener(HandleMapLevel);
            _levelManager.OnChangeMaxLevel.AddListener(HandleMapMax);

            _mapValue = _shell.AddValueLabel("LevelManager  level / max");
            _shell.AddButtonRow(
                ("Map: Complete", () =>
                {
                    _levelManager.SaveLevel(); // persists max level via SaveProvider
                    _shell.Log($"LevelManager.SaveLevel() → max {_levelManager.MaxLevel}");
                    RefreshMap();
                }),
                ("Map: Next", () =>
                {
                    _levelManager.NextLevel(); // advances current level (capped at max+1)
                    _shell.Log($"LevelManager.NextLevel() → level {_levelManager.CurrentLevel}");
                    RefreshMap();
                }));

            RefreshMap();
            _shell.Log("LevelManager headless — infinite Map, no scenes");
        }

        private void AddXp(int amount)
        {
            _level.AddXp(amount);
            _shell.Log($"LevelComponent.AddXp({amount}) → total {_level.TotalXp}");
            RefreshLevel();
        }

        private void LevelUp()
        {
            int toNext = _level.XpToNextLevel;
            if (toNext > 0)
            {
                _level.AddXp(toNext);
                _shell.Log($"AddXp(XpToNextLevel={toNext}) to force a level-up");
            }
            else
            {
                _level.SetLevel(_level.Level + 1);
                _shell.Log($"SetLevel({_level.Level})");
            }

            RefreshLevel();
        }

        private void HandleLevelUp(int newLevel)
        {
            _shell.Log($"event OnLevelUp → Level {newLevel}");
            RefreshLevel();
        }

        private void HandleMapLevel(int level)
        {
            _shell.Log($"event OnChangeLevel → {level}");
        }

        private void HandleMapMax(int max)
        {
            _shell.Log($"event OnChangeMaxLevel → {max}");
        }

        private void RefreshLevel()
        {
            int level = _level.Level;
            _levelBig.text = "Level " + level;
            _xpValue.text = _level.TotalXp.ToString();
            _toNextValue.text = _level.XpToNextLevel.ToString();

            int reqCur = _curve.GetRequiredXpForLevel(level);
            int reqNext = _curve.GetRequiredXpForLevel(level + 1);
            int span = reqNext - reqCur;
            float progress = span > 0 ? (_level.TotalXp - reqCur) / (float)span : 1f;
            Neo.Samples.Survivor.SurvivorUI.SetFill(_xpBar, Mathf.Clamp01(progress));
        }

        private void RefreshMap()
        {
            if (_mapValue != null)
            {
                _mapValue.text = $"{_levelManager.CurrentLevel} / {_levelManager.MaxLevel}";
            }
        }
    }
}
