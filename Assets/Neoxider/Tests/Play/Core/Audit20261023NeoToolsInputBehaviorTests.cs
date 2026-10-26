using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tests.Play
{
    public partial class Audit20261023NeoToolsInputBehaviorTests
    {
        private readonly List<GameObject> _objects = new();
        private MouseInputManager _manager;
        private Camera _camera;
        private GameObject _target;
        private Vector2 _screenPosition;
        private bool _savedAutoCreate;

        [SetUp]
        public void SetUp()
        {
            _savedAutoCreate = MouseInputManager.CreateInstance;
            MouseInputManager.CreateInstance = false;
            _camera = CreateObject("Audit Input Camera").AddComponent<Camera>();
            _camera.transform.position = new Vector3(12345f, 12345f, -10f);
            _camera.orthographic = true;
            _camera.orthographicSize = 5f;
            _screenPosition = _camera.pixelRect.center;

            _manager = CreateObject("Audit Mouse Manager").AddComponent<MouseInputManager>();
            JsonUtility.FromJsonOverwrite("{\"useMainCameraFallback\":false}", _manager);
            _manager.SetTargetCamera(_camera);
            Assert.That(MouseInputManager.I, Is.SameAs(_manager), "Run in an isolated Play Mode test scene.");

            _target = CreateObject("Audit Click Target");
            _target.transform.position = new Vector3(12345f, 12345f, 0f);
            _target.AddComponent<BoxCollider>();
            Physics.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                Object.DestroyImmediate(_objects[i]);
            }

            _objects.Clear();
            MouseInputManager.CreateInstance = _savedAutoCreate;
            MouseInputManager.ResetSubsystemPollingState();
        }

        [Test]
        public void DisablingPressEvents_PreservesHoldClickAndPollingData()
        {
            _manager.enablePress = false;
            _manager.enableHold = true;
            int presses = 0;
            int holds = 0;
            int clicks = 0;
            int clicksIn = 0;
            _manager.OnPress += data => presses++;
            _manager.OnHold += data => holds++;
            _manager.OnClick += data =>
            {
                clicks++;
                Assert.That(data.HitObject, Is.SameAs(_target));
            };
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) =>
            {
                clicksIn++;
                Assert.That(data.ScreenPosition, Is.EqualTo(_screenPosition));
            };

            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);

            Assert.That(presses, Is.Zero);
            Assert.That(holds, Is.EqualTo(1));
            Assert.That(clicks, Is.EqualTo(1));
            Assert.That(clicksIn, Is.EqualTo(1));
            Assert.That(MouseInputManager.HasEventData, Is.True);
            Assert.That(MouseInputManager.LastEventData.HitObject, Is.SameAs(_target));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void InterruptedMouseGesture_CannotClickAfterRecovery(bool loseCamera)
        {
            int clicks = 0;
            _manager.OnClick += data => clicks++;
            _manager.ProcessMouseState(_screenPosition, true, true, false);

            if (loseCamera)
            {
                _manager.SetTargetCamera(null);
                _manager.ProcessMouseState(_screenPosition, false, true, false);
                _manager.SetTargetCamera(_camera);
            }
            else
            {
                _manager.enabled = false;
                _manager.enabled = true;
            }

            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(clicks, Is.Zero);

            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(clicks, Is.EqualTo(1));
        }

        [Test]
        public void MouseEffect_DisableHidesVisualsAndBalancesFollowEvents()
        {
            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);

            _manager.ProcessMouseState(_screenPosition, true, true, false);
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);

            effect.enabled = false;
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));

            effect.enabled = true;
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(stops, Is.EqualTo(1), "An unmatched release must not emit a second stop.");
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(starts, Is.EqualTo(2));
            Assert.That(stops, Is.EqualTo(2));
        }

        [Test]
        public void MouseEffect_ReleaseWhileNotInteractableStillStopsVisuals()
        {
            MouseEffect effect = CreateEffect();
            int stops = 0;
            effect.onStopFollow.AddListener(() => stops++);
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            effect.interactable = false;
            _manager.ProcessMouseState(_screenPosition, false, false, true);

            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            Assert.That(stops, Is.EqualTo(1));
        }

        [Test]
        public void MouseEffect_FirstHoldSpawnsOnceWhenRepeatIntervalIsZero()
        {
            MouseEffect effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Hold;
            effect.spawnDuringHold = true;
            effect.holdInterval = 0f;
            _manager.enableHold = true;
            int spawns = 0;
            effect.onSpawn.AddListener(() => spawns++);

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, false, true, false);
            Assert.That(spawns, Is.EqualTo(1));
            Assert.That(effect.transform.childCount, Is.EqualTo(1));

            _manager.ProcessMouseState(_screenPosition, false, true, false);
            Assert.That(spawns, Is.EqualTo(2));
            Assert.That(effect.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void Swipe_SecondaryFingerCannotReplaceGestureOrigin()
        {
            SwipeController controller = CreateObject("Audit Swipe").AddComponent<SwipeController>();
            controller.detectSwipeOnlyAfterRelease = true;
            List<SwipeData> swipes = new();
            controller.OnSwipe.AddListener(swipes.Add);

            controller.ProcessTouch(1, Vector2.zero, TouchPhase.Began);
            controller.ProcessTouch(2, new Vector2(500f, 500f), TouchPhase.Began);
            controller.ProcessTouch(2, new Vector2(600f, 500f), TouchPhase.Moved);
            controller.ProcessTouch(1, new Vector2(60f, 0f), TouchPhase.Ended);
            controller.ProcessTouch(2, new Vector2(600f, 500f), TouchPhase.Ended);

            Assert.That(swipes.Count, Is.EqualTo(1));
            Assert.That(swipes[0].Direction, Is.EqualTo(SwipeDirection.Right));
            Assert.That(swipes[0].StartPosition, Is.EqualTo(Vector2.zero));
            Assert.That(swipes[0].EndPosition, Is.EqualTo(new Vector2(60f, 0f)));
        }

        [Test]
        public void Swipe_CancelAllowsNextGestureInTheSameDirection()
        {
            SwipeController controller = CreateObject("Audit Swipe").AddComponent<SwipeController>();
            List<SwipeData> swipes = new();
            controller.OnSwipe.AddListener(swipes.Add);

            controller.ProcessTouch(1, Vector2.zero, TouchPhase.Began);
            controller.ProcessTouch(1, new Vector2(60f, 0f), TouchPhase.Moved);
            controller.ProcessTouch(1, new Vector2(60f, 0f), TouchPhase.Canceled);
            controller.ProcessTouch(2, new Vector2(100f, 0f), TouchPhase.Began);
            controller.ProcessTouch(2, new Vector2(160f, 0f), TouchPhase.Moved);

            Assert.That(swipes.Count, Is.EqualTo(2));
            Assert.That(swipes[1].Direction, Is.EqualTo(SwipeDirection.Right));
            Assert.That(swipes[1].StartPosition, Is.EqualTo(new Vector2(100f, 0f)));
        }

        [Test]
        public void Swipe_RequiresObservedPressAndCancelsOnDisable()
        {
            SwipeController controller = CreateObject("Audit Swipe").AddComponent<SwipeController>();
            controller.detectSwipeOnlyAfterRelease = true;
            List<SwipeData> swipes = new();
            controller.OnSwipe.AddListener(swipes.Add);
            controller.ProcessMouseState(new Vector2(100f, 0f), false, true, false);
            controller.ProcessMouseState(new Vector2(100f, 0f), false, false, true);
            Assert.That(swipes, Is.Empty);

            controller.ProcessMouseState(Vector2.zero, true, true, false);
            controller.enabled = false;
            controller.enabled = true;
            controller.ProcessMouseState(new Vector2(100f, 0f), false, false, true);
            Assert.That(swipes, Is.Empty);

            controller.ProcessMouseState(Vector2.zero, true, true, false);
            controller.ProcessMouseState(new Vector2(100f, 0f), false, false, true);
            Assert.That(swipes.Count, Is.EqualTo(1));
            Assert.That(SwipeController.GetSwipeDirection(out SwipeDirection direction), Is.True);
            Assert.That(direction, Is.EqualTo(SwipeDirection.Right));
            Assert.That(SwipeController.GetSwipeDirection(out _), Is.False);
        }

        [TestCase(100f, 20f, 30f, 25f, SwipeDirection.Up)]
        [TestCase(20f, 100f, 25f, 30f, SwipeDirection.Right)]
        public void Swipe_DirectionMustMeetItsOwnThreshold(float thresholdX, float thresholdY, float endX, float endY,
            SwipeDirection expectedDirection)
        {
            SwipeController controller = CreateObject("Audit Swipe").AddComponent<SwipeController>();
            string settings = "{\"minDistanceForSwipeX\":" + thresholdX.ToString(System.Globalization.CultureInfo.InvariantCulture) +
                              ",\"minDistanceForSwipeY\":" + thresholdY.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";
            JsonUtility.FromJsonOverwrite(settings, controller);
            controller.detectSwipeOnlyAfterRelease = true;
            List<SwipeData> swipes = new();
            controller.OnSwipe.AddListener(swipes.Add);
            controller.ProcessTouch(1, Vector2.zero, TouchPhase.Began);
            controller.ProcessTouch(1, new Vector2(endX, endY), TouchPhase.Moved);
            Assert.That(swipes, Is.Empty);
            controller.ProcessTouch(1, new Vector2(endX, endY), TouchPhase.Ended);

            Assert.That(swipes.Count, Is.EqualTo(1));
            Assert.That(swipes[0].Direction, Is.EqualTo(expectedDirection));
        }

        [TestCase("disable")]
        [TestCase("camera")]
        [TestCase("focus")]
        [TestCase("pause")]
        [TestCase("destroy")]
        public void ManagerCancellation_StopsSubscribedEffectOnceWithoutReleaseOrSpawn(string interruption)
        {
            MouseEffect effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Release Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Release;
            int canceled = 0;
            int stopped = 0;
            int released = 0;
            int clicked = 0;
            int spawned = 0;
            _manager.OnCanceled += () => canceled++;
            _manager.OnRelease += data => released++;
            _manager.OnClick += data => clicked++;
            effect.onStopFollow.AddListener(() => stopped++);
            effect.onSpawn.AddListener(() => spawned++);
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            Assert.That(effect.followObject.activeSelf, Is.True);

            switch (interruption)
            {
                case "disable": _manager.enabled = false; break;
                case "camera":
                    _manager.SetTargetCamera(null);
                    _manager.ProcessMouseState(_screenPosition, false, true, false);
                    _manager.ProcessMouseState(_screenPosition, false, true, false);
                    break;
                case "focus": _manager.SendMessage("OnApplicationFocus", false); break;
                case "pause": _manager.SendMessage("OnApplicationPause", true); break;
                case "destroy": Object.DestroyImmediate(_manager.gameObject); break;
            }

            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            Assert.That(released, Is.Zero);
            Assert.That(clicked, Is.Zero);
            Assert.That(spawned, Is.Zero);
            Assert.That(effect.transform.childCount, Is.Zero);
        }

        [Test]
        public void LiveFocusCancellation_EarlierObserverDisablesManagerBeforeLaterEffectCleanup()
        {
            MouseEffect effect = null;
            int canceled = 0;
            int started = 0;
            int stopped = 0;
            int released = 0;
            int releasedIn = 0;
            int clicked = 0;
            int clickedIn = 0;
            int spawned = 0;
            List<string> order = new();
            _manager.OnCanceled += () =>
            {
                canceled++;
                order.Add("canceled");
                Assert.That(_manager.isActiveAndEnabled, Is.True);
                Assert.That(effect.followObject.activeSelf, Is.True);
                Assert.That(effect.trail.emitting, Is.True);
                Assert.That(stopped, Is.Zero);
                // This live native setter enters OnDisable while the first cancel batch is active.
                _manager.enabled = false;
                Assert.That(_manager.enabled, Is.False);
                Assert.That(canceled, Is.EqualTo(1));
                Assert.That(stopped, Is.Zero, "The later effect subscriber must still own cleanup.");
                order.Add("disabled");
            };
            effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Nested Cancellation Release Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Release;
            effect.onStartFollow.AddListener(() => started++);
            effect.onStopFollow.AddListener(() =>
            {
                stopped++;
                order.Add("stopped");
            });
            effect.onSpawn.AddListener(() => spawned++);
            _manager.OnCanceled += () => order.Add("late-canceled");
            _manager.OnRelease += data => released++;
            _manager.OnReleaseIn += (in MouseInputManager.MouseEventData data) => releasedIn++;
            _manager.OnClick += data => clicked++;
            _manager.OnClickIn += (in MouseInputManager.MouseEventData data) => clickedIn++;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            Assert.That(started, Is.EqualTo(1));
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));

            _manager.SendMessage("OnApplicationFocus", false);
            _manager.SendMessage("OnApplicationFocus", false);

            CollectionAssert.AreEqual(new[] { "canceled", "disabled", "stopped", "late-canceled" }, order);
            Assert.That(canceled, Is.EqualTo(1));
            Assert.That(started, Is.EqualTo(1));
            Assert.That(stopped, Is.EqualTo(1));
            Assert.That(effect.isActiveAndEnabled, Is.True);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            Assert.That(released, Is.Zero);
            Assert.That(releasedIn, Is.Zero);
            Assert.That(clicked, Is.Zero);
            Assert.That(clickedIn, Is.Zero);
            Assert.That(spawned, Is.Zero);
            Assert.That(effect.transform.childCount, Is.Zero);
        }

        [Test]
        public void OrdinaryRelease_DoesNotPublishCancellation()
        {
            int cancellations = 0;
            int releases = 0;
            _manager.OnCanceled += () => cancellations++;
            _manager.OnRelease += data => releases++;
            _manager.ProcessMouseState(_screenPosition, true, true, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            _manager.enabled = false;
            Assert.That(releases, Is.EqualTo(1));
            Assert.That(cancellations, Is.Zero);
        }

        [Test]
        public void SpawnCallback_DisablingEffectCannotPublishAnOrphanStart()
        {
            MouseEffect effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Press Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Press;
            int starts = 0;
            int stops = 0;
            bool interrupted = false;
            effect.onStartFollow.AddListener(() => starts++);
            effect.onStopFollow.AddListener(() => stops++);
            effect.onSpawn.AddListener(() =>
            {
                if (!interrupted)
                {
                    interrupted = true;
                    effect.enabled = false;
                }
            });

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            Assert.That(starts, Is.Zero);
            Assert.That(stops, Is.Zero);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);

            effect.enabled = true;
            Assert.That(starts, Is.Zero);
            _manager.ProcessMouseState(_screenPosition, true, false, false);
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
        }

        [Test]
        public void StartCallback_DisablingEffectBalancesTheStartedFollow()
        {
            MouseEffect effect = CreateEffect();
            int starts = 0;
            int stops = 0;
            effect.onStartFollow.AddListener(() =>
            {
                starts++;
                effect.enabled = false;
            });
            effect.onStopFollow.AddListener(() => stops++);

            _manager.ProcessMouseState(_screenPosition, true, false, false);
            Assert.That(starts, Is.EqualTo(1));
            Assert.That(stops, Is.EqualTo(1));
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
        }

        [Test]
        public void SpawnCallback_NewIndependentPressSupersedesOuterContinuation()
        {
            MouseEffect effect = CreateEffect();
            effect.spawnPrefab = CreateObject("Audit Reentrant Spawn Source");
            effect.spawnTrigger = MouseEffect.SpawnTrigger.Press;
            bool nested = false;
            int starts = 0;
            effect.onStartFollow.AddListener(() => starts++);
            effect.onSpawn.AddListener(() =>
            {
                if (!nested)
                {
                    nested = true;
                    _manager.ProcessMouseState(_screenPosition + new Vector2(30f, 10f), true, false, false);
                }
            });

            _manager.ProcessMouseState(_screenPosition, true, false, false);

            Assert.That(starts, Is.EqualTo(1), "Only the replacement gesture may publish start.");
            Assert.That(effect.followObject.activeSelf, Is.True);
            Assert.That(effect.trail.emitting, Is.True);
            Assert.That(effect.followObject.transform.position, Is.EqualTo(MouseInputManager.LastEventData.WorldPosition));
            Assert.That(effect.transform.childCount, Is.EqualTo(2));
        }

        [Test]
        public void FollowerActivationCallback_CannotResumeAnInvalidatedPress()
        {
            MouseEffect effect = CreateEffect();
            Audit20261023NeoToolsInputVisualHook hook =
                effect.followObject.AddComponent<Audit20261023NeoToolsInputVisualHook>();
            hook.Activated = () => effect.interactable = false;
            int starts = 0;
            effect.onStartFollow.AddListener(() => starts++);

            _manager.ProcessMouseState(_screenPosition, true, false, false);

            Assert.That(starts, Is.Zero);
            Assert.That(effect.followObject.activeSelf, Is.False);
            Assert.That(effect.trail.emitting, Is.False);
            effect.interactable = true;
            _manager.ProcessMouseState(_screenPosition, false, false, true);
            Assert.That(starts, Is.Zero);
        }

        private MouseEffect CreateEffect()
        {
            GameObject owner = CreateObject("Audit Mouse Effect");
            owner.SetActive(false);
            MouseEffect effect = owner.AddComponent<MouseEffect>();
            effect.trail = CreateObject("Audit Trail").AddComponent<TrailRenderer>();
            effect.followObject = CreateObject("Audit Follower");
            effect.onStartFollow = new UnityEvent();
            effect.onStopFollow = new UnityEvent();
            effect.onSpawn = new UnityEvent();
            effect.SetTargetCamera(_camera);
            owner.SetActive(true);
            return effect;
        }

        private GameObject CreateObject(string objectName)
        {
            GameObject created = new(objectName);
            _objects.Add(created);
            return created;
        }
    }
}
