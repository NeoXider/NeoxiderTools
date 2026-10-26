using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using ContractComponent = global::Neo.Editor.Tests.Audit20261023FixtureNeoPropertyAttributeRegressionContractComponent;
using IContract = global::Neo.Editor.Tests.Audit20261023FixturePropertyRegressionContract;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class Audit20261023NeoPropertyAttributeRegressionTests
    {
        public enum ReferenceKind
        {
            GameObject,
            Component,
            ScriptableObject
        }





        private sealed class ContractAsset : ScriptableObject, IContract
        {
        }

        private sealed class PlainAsset : ScriptableObject
        {
        }

        private sealed class ReferenceHolder : ScriptableObject
        {
            [RequireInterface(typeof(IContract))]
            public Object Reference;

            public int Number = 37;
        }

        [Serializable]
        private sealed class Settings
        {
            public int Count = 3;
            public string Caption = "Nested settings";
        }

        private sealed class SettingsHolder : ScriptableObject
        {
            [GUIColor(0.5, 1.0, 0.5)]
            public Settings Colored = new();

            // An unsupported field must still render its ordinary serialized-property fallback.
            [RequireInterface(typeof(IContract))]
            public Settings Unsupported = new();

            public Settings Plain = new();
        }

        [Test]
        public void RequireInterface_NullType_ReportsTheInvalidArgument()
        {
            ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() => new RequireInterface(null));

            Assert.That(exception.ParamName, Is.EqualTo("requireType"));
        }

        [TestCase(ReferenceKind.GameObject, false)]
        [TestCase(ReferenceKind.GameObject, true)]
        [TestCase(ReferenceKind.Component, false)]
        [TestCase(ReferenceKind.Component, true)]
        [TestCase(ReferenceKind.ScriptableObject, false)]
        [TestCase(ReferenceKind.ScriptableObject, true)]
        public void ClearInvalidReference_PersistsOnlyReferencesSatisfyingTheContract(ReferenceKind kind, bool valid)
        {
            GameObject validObject = new("Audit_PropertyAttribute_Valid");
            GameObject invalidObject = new("Audit_PropertyAttribute_Invalid");
            ContractAsset validAsset = ScriptableObject.CreateInstance<ContractAsset>();
            PlainAsset invalidAsset = ScriptableObject.CreateInstance<PlainAsset>();
            ReferenceHolder holder = ScriptableObject.CreateInstance<ReferenceHolder>();
            try
            {
                ContractComponent component = validObject.AddComponent<ContractComponent>();
                Object reference;
                switch (kind)
                {
                    case ReferenceKind.GameObject:
                        reference = valid ? validObject : invalidObject;
                        break;
                    case ReferenceKind.Component:
                        // A sibling implementing the interface must not make this Transform valid.
                        reference = valid ? (Object)component : validObject.transform;
                        break;
                    default:
                        reference = valid ? (Object)validAsset : invalidAsset;
                        break;
                }

                holder.Reference = reference;
                using (SerializedObject serializedObject = new(holder))
                {
                    SerializedProperty property = serializedObject.FindProperty(nameof(ReferenceHolder.Reference));

                    bool cleared = RequireInterfaceDrawer.ClearInvalidReference(property, typeof(IContract));

                    Assert.That(cleared, Is.EqualTo(!valid));
                    Assert.That(property.objectReferenceValue, Is.EqualTo(valid ? reference : null));
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(holder.Reference, Is.EqualTo(valid ? reference : null));
            }
            finally
            {
                Object.DestroyImmediate(holder);
                Object.DestroyImmediate(validAsset);
                Object.DestroyImmediate(invalidAsset);
                Object.DestroyImmediate(validObject);
                Object.DestroyImmediate(invalidObject);
            }
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ClearInvalidReference_MixedSelection_DoesNotOverwriteEitherTarget(bool invalidFirst)
        {
            GameObject invalidObject = new("Audit_PropertyAttribute_Mixed");
            ContractAsset validAsset = ScriptableObject.CreateInstance<ContractAsset>();
            ReferenceHolder first = ScriptableObject.CreateInstance<ReferenceHolder>();
            ReferenceHolder second = ScriptableObject.CreateInstance<ReferenceHolder>();
            try
            {
                Object firstReference = invalidFirst ? (Object)invalidObject : validAsset;
                Object secondReference = invalidFirst ? (Object)validAsset : invalidObject;
                first.Reference = firstReference;
                second.Reference = secondReference;
                using (SerializedObject serializedObject = new(new Object[] { first, second }))
                {
                    SerializedProperty property = serializedObject.FindProperty(nameof(ReferenceHolder.Reference));
                    Assert.That(property.hasMultipleDifferentValues, Is.True);

                    Assert.That(RequireInterfaceDrawer.ClearInvalidReference(property, typeof(IContract)), Is.False);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(first.Reference, Is.SameAs(firstReference));
                Assert.That(second.Reference, Is.SameAs(secondReference));
            }
            finally
            {
                Object.DestroyImmediate(first);
                Object.DestroyImmediate(second);
                Object.DestroyImmediate(validAsset);
                Object.DestroyImmediate(invalidObject);
            }
        }

        [TestCase(null)]
        [TestCase(typeof(GameObject))]
        public void ClearInvalidReference_InvalidContractType_DoesNotDiscardTheReference(Type contractType)
        {
            PlainAsset asset = ScriptableObject.CreateInstance<PlainAsset>();
            ReferenceHolder holder = ScriptableObject.CreateInstance<ReferenceHolder>();
            try
            {
                holder.Reference = asset;
                using (SerializedObject serializedObject = new(holder))
                {
                    SerializedProperty property = serializedObject.FindProperty(nameof(ReferenceHolder.Reference));

                    Assert.That(RequireInterfaceDrawer.ClearInvalidReference(property, contractType), Is.False);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(holder.Reference, Is.SameAs(asset));
            }
            finally
            {
                Object.DestroyImmediate(holder);
                Object.DestroyImmediate(asset);
            }
        }

        [Test]
        public void ClearInvalidReference_UnassignedAndUnsupportedProperties_KeepTheirValues()
        {
            ReferenceHolder holder = ScriptableObject.CreateInstance<ReferenceHolder>();
            try
            {
                using (SerializedObject serializedObject = new(holder))
                {
                    SerializedProperty reference = serializedObject.FindProperty(nameof(ReferenceHolder.Reference));
                    SerializedProperty number = serializedObject.FindProperty(nameof(ReferenceHolder.Number));

                    Assert.That(RequireInterfaceDrawer.ClearInvalidReference(reference, typeof(IContract)), Is.False);
                    Assert.That(RequireInterfaceDrawer.ClearInvalidReference(number, typeof(IContract)), Is.False);
                    serializedObject.ApplyModifiedPropertiesWithoutUndo();
                }

                Assert.That(holder.Reference, Is.Null);
                Assert.That(holder.Number, Is.EqualTo(37));
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }

        [TestCase(false, false)]
        [TestCase(false, true)]
        [TestCase(true, false)]
        [TestCase(true, true)]
        public void Drawers_ReserveTheOrdinaryHeightForNestedProperties(bool interfaceDrawer, bool expanded)
        {
            SettingsHolder holder = ScriptableObject.CreateInstance<SettingsHolder>();
            try
            {
                using (SerializedObject serializedObject = new(holder))
                {
                    SerializedProperty property = serializedObject.FindProperty(interfaceDrawer
                        ? nameof(SettingsHolder.Unsupported)
                        : nameof(SettingsHolder.Colored));
                    SerializedProperty plain = serializedObject.FindProperty(nameof(SettingsHolder.Plain));
                    property.isExpanded = expanded;
                    plain.isExpanded = expanded;
                    PropertyDrawer drawer = interfaceDrawer
                        ? new RequireInterfaceDrawer()
                        : new GUIColorAttributeDrawer();
                    float expectedHeight = EditorGUI.GetPropertyHeight(plain, GUIContent.none, true);

                    Assert.That(drawer.GetPropertyHeight(property, GUIContent.none), Is.EqualTo(expectedHeight));
                    if (expanded)
                    {
                        Assert.That(expectedHeight, Is.GreaterThan(EditorGUIUtility.singleLineHeight));
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(holder);
            }
        }
    }
}
