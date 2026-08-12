using System.Collections;
using System.Collections.Generic;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Neo.Tests.Play
{
    public sealed class UIMeshRigPlayModeTests
    {
        private readonly List<Object> _ownedObjects = new List<Object>();
        private float _originalTimeScale;

        [SetUp]
        public void SetUp()
        {
            _originalTimeScale = Time.timeScale;
        }

        [TearDown]
        public void TearDown()
        {
            Time.timeScale = _originalTimeScale;

            for (int index = _ownedObjects.Count - 1; index >= 0; index--)
            {
                Object ownedObject = _ownedObjects[index];
                if (ownedObject != null)
                {
                    Object.DestroyImmediate(ownedObject);
                }
            }

            _ownedObjects.Clear();
        }

        [UnityTest]
        public IEnumerator RuntimeSprite_OnOverlayCanvas_PopulatesMeshAndTransformPoseDeforms()
        {
            Canvas canvas = CreateOverlayCanvas("RuntimeSpriteCanvas");
            RigBundle bundle = CreateRig(canvas.transform, "RuntimeSpriteRig");

            yield return null;
            Canvas.ForceUpdateCanvases();

            Mesh renderedMesh = bundle.Graphic.canvasRenderer.GetMesh();
            Assert.That(bundle.Graphic.mainTexture, Is.SameAs(bundle.Sprite.texture));
            Assert.That(renderedMesh.vertexCount, Is.GreaterThan(0),
                "A runtime-created Sprite should produce a renderable uGUI mesh on an overlay Canvas.");

            Vector2 normalizedCenter = bundle.Point.RestCenterNormalized;
            Vector2 before = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            bundle.PointRect.anchoredPosition += new Vector2(24f, -6f);

            yield return null;

            Vector2 after = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            Assert.That(after.x - before.x, Is.EqualTo(24f).Within(0.05f));
            Assert.That(after.y - before.y, Is.EqualTo(-6f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator DeformedMeshRaycast_AcceptsVisibleSpriteAndRejectsLetterboxArea()
        {
            Canvas canvas = CreateOverlayCanvas("RaycastCanvas");
            RigBundle bundle = CreateRig(canvas.transform, "RaycastRig");
            bundle.Graphic.raycastTarget = true;
            bundle.Graphic.SetPreserveAspect(true);
            bundle.Graphic.SetRaycastMode(UIMeshRigRaycastMode.DeformedMesh);

            yield return null;
            Canvas.ForceUpdateCanvases();

            Vector2 center = RectTransformUtility.WorldToScreenPoint(null, bundle.Graphic.rectTransform.position);
            Vector2 letterbox = center + new Vector2(80f, 0f);
            Assert.That(bundle.Graphic.Raycast(center, null), Is.True);
            Assert.That(bundle.Graphic.Raycast(letterbox, null), Is.False,
                "The clickable area should follow the rendered mesh, not the full letterboxed RectTransform.");
        }

        [UnityTest]
        public IEnumerator InteractiveRig_ButtonReceivesEventSystemClick_AndOverflowRemainsClickable()
        {
            Canvas canvas = CreateOverlayCanvas("InteractiveCanvas");
            GameObject eventSystemObject = Track(new GameObject("EventSystem", typeof(EventSystem)));
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            RigBundle bundle = CreateRig(canvas.transform, "InteractiveRig");
            bundle.Graphic.SetPreserveAspect(false);
            bundle.Graphic.SetRaycastMode(UIMeshRigRaycastMode.DeformedMesh);
            Button button = bundle.Graphic.gameObject.AddComponent<Button>();
            button.targetGraphic = bundle.Graphic;
            int clickCount = 0;
            button.onClick.AddListener(() => clickCount++);

            bundle.Point.SetInfluenceRadii(new Vector2(0.8f, 0.8f), Vector2.one);
            bundle.PointRect.anchoredPosition = new Vector2(120f, 0f);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Vector2 deformedCenter = RectTransformUtility.WorldToScreenPoint(
                null,
                bundle.Graphic.rectTransform.TransformPoint(new Vector3(120f, 0f, 0f)));
            Assert.That(bundle.Graphic.Raycast(deformedCenter, null), Is.True,
                "Auto-expanded padding should keep visible deformation outside the original Rect clickable.");

            PointerEventData pointer = new PointerEventData(eventSystem) { position = deformedCenter };
            List<RaycastResult> results = new List<RaycastResult>();
            eventSystem.RaycastAll(pointer, results);
            Assert.That(results.Exists(result => result.gameObject == bundle.Graphic.gameObject), Is.True);
            ExecuteEvents.Execute(bundle.Graphic.gameObject, pointer, ExecuteEvents.pointerClickHandler);
            Assert.That(clickCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RemovingSprite_ClearsDeformedRaycastCacheAndRestoresConfiguredPadding()
        {
            Canvas canvas = CreateOverlayCanvas("ClearedSpriteCanvas");
            RigBundle bundle = CreateRig(canvas.transform, "ClearedSpriteRig");
            bundle.Graphic.SetPreserveAspect(false);
            bundle.Graphic.SetRaycastMode(UIMeshRigRaycastMode.DeformedMesh);
            Vector4 configuredPadding = new Vector4(1f, 2f, 3f, 4f);
            bundle.Graphic.SetInteractionRaycastPadding(configuredPadding);
            bundle.Point.SetInfluenceRadii(new Vector2(0.8f, 0.8f), Vector2.one);
            bundle.PointRect.anchoredPosition = new Vector2(120f, 0f);

            yield return null;
            Canvas.ForceUpdateCanvases();

            Vector2 oldDeformedCenter = RectTransformUtility.WorldToScreenPoint(
                null,
                bundle.Graphic.rectTransform.TransformPoint(new Vector3(120f, 0f, 0f)));
            Assert.That(bundle.Graphic.Raycast(oldDeformedCenter, null), Is.True);
            Assert.That(bundle.Graphic.raycastPadding, Is.Not.EqualTo(configuredPadding));

            bundle.Graphic.SetSource(null, Color.white);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(bundle.Graphic.canvasRenderer.GetMesh().vertexCount, Is.Zero);
            Assert.That(bundle.Graphic.raycastPadding, Is.EqualTo(configuredPadding));
            Assert.That(bundle.Graphic.Raycast(oldDeformedCenter, null), Is.False,
                "A graphic with no Sprite must not retain the previous deformed mesh as a hit area.");

            bundle.Graphic.SetRaycastMode(UIMeshRigRaycastMode.Rect);
            Assert.That(bundle.Graphic.Raycast(oldDeformedCenter, null), Is.False,
                "Rect hit testing must not make a graphic with no rendered Sprite interactive.");
        }

        [UnityTest]
        public IEnumerator PointMotion_PresetAdvancesWithUnscaledTime_AndStopOrDisableRestoresIdentity()
        {
            Canvas canvas = CreateOverlayCanvas("MotionCanvas");
            RigBundle bundle = CreateRig(canvas.transform, "MotionRig");
            UIMeshRigPointMotion motion = bundle.Point.gameObject.AddComponent<UIMeshRigPointMotion>();
            motion.Stop();
            motion.PlayOnEnable = false;
            motion.UseUnscaledTime = true;
            motion.Speed = 20f;
            motion.Phase = 0f;

            Vector2 normalizedCenter = bundle.Point.RestCenterNormalized;
            Vector2 identity = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            motion.ApplyPreset(UIMeshRigMotionPreset.Float);
            Time.timeScale = 0f;
            motion.Restart();
            Vector2 first = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);

            yield return new WaitForSecondsRealtime(0.05f);

            Vector2 advanced = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            Assert.That(motion.CurrentTime, Is.GreaterThan(0f));
            Assert.That(Vector2.Distance(advanced, first), Is.GreaterThan(0.01f),
                "A preset motion configured for unscaled time should advance while Time.timeScale is zero.");

            motion.Stop();
            Vector2 afterStop = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            Assert.That(afterStop.x, Is.EqualTo(identity.x).Within(0.001f));
            Assert.That(afterStop.y, Is.EqualTo(identity.y).Within(0.001f));
            Assert.That(motion.CurrentPose.Position, Is.EqualTo(Vector2.zero));
            Assert.That(motion.CurrentPose.Scale, Is.EqualTo(Vector2.one));

            motion.Restart();
            motion.SetTime(0.21f);
            Assert.That(Vector2.Distance(
                    bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter),
                    identity),
                Is.GreaterThan(0.01f));

            motion.enabled = false;
            Vector2 afterDisable = bundle.Graphic.CalculateDeformedLocalPoint(normalizedCenter);
            Assert.That(afterDisable.x, Is.EqualTo(identity.x).Within(0.001f));
            Assert.That(afterDisable.y, Is.EqualTo(identity.y).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator AnimationClipRectTransformPose_ComposesAdditivelyWithProceduralMotion()
        {
            Canvas canvas = CreateOverlayCanvas("AnimatorCanvas");
            RigBundle bundle = CreateRig(canvas.transform, "AnimatorRig");
            Animator animator = bundle.Point.gameObject.AddComponent<Animator>();
            UIMeshRigPointMotion motion = bundle.Point.gameObject.AddComponent<UIMeshRigPointMotion>();
            motion.Stop();
            motion.PlayOnEnable = false;

            AnimationClip clip = Track(new AnimationClip());
            clip.name = "PointRectTransformPose";
            clip.SetCurve(
                string.Empty,
                typeof(RectTransform),
                "m_AnchoredPosition.x",
                AnimationCurve.Constant(0f, 1f, 18f));
            clip.SampleAnimation(bundle.Point.gameObject, 0.5f);

            UIMeshRigMotionProfile profile = motion.Profile;
            profile.duration = 1f;
            profile.positionAmplitudePixels = new Vector2(7f, 3f);
            profile.positionX = AnimationCurve.Constant(0f, 1f, 1f);
            profile.positionY = AnimationCurve.Constant(0f, 1f, 1f);
            profile.rotation = AnimationCurve.Constant(0f, 1f, 0f);
            profile.scaleX = AnimationCurve.Constant(0f, 1f, 0f);
            profile.scaleY = AnimationCurve.Constant(0f, 1f, 0f);
            motion.EvaluateAt(0f);

            yield return null;

            Vector2 result = bundle.Graphic.CalculateDeformedLocalPoint(bundle.Point.RestCenterNormalized);
            Assert.That(animator, Is.Not.Null,
                "The point remains a regular Animator-compatible RectTransform target.");
            Assert.That(bundle.PointRect.anchoredPosition.x, Is.EqualTo(18f).Within(0.01f),
                "The AnimationClip should own the authored RectTransform pose.");
            Assert.That(result.x, Is.EqualTo(25f).Within(0.05f),
                "Procedural X motion should be added after the AnimationClip's RectTransform pose.");
            Assert.That(result.y, Is.EqualTo(3f).Within(0.05f));
        }

        [UnityTest]
        public IEnumerator CanvasScalerAndRectResize_KeepDirectPointAtCapturedNormalizedPosition()
        {
            Canvas canvas = CreateOverlayCanvas("ResponsiveCanvas");
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            RigBundle bundle = CreateRig(canvas.transform, "ResponsiveRig");

            bundle.PointRect.anchoredPosition = new Vector2(52f, -17f);
            bundle.Point.CaptureRestPose(bundle.Graphic);
            Vector2 expectedNormalized = bundle.Point.RestCenterNormalized;

            bundle.Graphic.rectTransform.sizeDelta = new Vector2(520f, 310f);
            scaler.referenceResolution = new Vector2(1440f, 2560f);
            Canvas.ForceUpdateCanvases();
            yield return null;

            Vector2 actualNormalized = bundle.Graphic.WorldToNormalized(bundle.PointRect.position);
            Assert.That(bundle.PointRect.anchorMin.x, Is.EqualTo(expectedNormalized.x).Within(0.001f));
            Assert.That(bundle.PointRect.anchorMin.y, Is.EqualTo(expectedNormalized.y).Within(0.001f));
            Assert.That(bundle.PointRect.anchorMax, Is.EqualTo(bundle.PointRect.anchorMin));
            Assert.That(actualNormalized.x, Is.EqualTo(expectedNormalized.x).Within(0.001f));
            Assert.That(actualNormalized.y, Is.EqualTo(expectedNormalized.y).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator MaskAndRectMask2D_PreserveRenderingAcrossDisableEnableLifecycle()
        {
            Canvas canvas = CreateOverlayCanvas("MaskCanvas");

            GameObject maskObject = CreateUiObject("StencilMask", canvas.transform, new Vector2(360f, 260f));
            Image maskImage = maskObject.AddComponent<Image>();
            maskImage.color = Color.white;
            Mask mask = maskObject.AddComponent<Mask>();
            mask.showMaskGraphic = false;

            GameObject rectMaskObject = CreateUiObject(
                "RectMask",
                maskObject.transform,
                new Vector2(300f, 220f));
            RectMask2D rectMask = rectMaskObject.AddComponent<RectMask2D>();
            RigBundle bundle = CreateRig(rectMaskObject.transform, "MaskedRig");
            bundle.Graphic.rectTransform.sizeDelta = new Vector2(240f, 160f);

            yield return null;
            Canvas.ForceUpdateCanvases();

            AssertRenderedMesh(bundle.Graphic, "Both uGUI mask types should allow the rig to populate its mesh.");
            Assert.That(mask, Is.Not.Null);
            Assert.That(rectMask, Is.Not.Null);

            rectMaskObject.SetActive(false);
            yield return null;
            rectMaskObject.SetActive(true);
            yield return null;
            Canvas.ForceUpdateCanvases();

            Assert.That(bundle.Graphic.isActiveAndEnabled, Is.True);
            AssertRenderedMesh(bundle.Graphic,
                "Re-enabling a hierarchy containing Mask and RectMask2D should restore mesh rendering.");

            Vector2 before = bundle.Graphic.CalculateDeformedLocalPoint(bundle.Point.RestCenterNormalized);
            bundle.PointRect.anchoredPosition += new Vector2(9f, 5f);
            yield return null;
            Vector2 after = bundle.Graphic.CalculateDeformedLocalPoint(bundle.Point.RestCenterNormalized);
            Assert.That(after.x - before.x, Is.EqualTo(9f).Within(0.05f));
            Assert.That(after.y - before.y, Is.EqualTo(5f).Within(0.05f));
        }

        private Canvas CreateOverlayCanvas(string name)
        {
            GameObject canvasObject = Track(new GameObject(
                name,
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster)));
            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            return canvas;
        }

        private RigBundle CreateRig(Transform parent, string name)
        {
            GameObject rigObject = CreateUiObject(name, parent, new Vector2(200f, 100f));
            CanvasRenderer canvasRenderer = rigObject.AddComponent<CanvasRenderer>();
            UIMeshRigGraphic graphic = rigObject.AddComponent<UIMeshRigGraphic>();
            Sprite sprite = CreateRuntimeSprite(name + "Sprite");
            graphic.SetSource(sprite, Color.white);
            graphic.SetGridResolution(8, 6);

            GameObject pointObject = CreateUiObject("Point", rigObject.transform, Vector2.zero);
            RectTransform pointRect = (RectTransform)pointObject.transform;
            pointRect.anchorMin = new Vector2(0.5f, 0.5f);
            pointRect.anchorMax = new Vector2(0.5f, 0.5f);
            pointRect.anchoredPosition = Vector2.zero;
            UIMeshRigPoint point = pointObject.AddComponent<UIMeshRigPoint>();
            point.RadiusNormalized = new Vector2(0.35f, 0.35f);
            point.Falloff = 0.5f;
            point.CaptureRestPose(graphic);
            graphic.NotifyPointChanged();

            Assert.That(canvasRenderer, Is.SameAs(graphic.canvasRenderer));
            return new RigBundle(graphic, point, pointRect, sprite);
        }

        private Sprite CreateRuntimeSprite(string name)
        {
            Texture2D texture = Track(new Texture2D(8, 8, TextureFormat.RGBA32, false));
            texture.name = name + "Texture";
            Color32[] pixels = new Color32[64];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = new Color32(255, 255, 255, 255);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
            Sprite sprite = Track(Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f));
            sprite.name = name;
            return sprite;
        }

        private GameObject CreateUiObject(string name, Transform parent, Vector2 size)
        {
            GameObject uiObject = new GameObject(name, typeof(RectTransform));
            RectTransform rectTransform = (RectTransform)uiObject.transform;
            rectTransform.SetParent(parent, false);
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.sizeDelta = size;
            return uiObject;
        }

        private void AssertRenderedMesh(UIMeshRigGraphic graphic, string message)
        {
            Mesh mesh = graphic.canvasRenderer.GetMesh();
            Assert.That(mesh.vertexCount, Is.GreaterThan(0), message);
        }

        private T Track<T>(T ownedObject) where T : Object
        {
            _ownedObjects.Add(ownedObject);
            return ownedObject;
        }

        private readonly struct RigBundle
        {
            public RigBundle(
                UIMeshRigGraphic graphic,
                UIMeshRigPoint point,
                RectTransform pointRect,
                Sprite sprite)
            {
                Graphic = graphic;
                Point = point;
                PointRect = pointRect;
                Sprite = sprite;
            }

            public UIMeshRigGraphic Graphic { get; }
            public UIMeshRigPoint Point { get; }
            public RectTransform PointRect { get; }
            public Sprite Sprite { get; }
        }
    }
}
