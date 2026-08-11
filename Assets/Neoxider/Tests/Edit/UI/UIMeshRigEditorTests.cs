using System.Reflection;
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

            InvokeMenu("ConvertImage", new MenuCommand(source));

            UIMeshRigGraphic rig = source.GetComponentInChildren<UIMeshRigGraphic>(true);
            Assert.That(rig, Is.Not.Null);
            Assert.That(source.enabled, Is.False);
            Assert.That(rig.Sprite, Is.SameAs(sprite));
            Assert.That(rig.color, Is.EqualTo(source.color));
            Assert.That(rig.PreserveAspect, Is.True);
            Assert.That(rig.maskable, Is.False);
            Assert.That(rig.raycastTarget, Is.False);
            Assert.That(rig.raycastPadding, Is.EqualTo(source.raycastPadding));

            Object.DestroyImmediate(sprite);
            Object.DestroyImmediate(texture);
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
