using Neo.Core.Level;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     Demo UI for the LevelComponent + LevelCurveDefinition system using OnGUI.
    ///     Shows level, XP, XP-to-next, and provides buttons for adding XP.
    /// </summary>
    public class ProgressionDemoUI : MonoBehaviour
    {
        [Header("References")] [SerializeField]
        private LevelComponent _levelComponent;

        [Header("Settings")] [SerializeField] private int _xpPerClick = 25;

        private string _log = "";
        private Vector2 _scrollPos;

        private void Start()
        {
            if (_levelComponent == null)
            {
                _levelComponent = FindObjectOfType<LevelComponent>();
            }

            if (_levelComponent != null)
            {
                _levelComponent.OnLevelUp.AddListener(OnLevelUp);
                _levelComponent.OnXpGained.AddListener(OnXpGained);
            }

            Log("Progression Demo started. Use buttons below to add XP and level up!");
        }

        private void OnGUI()
        {
            float w = Screen.width;
            float h = Screen.height;

            GUI.skin.label.richText = true;
            var mainTitleStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 36, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var titleStyle = new GUIStyle(GUI.skin.label)
                { fontSize = 32, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
            var labelStyle = new GUIStyle(GUI.skin.label) { fontSize = 24 };
            var btnStyle = new GUIStyle(GUI.skin.button) { fontSize = 24 };
            var logStyle = new GUIStyle(GUI.skin.box) { fontSize = 20, alignment = TextAnchor.UpperLeft };

            int padding = 40;
            var rect = new Rect(padding, padding, w - padding * 2, h - padding * 2);

            GUILayout.BeginArea(rect, "", GUI.skin.window);
            GUILayout.Space(20);
            GUILayout.Label("✨ Progression (Level System) Demo ✨", mainTitleStyle);
            GUILayout.Space(30);

            if (_levelComponent == null)
            {
                GUILayout.Label("Status: <color=red>No LevelComponent found in scene!</color>", labelStyle);
                GUILayout.EndArea();
                return;
            }

            GUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();
            GUILayout.BeginVertical("box", GUILayout.Width(rect.width * 0.6f));
            GUILayout.Space(20);

            GUILayout.Label($"Level: <color=yellow>{_levelComponent.Level}</color>", titleStyle);
            GUILayout.Space(10);
            GUILayout.Label($"<b>Total XP:</b> {_levelComponent.TotalXp}", labelStyle);
            GUILayout.Label($"<b>XP To Next Level:</b> {_levelComponent.XpToNextLevel}", labelStyle);

            if (_levelComponent.LevelCurveDefinition != null)
            {
                int reqCurrent = _levelComponent.LevelCurveDefinition.GetRequiredXpForLevel(_levelComponent.Level);
                int reqNext = _levelComponent.LevelCurveDefinition.GetRequiredXpForLevel(_levelComponent.Level + 1);
                int range = reqNext - reqCurrent;
                float progress = range > 0 ? (float)(_levelComponent.TotalXp - reqCurrent) / range : 1f;

                GUILayout.Space(20);
                GUILayout.Label("<b>XP Progress:</b>", labelStyle);

                Rect barRect = GUILayoutUtility.GetRect(18, 35);
                GUI.Box(barRect, "");
                GUI.color = new Color(0.15f, 0.15f, 0.15f, 1f);
                GUI.Box(barRect, ""); // background
                GUI.color = Color.green;
                GUI.Box(new Rect(barRect.x, barRect.y, barRect.width * progress, barRect.height), "");
                GUI.color = Color.white;
                GUILayout.Space(20);
            }

            if (GUILayout.Button($"+ {_xpPerClick} XP", btnStyle, GUILayout.Height(50)))
            {
                AddSmallXp();
            }

            if (GUILayout.Button($"+ {_xpPerClick * 5} XP (Medium)", btnStyle, GUILayout.Height(50)))
            {
                AddMediumXp();
            }

            if (GUILayout.Button($"+ {_xpPerClick * 20} XP (Large)", btnStyle, GUILayout.Height(50)))
            {
                AddLargeXp();
            }

            GUILayout.Space(15);

            GUI.color = new Color(1f, 0.5f, 0.5f);
            if (GUILayout.Button("Reset Progress", btnStyle, GUILayout.Height(50)))
            {
                ResetLevel();
            }

            GUI.color = Color.white;

            GUILayout.Space(20);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(30);
            GUILayout.Label("<b>Activity Log:</b>", labelStyle);
            _scrollPos = GUILayout.BeginScrollView(_scrollPos, GUILayout.ExpandHeight(true));
            GUILayout.Label(_log, logStyle, GUILayout.ExpandHeight(true));
            GUILayout.EndScrollView();

            GUILayout.EndArea();
        }

        private void AddSmallXp()
        {
            _levelComponent.AddXp(_xpPerClick);
            Log($"+{_xpPerClick} XP added.");
        }

        private void AddMediumXp()
        {
            int amount = _xpPerClick * 5;
            _levelComponent.AddXp(amount);
            Log($"+{amount} XP added.");
        }

        private void AddLargeXp()
        {
            int amount = _xpPerClick * 20;
            _levelComponent.AddXp(amount);
            Log($"+{amount} XP added.");
        }

        private void ResetLevel()
        {
            _levelComponent.Reset();
            Log("<color=red>Progress reset to Start values.</color>");
        }

        private void OnLevelUp(int newLevel)
        {
            Log($"<color=yellow>★ LEVEL UP! You are now Level {newLevel}</color>");
        }

        private void OnXpGained() { }

        private void Log(string msg)
        {
            _log = $"[{Time.time:F1}] {msg}\n{_log}";
            if (_log.Length > 2000)
            {
                _log = _log.Substring(0, 2000);
            }
        }
    }
}
