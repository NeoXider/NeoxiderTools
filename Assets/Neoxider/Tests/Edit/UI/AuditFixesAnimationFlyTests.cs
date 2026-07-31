using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Edit
{
    /// <summary>
    ///     The static coordinate helpers fall back to the singleton's canvas when no canvas is passed in.
    ///     Without an <see cref="AnimationFly" /> in the scene the singleton resolves to null, so the lookup
    ///     has to be guarded: the helpers must reach their LogError + zero-vector fallback instead of
    ///     throwing a NullReferenceException.
    /// </summary>
    public sealed class AuditFixesAnimationFlyTests
    {
        private const string MissingCanvasMessage = "[AnimationFly] Canvas is not set and parentCanvas is not assigned!";

        [SetUp]
        public void SetUp()
        {
            RequireSceneWithoutAnimationFly();
            NeoDiagnostics.Configure(errors: true);
        }

        [Test]
        public void WorldToCanvasPosition_WithoutInstanceAndWithoutCanvas_ReturnsZeroInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Error, MissingCanvasMessage);

            Vector2 result = Vector2.one;
            Assert.DoesNotThrow(() => result = AnimationFly.WorldToCanvasPosition(new Vector3(3f, 4f, 5f)));

            Assert.That(result, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void CanvasToWorldPosition_WithoutInstanceAndWithoutCanvas_ReturnsZeroInsteadOfThrowing()
        {
            LogAssert.Expect(LogType.Error, MissingCanvasMessage);

            Vector3 result = Vector3.one;
            Assert.DoesNotThrow(() => result = AnimationFly.CanvasToWorldPosition(new Vector2(120f, 80f)));

            Assert.That(result, Is.EqualTo(Vector3.zero));
        }

        [Test]
        public void WorldToCanvasPosition_WithExplicitCanvas_NeedsNoInstance()
        {
            GameObject canvasObject = new("AuditFixesFlyCanvas", typeof(RectTransform), typeof(Canvas));

            try
            {
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;

                Vector2 result = AnimationFly.WorldToCanvasPosition(new Vector3(3f, 4f, 5f), canvas);

                Assert.That(result, Is.Not.EqualTo(Vector2.zero));
            }
            finally
            {
                Object.DestroyImmediate(canvasObject);
            }
        }

        // WHY: EditMode runs against whatever scene the user has open, and a stray AnimationFly there would
        // let the unguarded lookup succeed and silently hide the defect this fixture covers.
        private static void RequireSceneWithoutAnimationFly()
        {
            AnimationFly[] existing =
                Object.FindObjectsByType<AnimationFly>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            Assume.That(AnimationFly.HasInstance, Is.False,
                "These cases need a scene with no AnimationFly singleton.");
            Assume.That(existing, Is.Empty, "These cases need a scene with no AnimationFly component.");
        }
    }
}
