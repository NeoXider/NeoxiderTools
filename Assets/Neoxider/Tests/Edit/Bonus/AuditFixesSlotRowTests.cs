using Neo.Bonus;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <c>Row.GetVisibleTopDown</c> assigns elements into buckets indexed bottom-up (k = 0 is the lowest
    ///     visible slot) and only reverses them into a top-down array at the very end. Its recovery fallback
    ///     for empty buckets must therefore consume leftovers in ascending Y; taking them top-first handed
    ///     the reel column back vertically flipped, which the payline evaluation then reads upside-down.
    /// </summary>
    public sealed class AuditFixesSlotRowTests
    {
        [Test]
        public void GetVisibleTopDown_WhenBucketRecoveryRuns_StillReturnsElementsTopDown()
        {
            GameObject root = new("AuditFixesRowRoot");

            try
            {
                Row row = root.AddComponent<Row>();
                row.countSlotElement = 3;
                row.spaceY = 1f;
                row.offsetY = 0f;

                SlotElement inWindow = CreateElement(root.transform, "InWindow");
                SlotElement strayLow = CreateElement(root.transform, "StrayLow");
                SlotElement strayHigh = CreateElement(root.transform, "StrayHigh");

                // WHY: ApplyLayout rewrites every local Y, so the broken layout is staged afterwards.
                row.ApplyLayout();
                SetLocalY(inWindow, 0f);
                SetLocalY(strayLow, 10f);
                SetLocalY(strayHigh, 20f);

                SlotElement[] visible = row.GetVisibleTopDown();

                Assert.That(visible.Length, Is.EqualTo(3));
                Assert.That(visible[0], Is.SameAs(strayHigh), "Index 0 is the topmost visible slot.");
                Assert.That(visible[1], Is.SameAs(strayLow));
                Assert.That(visible[2], Is.SameAs(inWindow), "Index 2 is the bottom visible slot.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void GetVisibleTopDown_WithAValidLayout_IsUnaffectedByTheFallback()
        {
            GameObject root = new("AuditFixesRowValidLayoutRoot");

            try
            {
                Row row = root.AddComponent<Row>();
                row.countSlotElement = 3;
                row.spaceY = 1f;
                row.offsetY = 0f;

                SlotElement bottom = CreateElement(root.transform, "Bottom");
                SlotElement middle = CreateElement(root.transform, "Middle");
                SlotElement top = CreateElement(root.transform, "Top");

                row.ApplyLayout();
                SetLocalY(bottom, 0f);
                SetLocalY(middle, 1f);
                SetLocalY(top, 2f);

                SlotElement[] visible = row.GetVisibleTopDown();

                Assert.That(visible[0], Is.SameAs(top));
                Assert.That(visible[1], Is.SameAs(middle));
                Assert.That(visible[2], Is.SameAs(bottom));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SlotElement CreateElement(Transform parent, string name)
        {
            GameObject go = new(name);
            go.transform.SetParent(parent);
            return go.AddComponent<SlotElement>();
        }

        private static void SetLocalY(SlotElement element, float y)
        {
            Transform t = element.transform;
            Vector3 local = t.localPosition;
            local.y = y;
            t.localPosition = local;
        }
    }
}
