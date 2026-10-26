using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ConditionValueType = Neo.Condition.ValueType;

namespace Neo.Editor.Tests.Edit
{
    [TestFixture]
    public sealed class Audit20261023NeoConditionRegressionTests
    {
        private const BindingFlags PublicInstance = BindingFlags.Public | BindingFlags.Instance;
        private const BindingFlags PrivateInstance = BindingFlags.NonPublic | BindingFlags.Instance;
        private readonly List<GameObject> _objects = new();

        private sealed class CacheProbe
        {
            private int _hiddenField = 41;
            private int HiddenProperty => _hiddenField;
            private int HiddenMethod(int value) => _hiddenField + value;
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject target in _objects)
            {
                if (target != null)
                {
                    Object.DestroyImmediate(target);
                }
            }

            _objects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void PropertyCache_RespectsBindingFlags_AfterMissOrHit(bool privateFirst)
        {
            if (!privateFirst)
            {
                Assert.That(ReflectionCache.GetProperty(typeof(CacheProbe), "HiddenProperty", PublicInstance), Is.Null);
            }

            PropertyInfo member = ReflectionCache.GetProperty(typeof(CacheProbe), "HiddenProperty", PrivateInstance);
            Assert.That(member, Is.Not.Null);
            Assert.That(member.GetValue(new CacheProbe()), Is.EqualTo(41));
            Assert.That(ReflectionCache.GetProperty(typeof(CacheProbe), "HiddenProperty", PublicInstance), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void FieldCache_RespectsBindingFlags_AfterMissOrHit(bool privateFirst)
        {
            if (!privateFirst)
            {
                Assert.That(ReflectionCache.GetField(typeof(CacheProbe), "_hiddenField", PublicInstance), Is.Null);
            }

            FieldInfo member = ReflectionCache.GetField(typeof(CacheProbe), "_hiddenField", PrivateInstance);
            Assert.That(member, Is.Not.Null);
            Assert.That(member.GetValue(new CacheProbe()), Is.EqualTo(41));
            Assert.That(ReflectionCache.GetField(typeof(CacheProbe), "_hiddenField", PublicInstance), Is.Null);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void MethodCache_RespectsBindingFlags_AfterMissOrHit(bool privateFirst)
        {
            if (!privateFirst)
            {
                Assert.That(ReflectionCache.GetMethod(typeof(CacheProbe), "HiddenMethod", ArgumentKind.Int,
                    PublicInstance), Is.Null);
            }

            MethodInfo member = ReflectionCache.GetMethod(typeof(CacheProbe), "HiddenMethod", ArgumentKind.Int,
                PrivateInstance);
            Assert.That(member, Is.Not.Null);
            Assert.That(member.Invoke(new CacheProbe(), new object[] { 1 }), Is.EqualTo(42));
            Assert.That(ReflectionCache.GetMethod(typeof(CacheProbe), "HiddenMethod", ArgumentKind.Int,
                PublicInstance), Is.Null);
        }

        [TestCase(SourceMode.Component)]
        [TestCase(SourceMode.GameObject)]
        public void Evaluate_RebindsCachedMembers_WhenContextChanges(SourceMode source)
        {
            Audit20261023NeoConditionValueProbe first = NewProbe("first", 3);
            Audit20261023NeoConditionValueProbe second = NewProbe("second", 8);
            first.gameObject.layer = 3;
            second.gameObject.layer = 8;
            ConditionEntry entry = NewEntry(source == SourceMode.GameObject
                ? nameof(GameObject.layer)
                : nameof(Audit20261023NeoConditionValueProbe.Value), 3);
            entry.Source = source;

            Assert.That(entry.Evaluate(first.gameObject), Is.True);
            Assert.That(entry.Evaluate(second.gameObject), Is.False);
            Assert.That(entry.Evaluate(null), Is.False, "A missing context must not retain the previous owner.");
            Assert.That(entry.Evaluate(first.gameObject), Is.True);

            entry.SourceObject = first.gameObject;
            Assert.That(entry.Evaluate(second.gameObject), Is.True, "An explicit source takes precedence over context.");
        }

        [Test]
        public void Evaluate_RebindsOtherObjectBranch_WhenContextChanges()
        {
            Audit20261023NeoConditionValueProbe first = NewProbe("first", 3);
            Audit20261023NeoConditionValueProbe second = NewProbe("second", 3);
            first.Limit = 3;
            second.Limit = 8;
            ConditionEntry entry = NewEntry(nameof(Audit20261023NeoConditionValueProbe.Value), 0);
            JsonUtility.FromJsonOverwrite("{\"_thresholdSource\":1,\"_otherComponentTypeName\":\"" +
                typeof(Audit20261023NeoConditionValueProbe).FullName + "\",\"_otherPropertyName\":\"Limit\"}", entry);

            Assert.That(entry.Evaluate(first.gameObject), Is.True);
            Assert.That(entry.Evaluate(second.gameObject), Is.False);
            Assert.That(entry.Evaluate(first.gameObject), Is.True);
        }

        [TestCase("ReadShort", -32768)]
        [TestCase("ReadShort", 12)]
        [TestCase("ReadShort", 32767)]
        [TestCase("ReadByte", 0)]
        [TestCase("ReadByte", 255)]
        public void Evaluate_ConvertsIntegerArgument_ToSupportedNarrowParameter(string methodName, int argument)
        {
            Audit20261023NeoConditionValueProbe probe = NewProbe("method", 0);
            ConditionEntry entry = NewMethodEntry(methodName, argument);

            Assert.That(entry.Evaluate(probe.gameObject), Is.True);
            Assert.That(probe.LastArgument, Is.EqualTo(argument));
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [TestCase("ReadShort", -32769)]
        [TestCase("ReadShort", 32768)]
        [TestCase("ReadByte", -1)]
        [TestCase("ReadByte", 256)]
        public void Evaluate_RejectsOutOfRangeArgument_WithoutInvokingMethod(string methodName, int argument)
        {
            Audit20261023NeoConditionValueProbe probe = NewProbe("overflow", 0);
            ConditionEntry entry = NewMethodEntry(methodName, argument);

            Assert.That(entry.Evaluate(probe.gameObject), Is.False);
            Assert.That(probe.Calls, Is.Zero);
        }

        [Test]
        public void Evaluate_UsesUpdatedArgument_AfterMethodCacheIsBuilt()
        {
            Audit20261023NeoConditionValueProbe probe = NewProbe("argument", 0);
            ConditionEntry entry = NewMethodEntry("ReadShort", 12);
            Assert.That(entry.Evaluate(probe.gameObject), Is.True);

            JsonUtility.FromJsonOverwrite("{\"_propertyArgumentInt\":24}", entry);
            entry.ThresholdInt = 25;

            Assert.That(entry.Evaluate(probe.gameObject), Is.True);
            Assert.That(probe.LastArgument, Is.EqualTo(24));
            Assert.That(probe.Calls, Is.EqualTo(2));
        }

        [Test]
        public void Evaluate_ConvertsMethodArgument_OnOtherObjectBranch()
        {
            Audit20261023NeoConditionValueProbe probe = NewProbe("other method", 13);
            ConditionEntry entry = NewEntry(nameof(Audit20261023NeoConditionValueProbe.Value), 0);
            JsonUtility.FromJsonOverwrite("{\"_thresholdSource\":1,\"_otherComponentTypeName\":\"" +
                typeof(Audit20261023NeoConditionValueProbe).FullName +
                "\",\"_otherPropertyName\":\"ReadShort\",\"_otherIsMethodWithArgument\":true,\"_otherPropertyArgumentInt\":12}",
                entry);

            Assert.That(entry.Evaluate(probe.gameObject), Is.True);
            Assert.That(probe.LastArgument, Is.EqualTo(12));
            Assert.That(probe.Calls, Is.EqualTo(1));
        }

        [Test]
        public void MethodCache_ExcludesOpenGenericMethods()
        {
            MethodInfo method = ReflectionCache.GetMethod(typeof(Audit20261023NeoConditionValueProbe),
                "GenericOnly", ArgumentKind.Int, PublicInstance);

            Assert.That(method, Is.Null, "A method requiring type arguments cannot be a condition value source.");
        }

        [Test]
        public void BindOtherToSourceIfNull_PreservesDestroyedExplicitSource()
        {
            Audit20261023NeoConditionValueProbe host = NewProbe("host", 3);
            Audit20261023NeoConditionValueProbe other = NewProbe("destroyed", 3);
            GameObject explicitOther = other.gameObject;
            NeoCondition component = host.gameObject.AddComponent<NeoCondition>();
            ConditionEntry entry = NewEntry(nameof(Audit20261023NeoConditionValueProbe.Value), 3);
            component.AddCondition(entry);
            using (SerializedObject serialized = new(component))
            {
                SerializedProperty condition = serialized.FindProperty("_conditions").GetArrayElementAtIndex(0);
                string componentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName;
                string memberName = nameof(Audit20261023NeoConditionValueProbe.Value);

                // Configure the complete inline entry through the same native serialization path as its references.
                condition.FindPropertyRelative("_sourceMode").enumValueIndex = (int)SourceMode.Component;
                condition.FindPropertyRelative("_useSceneSearch").boolValue = false;
                condition.FindPropertyRelative("_sourceObject").objectReferenceValue = host.gameObject;
                condition.FindPropertyRelative("_componentTypeName").stringValue = componentTypeName;
                condition.FindPropertyRelative("_componentOccurrence").intValue = 0;
                condition.FindPropertyRelative("_propertyName").stringValue = memberName;
                condition.FindPropertyRelative("_isMethodWithArgument").boolValue = false;
                condition.FindPropertyRelative("_valueType").enumValueIndex = (int)ConditionValueType.Int;
                condition.FindPropertyRelative("_compareOp").enumValueIndex = (int)CompareOp.Equal;
                condition.FindPropertyRelative("_thresholdSource").enumValueIndex = (int)ThresholdSource.OtherObject;
                condition.FindPropertyRelative("_thresholdInt").intValue = 3;
                condition.FindPropertyRelative("_invert").boolValue = false;
                condition.FindPropertyRelative("_otherSourceMode").enumValueIndex = (int)SourceMode.Component;
                condition.FindPropertyRelative("_otherUseSceneSearch").boolValue = false;
                condition.FindPropertyRelative("_otherSourceObject").objectReferenceValue = explicitOther;
                condition.FindPropertyRelative("_otherComponentTypeName").stringValue = componentTypeName;
                condition.FindPropertyRelative("_otherComponentOccurrence").intValue = 0;
                condition.FindPropertyRelative("_otherPropertyName").stringValue = memberName;
                condition.FindPropertyRelative("_otherIsMethodWithArgument").boolValue = false;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                serialized.Update();
                condition = serialized.FindProperty("_conditions").GetArrayElementAtIndex(0);
                Assert.That(condition.FindPropertyRelative("_otherSourceMode").enumValueIndex,
                    Is.EqualTo((int)SourceMode.Component));
                Assert.That(condition.FindPropertyRelative("_otherUseSceneSearch").boolValue, Is.False);
                Assert.That(condition.FindPropertyRelative("_otherSourceObject").objectReferenceValue,
                    Is.EqualTo(explicitOther));
                Assert.That(condition.FindPropertyRelative("_otherComponentTypeName").stringValue,
                    Is.EqualTo(componentTypeName));
                Assert.That(condition.FindPropertyRelative("_otherComponentOccurrence").intValue, Is.Zero);
                Assert.That(condition.FindPropertyRelative("_otherPropertyName").stringValue, Is.EqualTo(memberName));
                Assert.That(condition.FindPropertyRelative("_otherIsMethodWithArgument").boolValue, Is.False);
            }

            entry = component.Conditions[0];
            Assert.That(host != null && other != null, Is.True);
            Assert.That(host.Value, Is.EqualTo(3));
            Assert.That(other.Value, Is.EqualTo(host.Value));
            Assert.That(entry.Source, Is.EqualTo(SourceMode.Component));
            Assert.That(entry.UseSceneSearch, Is.False);
            Assert.That(entry.SourceObject, Is.EqualTo(host.gameObject));
            Assert.That(entry.ComponentTypeName, Is.EqualTo(typeof(Audit20261023NeoConditionValueProbe).FullName));
            Assert.That(entry.ComponentOccurrence, Is.Zero);
            Assert.That(entry.PropertyName, Is.EqualTo(nameof(Audit20261023NeoConditionValueProbe.Value)));
            Assert.That(entry.CurrentValueType, Is.EqualTo(ConditionValueType.Int));
            Assert.That(entry.Compare, Is.EqualTo(CompareOp.Equal));
            Assert.That(entry.ThresholdSource, Is.EqualTo(ThresholdSource.OtherObject));
            Assert.That(entry.Invert, Is.False);
            Assert.That(entry.Evaluate(host.gameObject), Is.True);

            other.Value = 4;
            Assert.That(entry.Evaluate(host.gameObject), Is.False, "The explicit other source supplies the threshold.");
            other.Value = host.Value;
            Assert.That(entry.Evaluate(host.gameObject), Is.True);

            Object.DestroyImmediate(explicitOther);
            Assert.That(explicitOther == null, Is.True);
            Assert.That(ReferenceEquals(explicitOther, null), Is.False);
            Assert.That(host != null, Is.True);
            entry.InvalidateCacheFull();
            Assert.That(entry.Evaluate(host.gameObject), Is.False, "The destroyed explicit source is unavailable.");
            entry.BindOtherToSourceIfNull(host.gameObject);

            Assert.That(NewEntry(nameof(Audit20261023NeoConditionValueProbe.Value), 3).Evaluate(host.gameObject),
                Is.True, "The surviving host remains a valid value source.");
            Assert.That(entry.Evaluate(host.gameObject), Is.False,
                "A destroyed explicit source must not become a successful comparison against the host.");
        }

        [Test]
        public void ClientResultCommand_RejectsResult_InServerRevalidateMode()
        {
            NeoCondition component = NewProbe("server authority", 0).gameObject.AddComponent<NeoCondition>();
            component.isNetworked = true;
            int results = 0;
            component.OnResult.AddListener(result => results++);

            InvokeWovenCommand(component, "CmdClientResult", new object[] { true, null });

            Assert.That(component.LastResult, Is.False);
            Assert.That(results, Is.Zero);
        }

        [TestCase("CmdClientResult")]
        [TestCase("CmdRequestCheck")]
        public void NetworkCommand_RejectsIngress_WhenNetworkingIsDisabled(string commandName)
        {
            NeoCondition component = NewProbe("offline", 0).gameObject.AddComponent<NeoCondition>();
            JsonUtility.FromJsonOverwrite("{\"_authority\":1}", component);
            int results = 0;
            component.OnResult.AddListener(result => results++);

            InvokeWovenCommand(component, commandName, commandName == "CmdClientResult"
                ? new object[] { true, null }
                : new object[] { null });

            Assert.That(component.LastResult, Is.False);
            Assert.That(results, Is.Zero);
        }

        private Audit20261023NeoConditionValueProbe NewProbe(string name, int value)
        {
            GameObject target = new("Audit20261023NeoCondition " + name);
            _objects.Add(target);
            target.SetActive(false);
            Audit20261023NeoConditionValueProbe probe = target.AddComponent<Audit20261023NeoConditionValueProbe>();
            probe.Value = value;
            return probe;
        }

        private static ConditionEntry NewEntry(string memberName, int threshold)
        {
            return new ConditionEntry
            {
                ComponentTypeName = typeof(Audit20261023NeoConditionValueProbe).FullName,
                PropertyName = memberName,
                CurrentValueType = ConditionValueType.Int,
                ThresholdInt = threshold
            };
        }

        private static ConditionEntry NewMethodEntry(string methodName, int argument)
        {
            ConditionEntry entry = NewEntry(methodName, argument + 1);
            JsonUtility.FromJsonOverwrite("{\"_isMethodWithArgument\":true,\"_propertyArgumentInt\":" + argument + "}",
                entry);
            return entry;
        }

        private static void InvokeWovenCommand(NeoCondition component, string commandName, object[] arguments)
        {
            // Invoke the received command body, not Mirror's outgoing transport wrapper.
            MethodInfo body = Array.Find(typeof(NeoCondition).GetMethods(PrivateInstance), method =>
                method.Name.StartsWith("UserCode_" + commandName + "__", StringComparison.Ordinal));
            if (body == null)
            {
                Assert.Ignore("This ingress regression requires the Mirror-woven Neo.Condition assembly.");
                return;
            }

            body.Invoke(component, arguments);
        }
    }
}
