using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Tests.Performance;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Animator parameter caching used by the legacy controller animator drivers: parameters are written by hash,
    ///     and names the controller does not declare (or declares with another type) are skipped.
    /// </summary>
    [TestFixture]
    public sealed class Audit20261102NeoToolsMoveAnimatorParameterTests
    {
        private readonly List<Object> _objects = new();
        private bool _ownsPlayMode;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _ownsPlayMode = false;
            yield return new EnterPlayMode();
            _ownsPlayMode = true;
            Assert.That(Application.isPlaying, Is.True, "Animator initialization requires admitted Play Mode.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            _objects.Clear();
            if (_ownsPlayMode && Application.isPlaying)
            {
                yield return new ExitPlayMode();
            }

            _ownsPlayMode = false;
        }

        [UnityTest]
        public IEnumerator Cache_ResolvesDeclaredTypesAndRebindsOnControllerSwap()
        {
            AnimatorController first = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float));
            AnimatorController second = CreateController(("Jump", AnimatorControllerParameterType.Trigger));
            GameObject owner = Track(new GameObject("Animator cache owner"));
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = first;
            yield return WaitForInitialization(animator);
            AnimatorParameterCache cache = new();

            Assert.That(cache.Refresh(null), Is.False);
            Assert.That(cache.Refresh(animator), Is.True);
            int version = cache.Version;
            AnimatorParameterHandle grounded = cache.Resolve("IsGrounded", AnimatorControllerParameterType.Bool);
            Assert.That(grounded.Exists, Is.True);
            Assert.That(grounded.Hash, Is.EqualTo(Animator.StringToHash("IsGrounded")));
            Assert.That(cache.Resolve("IsGrounded", AnimatorControllerParameterType.Float).Exists, Is.False);
            Assert.That(cache.Resolve("Missing", AnimatorControllerParameterType.Bool).Exists, Is.False);
            Assert.That(cache.Resolve(" ", AnimatorControllerParameterType.Bool).Exists, Is.False);
            Assert.That(cache.Refresh(animator), Is.True);
            Assert.That(cache.Version, Is.EqualTo(version), "An unchanged binding must not rebuild the cache.");

            cache.Invalidate();
            Assert.That(cache.Refresh(animator), Is.True);
            Assert.That(cache.Version, Is.EqualTo(version + 1));

            animator.runtimeAnimatorController = second;
            yield return WaitForInitialization(animator);
            Assert.That(cache.Refresh(animator), Is.True);
            Assert.That(cache.Version, Is.EqualTo(version + 2));
            Assert.That(cache.Resolve("Jump", AnimatorControllerParameterType.Trigger).Exists, Is.True);
            Assert.That(cache.Resolve("Speed", AnimatorControllerParameterType.Float).Exists, Is.False);
        }

        [UnityTest]
        public IEnumerator Driver3D_WritesDeclaredParametersAndSkipsMissingOrMistyped()
        {
            AnimatorController controller = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float), ("MoveX", AnimatorControllerParameterType.Int));
            GameObject owner = Track(new GameObject("Animator driver owner"));
            owner.SetActive(false);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            player.enabled = false;
            player.CursorControlEnabled = false;
            SetField(player, "_groundCheck", owner.transform);
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            PlayerController3DAnimatorDriver driver = owner.AddComponent<PlayerController3DAnimatorDriver>();
            owner.SetActive(true);
            yield return WaitForInitialization(animator);
            animator.SetBool("IsGrounded", true);
            body.linearVelocity = new Vector3(3f, 0f, 4f);
            int frame = Time.frameCount;

            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (Time.frameCount < frame + 2)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "No LateUpdate was observed.");
                yield return null;
            }

            Assert.That(driver.IsReady, Is.True);
            Assert.That(animator.GetBool("IsGrounded"), Is.False, "The declared bool must be driven.");
            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(new Vector3(body.linearVelocity.x, 0f,
                body.linearVelocity.z).magnitude).Within(0.01f));
            Assert.That(animator.GetInteger("MoveX"), Is.Zero, "A parameter declared with another type is skipped.");
        }

        [UnityTest]
        public IEnumerator Driver3D_UsedMissingParameters_WarnOncePerBindingThroughTheGatedChannel()
        {
            AnimatorController controller = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float), ("IsMoving", AnimatorControllerParameterType.Int));
            GameObject owner = Track(new GameObject("Animator warning owner"));
            owner.SetActive(false);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            player.enabled = false;
            player.CursorControlEnabled = false;
            SetField(player, "_groundCheck", owner.transform);
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            PlayerController3DAnimatorDriver driver = owner.AddComponent<PlayerController3DAnimatorDriver>();
            SetField(driver, "_useLocomotionStateInt", false);
            SetField(driver, "_useJumpTrigger", false);
            SetField(driver, "_useDirectionalBlendTree", false);
            List<string> warnings = new();
            Application.LogCallback capture = (string message, string stackTrace, LogType type) =>
            {
                if (type == LogType.Warning && message.Contains(nameof(PlayerController3DAnimatorDriver)))
                {
                    warnings.Add(message);
                }
            };
            bool previousWarnings = NeoDiagnostics.RuntimeWarningsEnabled;
            NeoDiagnostics.Configure(warnings: true);
            Application.logMessageReceived += capture;
            try
            {
                owner.SetActive(true);
                yield return WaitForInitialization(animator);
                yield return WaitFrames(3);
            }
            finally
            {
                Application.logMessageReceived -= capture;
                NeoDiagnostics.Configure(warnings: previousWarnings);
            }

            Assert.That(warnings.Count, Is.EqualTo(2), string.Join("\n", warnings));
            Assert.That(warnings.Exists(message => message.Contains("has no Bool parameter 'IsRunning'")), Is.True);
            Assert.That(warnings.Exists(message => message.Contains("declares 'IsMoving' as Int, expected Bool")),
                Is.True);
        }

        [UnityTest]
        public IEnumerator Driver3D_AnimatorReinitializedMidAir_DoesNotFireAStaleJump()
        {
            AnimatorController controller = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float), ("Jump", AnimatorControllerParameterType.Trigger));
            GameObject owner = Track(new GameObject("Animator jump owner"));
            owner.SetActive(false);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            player.enabled = false;
            player.CursorControlEnabled = false;
            SetField(player, "_groundCheck", owner.transform);
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            owner.AddComponent<PlayerController3DAnimatorDriver>();
            SetGrounded(player, true);
            owner.SetActive(true);
            yield return WaitForInitialization(animator);
            yield return WaitFrames(2);
            Assert.That(animator.GetBool("Jump"), Is.False);

            animator.runtimeAnimatorController = null;
            SetGrounded(player, false);
            body.linearVelocity = Vector3.up * 3f;
            yield return WaitFrames(2);

            animator.runtimeAnimatorController = controller;
            yield return WaitForInitialization(animator);
            body.linearVelocity = new Vector3(2f, 3f, 0f);
            yield return WaitFrames(2);

            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(2f).Within(0.01f), "The driver must be writing again.");
            Assert.That(animator.GetBool("Jump"), Is.False,
                "Leaving the ground while the Animator was unbound must not fire Jump once it rebinds mid-air.");
        }

        [UnityTest]
        public IEnumerator Driver3D_WarmApplyAnimation_DoesNotAllocate()
        {
            AnimatorController controller = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("IsMoving", AnimatorControllerParameterType.Bool), ("IsRunning", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float),
                ("LocomotionState", AnimatorControllerParameterType.Int),
                ("Jump", AnimatorControllerParameterType.Trigger), ("MoveX", AnimatorControllerParameterType.Float),
                ("MoveY", AnimatorControllerParameterType.Float));
            GameObject owner = Track(new GameObject("Animator allocation owner"));
            owner.SetActive(false);
            Rigidbody body = owner.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.linearDamping = 0f;
            PlayerController3DPhysics player = owner.AddComponent<PlayerController3DPhysics>();
            player.enabled = false;
            player.CursorControlEnabled = false;
            SetField(player, "_groundCheck", owner.transform);
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            PlayerController3DAnimatorDriver driver = owner.AddComponent<PlayerController3DAnimatorDriver>();
            owner.SetActive(true);
            yield return WaitForInitialization(animator);
            yield return WaitFrames(2);
            MethodInfo method = typeof(PlayerController3DAnimatorDriver).GetMethod("ApplyAnimation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            System.Action apply = (System.Action)method.CreateDelegate(typeof(System.Action), driver);
            System.Action frames = () =>
            {
                for (int i = 0; i < 32; i++)
                {
                    apply();
                }
            };
            body.linearVelocity = new Vector3(2f, 0f, 1f);
            apply();
            Assert.That(driver.IsReady, Is.True);

            long calls = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(new Vector2(2f, 1f).magnitude).Within(0.01f),
                "The 3D driver must be writing.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "A warm driver writes every parameter by cached hash; no string hashing or lookup may allocate.");
        }

        [UnityTest]
        public IEnumerator Driver2D_WarmApplyAnimation_DoesNotAllocate()
        {
            AnimatorController controller = CreateController(("IsGrounded", AnimatorControllerParameterType.Bool),
                ("IsMoving", AnimatorControllerParameterType.Bool), ("IsRunning", AnimatorControllerParameterType.Bool),
                ("Speed", AnimatorControllerParameterType.Float),
                ("LocomotionState", AnimatorControllerParameterType.Int),
                ("Jump", AnimatorControllerParameterType.Trigger), ("MoveX", AnimatorControllerParameterType.Float),
                ("MoveY", AnimatorControllerParameterType.Float));
            GameObject owner = Track(new GameObject("Animator 2D allocation owner"));
            owner.SetActive(false);
            Rigidbody2D body = owner.AddComponent<Rigidbody2D>();
            body.gravityScale = 0f;
            body.linearDamping = 0f;
            PlayerController2DPhysics player = owner.AddComponent<PlayerController2DPhysics>();
            player.enabled = false;
            SetField(player, "_groundCheck", owner.transform);
            SetField(player, "_logSetupWarnings", false);
            Animator animator = owner.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            PlayerController2DAnimatorDriver driver = owner.AddComponent<PlayerController2DAnimatorDriver>();
            owner.SetActive(true);
            yield return WaitForInitialization(animator);
            yield return WaitFrames(2);
            MethodInfo method = typeof(PlayerController2DAnimatorDriver).GetMethod("ApplyAnimation",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            System.Action apply = (System.Action)method.CreateDelegate(typeof(System.Action), driver);
            System.Action frames = () =>
            {
                for (int i = 0; i < 32; i++)
                {
                    apply();
                }
            };
            body.linearVelocity = new Vector2(3f, 4f);
            apply();
            Assert.That(driver.IsReady, Is.True);

            long calls = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(animator.GetFloat("Speed"), Is.EqualTo(5f).Within(0.01f), "The 2D driver must be writing.");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(calls, Is.Zero,
                "A warm 2D driver writes every parameter by cached hash; no string hashing or lookup may allocate.");
        }

        private static void SetGrounded(PlayerController3DPhysics player, bool grounded)
        {
            PropertyInfo property = typeof(PlayerController3DPhysics).GetProperty(nameof(PlayerController3DPhysics
                .IsGrounded));
            Assert.That(property, Is.Not.Null);
            MethodInfo setter = property.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null);
            setter.Invoke(player, new object[] { grounded });
        }

        private static IEnumerator WaitFrames(int frames)
        {
            int target = Time.frameCount + frames;
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (Time.frameCount < target)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline), "Frames did not advance.");
                yield return null;
            }
        }

        private static IEnumerator WaitForInitialization(Animator animator)
        {
            double deadline = Time.realtimeSinceStartupAsDouble + 5d;
            while (!animator.isInitialized)
            {
                Assert.That(Time.realtimeSinceStartupAsDouble, Is.LessThan(deadline),
                    "The Animator did not initialize its controller.");
                yield return null;
            }
        }

        private AnimatorController CreateController(params (string Name, AnimatorControllerParameterType Type)[] parameters)
        {
            AnimatorController controller = Track(new AnimatorController());
            controller.name = "Audit20261102 animator";
            controller.AddLayer("Base Layer");
            foreach ((string Name, AnimatorControllerParameterType Type) parameter in parameters)
            {
                controller.AddParameter(parameter.Name, parameter.Type);
            }

            return controller;
        }

        private T Track<T>(T value) where T : Object
        {
            _objects.Add(value);
            return value;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }
    }
}
