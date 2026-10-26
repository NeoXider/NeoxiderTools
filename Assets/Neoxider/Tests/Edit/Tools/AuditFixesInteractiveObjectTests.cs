using System.Collections;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.ExceptionServices;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Native Update/physics plus the public pointer API under explicit screen-center policy.
    ///     Hover-off must retain pointer clicks while suppressing hover events. Physical mouse-button
    ///     delivery and cursor-only targeting are separate input contracts.
    /// </summary>
    [TestFixture]
    public class AuditFixesInteractiveObjectTests
    {
        private const string PlaySceneStateKey = "Audit20261023NeoEditorTests.InteractiveObject.PreviousPlayScene";
        private GameObject _cameraGo;
        private GameObject _targetGo;
        private InteractiveObject _interactive;
        private InteractionRayProvider _provider;
        private RenderTexture _cameraTexture;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            if (!Application.isPlaying)
            {
                ConfigureOwnedPlayScene();
                yield return new EnterPlayMode();
            }
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(SessionState.GetBool(PlaySceneStateKey + ".Owned", false), Is.True,
                "Native queries require this fixture's owned Play-start context.");
            _cameraGo = new GameObject("AuditFixesCamera");
            _cameraGo.tag = "MainCamera";
            Camera camera = _cameraGo.AddComponent<Camera>();
            _cameraTexture = new RenderTexture(256, 256, 0);
            camera.targetTexture = _cameraTexture;
            _provider = _cameraGo.AddComponent<InteractionRayProvider>();
            _provider.Mode = InteractionRayProvider.RayMode.ScreenCenter;
            _cameraGo.transform.position = new Vector3(1500f, 1500f, -10f);
            _cameraGo.transform.rotation = Quaternion.identity;

#if MIRROR
            _targetGo = new GameObject("AuditFixesInteractive", typeof(NetworkIdentity));
#else
            _targetGo = new GameObject("AuditFixesInteractive");
#endif
            _targetGo.SetActive(false);
            _targetGo.transform.position = new Vector3(1500f, 1500f, 10f);

            BoxCollider box = _targetGo.AddComponent<BoxCollider>();
            box.size = new Vector3(4f, 4f, 1f);

            _interactive = _targetGo.AddComponent<InteractiveObject>();
            _interactive.onClick = new UnityEvent();
            _interactive.onHoverEnter = new UnityEvent();
            _interactive.onHoverExit = new UnityEvent();

            // Arrange authored options before native Awake, without creating a foreign EventSystem.
            SetPrivateField(_interactive, "_autoCheckEventSystem", false);
            SetPrivateField(_interactive, "_autoCreateEventSystemIfMissing", false);
            _interactive.InteractionDistance = 0f;
            _interactive.DistanceCheckPoint = _cameraGo.transform;
            _interactive.IsNetworked = false;
            _interactive.UseMouseInteraction = true;
            _interactive.UseKeyboardInteraction = false;
            _interactive.UseHoverDetection = false;
            _interactive.UseScreenCenterRay = true;
            _targetGo.SetActive(true);
            Physics.SyncTransforms();
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            ExceptionDispatchInfo failure = null;
            try
            {
                try { if (_targetGo != null) Object.DestroyImmediate(_targetGo); }
                finally
                {
                    try { if (_cameraGo != null) Object.DestroyImmediate(_cameraGo); }
                    finally
                    {
                        if (_cameraTexture != null)
                        {
                            _cameraTexture.Release();
                            Object.DestroyImmediate(_cameraTexture);
                        }
                    }
                }
            }
            catch (System.Exception exception) { failure = ExceptionDispatchInfo.Capture(exception); }

            if (Application.isPlaying) yield return new ExitPlayMode();
            try { RestorePreviousPlayScene(); }
            catch (System.Exception exception)
            {
                failure = ExceptionDispatchInfo.Capture(failure == null
                    ? exception
                    : new System.AggregateException("Owned camera cleanup and Play-start restoration failed.",
                        failure.SourceException, exception));
            }
            Assert.That(Application.isPlaying, Is.False);
            failure?.Throw();
        }

        [UnityTest]
        public IEnumerator MouseTargetRaycast_RunsWithHoverDetectionOff()
        {
            _interactive.UseHoverDetection = false;
            AssertPositiveTarget();
            int clicks = 0;
            _interactive.onClick.AddListener(() => clicks++);
            yield return WaitForNativeUpdates();

            _interactive.OnPointerClick(LeftClick());
            Assert.That(clicks, Is.EqualTo(1), "The native target query must feed clicks with hover disabled.");

            _targetGo.GetComponent<Collider>().enabled = false;
            Physics.SyncTransforms();
            InteractionRayProvider.InvalidateQueries();
            yield return WaitForNativeUpdates();
            _interactive.OnPointerClick(LeftClick());
            Assert.That(clicks, Is.EqualTo(1), "Removing the real target must reject a later pointer click.");
        }

        [UnityTest]
        public IEnumerator PointerClick_FiresOnClick_WithHoverDetectionOff()
        {
            _interactive.UseHoverDetection = false;
            bool clicked = false;
            _interactive.onClick.AddListener(() => clicked = true);

            AssertPositiveTarget();
            yield return WaitForNativeUpdates();
            _interactive.OnPointerClick(LeftClick());

            Assert.IsTrue(clicked,
                "mouse interaction must be independent of hover detection, as the inspector tooltip promises");
        }

        [UnityTest]
        public IEnumerator HoverEvents_StayGated_WhenHoverDetectionIsOff()
        {
            _interactive.UseHoverDetection = false;
            bool hovered = false;
            _interactive.onHoverEnter.AddListener(() => hovered = true);

            AssertPositiveTarget();
            yield return WaitForNativeUpdates();

            Assert.IsFalse(hovered, "hover enter must not fire while hover detection is disabled");
            Assert.IsFalse(_interactive.IsHovered, "IsHovered must stay false while hover detection is disabled");
        }

        [UnityTest]
        public IEnumerator HoverDetectionOn_StillEntersHoverExactlyOnce()
        {
            _interactive.UseHoverDetection = true;
            int hoverEnters = 0;
            _interactive.onHoverEnter.AddListener(() => hoverEnters++);

            AssertPositiveTarget();
            yield return WaitForNativeUpdates();
            yield return WaitForNativeUpdates();

            Assert.IsTrue(_interactive.IsHovered, "hover detection must still report hover over the collider");
            Assert.AreEqual(1, hoverEnters, "hover enter must fire once while the pointer stays on the object");
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo field = target.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(target, value);
        }

        private void AssertPositiveTarget()
        {
            Assert.That(Camera.main, Is.SameAs(_cameraGo.GetComponent<Camera>()),
                "The owned camera must supply this native query context.");
            Assert.That(InteractionRayProvider.FindOnMainCamera(), Is.SameAs(_provider));
            Assert.That(_interactive.isActiveAndEnabled, Is.True);
            Assert.That(_provider.UseScreenCenterForHover, Is.True);
            Assert.That(_provider.UseScreenCenterForClick, Is.True);
            Assert.That(_provider.TryGetClickRay(out Ray ray), Is.True, "The configured center ray must exist.");
            Assert.That(Physics.Raycast(ray, out RaycastHit hit, 100f, ~0, QueryTriggerInteraction.Collide), Is.True);
            Assert.That(hit.collider, Is.SameAs(_targetGo.GetComponent<Collider>()));
        }

        private static PointerEventData LeftClick()
        {
            return new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
        }

        private static IEnumerator WaitForNativeUpdates()
        {
            int firstFrame = Time.frameCount;
            Stopwatch deadline = Stopwatch.StartNew();
            while (Time.frameCount - firstFrame < 2 && deadline.Elapsed.TotalSeconds < 10d)
                yield return null;
            Assert.That(Application.isPlaying, Is.True);
            Assert.That(Time.frameCount - firstFrame, Is.GreaterThanOrEqualTo(2),
                "The assertions require real PlayerLoop frames, not repeated Editor iterator ticks.");
        }

        private static void ConfigureOwnedPlayScene()
        {
            // A supported play-start scene selects the owned camera context without touching any
            // borrowed camera's tag/enabled state or the runtime's Camera.main selection policy.
            SceneAsset owned = Audit20261023NeoEditorTestsSceneAssets.EmptySceneAsset;
            SceneAsset previous = EditorSceneManager.playModeStartScene;
            string previousPath = previous == null ? string.Empty : AssetDatabase.GetAssetPath(previous);
            Assert.That(previous == null || !string.IsNullOrEmpty(previousPath), Is.True);
            Assert.That(SessionState.GetBool(PlaySceneStateKey + ".Owned", false), Is.False,
                "A previous owned Play-context restoration is still pending.");
            SessionState.SetString(PlaySceneStateKey, previousPath);
            SessionState.SetBool(PlaySceneStateKey + ".Owned", true);
            try
            {
                EditorSceneManager.playModeStartScene = owned;
                Assert.That(EditorSceneManager.playModeStartScene, Is.SameAs(owned));
            }
            catch
            {
                RestorePreviousPlayScene();
                throw;
            }
        }

        private static void RestorePreviousPlayScene()
        {
            if (!SessionState.GetBool(PlaySceneStateKey + ".Owned", false)) return;
            string previousPath = SessionState.GetString(PlaySceneStateKey, string.Empty);
            SceneAsset previous = string.IsNullOrEmpty(previousPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<SceneAsset>(previousPath);
            if (!string.IsNullOrEmpty(previousPath)) Assert.That(previous, Is.Not.Null, previousPath);
            EditorSceneManager.playModeStartScene = previous;
            Assert.That(EditorSceneManager.playModeStartScene, Is.SameAs(previous));
            SessionState.SetBool(PlaySceneStateKey + ".Owned", false);
            SessionState.EraseString(PlaySceneStateKey);
        }
    }
}
