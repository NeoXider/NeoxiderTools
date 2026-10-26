using System;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorTestsRuntimeFixtureBoundaryTests
    {
        [Test]
        public void NativeHelpers_AttachFromRuntimeFixtureAssembly_AndKeepGetterContract()
        {
            GameObject root = new GameObject(nameof(NativeHelpers_AttachFromRuntimeFixtureAssembly_AndKeepGetterContract));
            try
            {
                Type[] helperTypes =
                {
                    typeof(Audit20261023CoroutineUnityPhaseProbe),
                    typeof(Audit20261023CoroutineUnityRaycaster),
                    typeof(Audit20261023NeoQuestConditionProbe)
                };
                foreach (Type helperType in helperTypes)
                {
                    Assert.That(helperType.Assembly.GetName().Name, Is.EqualTo("Neo.Tests.Fixtures"));
                    Component component = root.AddComponent(helperType);
                    Assert.That(component, Is.Not.Null, "Unity must attach the actual runtime helper: " + helperType.FullName);
                    Assert.That(component.gameObject, Is.SameAs(root));
                }

                Audit20261023NeoQuestConditionProbe probe = root.GetComponent<Audit20261023NeoQuestConditionProbe>();
                int callbacks = 0;
                probe.OnRead = () => callbacks++;
                probe.Result = true;
                Assert.That(probe.Value, Is.True);
                Assert.That(probe.ReadCount, Is.EqualTo(1));
                Assert.That(callbacks, Is.EqualTo(1));
                Assert.That(probe.OnRead, Is.Null);
                Assert.That(probe.Value, Is.True);
                Assert.That(probe.ReadCount, Is.EqualTo(2));
                Assert.That(callbacks, Is.EqualTo(1), "The consumed read callback must not replay.");
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
