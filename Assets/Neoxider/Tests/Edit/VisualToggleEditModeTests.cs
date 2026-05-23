using Neo.UI;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Edit
{
    public sealed class VisualToggleEditModeTests
    {
        [Test]
        public void VisualToggle_Set_UpdatesReactiveValue()
        {
            var go = new GameObject("VT");
            try
            {
                var vt = go.AddComponent<VisualToggle>();
                vt.Set(true);
                Assert.That(vt.ValueBool, Is.True);
                vt.Set(false);
                Assert.That(vt.ValueBool, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(go);
            }
        }
    }
}
