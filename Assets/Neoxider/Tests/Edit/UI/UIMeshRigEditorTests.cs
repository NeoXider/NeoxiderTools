using System.Reflection;
using Neo.Editor;
using Neo.UI;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using PanelSettings = UnityEngine.UIElements.PanelSettings;
using UIDocument = UnityEngine.UIElements.UIDocument;

namespace Neo.Tests.UI
{
    public sealed class UIMeshRigEditorTests
    {
        private static readonly System.Type[] RigEditors =
        {
            typeof(UIMeshRigGraphicEditor),
            typeof(UIMeshRigPointEditor),
            typeof(UIMeshRigPointMotionEditor),
            typeof(UIMeshRigWorldRendererEditor),
            typeof(UIMeshRigSpriteRendererEditor),
            typeof(UIMeshRigUIToolkitHostEditor)
        };

        private static readonly System.Type[] RigComponents =
        {
            typeof(UIMeshRigGraphic),
            typeof(UIMeshRigWorldRenderer),
            typeof(UIMeshRigSpriteRenderer),
            typeof(UIMeshRigUIToolkitHost),
            typeof(UIMeshRigPoint),
            typeof(UIMeshRigPointMotion)
        };

        [Test]
        public void Inspectors_UseSharedNeoxiderChrome()
        {
            for (int index = 0; index < RigEditors.Length; index++)
            {
                Assert.That(typeof(CustomEditorBase).IsAssignableFrom(RigEditors[index]), Is.True,
                    RigEditors[index].Name + " must derive from CustomEditorBase.");
            }
        }

        // WHY: the rig inspectors once drew every field by hand and lost the whole package inspector system
        // at once — collapsible [Header] sections with counts, ON/OFF switches, coloured rails — and buried
        // Raycast Target / Raycast Padding / Maskable inside a collapsed foldout. Fields belong to attributes.
        [Test]
        public void Inspectors_LeaveFieldDrawingToTheAttributeDrivenBase()
        {
            MethodInfo baseDraw = typeof(CustomEditorBase).GetMethod(
                "DrawCustomNeoxiderInspectorGUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            PropertyInfo useCustom = typeof(CustomEditorBase).GetProperty(
                "UseCustomNeoxiderInspectorGUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(baseDraw, Is.Not.Null);
            Assert.That(useCustom, Is.Not.Null);

            for (int index = 0; index < RigEditors.Length; index++)
            {
                System.Type editorType = RigEditors[index];
                MethodInfo declared = editorType.GetMethod(
                    "DrawCustomNeoxiderInspectorGUI",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(declared, Is.Not.Null);
                Assert.That(declared.DeclaringType, Is.EqualTo(typeof(CustomEditorBase)),
                    editorType.Name + " overrides DrawCustomNeoxiderInspectorGUI — fields must come from " +
                    "[Header]/[Tooltip] through CustomEditorBase instead.");

                UnityEditor.Editor editorInstance = (UnityEditor.Editor)ScriptableObject.CreateInstance(editorType);
                try
                {
                    Assert.That((bool)useCustom.GetValue(editorInstance), Is.False,
                        editorType.Name + " must not opt out of the shared property pass.");
                }
                finally
                {
                    Object.DestroyImmediate(editorInstance);
                }
            }
        }

        [Test]
        public void RigComponents_DescribeEveryVisibleFieldWithHeaderAndTooltip()
        {
            for (int typeIndex = 0; typeIndex < RigComponents.Length; typeIndex++)
            {
                System.Type componentType = RigComponents[typeIndex];
                FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic);
                bool hasHeader = false;
                for (int index = 0; index < fields.Length; index++)
                {
                    FieldInfo field = fields[index];
                    // WHY: only fields the module itself declares. Inherited uGUI state (m_Material,
                    // m_Color, m_RaycastTarget, m_Maskable) belongs to Graphic and cannot be annotated here —
                    // it is drawn by the same default pass and is visible, which is the point.
                    if (field.DeclaringType != componentType ||
                        field.GetCustomAttribute<SerializeField>() == null ||
                        field.GetCustomAttribute<HideInInspector>() != null)
                    {
                        continue;
                    }

                    hasHeader |= field.GetCustomAttribute<HeaderAttribute>() != null;
                    Assert.That(field.GetCustomAttribute<TooltipAttribute>(), Is.Not.Null,
                        componentType.Name + "." + field.Name +
                        " is visible in the Inspector but has no [Tooltip]; the custom editor no longer " +
                        "supplies GUIContent by hand.");
                }

                Assert.That(hasHeader, Is.True,
                    componentType.Name + " has no [Header], so CustomEditorBase cannot build sections for it.");
            }
        }

        [TearDown]
        public void TearDown()
        {
            UIMeshRigUIToolkitHost[] toolkitHosts = Object.FindObjectsByType<UIMeshRigUIToolkitHost>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < toolkitHosts.Length; index++)
            {
                Object.DestroyImmediate(toolkitHosts[index].gameObject);
            }

            UIMeshRigWorldRenderer[] worldRigs = Object.FindObjectsByType<UIMeshRigWorldRenderer>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < worldRigs.Length; index++)
            {
                Object.DestroyImmediate(worldRigs[index].gameObject);
            }

            UIMeshRigGraphic[] rigs = Object.FindObjectsByType<UIMeshRigGraphic>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < rigs.Length; index++)
            {
                Object.DestroyImmediate(rigs[index].gameObject);
            }

            Canvas[] canvases = Object.FindObjectsByType<Canvas>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < canvases.Length; index++)
            {
                Object.DestroyImmediate(canvases[index].gameObject);
            }

            EventSystem[] eventSystems = Object.FindObjectsByType<EventSystem>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);
            for (int index = 0; index < eventSystems.Length; index++)
            {
                Object.DestroyImmediate(eventSystems[index].gameObject);
            }
        }

        [Test]
        public void GameObjectMenu_CreatesVisibleRigUnderCanvas()
        {
            InvokeMenu("Create", new MenuCommand(null));

            UIMeshRigGraphic rig = Object.FindFirstObjectByType<UIMeshRigGraphic>(FindObjectsInactive.Include);
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponentInParent<Canvas>(), Is.Not.Null);
            Assert.That(rig.rectTransform.sizeDelta, Is.EqualTo(new Vector2(300f, 300f)));
            Assert.That(rig.Sprite, Is.Not.Null, "The one-click menu must create a visible example, not an empty graphic.");
            Assert.That(rig.Points.Count, Is.EqualTo(1));
            UIMeshRigPointMotion motion = rig.Points[0].GetComponent<UIMeshRigPointMotion>();
            Assert.That(motion, Is.Not.Null);
            Assert.That(motion.Preset, Is.EqualTo(UIMeshRigMotionPreset.SquashStretch));
            Assert.That(motion.PreviewInEditMode, Is.True);
        }

        [Test]
        public void WorldMenu_CreatesVisibleReadyToAnimateMeshRig()
        {
            InvokeMenu("CreateWorld", new MenuCommand(null));

            UIMeshRigWorldRenderer rig = Object.FindFirstObjectByType<UIMeshRigWorldRenderer>(
                FindObjectsInactive.Include);
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.GetComponent<MeshFilter>(), Is.Not.Null);
            Assert.That(rig.GetComponent<MeshRenderer>(), Is.Not.Null);
            Assert.That(rig.Sprite, Is.Not.Null);
            Assert.That(rig.Size, Is.EqualTo(new Vector2(3f, 3f)));
            Assert.That(rig.Points.Count, Is.EqualTo(4));
            Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
        }

        [Test]
        public void UIToolkitMenu_CreatesReadyHostAndPanelSettings()
        {
            const string panelSettingsPath = "Assets/Neoxider UI Mesh Rig Panel Settings.asset";
            const string generatedThemeFolder = "Assets/UI Toolkit";
            bool panelSettingsExisted = AssetDatabase.LoadAssetAtPath<PanelSettings>(panelSettingsPath) != null;
            bool generatedThemeFolderExisted = AssetDatabase.IsValidFolder(generatedThemeFolder);
            try
            {
                InvokeMenu("CreateUIToolkit", new MenuCommand(null));

                UIMeshRigUIToolkitHost host = Object.FindFirstObjectByType<UIMeshRigUIToolkitHost>(
                    FindObjectsInactive.Include);
                Assert.That(host, Is.Not.Null);
                Assert.That(host.Element, Is.Not.Null);
                Assert.That(host.Sprite, Is.Not.Null);
                Assert.That(host.LayoutPreset, Is.EqualTo(UIMeshRigLayoutPreset.Character));

                // WHY: from Unity 6.4 world-space UI Toolkit renders through PanelRenderer, so the menu
                // creates that; UIDocument is only produced on editors that have no PanelRenderer at all.
#if UNITY_6000_4_OR_NEWER
                Assert.That(host.HostKind, Is.EqualTo(UIMeshRigPanelHostKind.PanelRenderer));
                Assert.That(host.GetComponent<UIDocument>(), Is.Null,
                    "A migrated project must not be forced to carry the legacy UIDocument.");
#else
                Assert.That(host.HostKind, Is.EqualTo(UIMeshRigPanelHostKind.UIDocument));
                Assert.That(host.GetComponent<UIDocument>(), Is.Not.Null);
                Assert.That(host.GetComponent<UIDocument>().panelSettings, Is.Not.Null);
#endif
            }
            finally
            {
                if (!panelSettingsExisted)
                {
                    AssetDatabase.DeleteAsset(panelSettingsPath);
                }

                if (!generatedThemeFolderExisted && AssetDatabase.IsValidFolder(generatedThemeFolder))
                {
                    AssetDatabase.DeleteAsset(generatedThemeFolder);
                }
            }
        }

        [Test]
        public void SpriteRendererMenu_CreatesVisibleReadyToAnimateSpriteRig()
        {
            InvokeMenu("CreateSpriteRenderer", new MenuCommand(null));

            UIMeshRigSpriteRenderer rig = Object.FindFirstObjectByType<UIMeshRigSpriteRenderer>(
                FindObjectsInactive.Include);
            try
            {
                Assert.That(rig, Is.Not.Null);
                Assert.That(rig.GetComponent<SpriteRenderer>(), Is.Not.Null);
                Assert.That(rig.Sprite, Is.Not.Null, "The one-click menu must create a visible example.");
                Assert.That(rig.Points.Count, Is.EqualTo(1));
                Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Pose));
                Assert.That(rig.Points[0].GetComponent<UIMeshRigPointMotion>(), Is.Not.Null);
            }
            finally
            {
                if (rig != null)
                {
                    Object.DestroyImmediate(rig.gameObject);
                }
            }
        }

        [Test]
        public void SimpleImageConversion_PreservesSupportedGraphicContract()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject sourceObject = new GameObject(
                "Source",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            sourceObject.transform.SetParent(canvasObject.transform, false);
            Image source = sourceObject.GetComponent<Image>();
            Texture2D texture = new Texture2D(4, 4);
            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, 4f, 4f), new Vector2(0.5f, 0.5f));
            source.overrideSprite = sprite;
            source.color = new Color(0.3f, 0.5f, 0.7f, 0.8f);
            source.preserveAspect = true;
            source.maskable = false;
            source.raycastTarget = false;
            source.raycastPadding = new Vector4(1f, 2f, 3f, 4f);
            Color expectedColor = source.color;
            Vector4 expectedPadding = source.raycastPadding;

            InvokeMenu("ConvertImage", new MenuCommand(source));

            UIMeshRigGraphic rig = sourceObject.GetComponent<UIMeshRigGraphic>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(sourceObject.GetComponent<Image>(), Is.Null);
            Assert.That(rig.Sprite, Is.SameAs(sprite));
            Assert.That(rig.color, Is.EqualTo(expectedColor));
            Assert.That(rig.PreserveAspect, Is.True);
            Assert.That(rig.maskable, Is.False);
            Assert.That(rig.raycastTarget, Is.False);
            Assert.That(rig.raycastPadding, Is.EqualTo(expectedPadding));
            Assert.That(rig.preferredWidth, Is.EqualTo(4f).Within(0.001f));
            Assert.That(rig.preferredHeight, Is.EqualTo(4f).Within(0.001f));
            Assert.That(rig.Points.Count, Is.EqualTo(1));
            Assert.That(rig.Points[0].GetComponent<UIMeshRigPointMotion>(), Is.Not.Null);

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
        }

        [Test]
        public void InteractiveImageConversion_KeepsButtonAndRetargetsItsGraphic()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject sourceObject = new GameObject(
                "Interactive",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            sourceObject.transform.SetParent(canvasObject.transform, false);
            Image source = sourceObject.GetComponent<Image>();
            Button button = sourceObject.GetComponent<Button>();
            button.targetGraphic = source;

            InvokeMenu("ConvertImage", new MenuCommand(source));

            UIMeshRigGraphic rig = sourceObject.GetComponent<UIMeshRigGraphic>();
            Assert.That(rig, Is.Not.Null);
            Assert.That(sourceObject.GetComponent<Button>(), Is.SameAs(button));
            Assert.That(button.targetGraphic, Is.SameAs(rig));
            Assert.That(rig.raycastTarget, Is.True);
        }

        [Test]
        public void NonDestructiveImageConversion_KeepsInteractionVisible_AndSingleUndoRestoresSource()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject sourceObject = new GameObject(
                "Interactive",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));
            sourceObject.transform.SetParent(canvasObject.transform, false);
            Image source = sourceObject.GetComponent<Image>();
            Button button = sourceObject.GetComponent<Button>();
            button.targetGraphic = source;

            Undo.IncrementCurrentGroup();
            int conversionGroup = Undo.GetCurrentGroup();
            InvokeMenu("CreateNonDestructiveChild", new MenuCommand(source));
            Undo.CollapseUndoOperations(conversionGroup);

            UIMeshRigGraphic rig = sourceObject.GetComponentInChildren<UIMeshRigGraphic>(true);
            Assert.That(rig, Is.Not.Null);
            Assert.That(source.enabled, Is.False);
            Assert.That(button.targetGraphic, Is.SameAs(rig),
                "A Button must tint and interact with the visible child, not the hidden source Image.");

            Undo.PerformUndo();

            Assert.That(source, Is.Not.Null);
            Assert.That(source.enabled, Is.True);
            Assert.That(button.targetGraphic, Is.SameAs(source));
            Assert.That(sourceObject.GetComponentInChildren<UIMeshRigGraphic>(true), Is.Null);
        }

        [Test]
        public void NonDestructiveImageConversion_PreservesDisabledRenderingState()
        {
            GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
            GameObject sourceObject = new GameObject(
                "Disabled Image",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));
            sourceObject.transform.SetParent(canvasObject.transform, false);
            Image source = sourceObject.GetComponent<Image>();
            source.enabled = false;

            InvokeMenu("CreateNonDestructiveChild", new MenuCommand(source));

            UIMeshRigGraphic rig = sourceObject.GetComponentInChildren<UIMeshRigGraphic>(true);
            Assert.That(rig, Is.Not.Null);
            Assert.That(rig.enabled, Is.False,
                "Converting a hidden Image must not unexpectedly make its artwork visible.");
        }

        [Test]
        public void EditModePreview_DoesNotWritePointTransform_WhenTimeOrBindAnchorChanges()
        {
            GameObject rigObject = new GameObject(
                "Preview Rig",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UIMeshRigGraphic));
            GameObject pointObject = new GameObject(
                "Preview Point",
                typeof(RectTransform),
                typeof(UIMeshRigPoint),
                typeof(UIMeshRigPointMotion));
            pointObject.transform.SetParent(rigObject.transform, false);
            UIMeshRigGraphic rig = rigObject.GetComponent<UIMeshRigGraphic>();
            UIMeshRigPoint point = pointObject.GetComponent<UIMeshRigPoint>();
            UIMeshRigPointMotion motion = pointObject.GetComponent<UIMeshRigPointMotion>();
            RectTransform pointTransform = (RectTransform)pointObject.transform;

            try
            {
                rig.NotifyPointChanged();
                point.CaptureRestPose(rig);
                motion.ApplyPreset(UIMeshRigMotionPreset.Float);
                Vector3 authoredPosition = new Vector3(37f, -19f, 0f);
                Quaternion authoredRotation = Quaternion.Euler(0f, 0f, 23f);
                Vector3 authoredScale = new Vector3(1.2f, 0.8f, 1f);
                pointTransform.localPosition = authoredPosition;
                pointTransform.localRotation = authoredRotation;
                pointTransform.localScale = authoredScale;
                pointTransform.hasChanged = false;

                UIMeshRigMotionPreviewDriver.StartPreview(motion);
                UIMeshRigMotionPreviewDriver.TickForTests(0.35f);
                UIMeshRigEditorUtility.SetRestCenterPreservingPose(rig, point, new Vector2(0.75f, 0.2f));

                Assert.That(pointTransform.localPosition, Is.EqualTo(authoredPosition));
                Assert.That(pointTransform.localRotation, Is.EqualTo(authoredRotation));
                Assert.That(pointTransform.localScale, Is.EqualTo(authoredScale));
                Assert.That(rig.AuthoringMode, Is.EqualTo(UIMeshRigAuthoringMode.Setup),
                    "A transient preview must not serialize a switch to Pose mode.");

                UIMeshRigMotionPreviewDriver.StopPreview(motion);

                Assert.That(pointTransform.localPosition, Is.EqualTo(authoredPosition));
                Assert.That(pointTransform.localRotation, Is.EqualTo(authoredRotation));
                Assert.That(pointTransform.localScale, Is.EqualTo(authoredScale));
                Assert.That(motion.CurrentPose.Position, Is.EqualTo(Vector2.zero));
                Assert.That(motion.CurrentPose.RotationDegrees, Is.EqualTo(0f));
                Assert.That(motion.CurrentPose.Scale, Is.EqualTo(Vector2.one));
            }
            finally
            {
                UIMeshRigMotionPreviewDriver.StopAllPreviews();
                Object.DestroyImmediate(rigObject);
            }
        }

        [Test]
        public void EditModePreview_UnsubscribesEditorUpdate_WhenLastPreviewStops()
        {
            GameObject pointObject = new GameObject(
                "Preview Subscription Point",
                typeof(RectTransform),
                typeof(UIMeshRigPoint),
                typeof(UIMeshRigPointMotion));
            UIMeshRigPointMotion motion = pointObject.GetComponent<UIMeshRigPointMotion>();

            try
            {
                UIMeshRigMotionPreviewDriver.StopAllPreviews();
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.False);

                UIMeshRigMotionPreviewDriver.StartPreview(motion);

                Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(1));
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.True);

                UIMeshRigMotionPreviewDriver.StopPreview(motion);

                Assert.That(UIMeshRigMotionPreviewDriver.ActivePreviewCount, Is.EqualTo(0));
                Assert.That(UIMeshRigMotionPreviewDriver.IsUpdateSubscribed, Is.False,
                    "No EditorApplication.update callback may survive the last stopped preview.");
            }
            finally
            {
                UIMeshRigMotionPreviewDriver.StopAllPreviews();
                Object.DestroyImmediate(pointObject);
            }
        }

        private static void InvokeMenu(string methodName, MenuCommand command)
        {
            MethodInfo method = typeof(UIMeshRigMenu).GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, "Menu method " + methodName + " was not found.");
            method.Invoke(null, new object[] { command });
        }
    }
}
