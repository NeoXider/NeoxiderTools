using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.Match3;
using Neo.Tools;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Events;
#endif

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemMatch3DemoSetup")]
    public class GridSystemMatch3DemoSetup : MonoBehaviour
    {
        [SerializeField] private bool _repairSceneOnStart = true;

        private void Start()
        {
            if (Application.isPlaying && _repairSceneOnStart)
            {
                RepairRuntimeScene();
            }
        }

#if UNITY_EDITOR
        [Button("Setup Scene")]
        public void SetupScene()
        {
            EnsureCamera();
            EnsureEventSystem();

            GameObject root = CreateGO("GridSystem_Match3");
            root.transform.position = Vector3.zero;
            root.AddComponent<Grid>();

            FieldGenerator generator = root.AddComponent<FieldGenerator>();
            generator.Config.Size = new Vector3Int(8, 8, 1);
            generator.Config.GridType = GridType.Rectangular;
            generator.Config.MovementRule = MovementRule.FourDirections2D;
            generator.Config.PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;

            FieldDebugDrawer drawer = root.AddComponent<FieldDebugDrawer>();
            drawer.DrawCoordinates = true;
            drawer.DrawPath = true;

            Match3BoardService match3 = root.AddComponent<Match3BoardService>();
            GridSystemMatch3BoardView boardView = root.AddComponent<GridSystemMatch3BoardView>();

            RectTransform canvas = CreateCanvas();
            PinTopCenter(CreateText("GridSystem Match3 Demo", canvas, new Vector2(0, 330), 30, Color.white)
                .GetComponent<RectTransform>(), 28f);
            TMP_Text status = CreateText("Ready", canvas, new Vector2(0, 296), 20, new Color(0.7f, 0.9f, 1f))
                .GetComponent<TMP_Text>();
            PinTopCenter(status.rectTransform, 62f);

            GameObject uiObject = CreateGO("GridSystemMatch3DemoUI");
            GridSystemMatch3DemoUI ui = uiObject.AddComponent<GridSystemMatch3DemoUI>();
            SetRef(ui, "_generator", generator);
            SetRef(ui, "_match3", match3);
            SetRef(ui, "_debugDrawer", drawer);
            SetRef(ui, "_statusText", status);
            ui.Configure(generator, match3, drawer, status);

            SetRef(boardView, "_generator", generator);
            SetRef(boardView, "_match3", match3);
            SetRef(boardView, "_camera", Camera.main);
            SetRef(boardView, "_statusText", status);
            boardView.Configure(generator, match3, Camera.main, status);

            // WHY: side columns keep the centered board fully visible; ±510 leaves a margin at 1280 ref width.
            float y = 120f;
            float spacing = 45f;
            CreateButton("Generate Rect", canvas, new Vector2(-510, y), ui.GenerateRectBoard);
            CreateButton("Generate Diamond", canvas, new Vector2(510, y), ui.GenerateDiamondBoard);
            CreateButton("Toggle Blocked", canvas, new Vector2(-510, y - spacing), ui.ToggleRandomBlocked);
            CreateButton("Disable Cell", canvas, new Vector2(510, y - spacing), ui.ToggleRandomDisabled);
            CreateButton("Toggle Occupied", canvas, new Vector2(-510, y - spacing * 2), ui.ToggleRandomOccupied);
            CreateButton("Run Path Demo", canvas, new Vector2(510, y - spacing * 2), ui.RunPathDemo);
            CreateButton("Swap Random", canvas, new Vector2(-510, y - spacing * 3), ui.SwapRandom);
            CreateButton("Restart Board", canvas, new Vector2(510, y - spacing * 3), ui.RestartBoard);

            generator.GenerateField();
            match3.InitializeBoard();

            global::Neo.Demo.SampleDiagnostics.Log("[GridSystemMatch3DemoSetup] Scene is ready.", this);
        }

        private static void EnsureCamera()
        {
            if (Camera.main != null)
            {
                return;
            }

            GameObject obj = new("Main Camera");
            obj.tag = "MainCamera";
            Camera cam = obj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.16f, 0.2f);
            obj.transform.position = new Vector3(3.5f, 3.5f, -10f);
            obj.AddComponent<AudioListener>();
        }

        private static void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject obj = new("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();
        }

        private static GameObject CreateGO(string name)
        {
            GameObject go = new(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            return go;
        }

        private static RectTransform CreateCanvas()
        {
            GameObject obj = CreateGO("Canvas");
            obj.layer = LayerMask.NameToLayer("UI");
            Canvas canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            obj.AddComponent<GraphicRaycaster>();
            return obj.GetComponent<RectTransform>();
        }

        private static GameObject CreateText(string text, RectTransform parent, Vector2 pos, int size, Color color)
        {
            GameObject obj = CreateGO("Text_" + text.Replace(" ", "_"));
            obj.transform.SetParent(parent, false);
            obj.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(850, size + 16);

            TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return obj;
        }

        private static void CreateButton(string label, RectTransform parent, Vector2 pos,
            UnityEngine.Events.UnityAction action)
        {
            GameObject obj = CreateGO("Btn_" + label.Replace(" ", "_"));
            obj.transform.SetParent(parent, false);
            obj.layer = LayerMask.NameToLayer("UI");
            RectTransform rect = obj.AddComponent<RectTransform>();
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 38);

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.7f, 0.95f);

            Button button = obj.AddComponent<Button>();
            UnityEventTools.AddVoidPersistentListener(button.onClick, action.Invoke);

            GameObject txt = CreateGO("Label");
            txt.transform.SetParent(obj.transform, false);
            txt.layer = LayerMask.NameToLayer("UI");
            RectTransform txtRect = txt.AddComponent<RectTransform>();
            txtRect.anchorMin = Vector2.zero;
            txtRect.anchorMax = Vector2.one;
            txtRect.offsetMin = Vector2.zero;
            txtRect.offsetMax = Vector2.zero;

            TextMeshProUGUI tmp = txt.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = 16;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
        }

        private static void SetRef(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.NonPublic |
                BindingFlags.Instance);
            field?.SetValue(target, value);
            if (target is Object obj)
            {
                EditorUtility.SetDirty(obj);
            }
        }
#endif

        private void RepairRuntimeScene()
        {
            Camera sceneCamera = EnsureRuntimeCamera();
            EnsureRuntimeEventSystem();

            Match3BoardService match3 = FindFirstObjectByType<Match3BoardService>();
            FieldGenerator generator = match3 != null
                ? match3.GetComponent<FieldGenerator>()
                : FindFirstObjectByType<FieldGenerator>();

            if (generator == null)
            {
                GameObject root = CreateRuntimeGO("GridSystem_Match3");
                root.AddComponent<Grid>();
                generator = root.AddComponent<FieldGenerator>();
                generator.Config.Size = new Vector3Int(8, 8, 1);
                generator.Config.GridType = GridType.Rectangular;
                generator.Config.MovementRule = MovementRule.FourDirections2D;
                generator.Config.PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;
            }

            FieldDebugDrawer drawer = generator.GetComponent<FieldDebugDrawer>();
            if (drawer == null)
            {
                drawer = generator.gameObject.AddComponent<FieldDebugDrawer>();
            }

            match3 = generator.GetComponent<Match3BoardService>();
            if (match3 == null)
            {
                match3 = generator.gameObject.AddComponent<Match3BoardService>();
            }

            GridSystemMatch3BoardView boardView = generator.GetComponent<GridSystemMatch3BoardView>();
            if (boardView == null)
            {
                boardView = generator.gameObject.AddComponent<GridSystemMatch3BoardView>();
            }

            RectTransform canvas = EnsureRuntimeCanvas();
            TMP_Text title = EnsureRuntimeText("Text_GridSystem_Match3_Demo", "GridSystem Match3 Demo", canvas,
                new Vector2(0, 330), 30, Color.white);
            PinTopCenter(title.rectTransform, 28f);
            TMP_Text status = EnsureRuntimeText("Text_Ready", "Ready", canvas, new Vector2(0, 296), 20,
                new Color(0.7f, 0.9f, 1f));
            PinTopCenter(status.rectTransform, 62f);

            GridSystemMatch3DemoUI ui = FindFirstObjectByType<GridSystemMatch3DemoUI>();
            if (ui == null)
            {
                ui = CreateRuntimeGO("GridSystemMatch3DemoUI").AddComponent<GridSystemMatch3DemoUI>();
            }

            _ = title;
            ui.Configure(generator, match3, drawer, status);
            boardView.Configure(generator, match3, sceneCamera, status);

            // WHY: side columns keep the centered board fully visible; ±510 leaves a margin at 1280 ref width.
            float y = 120f;
            float spacing = 45f;
            EnsureRuntimeButton("Btn_Generate_Rect", "Generate Rect", canvas, new Vector2(-510, y), ui.GenerateRectBoard);
            EnsureRuntimeButton("Btn_Generate_Diamond", "Generate Diamond", canvas, new Vector2(510, y),
                ui.GenerateDiamondBoard);
            EnsureRuntimeButton("Btn_Toggle_Blocked", "Toggle Blocked", canvas, new Vector2(-510, y - spacing),
                ui.ToggleRandomBlocked);
            EnsureRuntimeButton("Btn_Disable_Cell", "Disable Cell", canvas, new Vector2(510, y - spacing),
                ui.ToggleRandomDisabled);
            EnsureRuntimeButton("Btn_Toggle_Occupied", "Toggle Occupied", canvas, new Vector2(-510, y - spacing * 2),
                ui.ToggleRandomOccupied);
            EnsureRuntimeButton("Btn_Run_Path_Demo", "Run Path Demo", canvas, new Vector2(510, y - spacing * 2),
                ui.RunPathDemo);
            EnsureRuntimeButton("Btn_Swap_Random", "Swap Valid", canvas, new Vector2(-510, y - spacing * 3),
                ui.SwapRandom);
            EnsureRuntimeButton("Btn_Restart_Board", "Restart Board", canvas, new Vector2(510, y - spacing * 3),
                ui.RestartBoard);

            if (generator.Cells == null || generator.Cells.Length == 0)
            {
                generator.GenerateField();
            }

            match3.InitializeBoard();
            boardView.Rebuild();
        }

        private static Camera EnsureRuntimeCamera()
        {
            if (Camera.main != null)
            {
                return Camera.main;
            }

            GameObject obj = CreateRuntimeGO("Main Camera");
            obj.tag = "MainCamera";
            Camera cam = obj.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 6;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.16f, 0.2f);
            obj.transform.position = new Vector3(3.5f, 3.5f, -10f);
            obj.AddComponent<AudioListener>();
            return cam;
        }

        private static void EnsureRuntimeEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject obj = CreateRuntimeGO("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform EnsureRuntimeCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                GameObject obj = CreateRuntimeGO("Canvas");
                obj.layer = LayerMask.NameToLayer("UI");
                canvas = obj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1280, 720);
                obj.AddComponent<GraphicRaycaster>();
            }

            RectTransform rect = canvas.GetComponent<RectTransform>();
            rect.localScale = Vector3.one;
            return rect;
        }

        /// <summary>Glues a rect to the top edge so it clears the board at any viewport aspect.</summary>
        private static void PinTopCenter(RectTransform rect, float yFromTop)
        {
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -yFromTop);
        }

        private static TMP_Text EnsureRuntimeText(
            string objectName,
            string text,
            RectTransform parent,
            Vector2 pos,
            int size,
            Color color)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = CreateRuntimeGO(objectName);
                obj.transform.SetParent(parent, false);
                obj.layer = LayerMask.NameToLayer("UI");
                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(850, size + 16);
                obj.AddComponent<TextMeshProUGUI>();
            }

            RectTransform textRect = obj.GetComponent<RectTransform>();
            textRect.SetParent(parent, false);
            // WHY: scene-baked objects may carry edge anchors; positions below assume a centered anchor.
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.pivot = new Vector2(0.5f, 0.5f);
            textRect.anchoredPosition = pos;
            textRect.sizeDelta = new Vector2(850, size + 16);
            textRect.localScale = Vector3.one;

            TMP_Text tmp = obj.GetComponent<TMP_Text>();
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            return tmp;
        }

        private static void EnsureRuntimeButton(
            string objectName,
            string label,
            RectTransform parent,
            Vector2 pos,
            UnityEngine.Events.UnityAction action)
        {
            GameObject obj = GameObject.Find(objectName);
            if (obj == null)
            {
                obj = CreateRuntimeGO(objectName);
                obj.transform.SetParent(parent, false);
                obj.layer = LayerMask.NameToLayer("UI");
                obj.AddComponent<RectTransform>();
                Image image = obj.AddComponent<Image>();
                image.color = new Color(0.2f, 0.45f, 0.7f, 0.95f);
                obj.AddComponent<Button>();
            }

            RectTransform rect = obj.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            // WHY: scene-baked objects may carry edge anchors; positions below assume a centered anchor.
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = new Vector2(220, 38);
            rect.localScale = Vector3.one;

            Button button = obj.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(action);

            TMP_Text text = obj.GetComponentInChildren<TMP_Text>(true);
            if (text == null)
            {
                GameObject labelObject = CreateRuntimeGO("Label");
                labelObject.transform.SetParent(obj.transform, false);
                labelObject.layer = LayerMask.NameToLayer("UI");
                RectTransform labelRect = labelObject.AddComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
                text = labelObject.AddComponent<TextMeshProUGUI>();
            }

            text.text = label;
            text.fontSize = 16;
            text.alignment = TextAlignmentOptions.Center;
            text.color = Color.white;
        }

        private static GameObject CreateRuntimeGO(string name)
        {
            return new GameObject(name);
        }
    }
}
