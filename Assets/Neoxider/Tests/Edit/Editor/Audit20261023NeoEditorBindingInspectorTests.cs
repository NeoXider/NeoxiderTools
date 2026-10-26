using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using Neo.Editor.Binding;
using Neo.Editor.Condition;
using Neo.Editor.Network;
using Neo.NoCode;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using NumericProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorBindingInspectorNumericProbe;
using BindingProbe = global::Neo.Editor.Tests.Audit20261023FixtureNeoEditorBindingInspectorBindingProbe;

namespace Neo.Editor.Tests
{
    public sealed class Audit20261023NeoEditorBindingInspectorTests
    {


        private sealed class Arguments : ScriptableObject
        {
            public ArgumentKind Kind;
            public int Int;
            public float Float;
            public string String;
        }



        [Test]
        public void ComponentPicker_MapsInterleavedTypesToTheirOwnOccurrencesAndPreview()
        {
            GameObject root = new("Audit20261023NeoEditorOccurrences");
            try
            {
                BoxCollider first = root.AddComponent<BoxCollider>();
                root.AddComponent<SphereCollider>();
                BoxCollider second = root.AddComponent<BoxCollider>();
                second.isTrigger = true;
                List<string> names = new();
                List<string> types = new();
                ComponentBindingInspectorShared.BuildComponentPickLists(root, names, types);

                int row = ComponentBindingInspectorShared.IndexOfComponent(types, typeof(BoxCollider).FullName, 1);
                Assert.That(row, Is.EqualTo(2));
                Assert.That(names[row], Is.EqualTo("BoxCollider (2)"));
                Assert.That(ComponentBindingInspectorShared.ComponentOccurrenceAt(types, row), Is.EqualTo(1));
                Component selected = ComponentBindingInspectorShared.FindComponentByTypeName(root,
                    typeof(BoxCollider).FullName, 1);
                Assert.That(selected, Is.SameAs(second));
                Assert.That(NeoConditionEditor.GetCurrentValueFromComponent(selected, nameof(BoxCollider.isTrigger),
                    false, null, null, null, null), Is.EqualTo(true));
                Assert.That(ComponentBindingInspectorShared.FindComponentByTypeName(root, typeof(BoxCollider).FullName),
                    Is.SameAs(first), "The existing overload retains occurrence zero.");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(-1)]
        [TestCase(2)]
        public void ComponentPicker_InvalidOccurrenceDoesNotFallBack(int occurrence)
        {
            GameObject root = new("Audit20261023NeoEditorInvalidOccurrence");
            try
            {
                root.AddComponent<BoxCollider>();
                root.AddComponent<BoxCollider>();
                List<string> names = new();
                List<string> types = new();
                ComponentBindingInspectorShared.BuildComponentPickLists(root, names, types);
                Assert.That(ComponentBindingInspectorShared.IndexOfComponent(types, typeof(BoxCollider).FullName,
                    occurrence), Is.EqualTo(-1));
                Assert.That(ComponentBindingInspectorShared.FindComponentByTypeName(root, typeof(BoxCollider).FullName,
                    occurrence), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SerializedOccurrences_RoundTripAndReadTheSecondInstanceWithoutReinterpretingLegacyIndex()
        {
            GameObject root = new("Audit20261023NeoEditorSerializedOccurrence");
            try
            {
                BoxCollider first = root.AddComponent<BoxCollider>();
                BoxCollider second = root.AddComponent<BoxCollider>();
                second.isTrigger = true;
                BindingProbe probe = root.AddComponent<BindingProbe>();
                probe.Binding.SourceRoot = root;
                probe.Binding.ComponentTypeName = typeof(BoxCollider).FullName;
                probe.Binding.MemberName = nameof(BoxCollider.isTrigger);
                Assert.That(probe.gameObject, Is.SameAs(root));
                using (SerializedObject serialized = new(probe))
                {
                    SerializedProperty sourceRoot = serialized.FindProperty("Binding._sourceRoot");
                    Assert.That(sourceRoot, Is.Not.Null);
                    Assert.That(sourceRoot.objectReferenceValue, Is.SameAs(root));
                    serialized.FindProperty("Binding._componentOccurrence").intValue = 1;
                    serialized.FindProperty("Entry._componentOccurrence").intValue = 1;
                    serialized.FindProperty("Entry._otherComponentOccurrence").intValue = 1;
                    serialized.FindProperty("Entry._componentIndex").intValue = 91;
                    serialized.FindProperty("Entry._otherComponentIndex").intValue = 92;
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(probe.Binding.SourceRoot, Is.SameAs(root));
                Assert.That(first.isTrigger, Is.False);
                Assert.That(probe.Binding.EnsureCache(probe, out string originalError), Is.True, originalError);
                Assert.That(probe.Binding.TryReadFloat(probe, out float originalValue), Is.True);
                Assert.That(originalValue, Is.EqualTo(1f), "The attached source must read the second instance before serialization.");

                BindingProbe restored = root.AddComponent<BindingProbe>();
                // EditorJsonUtility excludes scene references; this is a same-session roundtrip of a live source.
                string json = JsonUtility.ToJson(probe);
                JsonUtility.FromJsonOverwrite(json, restored);
                Assert.That(restored.Binding.SourceRoot, Is.SameAs(root));
                Assert.That(restored.Binding.ComponentTypeName, Is.EqualTo(typeof(BoxCollider).FullName));
                Assert.That(restored.Binding.MemberName, Is.EqualTo(nameof(BoxCollider.isTrigger)));
                Assert.That(restored.Binding.ComponentOccurrence, Is.EqualTo(1));
                Assert.That(restored.Entry.ComponentOccurrence, Is.EqualTo(1));
                Assert.That(restored.Entry.OtherComponentOccurrence, Is.EqualTo(1));
                Assert.That(restored.Entry.ComponentIndex, Is.EqualTo(91));
                using (SerializedObject serialized = new(restored))
                {
                    Assert.That(serialized.FindProperty("Entry._otherComponentIndex").intValue, Is.EqualTo(92));
                }

                Assert.That(restored.Binding.EnsureCache(restored, out string restoredError), Is.True, restoredError);
                Assert.That(restored.Binding.TryReadFloat(restored, out float value), Is.True);
                Assert.That(value, Is.EqualTo(1f));
                Object.DestroyImmediate(second);
                Assert.That(ComponentBindingInspectorShared.FindComponentByTypeName(root, typeof(BoxCollider).FullName,
                    restored.Binding.ComponentOccurrence), Is.Null);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [TestCase("ReadByte", 200)]
        [TestCase("ReadShort", 400)]
        public void ConditionPreview_ConvertsSerializedIntegerToDeclaredNarrowParameter(string method, int argument)
        {
            GameObject root = new("Audit20261023NeoEditorBinding");
            Arguments arguments = ScriptableObject.CreateInstance<Arguments>();
            try
            {
                NumericProbe component = root.AddComponent<NumericProbe>();
                arguments.Kind = ArgumentKind.Int;
                arguments.Int = argument;
                using (SerializedObject serialized = new(arguments))
                {
                    object value = NeoConditionEditor.GetCurrentValueFromComponent(component, method, true,
                        serialized.FindProperty(nameof(Arguments.Kind)),
                        serialized.FindProperty(nameof(Arguments.Int)),
                        serialized.FindProperty(nameof(Arguments.Float)),
                        serialized.FindProperty(nameof(Arguments.String)));

                    Assert.That(value, Is.EqualTo(argument * 2));
                    Assert.That(component.LastArgument, Is.EqualTo(argument));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(arguments);
            }
        }

        [Test]
        public void ConditionMenu_ExcludesOpenGenericMethodsThatCannotBeInvoked()
        {
            GameObject root = new("Audit20261023NeoEditorConditionMenu");
            try
            {
                NumericProbe component = root.AddComponent<NumericProbe>();
                List<string> names = new();
                List<Neo.Condition.ValueType> types = new();
                List<string> labels = new();
                List<bool> methods = new();
                List<ArgumentKind> arguments = new();

                ComponentBindingInspectorShared.BuildConditionMemberLists(component, true, names, types, labels,
                    methods, arguments);

                Assert.That(names, Does.Contain(nameof(NumericProbe.ReadByte)));
                Assert.That(names, Does.Contain(nameof(NumericProbe.ReadShort)));
                Assert.That(names, Does.Not.Contain(nameof(NumericProbe.ReadGeneric)));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RelayMenu_OffersNumericSignaturesCompatibleWithRuntimeFloatInput()
        {
            GameObject root = new("Audit20261023NeoEditorRelayMenu");
            NetworkContextActionRelayEditor editor = ScriptableObject.CreateInstance<NetworkContextActionRelayEditor>();
            try
            {
                NumericProbe component = root.AddComponent<NumericProbe>();
                IReadOnlyList<MethodInfo> candidates = editor.BuildMethodList(component);
                List<string> names = new();
                foreach (MethodInfo method in candidates)
                {
                    if (method != null)
                    {
                        names.Add(method.Name);
                    }
                }

                Assert.That(names, Does.Contain(nameof(NumericProbe.FloatAction)));
                Assert.That(names, Does.Contain(nameof(NumericProbe.DoubleAction)));
                Assert.That(names, Does.Not.Contain(nameof(NumericProbe.IntAction)));
                Assert.That(names, Does.Not.Contain(nameof(NumericProbe.LongAction)));
            }
            finally
            {
                Object.DestroyImmediate(editor);
                Object.DestroyImmediate(root);
            }
        }
    }
}
