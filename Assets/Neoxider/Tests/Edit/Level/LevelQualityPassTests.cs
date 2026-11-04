using System;
using System.Reflection;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Level.Tests
{
    /// <summary>
    ///     <see cref="Map.IsComplete" /> semantics and the allocation-free progress label of
    ///     <see cref="SceneFlowController" />.
    /// </summary>
    public sealed class LevelQualityPassTests
    {
        private GameObject _host;

        [TearDown]
        public void TearDown()
        {
            if (_host != null)
            {
                Object.DestroyImmediate(_host);
            }
        }

        [TestCase(true, 3, 0, false)]
        [TestCase(true, 3, 10, false)]
        [TestCase(false, 3, 2, false)]
        [TestCase(false, 3, 3, true)]
        [TestCase(false, 0, 0, true)]
        public void Map_IsComplete_FollowsFiniteAndInfiniteRules(bool infinite, int count, int frontier, bool expected)
        {
            Map map = new Map { isInfinity = infinite, countLevels = count };
            FieldInfo levelField = typeof(Map).GetField("_level", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(levelField, Is.Not.Null);
            levelField.SetValue(map, frontier);

            Assert.That(map.IsComplete, Is.EqualTo(expected));
#pragma warning disable CS0618
            Assert.That(map.GetCopmplete(), Is.EqualTo(expected), "the legacy name must stay equivalent");
#pragma warning restore CS0618
        }

        [Test]
        public void ProgressLabel_IsRebuiltOnlyWhenThePercentageChanges()
        {
            SceneFlowController controller = CreateController(out Text label);
            Action<float> apply = CreateApply(controller);

            apply(0.42f);
            Assert.That(label.text, Is.EqualTo("Loading... 42%"));

            label.text = "external";
            apply(0.421f);
            Assert.That(label.text, Is.EqualTo("external"), "an unchanged percentage must not rebuild the label");

            apply(0.43f);
            Assert.That(label.text, Is.EqualTo("Loading... 43%"));
        }

        [Test]
        public void ProgressLabel_AfterReadyText_WritesTheFinalPercentageAgain()
        {
            SceneFlowController controller = CreateController(out Text label);
            Action<float> apply = CreateApply(controller);
            MethodInfo setText = typeof(SceneFlowController).GetMethod("SetProgressText",
                BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string) }, null);
            Assert.That(setText, Is.Not.Null);

            apply(1f);
            setText.Invoke(controller, new object[] { "Press to continue" });
            Assert.That(label.text, Is.EqualTo("Press to continue"));

            apply(1f);
            Assert.That(label.text, Is.EqualTo("Loading... 100%"));
        }

        [Test]
        public void ProgressLabel_RepeatedPercentage_AllocatesNothing()
        {
            SceneFlowController controller = CreateController(out Text _);
            Action<float> apply = CreateApply(controller);
            apply(0.5f);
            apply(0.5f);

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < 100; i++)
            {
                apply(0.5f);
            }

            long allocated = allocationCounter.End();
            Assert.That(allocated, Is.EqualTo(0L), "a loading screen pushes progress every frame");
        }

        private SceneFlowController CreateController(out Text label)
        {
            _host = new GameObject("LevelQualityPassTests.SceneFlow");
            SceneFlowController controller = _host.AddComponent<SceneFlowController>();
            GameObject labelObject = new GameObject("Label", typeof(RectTransform));
            labelObject.transform.SetParent(_host.transform, false);
            label = labelObject.AddComponent<Text>();
            FieldInfo textField = typeof(SceneFlowController).GetField("_textProgress",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(textField, Is.Not.Null);
            textField.SetValue(controller, label);
            return controller;
        }

        private static Action<float> CreateApply(SceneFlowController controller)
        {
            MethodInfo method = typeof(SceneFlowController).GetMethod("ApplyProgressToUI",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Action<float>)Delegate.CreateDelegate(typeof(Action<float>), controller, method);
        }
    }
}
