using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoToolsMovePhysics2DTests
    {
        private Scene _scene;
        private PhysicsScene2D _physicsScene;
        private GameObject _owner;
        private Rigidbody2D _body;
        private bool _enteredPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _enteredPlayMode = false;
            Assert.That(Application.isPlaying, Is.False, "This Edit fixture must own its Play transition.");
            yield return new EnterPlayMode();
            _enteredPlayMode = true;
            Assert.That(Application.isPlaying, Is.True);
            _scene = SceneManager.CreateScene("Audit20261023NeoToolsMovePhysics2D",
                new CreateSceneParameters(LocalPhysicsMode.Physics2D));
            _physicsScene = _scene.GetPhysicsScene2D();
            Assert.That(_physicsScene.IsValid(), Is.True);
            Assert.That(_physicsScene, Is.Not.EqualTo(Physics2D.defaultPhysicsScene));
            _owner = new GameObject("Audit20261023NeoToolsMoveBody");
            SceneManager.MoveGameObjectToScene(_owner, _scene);
            _body = _owner.AddComponent<Rigidbody2D>();
            _body.gravityScale = 0f;
            _body.mass = 1f;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (!_enteredPlayMode)
            {
                yield break;
            }

            if (_owner != null)
            {
                Object.DestroyImmediate(_owner);
            }

            bool sceneReleased = true;
            if (_scene.IsValid() && _scene.isLoaded)
            {
                AsyncOperation unload = SceneManager.UnloadSceneAsync(_scene);
                if (unload != null)
                {
                    yield return unload;
                }

                sceneReleased = !_scene.IsValid() || !_scene.isLoaded;
            }

            if (Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            _enteredPlayMode = false;
            Assert.That(sceneReleased, Is.True, "The exact owned local-physics scene must unload before leaving Play.");
        }

        [TestCase(AdvancedForceApplier.DirectionMode.Velocity, false, 2f)]
        [TestCase(AdvancedForceApplier.DirectionMode.Velocity, true, -2f)]
        [TestCase(AdvancedForceApplier.DirectionMode.CustomVector, false, 2f)]
        [TestCase(AdvancedForceApplier.DirectionMode.CustomVector, true, -2f)]
        [TestCase(AdvancedForceApplier.DirectionMode.ToTarget, false, 2f)]
        public void ForceApplier2D_UsesAnXYFallbackWhenDirectionHasNoXYComponent(
            AdvancedForceApplier.DirectionMode mode, bool inverted, float expectedX)
        {
            _owner.SetActive(false);
            AdvancedForceApplier applier = _owner.AddComponent<AdvancedForceApplier>();
            applier.playOnAwake = false;
            applier.directionMode = mode;
            applier.invertDirection = inverted;
            applier.forceMode2D = ForceMode2D.Impulse;
            _owner.SetActive(true);

            applier.ApplyForce(2f);
            Assert.That(_physicsScene.Simulate(Time.fixedDeltaTime), Is.True);

            Assert.That(_body.linearVelocity.x, Is.EqualTo(expectedX).Within(1e-4f));
            Assert.That(_body.linearVelocity.y, Is.EqualTo(0f).Within(1e-4f));
        }

        [UnityTest]
        public IEnumerator ConstantRotator2D_OnlyRotatesAroundTheZComponent_PositiveX()
        {
            yield return AssertConstantRotation(Vector3.right, 0f);
        }

        [UnityTest]
        public IEnumerator ConstantRotator2D_OnlyRotatesAroundTheZComponent_PositiveY()
        {
            yield return AssertConstantRotation(Vector3.up, 0f);
        }

        [UnityTest]
        public IEnumerator ConstantRotator2D_OnlyRotatesAroundTheZComponent_PositiveZ()
        {
            yield return AssertConstantRotation(Vector3.forward, 1f);
        }

        [UnityTest]
        public IEnumerator ConstantRotator2D_OnlyRotatesAroundTheZComponent_NegativeZ()
        {
            yield return AssertConstantRotation(Vector3.back, -1f);
        }

        private IEnumerator AssertConstantRotation(Vector3 axis, float expectedAngle)
        {
            Assert.That(Time.timeScale, Is.GreaterThan(0f), "Native FixedUpdate requires advancing scaled time.");
            Assert.That(Time.fixedDeltaTime, Is.GreaterThan(0f));
            _owner.SetActive(false);
            ConstantRotator rotator = _owner.AddComponent<ConstantRotator>();
            rotator.mode = ConstantRotator.RotationMode.Rigidbody2D;
            rotator.axisSource = ConstantRotator.AxisSource.Custom;
            rotator.customAxis = Vector3.forward;
            rotator.spaceLocal = false;
            rotator.useDeltaTime = false;
            rotator.degreesPerSecond = 1f;
            _owner.SetActive(true);

            // Prove real Awake/FixedUpdate delivery and local-world simulation before the negative XY controls.
            yield return WaitForNativeFixedStep();
            rotator.enabled = false;
            Assert.That(_physicsScene.Simulate(Time.fixedDeltaTime), Is.True);
            Assert.That(Mathf.DeltaAngle(0f, _body.rotation), Is.EqualTo(1f).Within(1e-3f));

            _body.rotation = 0f;
            _body.angularVelocity = 0f;
            rotator.customAxis = axis;
            rotator.enabled = true;
            yield return WaitForNativeFixedStep();
            rotator.enabled = false;
            Assert.That(_physicsScene.Simulate(Time.fixedDeltaTime), Is.True);

            Assert.That(Mathf.DeltaAngle(0f, _body.rotation), Is.EqualTo(expectedAngle).Within(1e-3f));
        }

        private static IEnumerator WaitForNativeFixedStep()
        {
            // The Edit runner advances its enumerator on editor updates, even after entering Play.
            double before = Time.fixedTimeAsDouble;
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (Time.fixedTimeAsDouble <= before && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(Time.fixedTimeAsDouble, Is.GreaterThan(before), "The native fixed loop did not advance in Play.");
        }
    }
}
