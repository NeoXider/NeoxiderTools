using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Samples;
using Neo.Shop;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Editor inspector-attribute demo (Scenes/Tools/AttributeExample.unity) around its
    ///     hand-authored objects: 'Attribute' (AttributeTest, ToggleObject and three children), 'Money', 'Main Camera',
    ///     'Cube', 'Ball' and 'Ball (1)'. The Rigidbodies are placed on a lit stage in view, a second AttributeTest
    ///     ('[Demo] Inspector/Attribute B') makes multi-object [Button] calls visible, and the controls card shows the
    ///     values the inspector resolved (already in Edit Mode), runs the equivalent C# queries and calls the [Button]
    ///     methods through persistent listeners.
    /// </summary>
    internal static class AttributeExampleBuilder
    {
        internal const string ScenePath = "Scenes/Tools/AttributeExample.unity";
        internal const string MoneySaveKey = "NeoDemo.Attributes.Money";
        internal const string AttributeObjectName = "Attribute";
        internal const string SecondTargetName = "Attribute B";
        internal const string InspectorRootKey = "Inspector";
        internal const string WorldRootKey = "World";
        internal const string CompareLabel = "Compare with C#";
        internal const string KickLabel = "Kick bodies";
        internal const string ResetLabel = "Reset bodies";
        internal const string PrintHelloLabel = "PrintHello()";
        internal const string AddScoreLabel = "AddScore(10)";
        internal const string ResetScoreLabel = "ResetScore()";
        internal const string PrintHelloLog = "PrintHello() ran · Console shows Hello World!";
        internal const int AddScoreAmount = 10;

        private const string SceneName = "AttributeExample.unity";
        private const string MaterialFolder = "Materials/";
        private const float ContentWidth = 540f;
        private const float InfoCardWidth = 400f;
        private const float CameraSize = 5f;
        private const int LogLines = 5;

        private static readonly Color Accent = new(0.64f, 0.50f, 1f);
        private static readonly Color RigidbodyTint = new(1f, 1f, 0f);
        private static readonly Color SphereTint = new(1f, 0.35f, 1f);
        private static readonly Vector3 FloorPosition = new(-0.7f, -3.5f, 0f);
        private static readonly Vector3 FloorScale = new(7.6f, 0.4f, 4f);

        /// <summary>Stage slots of the hand-authored Rigidbodies (name, resting position, demo material).</summary>
        internal static readonly BodySlot[] Bodies =
        {
            new("Cube", new Vector3(-3f, -2.8f, 0f), "yellow.mat"),
            new("Ball", new Vector3(-0.7f, -2.8f, 0f), "red.mat"),
            new("Ball (1)", new Vector3(1.6f, -2.8f, 0f), "blue.mat")
        };

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            AttributeTest attributes = Step("find the hand-authored 'Attribute' (AttributeTest)", () => FindAttributes(ctx));
            Step("fill empty attributed fields the way the Neo inspector does", () => FillEmptyFields(ctx, attributes));
            Step("demo save keys", () => DemoSaveKeys.UseForAll<Money>(ctx, "_moneySave", MoneySaveKey));
            ModuleDemoSceneInfo info = Step("ensure the info service",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            AttributeDemoController controller = Step("ensure the demo controller",
                () => ctx.FindOrCreate<AttributeDemoController>("Services", "Demo Controller"));

            DemoFrame frame = Step("author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Editor · Inspector attributes", Accent)
                {
                    Overline = "NEOXIDER · EDITOR MODULE DEMO",
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = ContentWidth,
                    ContentOffsetY = 0f,
                    LogFeed = false
                }));
            Step("frame the stage with the scene camera", () => StyleCamera(ctx, frame.Camera));
            Step("place the hand-authored Rigidbodies on the stage", () => PlaceBodies(ctx));
            Step("author the stage", () => BuildStage(ctx));
            Step("author the multi-select target " + SecondTargetName, () => BuildSecondTarget(ctx, attributes));
            Step("author the controls card and wiring", () => BuildControls(ctx, frame.Content, attributes, controller));
            Step("author the info card", () => frame.InfoCard(info, InfoContent()));
        }

        private static AttributeTest FindAttributes(DemoSceneBuildContext ctx)
        {
            List<AttributeTest> all = ctx.FindAll<AttributeTest>();
            foreach (AttributeTest candidate in all)
            {
                if (candidate.name == AttributeObjectName)
                {
                    return candidate;
                }
            }

            if (all.Count == 1)
            {
                ctx.Note("Using the only AttributeTest '" + DemoSceneBuildContext.PathOf(all[0].transform) + "' (expected '" +
                         AttributeObjectName + "').");
                return all[0];
            }

            throw new InvalidOperationException("expected a hand-authored AttributeTest on '" + AttributeObjectName +
                                                "', the scene has " + all.Count + ". Restore " + SceneName +
                                                " from version control instead of rebuilding it.");
        }

        /// <summary>
        ///     The Neo inspector fills these fields on selection; a field that is still empty (a fresh copy of the
        ///     scene, or one cleared on purpose) is filled with the same lookup so the saved scene always shows the
        ///     resolved references. Filled fields are never touched.
        /// </summary>
        private static void FillEmptyFields(DemoSceneBuildContext ctx, AttributeTest attributes)
        {
            if (IsEmpty(attributes.rbsFindAllInScene))
            {
                ctx.Set(attributes, nameof(AttributeTest.rbsFindAllInScene), ctx.FindAll<Rigidbody>(true));
                ctx.Note("Filled the empty rbsFindAllInScene ([FindAllInScene]).");
            }

            if (IsEmpty(attributes.ballsFindAllInScene))
            {
                ctx.Set(attributes, nameof(AttributeTest.ballsFindAllInScene), ctx.FindAll<SphereCollider>(true));
                ctx.Note("Filled the empty ballsFindAllInScene ([FindAllInScene]).");
            }

            if (attributes.camFindInScene == null)
            {
                Camera camera = ctx.Find<Camera>(true);
                if (camera != null)
                {
                    ctx.Set(attributes, nameof(AttributeTest.camFindInScene), camera);
                    ctx.Note("Filled the empty camFindInScene ([FindInScene]).");
                }
            }

            if (attributes.moneyRequireInterface == null)
            {
                foreach (MonoBehaviour behaviour in ctx.FindAll<MonoBehaviour>())
                {
                    if (behaviour is IMoneyAdd)
                    {
                        ctx.Set(attributes, nameof(AttributeTest.moneyRequireInterface), behaviour.gameObject);
                        ctx.Note("Assigned moneyRequireInterface to '" + behaviour.name + "' (implements IMoneyAdd).");
                        break;
                    }
                }
            }

            if (IsEmpty(attributes.childrensGetComponents))
            {
                List<GameObject> children = new();
                foreach (Transform node in attributes.GetComponentsInChildren<Transform>())
                {
                    children.Add(node.gameObject);
                }

                ctx.Set(attributes, nameof(AttributeTest.childrensGetComponents), children);
                ctx.Note("Filled the empty childrensGetComponents ([GetComponents(true)]).");
            }

            if (attributes.toggleGetComponent == null)
            {
                ToggleObject toggle = attributes.GetComponent<ToggleObject>();
                if (toggle == null)
                {
                    ctx.Warn("'" + AttributeObjectName + "' has no ToggleObject; toggleGetComponent stays empty.");
                }
                else
                {
                    ctx.Set(attributes, nameof(AttributeTest.toggleGetComponent), toggle);
                    ctx.Note("Filled the empty toggleGetComponent ([GetComponent]).");
                }
            }
        }

        private static void StyleCamera(DemoSceneBuildContext ctx, Camera camera)
        {
            if (camera == null)
            {
                throw new InvalidOperationException("the frame found no camera.");
            }

            // WHY: the stage layout below is measured for an orthographic size-5 camera at the origin.
            camera.orthographic = true;
            camera.orthographicSize = CameraSize;
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(Accent);
            ctx.MarkModified(camera);
            ctx.MarkModified(camera.transform);
        }

        private static void PlaceBodies(DemoSceneBuildContext ctx)
        {
            List<Rigidbody> all = ctx.FindAll<Rigidbody>();
            foreach (BodySlot slot in Bodies)
            {
                Rigidbody body = all.Find(candidate => candidate.name == slot.Name);
                if (body == null)
                {
                    ctx.Warn(SceneName + ": no hand-authored Rigidbody '" + slot.Name + "'; its stage slot stays empty.");
                    continue;
                }

                body.transform.SetPositionAndRotation(slot.Position, Quaternion.identity);
                ctx.MarkModified(body.transform);
                Paint(ctx, body.GetComponent<Renderer>(), slot.Material);
            }
        }

        private static void BuildStage(DemoSceneBuildContext ctx)
        {
            Transform world = ctx.Root(WorldRootKey).transform;
            if (ctx.Find<Light>() == null)
            {
                GameObject sun = ctx.Child(world, "Sun");
                sun.transform.SetPositionAndRotation(new Vector3(0f, 3f, -6f), Quaternion.Euler(32f, -24f, 0f));
                Light light = ctx.Ensure<Light>(sun);
                light.type = LightType.Directional;
                light.color = new Color(1f, 0.97f, 0.92f);
                light.intensity = 1.15f;
                light.shadows = LightShadows.None;
            }

            GameObject floor = ctx.Primitive(world, "Floor", PrimitiveType.Cube);
            floor.transform.SetLocalPositionAndRotation(FloorPosition, Quaternion.identity);
            floor.transform.localScale = FloorScale;

            float halfWidth = FloorScale.x * 0.5f;
            Wall(ctx, world, "Wall Left", FloorPosition.x - halfWidth + 0.15f);
            Wall(ctx, world, "Wall Right", FloorPosition.x + halfWidth - 0.15f);

            // WHY: a 3D TextMeshPro em is fontSize / 10 world units; both lines stay within the 7.6-unit floor so
            // they never run under the info card or the controls card.
            DemoSceneKit.WorldLabel(ctx, world, "Stage Title", "FOUND BY THE NEO INSPECTOR", new Vector3(FloorPosition.x, 0.95f, -0.5f),
                3f, Accent, new Vector2(7.6f, 0.6f)).characterSpacing = 6f;
            DemoSceneKit.WorldLabel(ctx, world, "Stage Legend",
                Tag("Rigidbody[]", RigidbodyTint) + " = 3 bodies    " + Tag("SphereCollider[]", SphereTint) + " = 2 balls",
                new Vector3(FloorPosition.x, 0.4f, -0.5f), 2.2f, DemoPalette.Muted, new Vector2(7.6f, 0.5f));
            foreach (BodySlot slot in Bodies)
            {
                DemoSceneKit.WorldLabel(ctx, world, "Plate · " + slot.Name, slot.Name, new Vector3(slot.Position.x, -4.15f, -0.5f), 2.8f,
                    DemoPalette.Text, new Vector2(2.2f, 0.5f));
            }
        }

        private static void Wall(DemoSceneBuildContext ctx, Transform world, string name, float x)
        {
            GameObject wall = ctx.Primitive(world, name, PrimitiveType.Cube);
            wall.transform.SetLocalPositionAndRotation(new Vector3(x, FloorPosition.y + 0.8f, 0f), Quaternion.identity);
            wall.transform.localScale = new Vector3(0.3f, 1.6f, FloorScale.z);
        }

        /// <summary>
        ///     Second AttributeTest so that selecting both objects shows [Button] running on every selected object.
        ///     Its references mirror the hand-authored one; the score starts at zero on every rebuild.
        /// </summary>
        private static AttributeTest BuildSecondTarget(DemoSceneBuildContext ctx, AttributeTest source)
        {
            GameObject host = ctx.Child(ctx.Root(InspectorRootKey).transform, SecondTargetName);
            AttributeTest second = ctx.Ensure<AttributeTest>(host);
            ToggleObject toggle = ctx.Ensure<ToggleObject>(host);
            ctx.Set(second, nameof(AttributeTest.rbsFindAllInScene), ToList(source.rbsFindAllInScene));
            ctx.Set(second, nameof(AttributeTest.ballsFindAllInScene), ToList(source.ballsFindAllInScene));
            ctx.Set(second, nameof(AttributeTest.camFindInScene), source.camFindInScene);
            ctx.Set(second, nameof(AttributeTest.moneyRequireInterface), source.moneyRequireInterface);
            ctx.Set(second, nameof(AttributeTest.childrensGetComponents), new List<GameObject> { host });
            ctx.Set(second, nameof(AttributeTest.toggleGetComponent), toggle);
            ctx.Set(second, nameof(AttributeTest.score), 0);
            return second;
        }

        private static void BuildControls(DemoSceneBuildContext ctx, DemoPanel card, AttributeTest attributes,
            AttributeDemoController controller)
        {
            string[] lines = AttributeDemoController.DescribeFields(attributes);
            card.Section("Resolved in the editor · saved in the scene");
            TMP_Text rigidbodies = FieldRow(ctx, card, "rbsFindAllInScene", "[FindAllInScene]", RigidbodyTint,
                lines[AttributeDemoController.RigidbodiesField]);
            TMP_Text spheres = FieldRow(ctx, card, "ballsFindAllInScene", "[FindAllInScene]", SphereTint,
                lines[AttributeDemoController.SpheresField]);
            TMP_Text camera = FieldRow(ctx, card, "camFindInScene", "[FindInScene]", Accent,
                lines[AttributeDemoController.CameraField]);
            TMP_Text money = FieldRow(ctx, card, "moneyRequireInterface", "[RequireInterface(IMoneyAdd)]", Accent,
                lines[AttributeDemoController.MoneyField]);
            TMP_Text children = FieldRow(ctx, card, "childrensGetComponents", "[GetComponents(true)]", Accent,
                lines[AttributeDemoController.ChildrenField]);
            TMP_Text toggle = FieldRow(ctx, card, "toggleGetComponent", "[GetComponent]", Accent,
                lines[AttributeDemoController.ToggleField]);

            card.Section("Play Mode · plain references");
            card.Note("Compare runs the same lookups in C#. Kick pushes every body listed in rbsFindAllInScene.", 42f);
            Button[] play = card.ButtonRow(CompareLabel, KickLabel, ResetLabel);
            DemoWiring.AddVoid(play[0].onClick, controller.CompareWithCSharp);
            DemoWiring.AddVoid(play[1].onClick, controller.KickBodies);
            DemoWiring.AddVoid(play[2].onClick, controller.ResetBodies);
            TMP_Text check = card.CompactValue("C# check", "press " + CompareLabel, 0.4f);

            card.Section("[Button] methods · callable from UI");
            Button[] methods = card.ButtonRow(PrintHelloLabel, AddScoreLabel, ResetScoreLabel);
            TMP_Text score = card.CompactValue("AttributeTest.score", attributes.score.ToString(), 0.4f);
            DemoLogFeed log = card.LogRow(LogLines, "select Attribute in the Hierarchy");

            // WHY: NoCode path - the buttons call the [Button] methods of the hand-authored AttributeTest directly.
            DemoWiring.AddVoid(methods[0].onClick, attributes.PrintHello);
            DemoWiring.AddString(methods[0].onClick, log.Log, PrintHelloLog);
            DemoWiring.AddInt(methods[1].onClick, attributes.AddScore, AddScoreAmount);
            DemoWiring.AddVoid(methods[1].onClick, controller.RefreshScore);
            DemoWiring.AddVoid(methods[2].onClick, attributes.ResetScore);
            DemoWiring.AddVoid(methods[2].onClick, controller.RefreshScore);

            ctx.Set(controller, "_attributes", attributes);
            ctx.Set(controller, "_rigidbodiesValue", rigidbodies);
            ctx.Set(controller, "_spheresValue", spheres);
            ctx.Set(controller, "_cameraValue", camera);
            ctx.Set(controller, "_moneyValue", money);
            ctx.Set(controller, "_childrenValue", children);
            ctx.Set(controller, "_toggleValue", toggle);
            ctx.Set(controller, "_checkValue", check);
            ctx.Set(controller, "_scoreValue", score);
            ctx.Set(controller, "_log", log);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Editor · Inspector attributes",
                Purpose = "AttributeTest on 'Attribute' declares its references with attributes. Select it: the Neo " +
                          "inspector fills them, checks [RequireInterface], tints [GUIColor] rows and draws [Button] " +
                          "methods. The result is saved in the scene, so Play Mode does no lookups.",
                RuntimeApi = "The same lookups in C#: GetComponent<T>(), GetComponentsInChildren<T>(), " +
                             "Object.FindObjectsByType<T>(), FindFirstObjectByType<T>(). [Button] methods are plain methods.",
                SceneWorkflow = "Clear 'Cam Find In Scene' on Attribute: the inspector refills it at once and Edit > " +
                                "Undo History lists 'Find attributed component'. Neoxider/Settings > Enable Attribute " +
                                "Search turns auto-fill off.",
                Verification = "Play: " + CompareLabel + " reports 6 / 6 fields equal the C# queries; " + KickLabel +
                               " pushes the bodies of rbsFindAllInScene.",
                Hints = new[]
                {
                    "Actions on Attribute: set parameters, press Say or Get Money; output goes to the Console.",
                    "Select Attribute and [Demo] Inspector/" + SecondTargetName + ", press Add Score: both scores " +
                    "change, one Ctrl+Z reverts them."
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = InfoCardWidth,
                StartCollapsed = false
            };
        }

        /// <summary>Two-line row: attribute tag and field name on top, the resolved value below.</summary>
        private static TMP_Text FieldRow(DemoSceneBuildContext ctx, DemoPanel card, string field, string attributeTag,
            Color tagColor, string initial)
        {
            RectTransform row = card.Row("Field · " + field, 46f);
            TMP_Text caption = DemoSceneKit.Text(ctx, row, "Caption", Tag(attributeTag, tagColor) + "  " + field, 14f,
                DemoPalette.Muted, TextAlignmentOptions.TopLeft);
            DemoSceneKit.Anchor(caption.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 20f));
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.overflowMode = TextOverflowModes.Ellipsis;

            TMP_Text value = DemoSceneKit.Text(ctx, row, "Value", initial, 18f, DemoPalette.Text,
                TextAlignmentOptions.BottomLeft, FontStyles.Bold);
            DemoSceneKit.Anchor(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                Vector2.zero, new Vector2(0f, 24f));
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.overflowMode = TextOverflowModes.Ellipsis;
            // WHY: after Compare with C# the controller appends a verdict; the line shrinks instead of being cut.
            value.enableAutoSizing = true;
            value.fontSizeMin = 12f;
            value.fontSizeMax = 18f;
            return value;
        }

        private static void Paint(DemoSceneBuildContext ctx, Renderer renderer, string materialFile)
        {
            if (renderer == null)
            {
                return;
            }

            string path = DemoSampleRoots.Demo + "/" + MaterialFolder + materialFile;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null)
            {
                ctx.Warn("Demo material missing: " + path + "; '" + renderer.name + "' keeps its material.");
                return;
            }

            if (renderer.sharedMaterial != material)
            {
                renderer.sharedMaterial = material;
            }

            ctx.MarkModified(renderer);
        }

        private static string Tag(string text, Color color)
        {
            return "<color=#" + ColorUtility.ToHtmlStringRGB(color) + ">" + text + "</color>";
        }

        private static bool IsEmpty<T>(T[] items)
        {
            return items == null || items.Length == 0;
        }

        private static List<T> ToList<T>(T[] items) where T : UnityEngine.Object
        {
            return items != null ? new List<T>(items) : new List<T>();
        }

        private static T Step<T>(string step, Func<T> action)
        {
            return DemoSceneKit.Step(SceneName, step, action);
        }

        private static void Step(string step, Action action)
        {
            DemoSceneKit.Step(SceneName, step, action);
        }

        /// <summary>Stage slot of one hand-authored Rigidbody.</summary>
        internal readonly struct BodySlot
        {
            public BodySlot(string name, Vector3 position, string material)
            {
                Name = name;
                Position = position;
                Material = material;
            }

            public string Name { get; }
            public Vector3 Position { get; }
            public string Material { get; }
        }
    }
}
