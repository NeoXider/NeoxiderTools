using Neo.UI;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoUIViewTests
    {
        [Test]
        public void VariantView_AddStateCopy_DoesNotHideSharedObjectForEarlierState()
        {
            GameObject root = new GameObject("Variant audit");
            try
            {
                GameObject visual = new GameObject("Shared visual");
                visual.transform.SetParent(root.transform);
                VariantView view = root.AddComponent<VariantView>();
                SerializedObject settings = new SerializedObject(view);
                settings.FindProperty("_isBuildPhase").boolValue = true;
                SerializedProperty variants = settings.FindProperty("_objectVariants");
                variants.arraySize = 1;
                SerializedProperty objects = variants.GetArrayElementAtIndex(0).FindPropertyRelative("objects");
                objects.arraySize = 1;
                objects.GetArrayElementAtIndex(0).objectReferenceValue = visual;
                settings.ApplyModifiedPropertiesWithoutUndo();

                view.AddState(2);
                view.SetCurrentState(0);

                Assert.That(view.MaxStates, Is.EqualTo(2));
                Assert.That(view.currentStateIndex, Is.Zero);
                Assert.That(visual.activeSelf, Is.True);
                view.SetCurrentState(1);
                Assert.That(visual.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VariantView_EmptyInspectorConfiguration_KeepsZeroIndex()
        {
            GameObject root = new GameObject("Empty variant audit");
            try
            {
                VariantView view = root.AddComponent<VariantView>();
                SerializedObject settings = new SerializedObject(view);
                settings.FindProperty("_currentStateIndex").intValue = 5;
                settings.ApplyModifiedPropertiesWithoutUndo();

                Assert.That(view.MaxStates, Is.Zero);
                Assert.That(view.currentStateIndex, Is.Zero);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void VisualToggle_StateEventsObserveUpdatedReactiveValue()
        {
            GameObject root = new GameObject("Toggle event audit");
            try
            {
                VisualToggle toggle = root.AddComponent<VisualToggle>();
                toggle.On = new UnityEvent();
                toggle.Off = new UnityEvent();
                bool valueSeenByOn = false;
                bool valueSeenByOff = true;
                toggle.On.AddListener(() => valueSeenByOn = toggle.ValueBool);
                toggle.Off.AddListener(() => valueSeenByOff = toggle.ValueBool);

                toggle.Set(true);
                toggle.Set(false);

                Assert.That(valueSeenByOn, Is.True);
                Assert.That(valueSeenByOff, Is.False);
                Assert.That(toggle.IsActive, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AnimationFly_StaticConversionsUseExplicitCanvasCamera()
        {
            GameObject root = new GameObject("Conversion audit");
            try
            {
                GameObject cameraObject = new GameObject("Untagged canvas camera", typeof(Camera));
                cameraObject.transform.SetParent(root.transform);
                Camera camera = cameraObject.GetComponent<Camera>();
                camera.transform.position = new Vector3(3f, 2f, -10f);
                GameObject canvasObject = new GameObject("Canvas", typeof(RectTransform), typeof(Canvas));
                canvasObject.transform.SetParent(root.transform);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
                canvas.planeDistance = 5f;
                Vector3 point = new Vector3(1f, 1f, 0f);
                Vector2 expectedScreen = RectTransformUtility.WorldToScreenPoint(camera, point);

                Vector2 actualScreen = AnimationFly.WorldToCanvasPosition(point, canvas);
                Assert.That(Vector2.Distance(actualScreen, expectedScreen), Is.LessThan(0.001f));

                RectTransformUtility.ScreenPointToWorldPointInRectangle(
                    (RectTransform)canvas.transform, expectedScreen, camera, out Vector3 expectedWorld);
                Vector3 actualWorld = AnimationFly.CanvasToWorldPosition(expectedScreen, canvas);
                Assert.That(Vector3.Distance(actualWorld, expectedWorld), Is.LessThan(0.001f));

                Camera overrideCamera = root.AddComponent<Camera>();
                overrideCamera.transform.position = new Vector3(-7f, 4f, -20f);
                Vector2 expectedOverride = RectTransformUtility.WorldToScreenPoint(overrideCamera, point);
                Assert.That(Vector2.Distance(AnimationFly.WorldToCanvasPosition(point, canvas, overrideCamera),
                    expectedOverride), Is.LessThan(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
