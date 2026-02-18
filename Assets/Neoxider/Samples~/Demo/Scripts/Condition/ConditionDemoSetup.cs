using System.Reflection;
using Neo.Condition;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace Neo.Demo.Condition
{
    /// <summary>
    ///     Настройка демо-сцены NeoCondition в Edit Mode.
    ///     Использует существующие компоненты библиотеки: Health и ScoreManager.
    ///     Иерархия после Setup:
    ///     Health              — Neo.Tools.Health
    ///     CheckDead         — NeoCondition (Hp &lt;= 0)
    ///     CheckLowHP        — NeoCondition (Hp &lt;= 30 AND IsAlive)
    ///     Score               — Neo.Tools.ScoreManager
    ///     CheckWin          — NeoCondition (Score &gt;= 100)
    ///     Canvas              — UI
    ///     DemoUIController    — ConditionDemoUI
    /// </summary>
    [AddComponentMenu("Neoxider/Demo/Condition/ConditionDemoSetup")]
    public class ConditionDemoSetup : MonoBehaviour
    {
        [Header("Settings")] [SerializeField] private int _startHealth = 100;

        [SerializeField] private int _damagePerHit = 25;
        [SerializeField] private int _healPerClick = 10;
        [SerializeField] private int _pointsPerClick = 25;
        [SerializeField] private int _winScore = 100;

        [Header("Scene References (auto-filled on Setup)")] [SerializeField]
        private Health _health;

        [SerializeField] private ScoreManager _scoreManager;
        [SerializeField] private ConditionDemoUI _demoUI;

        public bool IsSetUp => _health != null && _scoreManager != null && _demoUI != null;

#if UNITY_EDITOR
        [Button("Setup Scene")]
        public void SetupScene()
        {
            if (IsSetUp)
            {
                Debug.Log("[ConditionDemoSetup] Сцена уже настроена. Удалите созданные объекты для пересоздания.");
                return;
            }

            Undo.SetCurrentGroupName("Setup NeoCondition Demo Scene");
            int undoGroup = Undo.GetCurrentGroup();

            EnsureCamera();
            EnsureEventSystem();

            // ========================================
            // Health
            //   ├── CheckDead   (NeoCondition: Hp <= 0)
            //   └── CheckLowHP  (NeoCondition: Hp <= 30 AND IsAlive)
            // ========================================
            GameObject healthObj = CreateGO("Health");
            _health = healthObj.AddComponent<Health>();
            _health.maxHp = _startHealth;
            SetField(_health, "hp", _startHealth);
            SetField(_health, "restoreOnAwake", true);
            EditorUtility.SetDirty(_health);

            // CheckDead
            GameObject checkDeadObj = CreateChildGO("CheckDead", healthObj.transform);
            NeoCondition deadCondition = checkDeadObj.AddComponent<NeoCondition>();
            deadCondition.Logic = LogicMode.AND;
            deadCondition.Mode = CheckMode.EveryFrame;
            SetField(deadCondition, "_onlyOnChange", true);
            deadCondition.AddCondition(new ConditionEntry
            {
                ComponentTypeName = typeof(Health).FullName,
                PropertyName = "Hp",
                CurrentValueType = ValueType.Int,
                Compare = CompareOp.LessOrEqual,
                ThresholdInt = 0
            });

            // CheckLowHP
            GameObject checkLowHPObj = CreateChildGO("CheckLowHP", healthObj.transform);
            NeoCondition lowHPCondition = checkLowHPObj.AddComponent<NeoCondition>();
            lowHPCondition.Logic = LogicMode.AND;
            lowHPCondition.Mode = CheckMode.EveryFrame;
            SetField(lowHPCondition, "_onlyOnChange", true);
            lowHPCondition.AddCondition(new ConditionEntry
            {
                ComponentTypeName = typeof(Health).FullName,
                PropertyName = "Hp",
                CurrentValueType = ValueType.Int,
                Compare = CompareOp.LessOrEqual,
                ThresholdInt = 30
            });
            lowHPCondition.AddCondition(new ConditionEntry
            {
                ComponentTypeName = typeof(Health).FullName,
                PropertyName = "IsAlive",
                CurrentValueType = ValueType.Bool,
                Compare = CompareOp.Equal,
                ThresholdBool = true
            });

            // ========================================
            // Score
            //   └── CheckWin (NeoCondition: Score >= target)
            // ========================================
            GameObject scoreObj = CreateGO("Score");
            _scoreManager = scoreObj.AddComponent<ScoreManager>();
            SetField(_scoreManager, "_targetScore", _winScore);
            EditorUtility.SetDirty(_scoreManager);

            // CheckWin
            GameObject checkWinObj = CreateChildGO("CheckWin", scoreObj.transform);
            NeoCondition winCondition = checkWinObj.AddComponent<NeoCondition>();
            winCondition.Logic = LogicMode.AND;
            winCondition.Mode = CheckMode.EveryFrame;
            SetField(winCondition, "_onlyOnChange", true);
            winCondition.AddCondition(new ConditionEntry
            {
                ComponentTypeName = typeof(ScoreManager).FullName,
                PropertyName = "Score",
                CurrentValueType = ValueType.Int,
                Compare = CompareOp.GreaterOrEqual,
                ThresholdInt = _winScore
            });

            // ========================================
            // Canvas — UI
            // ========================================
            RectTransform canvasRT = CreateCanvas();

            CreateText("NeoCondition Demo", canvasRT, new Vector2(0, 220), 32, Color.white);

            // HP text — подключим к Health.OnChange через SetText или напрямую
            GameObject hpTxtObj = CreateText($"HP: {_startHealth} / {_startHealth}", canvasRT, new Vector2(0, 160), 24,
                new Color(0.3f, 0.9f, 0.3f));
            TMP_Text hpTmp = hpTxtObj.GetComponent<TMP_Text>();

            // Score text — подключим к ScoreManager.textScores
            GameObject scoreTxtObj = CreateText($"Score: 0 / {_winScore}", canvasRT, new Vector2(0, 120), 24,
                new Color(0.9f, 0.9f, 0.3f));
            TMP_Text scoreTmp = scoreTxtObj.GetComponent<TMP_Text>();
            _scoreManager.textScores = new[] { scoreTmp };
            EditorUtility.SetDirty(_scoreManager);

            // Status text
            GameObject statusTxtObj =
                CreateText("Playing...", canvasRT, new Vector2(0, 75), 20, new Color(0.7f, 0.7f, 0.7f));
            TMP_Text statusTmp = statusTxtObj.GetComponent<TMP_Text>();

            // Warning bar
            GameObject warningObj = CreatePanel("WarningIcon", canvasRT, new Vector2(0, 35), new Vector2(300, 30),
                new Color(0.9f, 0.2f, 0.1f, 0.8f));
            CreateText("LOW HEALTH!", warningObj.GetComponent<RectTransform>(), Vector2.zero, 16, Color.white);
            warningObj.SetActive(false);

            // Game Over panel
            GameObject gameOverObj = CreatePanel("GameOverPanel", canvasRT, Vector2.zero, new Vector2(400, 200),
                new Color(0.8f, 0.1f, 0.1f, 0.85f));
            CreateText("GAME OVER", gameOverObj.GetComponent<RectTransform>(), new Vector2(0, 40), 36, Color.white);
            CreateText("Your health reached 0", gameOverObj.GetComponent<RectTransform>(), new Vector2(0, -10), 18,
                new Color(1, 0.8f, 0.8f));
            gameOverObj.SetActive(false);

            // Win panel
            GameObject winObj = CreatePanel("WinPanel", canvasRT, Vector2.zero, new Vector2(400, 200),
                new Color(0.1f, 0.6f, 0.1f, 0.85f));
            CreateText("YOU WIN!", winObj.GetComponent<RectTransform>(), new Vector2(0, 40), 36, Color.white);
            CreateText($"Score reached {_winScore}", winObj.GetComponent<RectTransform>(), new Vector2(0, -10), 18,
                new Color(0.8f, 1, 0.8f));
            winObj.SetActive(false);

            // ========================================
            // DemoUIController
            // ========================================
            GameObject uiObj = CreateGO("DemoUIController");
            _demoUI = uiObj.AddComponent<ConditionDemoUI>();
            SetField(_demoUI, "_health", _health);
            SetField(_demoUI, "_scoreManager", _scoreManager);
            SetField(_demoUI, "_gameOverPanel", gameOverObj);
            SetField(_demoUI, "_winPanel", winObj);
            SetField(_demoUI, "_warningIcon", warningObj);
            SetField(_demoUI, "_statusText", statusTmp);

            // ========================================
            // HP text обновляем дополнительным скриптом — 
            // добавим простой компонент HealthTextUpdater на hpTxtObj
            // Но проще: подпишемся через Health.OnChange на UpdateHpText в DemoUI
            // ========================================

            // Добавим поле для hpText в DemoUI и метод UpdateHpText
            // Нет — сделаем ещё проще: повесим на Health.OnChange лямбду... 
            // но persistent listener не может быть лямбдой.
            // Используем уже существующий подход — Health имеет OnChange<int>,
            // а TMP_Text обновляет себя. Сделаем маленький helper компонент прямо на hpTxtObj.
            HealthTextDisplay hpUpdater = hpTxtObj.AddComponent<HealthTextDisplay>();
            SetField(hpUpdater, "_health", _health);
            EditorUtility.SetDirty(hpUpdater);

            // ========================================
            // Wire events (persistent listeners)
            // ========================================

            // CheckDead → ShowGameOver
            UnityEventTools.AddVoidPersistentListener(
                deadCondition.OnTrue,
                _demoUI.ShowGameOver);

            // CheckLowHP → ShowWarning / HideWarning
            UnityEventTools.AddVoidPersistentListener(
                lowHPCondition.OnTrue,
                _demoUI.ShowWarning);
            UnityEventTools.AddVoidPersistentListener(
                lowHPCondition.OnFalse,
                _demoUI.HideWarning);

            // CheckWin → ShowWin
            UnityEventTools.AddVoidPersistentListener(
                winCondition.OnTrue,
                _demoUI.ShowWin);

            // Buttons
            float btnY = -80f;
            float btnSpacing = 50f;

            Button btnDmg = CreateButton($"Take Damage ({_damagePerHit})", canvasRT, new Vector2(-120, btnY),
                new Color(0.9f, 0.3f, 0.3f));
            UnityEventTools.AddVoidPersistentListener(btnDmg.onClick, _demoUI.DealDamage);

            Button btnHeal = CreateButton($"Heal ({_healPerClick})", canvasRT, new Vector2(120, btnY),
                new Color(0.3f, 0.9f, 0.3f));
            UnityEventTools.AddVoidPersistentListener(btnHeal.onClick, _demoUI.HealPlayer);

            Button btnScore = CreateButton($"Add Score ({_pointsPerClick})", canvasRT,
                new Vector2(0, btnY - btnSpacing), new Color(0.3f, 0.6f, 0.9f));
            UnityEventTools.AddVoidPersistentListener(btnScore.onClick, _demoUI.AddScore);

            Button btnReset = CreateButton("Reset All", canvasRT, new Vector2(0, btnY - btnSpacing * 2),
                new Color(0.6f, 0.6f, 0.6f));
            UnityEventTools.AddVoidPersistentListener(btnReset.onClick, _demoUI.ResetAll);

            // Info text
            CreateText(
                "NeoCondition + Health + ScoreManager:\n" +
                "  Health / CheckDead:  Hp <= 0 → Game Over\n" +
                "  Health / CheckLowHP: Hp <= 30 AND IsAlive → Warning\n" +
                $"  Score / CheckWin:    Score >= {_winScore} → Win",
                canvasRT, new Vector2(0, -270), 14, new Color(0.5f, 0.5f, 0.5f));

            // ========================================
            // Finalize
            // ========================================
            EditorUtility.SetDirty(this);
            Undo.CollapseUndoOperations(undoGroup);

            Debug.Log("[ConditionDemoSetup] Демо-сцена настроена! Сохраните сцену (Ctrl+S).");
        }
#endif

        // =========================================================================
        // Helpers (editor-only)
        // =========================================================================

#if UNITY_EDITOR
        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject obj = new("Main Camera");
            Undo.RegisterCreatedObjectUndo(obj, "Create Camera");
            obj.tag = "MainCamera";
            Camera cam = obj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.2f);
            cam.transform.position = new Vector3(0, 0, -10);
            obj.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject obj = new("EventSystem");
            Undo.RegisterCreatedObjectUndo(obj, "Create EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateGO(string name)
        {
            GameObject go = new(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        private static GameObject CreateChildGO(string name, Transform parent)
        {
            GameObject go = new(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent, false);
            return go;
        }

        private static RectTransform CreateCanvas()
        {
            GameObject obj = new("Canvas");
            Undo.RegisterCreatedObjectUndo(obj, "Create Canvas");
            obj.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(800, 600);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            obj.AddComponent<GraphicRaycaster>();
            return obj.GetComponent<RectTransform>();
        }

        private static GameObject CreateText(string text, RectTransform parent, Vector2 pos, int size, Color color)
        {
            string shortName = text.Length > 15 ? text.Substring(0, 15) : text;
            GameObject obj = new($"Text_{shortName}");
            Undo.RegisterCreatedObjectUndo(obj, "Create Text");
            obj.transform.SetParent(parent, false);
            obj.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(700, size * 2 + 20);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.enableWordWrapping = true;
            tmp.overflowMode = TextOverflowModes.Overflow;

            return obj;
        }

        private static GameObject CreatePanel(string name, RectTransform parent, Vector2 pos, Vector2 size, Color color)
        {
            GameObject obj = new(name);
            Undo.RegisterCreatedObjectUndo(obj, $"Create {name}");
            obj.transform.SetParent(parent, false);
            obj.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;
            obj.AddComponent<Image>().color = color;
            return obj;
        }

        private static Button CreateButton(string label, RectTransform parent, Vector2 pos, Color color)
        {
            GameObject obj = new($"Btn_{label}");
            Undo.RegisterCreatedObjectUndo(obj, $"Create Button {label}");
            obj.transform.SetParent(parent, false);
            obj.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(200, 40);

            Image img = obj.AddComponent<Image>();
            img.color = color;
            Button btn = obj.AddComponent<Button>();
            btn.targetGraphic = img;
            ColorBlock c = btn.colors;
            c.normalColor = color;
            c.highlightedColor = color * 1.1f;
            c.pressedColor = color * 0.8f;
            btn.colors = c;

            GameObject txtObj = new("Label");
            txtObj.transform.SetParent(obj.transform, false);
            txtObj.layer = LayerMask.NameToLayer("UI");
            RectTransform txtRect = txtObj.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txtObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btn;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic | BindingFlags.Instance |
                BindingFlags.Public);
            if (field != null)
            {
                field.SetValue(target, value);
                if (target is Object uo)
                {
                    EditorUtility.SetDirty(uo);
                }
            }
        }
#endif
    }
}
