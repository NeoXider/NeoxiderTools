using System;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     <see cref="ChanceData" /> is a ScriptableObject, so its scene-wiring event is obsolete (a warning, never an
    ///     error) in favour of <see cref="ChanceSystemBehaviour" /> events, but it keeps loading listeners saved under
    ///     the old public field name and keeps firing on <see cref="ChanceData.GenerateId" />.
    /// </summary>
    public sealed class ChanceDataObsoleteEventTests
    {
        private ChanceData _data;

        [SetUp]
        public void SetUp()
        {
            _data = ScriptableObject.CreateInstance<ChanceData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_data);
        }

        [Test]
        public void OnIdGenerated_IsObsoleteAsAWarningPointingAtChanceSystemBehaviour()
        {
            PropertyInfo property = typeof(ChanceData).GetProperty("OnIdGenerated");
            Assert.That(property, Is.Not.Null, "The public accessor must stay for source compatibility.");
            ObsoleteAttribute obsolete = property.GetCustomAttribute<ObsoleteAttribute>();
            Assert.That(obsolete, Is.Not.Null);
            Assert.That(obsolete.IsError, Is.False, "Obsolete members must warn, never break consumer builds.");
            Assert.That(obsolete.Message, Does.Contain(nameof(ChanceSystemBehaviour)));
        }

        [Test]
        public void OnIdGenerated_StillLoadsListenersSavedUnderTheOldFieldName()
        {
            FieldInfo field = typeof(ChanceData).GetField("_onIdGenerated",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            Assert.That(field.GetCustomAttribute<SerializeField>(), Is.Not.Null);
            FormerlySerializedAsAttribute former = field.GetCustomAttribute<FormerlySerializedAsAttribute>();
            Assert.That(former, Is.Not.Null);
            Assert.That(former.oldName, Is.EqualTo("OnIdGenerated"),
                "Listeners saved on existing assets under the old public field name must still load.");

            using (SerializedObject serialized = new(_data))
            {
                Assert.That(serialized.FindProperty("_onIdGenerated"), Is.Not.Null);
            }
        }

        [Test]
        public void GenerateId_StillInvokesTheObsoleteEvent()
        {
            _data.AddChance(1f);
            int received = -2;
#pragma warning disable CS0618 // WHY: pins that the obsolete event keeps working until it is removed.
            Assert.That(_data.OnIdGenerated, Is.Not.Null);
            _data.OnIdGenerated.AddListener(id => received = id);
#pragma warning restore CS0618

            int generated = _data.GenerateId();

            Assert.That(generated, Is.EqualTo(0), "Control: a one-entry table always rolls index 0.");
            Assert.That(received, Is.EqualTo(generated));
        }
    }
}
