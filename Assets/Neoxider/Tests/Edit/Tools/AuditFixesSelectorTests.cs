using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     Unique-mode tracking can be cleared two ways: manually through <c>ResetUnique()</c> and
    ///     automatically at the end of a cycle when Reset Unique When Cycle Complete is on. Both clear the
    ///     same state, so both must raise <c>OnUniqueReset</c> or per-cycle listeners stay stale for a
    ///     whole extra cycle.
    /// </summary>
    public sealed class AuditFixesSelectorTests
    {
        [Test]
        public void SetRandom_AutomaticCycleReset_InvokesOnUniqueResetTogetherWithCycleComplete()
        {
            GameObject root = CreateSelectorRoot("AuditFixesSelectorAutoResetRoot", 3, out Selector selector);

            try
            {
                SetPrivateBool(selector, "_useRandomSelection", true);
                SetPrivateBool(selector, "_uniqueSelectionMode", true);
                SetPrivateBool(selector, "_resetUniqueWhenCycleComplete", true);

                int cycleComplete = 0;
                int uniqueReset = 0;
                selector.OnUniqueCycleComplete.AddListener(() => cycleComplete++);
                selector.OnUniqueReset.AddListener(() => uniqueReset++);

                for (int i = 0; i < 3; i++)
                {
                    selector.SetRandom();
                }

                Assert.That(cycleComplete, Is.Zero, "The cycle only completes once every index has been used.");
                Assert.That(uniqueReset, Is.Zero);

                selector.SetRandom();

                Assert.That(cycleComplete, Is.EqualTo(1));
                Assert.That(uniqueReset, Is.EqualTo(1),
                    "The automatic reset clears the same tracking as ResetUnique() and must notify as well.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetRandom_WithoutAutomaticReset_DoesNotInvokeOnUniqueReset()
        {
            GameObject root = CreateSelectorRoot("AuditFixesSelectorNoAutoResetRoot", 3, out Selector selector);

            try
            {
                SetPrivateBool(selector, "_useRandomSelection", true);
                SetPrivateBool(selector, "_uniqueSelectionMode", true);
                SetPrivateBool(selector, "_resetUniqueWhenCycleComplete", false);

                int cycleComplete = 0;
                int uniqueReset = 0;
                selector.OnUniqueCycleComplete.AddListener(() => cycleComplete++);
                selector.OnUniqueReset.AddListener(() => uniqueReset++);

                for (int i = 0; i < 4; i++)
                {
                    selector.SetRandom();
                }

                Assert.That(cycleComplete, Is.EqualTo(1));
                Assert.That(uniqueReset, Is.Zero, "Nothing was cleared, so no reset must be reported.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResetUnique_ManualCall_StillInvokesOnUniqueReset()
        {
            GameObject root = CreateSelectorRoot("AuditFixesSelectorManualResetRoot", 3, out Selector selector);

            try
            {
                SetPrivateBool(selector, "_useRandomSelection", true);
                SetPrivateBool(selector, "_uniqueSelectionMode", true);
                SetPrivateBool(selector, "_resetUniqueWhenCycleComplete", false);

                int uniqueReset = 0;
                selector.OnUniqueReset.AddListener(() => uniqueReset++);

                selector.SetRandom();
                selector.ResetUnique();

                Assert.That(uniqueReset, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateSelectorRoot(string name, int itemCount, out Selector selector)
        {
            GameObject root = new(name);
            for (int i = 0; i < itemCount; i++)
            {
                GameObject item = new($"Item{i}");
                item.transform.SetParent(root.transform);
            }

            selector = root.AddComponent<Selector>();
            selector.startOnAwake = false;
            return root;
        }

        private static void SetPrivateBool(object target, string fieldName, bool value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field `{fieldName}` was not found.");
            field.SetValue(target, value);
        }
    }
}
