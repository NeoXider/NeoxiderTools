using System;
using System.Collections.Generic;
using Neo.Demo.GridSystem;
using Neo.GridSystem;
using Neo.Samples;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Shared layout and clean-up of the GridSystem demo scenes (Match3, TicTacToe, Dice merge): the board is a
    ///     world object in the middle of the screen, the DemoSceneKit frame draws the header and log around it
    ///     without a backdrop, the controls sit in a card on the right and the info card in the top-left corner.
    ///     Board tiles are drawn by the runtime views from the live field, so every scene also gets an
    ///     "[Demo] Board Preview" root that shows the same layout in Edit Mode and hides itself on Play.
    /// </summary>
    internal static class GridSystemDemoStage
    {
        public const float CardWidth = 440f;
        public const float InfoWidth = 420f;
        public const string WorldRootKey = "World";
        public const string PreviewRootKey = "Board Preview";

        private const string LegacySetupName = "setup";
        private const string LegacyCanvasName = "Canvas";
        private const float LogLineHeight = 19f;

        /// <summary>Frame of every GridSystem demo: no backdrop (the board stays visible), card on the right.</summary>
        public static DemoFrameOptions FrameOptions(string title, Color accent, bool logFeed)
        {
            return new DemoFrameOptions(title, accent)
            {
                Overline = "NEOXIDER · GRIDSYSTEM DEMO",
                Backdrop = DemoBackdrop.None,
                Placement = DemoContentPlacement.Right,
                ContentWidth = CardWidth,
                ContentOffsetY = 0f,
                LogFeed = logFeed,
                LogLines = 5
            };
        }

        /// <summary>
        ///     The scene's hand-authored board component when it has one; otherwise a generated
        ///     "[Demo] World/<paramref name="objectName" />" host with it (empty scenes, tests).
        /// </summary>
        public static T FindOrCreateBoard<T>(DemoSceneBuildContext ctx, string objectName) where T : Component
        {
            T existing = ctx.Find<T>();
            if (existing != null)
            {
                return existing;
            }

            GameObject host = ctx.Child(ctx.Root(WorldRootKey).transform, objectName);
            ctx.Note("No hand-authored " + typeof(T).Name + "; authored '" + DemoSceneBuildContext.PathOf(host.transform) +
                     "'.");
            return ctx.Ensure<T>(host);
        }

        /// <summary>Puts the hand-authored or generated board host at a fixed world pose.</summary>
        public static void PlaceHost(DemoSceneBuildContext ctx, GameObject host, Vector3 position)
        {
            Transform transform = host.transform;
            transform.SetPositionAndRotation(position, Quaternion.identity);
            transform.localScale = Vector3.one;
            ctx.MarkModified(transform);
        }

        /// <summary>
        ///     Deletes the pre-kit bootstrap objects: the "setup" root whose runtime setup script was removed (only
        ///     missing scripts left) and the old "Canvas" whose direct children all match <paramref name="isLegacyChild" />.
        ///     A canvas with other children is kept and reported.
        /// </summary>
        public static void RetireBootstrap(DemoSceneBuildContext ctx, Predicate<string> isLegacyChild)
        {
            List<GameObject> retire = new();
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (DemoSceneBuildContext.IsGenerated(root))
                {
                    continue;
                }

                if (root.name == LegacySetupName && IsSetupLeftover(root))
                {
                    retire.Add(root);
                    continue;
                }

                if (root.name != LegacyCanvasName || root.GetComponent<Canvas>() == null || root.transform.childCount == 0)
                {
                    continue;
                }

                string unknown = null;
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    string childName = root.transform.GetChild(i).name;
                    if (!isLegacyChild(childName))
                    {
                        unknown = childName;
                        break;
                    }
                }

                if (unknown == null)
                {
                    retire.Add(root);
                }
                else
                {
                    ctx.Warn("Kept hand-authored '" + root.name + "': it holds '" + unknown +
                             "', which is not part of the pre-kit demo UI. Delete it by hand if it is a leftover.");
                }
            }

            foreach (GameObject legacy in retire)
            {
                ctx.Retire(legacy, "pre-kit bootstrap object; the demo UI is authored by the builder");
            }
        }

        /// <summary>Deletes runtime-only view objects that an earlier edit-time rebuild saved under a hand-authored view.</summary>
        public static void RetireViewLeftovers(DemoSceneBuildContext ctx, Transform viewHost, Predicate<string> isRuntimeChild)
        {
            if (viewHost == null || DemoSceneBuildContext.IsGenerated(viewHost.gameObject))
            {
                return;
            }

            List<GameObject> stale = new();
            for (int i = 0; i < viewHost.childCount; i++)
            {
                Transform child = viewHost.GetChild(i);
                if (isRuntimeChild(child.name) && !DemoSceneBuildContext.IsGenerated(child.gameObject))
                {
                    stale.Add(child.gameObject);
                }
            }

            foreach (GameObject node in stale)
            {
                Object.DestroyImmediate(node);
            }

            if (stale.Count > 0)
            {
                ctx.Note("Removed " + stale.Count + " saved runtime view object(s) under '" +
                         DemoSceneBuildContext.PathOf(viewHost) + "'; the view recreates them on Play.");
                ctx.MarkModified(viewHost.gameObject);
            }
        }

        /// <summary>Orthographic scene camera at a fixed pose, cleared to the accent-tinted demo background.</summary>
        public static void FrameCamera(DemoSceneBuildContext ctx, Camera camera, Vector3 position, float orthographicSize,
            Color accent)
        {
            if (camera == null)
            {
                throw new InvalidOperationException("the scene has no camera to frame the board.");
            }

            camera.transform.SetPositionAndRotation(position, Quaternion.identity);
            camera.orthographic = true;
            camera.orthographicSize = orthographicSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = DemoPalette.CameraBackground(accent);
            ctx.MarkModified(camera);
            ctx.MarkModified(camera.transform);
        }

        /// <summary>"[Demo] Board Preview": edit-mode picture of the runtime board, hidden on Play.</summary>
        public static Transform PreviewRoot(DemoSceneBuildContext ctx)
        {
            GameObject root = ctx.Root(PreviewRootKey);
            root.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            root.transform.localScale = Vector3.one;
            ctx.Ensure<GridDemoEditModePreview>(root);
            return root.transform;
        }

        /// <summary>World sprite child at a world position, sized in world units (9-sliced sprites are drawn sliced).</summary>
        public static SpriteRenderer WorldSprite(DemoSceneBuildContext ctx, Transform parent, string name, Sprite sprite,
            Color color, Vector3 worldPosition, Vector2 size, int sortingOrder)
        {
            if (sprite == null)
            {
                throw new InvalidOperationException("sprite for '" + name + "' is missing.");
            }

            GameObject host = ctx.Child(parent, name);
            host.transform.SetPositionAndRotation(worldPosition, Quaternion.identity);
            SpriteRenderer renderer = ctx.Ensure<SpriteRenderer>(host);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
            if (sprite.border != Vector4.zero)
            {
                renderer.drawMode = SpriteDrawMode.Sliced;
                renderer.size = size;
                host.transform.localScale = Vector3.one;
                return renderer;
            }

            Vector2 bounds = sprite.bounds.size;
            renderer.drawMode = SpriteDrawMode.Simple;
            host.transform.localScale = new Vector3(size.x / Mathf.Max(bounds.x, 0.0001f),
                size.y / Mathf.Max(bounds.y, 0.0001f), 1f);
            return renderer;
        }

        /// <summary>World-space TextMeshPro label (stays visible in Play Mode when placed under "[Demo] World").</summary>
        public static TextMeshPro WorldLabel(DemoSceneBuildContext ctx, Transform parent, string name, string text,
            float fontSize, Color color, Vector3 worldPosition, Vector2 size, int sortingOrder)
        {
            RectTransform rect = ctx.UiChild(parent, name);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = size;
            rect.SetPositionAndRotation(worldPosition, Quaternion.identity);
            DemoSceneKit.SyncAnchoredPosition(rect);
            rect.localScale = Vector3.one;
            ctx.MarkModified(rect);
            TextMeshPro label = ctx.Ensure<TextMeshPro>(rect.gameObject);
            DemoSceneKit.StyleText(label, text, fontSize, color, TextAlignmentOptions.Center, FontStyles.Bold);
            label.sortingOrder = sortingOrder;
            return label;
        }

        /// <summary>Single status line in a card (ellipsis instead of wrapping into the next row).</summary>
        public static TMP_Text StatusLine(DemoPanel panel, string initial)
        {
            TMP_Text status = panel.Label(initial, 19f);
            status.color = DemoPalette.Text;
            return status;
        }

        /// <summary>
        ///     <see cref="DemoPanel.ValueLabel" /> whose value may be long ("8×8 cut corners · 60 cells"): the value
        ///     gets three quarters of the row and shrinks to one line instead of wrapping over the caption.
        /// </summary>
        public static TMP_Text WideValue(DemoPanel panel, string caption, string initial)
        {
            TMP_Text value = panel.ValueLabel(caption, initial);
            RectTransform rect = value.rectTransform;
            rect.anchorMin = new Vector2(0.25f, rect.anchorMin.y);
            value.textWrappingMode = TextWrappingModes.NoWrap;
            value.enableAutoSizing = true;
            value.fontSizeMin = 13f;
            value.fontSizeMax = 22f;
            value.fontSize = 22f;
            return value;
        }

        /// <summary>Log feed inside a card (scenes whose bottom edge is used by the board).</summary>
        public static DemoLogFeed CardLog(DemoSceneBuildContext ctx, DemoPanel panel, int lines)
        {
            RectTransform row = panel.Row("Log", lines * LogLineHeight + 4f);
            TMP_Text text = DemoSceneKit.Text(ctx, row, "LogText", "› ready", 14f, DemoPalette.Text,
                TextAlignmentOptions.TopLeft);
            DemoSceneKit.Stretch(text.rectTransform);
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            DemoLogFeed log = ctx.Ensure<DemoLogFeed>(row.gameObject);
            ctx.Set(log, "_text", text);
            ctx.Set(log, "_maxLines", lines);
            ctx.Set(log, "_lineColor", DemoPalette.Text);
            return log;
        }

        /// <summary>Authors the info card on the frame (top-left, always shown).</summary>
        public static void InfoCard(DemoSceneBuildContext ctx, DemoFrame frame, ModuleDemoSceneInfo info,
            DemoInfoContent content)
        {
            content.ShowRuntimeOverlay = true;
            content.Corner = ModuleDemoSceneInfo.Corner.TopLeft;
            content.PanelWidth = InfoWidth;
            content.StartCollapsed = false;
            frame.InfoCard(info, content);
        }

        /// <summary>Asset by GUID (GUIDs survive the UPM install, paths do not); throws naming the scene when missing.</summary>
        public static T RequireAsset<T>(string guid, string label, string scene) where T : Object
        {
            T asset = LoadAsset<T>(guid);
            if (asset == null)
            {
                throw new InvalidOperationException(scene + ": " + label + " (guid " + guid + ") was not found; " +
                                                    "reimport com.neoxider.tools.");
            }

            return asset;
        }

        public static T LoadAsset<T>(string guid) where T : Object
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<T>(path);
        }

        /// <summary>Component on the root of a prefab asset found by GUID.</summary>
        public static T RequirePrefabComponent<T>(string guid, string label, string scene) where T : Component
        {
            GameObject prefab = RequireAsset<GameObject>(guid, label, scene);
            T component = prefab.GetComponent<T>();
            if (component == null)
            {
                throw new InvalidOperationException(scene + ": prefab '" + AssetDatabase.GetAssetPath(prefab) + "' has no " +
                                                    typeof(T).Name + " on its root.");
            }

            return component;
        }

        /// <summary>Generates the field in Edit Mode so cell positions are known (no listeners run outside Play Mode).</summary>
        public static void GenerateForAuthoring(DemoSceneBuildContext ctx, FieldGenerator generator)
        {
            generator.GenerateField();
            if (generator.Cells == null || generator.Cells.Length == 0)
            {
                throw new InvalidOperationException("FieldGenerator on '" + DemoSceneBuildContext.PathOf(generator.transform) +
                                                    "' generated no cells (check Config.Size).");
            }

            ctx.MarkModified(generator);
        }

        private static bool IsSetupLeftover(GameObject root)
        {
            if (root.transform.childCount > 0)
            {
                return false;
            }

            foreach (Component component in root.GetComponents<Component>())
            {
                if (component == null || component is Transform)
                {
                    continue;
                }

                if (!component.GetType().Name.EndsWith("DemoSetup", StringComparison.Ordinal))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
