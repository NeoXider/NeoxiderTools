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
    [AddComponentMenu("Neo/Demo/GridSystem/GridSystemMatch3DemoSetup")]
    public class GridSystemMatch3DemoSetup : MonoBehaviour
    {
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
            CreateText("GridSystem Match3 Demo", canvas, new Vector2(0, 220), 30, Color.white);
            TMP_Text status = CreateText("Ready", canvas, new Vector2(0, 180), 20, new Color(0.7f, 0.9f, 1f))
                .GetComponent<TMP_Text>();

            GameObject uiObject = CreateGO("GridSystemMatch3DemoUI");
            GridSystemMatch3DemoUI ui = uiObject.AddComponent<GridSystemMatch3DemoUI>();
            SetRef(ui, "_generator", generator);
            SetRef(ui, "_match3", match3);
            SetRef(ui, "_debugDrawer", drawer);
            SetRef(ui, "_statusText", status);

            SetRef(boardView, "_generator", generator);
            SetRef(boardView, "_match3", match3);
            SetRef(boardView, "_camera", Camera.main);
            SetRef(boardView, "_statusText", status);

            float y = -20f;
            float spacing = 45f;
            CreateButton("Generate Rect", canvas, new Vector2(-150, y), ui.GenerateRectBoard);
            CreateButton("Generate Diamond", canvas, new Vector2(150, y), ui.GenerateDiamondBoard);
            CreateButton("Toggle Blocked", canvas, new Vector2(-150, y - spacing), ui.ToggleRandomBlocked);
            CreateButton("Disable Cell", canvas, new Vector2(150, y - spacing), ui.ToggleRandomDisabled);
            CreateButton("Toggle Occupied", canvas, new Vector2(-150, y - spacing * 2), ui.ToggleRandomOccupied);
            CreateButton("Run Path Demo", canvas, new Vector2(150, y - spacing * 2), ui.RunPathDemo);
            CreateButton("Swap Random", canvas, new Vector2(-150, y - spacing * 3), ui.SwapRandom);
            CreateButton("Restart Board", canvas, new Vector2(150, y - spacing * 3), ui.RestartBoard);

            generator.GenerateField();
            match3.InitializeBoard();

            Debug.Log("[GridSystemMatch3DemoSetup] Scene is ready.");
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
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance);
            field?.SetValue(target, value);
            if (target is Object obj)
            {
                EditorUtility.SetDirty(obj);
            }
        }
#endif
    }
}