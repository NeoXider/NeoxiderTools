using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.TicTacToe;
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
    [AddComponentMenu("Neoxider/Demo/GridSystem/GridSystemTicTacToeDemoSetup")]
    public class GridSystemTicTacToeDemoSetup : MonoBehaviour
    {
#if UNITY_EDITOR
        [Button("Setup Scene")]
        public void SetupScene()
        {
            EnsureCamera();
            EnsureEventSystem();

            GameObject root = CreateGO("GridSystem_TicTacToe");
            root.transform.position = Vector3.zero;
            root.AddComponent<Grid>();

            FieldGenerator generator = root.AddComponent<FieldGenerator>();
            generator.Config.Size = new Vector3Int(3, 3, 1);
            generator.Config.GridType = GridType.Rectangular;
            generator.Config.MovementRule = MovementRule.FourDirections2D;
            generator.Config.PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;

            FieldDebugDrawer drawer = root.AddComponent<FieldDebugDrawer>();
            drawer.DrawCoordinates = true;
            drawer.DrawPath = true;

            TicTacToeBoardService board = root.AddComponent<TicTacToeBoardService>();
            GridSystemTicTacToeBoardView boardView = root.AddComponent<GridSystemTicTacToeBoardView>();
            SetRef(boardView, "_generator", generator);
            SetRef(boardView, "_board", board);
            SetRef(boardView, "_camera", Camera.main);

            RectTransform canvas = CreateCanvas();
            CreateText("GridSystem TicTacToe Demo", canvas, new Vector2(0, 220), 30, Color.white);
            TMP_Text status = CreateText("Ready", canvas, new Vector2(0, 180), 20, new Color(0.7f, 0.9f, 1f))
                .GetComponent<TMP_Text>();

            GameObject uiObject = CreateGO("GridSystemTicTacToeDemoUI");
            GridSystemTicTacToeDemoUI ui = uiObject.AddComponent<GridSystemTicTacToeDemoUI>();
            SetRef(ui, "_generator", generator);
            SetRef(ui, "_board", board);
            SetRef(ui, "_debugDrawer", drawer);
            SetRef(ui, "_statusText", status);

            float y = -20f;
            float spacing = 45f;
            CreateButton("Reset Board", canvas, new Vector2(-150, y), ui.ResetBoard);
            CreateButton("Random Move", canvas, new Vector2(150, y), ui.MakeRandomMove);
            CreateButton("Toggle Center Block", canvas, new Vector2(-150, y - spacing), ui.ToggleCenterBlocked);
            CreateButton("Toggle Corner Disabled", canvas, new Vector2(150, y - spacing), ui.ToggleCornerDisabled);
            CreateButton("Run Path Demo", canvas, new Vector2(0, y - spacing * 2), ui.RunPathDemo);

            generator.GenerateField();
            board.ResetBoard();

            Debug.Log("[GridSystemTicTacToeDemoSetup] Scene is ready.");
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
            cam.orthographicSize = 5;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.16f, 0.2f);
            obj.transform.position = new Vector3(1f, 1f, -10f);
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
            rect.sizeDelta = new Vector2(240, 38);

            Image image = obj.AddComponent<Image>();
            image.color = new Color(0.35f, 0.35f, 0.7f, 0.95f);

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
