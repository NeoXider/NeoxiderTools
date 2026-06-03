using System.Reflection;
using Neo.GridSystem;
using Neo.GridSystem.Dice;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Neo.Demo.GridSystem
{
    [AddComponentMenu("Neoxider/Demo/GridSystem/DiceMergeDemoSetup")]
    public sealed class DiceMergeDemoSetup : MonoBehaviour
    {
        [SerializeField] private bool _repairSceneOnStart = true;

        private void Start()
        {
            if (_repairSceneOnStart)
            {
                RepairRuntimeScene();
            }
        }

        public void RepairRuntimeScene()
        {
            Camera sceneCamera = EnsureCamera();
            EnsureEventSystem();

            GameObject root = GameObject.Find("GridSystem_DiceMerge");
            if (root == null)
            {
                root = new GameObject("GridSystem_DiceMerge");
            }

            root.transform.position = new Vector3(-1.64f, 0.35f, 0f);
            Grid grid = root.GetComponent<Grid>();
            if (grid == null)
            {
                grid = root.AddComponent<Grid>();
            }

            grid.cellSize = new Vector3(0.82f, 0.82f, 1f);

            FieldGenerator generator = root.GetComponent<FieldGenerator>();
            if (generator == null)
            {
                generator = root.AddComponent<FieldGenerator>();
            }

            generator.Config.Size = new Vector3Int(5, 5, 1);
            generator.Config.GridType = GridType.Rectangular;
            generator.Config.MovementRule = MovementRule.FourDirections2D;
            generator.Config.PassabilityMode = CellPassabilityMode.WalkableEnabledAndUnoccupied;
            generator.Config.Origin2D = GridOrigin2D.BottomLeft;
            generator.DebugEnabled = false;

            DiceBoardService diceBoard = root.GetComponent<DiceBoardService>();
            if (diceBoard == null)
            {
                diceBoard = root.AddComponent<DiceBoardService>();
            }

            DiceMergeDemoController controller = root.GetComponent<DiceMergeDemoController>();
            if (controller == null)
            {
                controller = root.AddComponent<DiceMergeDemoController>();
            }

            DiceMergeDemoView view = root.GetComponent<DiceMergeDemoView>();
            if (view == null)
            {
                view = root.AddComponent<DiceMergeDemoView>();
            }

            RectTransform canvas = EnsureCanvas();
            TMP_Text score = EnsureText("DiceScoreText", "Score: 0", canvas, new Vector2(0, 420), 30);
            TMP_Text pool = EnsureText("DicePoolText", "Pool: 1, 2, 3, 4, 5", canvas, new Vector2(0, 380), 18);
            TMP_Text status = EnsureText("DiceStatusText", "Drag dice onto the board", canvas, new Vector2(0, -390), 20);

            controller.Configure(generator, diceBoard, score, pool, status);
            view.Configure(generator, diceBoard, controller, sceneCamera, LoadSprite("cell"), LoadCellPrefab(), LoadDiceSprites());

            ModuleDemoSceneInfo info = FindFirstObjectByType<ModuleDemoSceneInfo>();
            if (info == null)
            {
                info = new GameObject("DiceMergeDemoInfo").AddComponent<ModuleDemoSceneInfo>();
            }

            info.Configure(
                "GridSystem Dice Merge Demo",
                "Playable 5x5 dice merge puzzle sample built from GridSystem, Neo.Merge and Dice.",
                "Use Neo.Merge for generic groups, GridMergeResolver for FieldGenerator, DiceBoardService for dice placement.",
                "Drag the tray dice onto the board. Tap a pair in the tray to rotate it.",
                "Enter Play Mode, place dice, merge 3+ connected equal values, and reach game over.",
                false);

            if (generator.Cells == null || generator.Cells.Length == 0)
            {
                generator.GenerateField();
            }
        }

        private static Camera EnsureCamera()
        {
            Camera cam = Camera.main;
            GameObject obj;
            if (cam == null)
            {
                obj = new GameObject("Main Camera");
                obj.tag = "MainCamera";
                cam = obj.AddComponent<Camera>();
                obj.AddComponent<AudioListener>();
            }
            else
            {
                obj = cam.gameObject;
            }

            cam.orthographic = true;
            cam.orthographicSize = 4.6f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.085f, 0.085f, 0.085f, 1f);
            obj.transform.position = new Vector3(0f, 0.45f, -10f);
            obj.transform.rotation = Quaternion.identity;
            return cam;
        }

        private static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null)
            {
                return;
            }

            GameObject obj = new("EventSystem");
            obj.AddComponent<EventSystem>();
            obj.AddComponent<StandaloneInputModule>();
        }

        private static RectTransform EnsureCanvas()
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            if (canvas != null)
            {
                return canvas.GetComponent<RectTransform>();
            }

            GameObject obj = new("Canvas");
            canvas = obj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            CanvasScaler scaler = obj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(720, 1280);
            obj.AddComponent<GraphicRaycaster>();
            return obj.GetComponent<RectTransform>();
        }

        private static TMP_Text EnsureText(string name, string text, RectTransform parent, Vector2 position, int size)
        {
            GameObject obj = GameObject.Find(name);
            if (obj == null)
            {
                obj = new GameObject(name);
                obj.transform.SetParent(parent, false);
                RectTransform rect = obj.AddComponent<RectTransform>();
                rect.anchoredPosition = position;
                rect.sizeDelta = new Vector2(620, size + 24);
                TextMeshProUGUI tmp = obj.AddComponent<TextMeshProUGUI>();
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.color = Color.white;
            }

            TMP_Text label = obj.GetComponent<TMP_Text>();
            label.text = text;
            label.fontSize = size;
            return label;
        }

        private static Sprite[] LoadDiceSprites()
        {
            Sprite[] sprites = new Sprite[9];
            for (int i = 0; i < sprites.Length; i++)
            {
                sprites[i] = LoadSprite((i + 1).ToString());
            }

            return sprites;
        }

        private static Sprite LoadSprite(string name)
        {
#if UNITY_EDITOR
            string path = $"Assets/Neoxider/Sprites/Dice/{name}.png";
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                return sprite;
            }
#endif
            return null;
        }

        private static DiceCellMarker LoadCellPrefab()
        {
#if UNITY_EDITOR
            GameObject prefab =
                AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Neoxider/Samples/Demo/Prefabs/Dice/DiceCell.prefab");
            if (prefab != null && prefab.TryGetComponent(out DiceCellMarker marker))
            {
                return marker;
            }
#endif
            return null;
        }
    }
}
