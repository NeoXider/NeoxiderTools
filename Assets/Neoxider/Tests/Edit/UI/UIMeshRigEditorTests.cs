using System.Reflection;
using Neo.Editor;
using Neo.UI;
using Neo.UI.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Tests.UI
{
    public sealed class UIMeshRigEditorTests
    {
        [Test]
        public void Inspectors_UseSharedNeoxiderChrome()
        {
            Assert.That(typeof(CustomEditorBase).IsAssignableFrom(typeof(UIMeshRigGraphicEditor)), Is.True);
            Assert.That(typeof(CustomEditorBase).IsAssignableFrom(typeof(UIMeshRigPointEditor)), Is.True);
            Assert.That(typeof(CustomEditorBase).IsAssignableFrom(typeof(UIMeshRigPointMotionEditor)), Is.True);
        }

        [TearDown]
        public void TearDown()
        {
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
