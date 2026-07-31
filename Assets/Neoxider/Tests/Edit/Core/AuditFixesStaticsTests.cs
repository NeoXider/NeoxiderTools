using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.GridSystem;
using Neo.Save;
using Neo.Shop;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
using UIManager = Neo.UI.UI;

namespace Neo.Tools.Tests
{
    /// <summary>
    ///     Guards the SubsystemRegistration resets that keep mutable statics from leaking into the next
    ///     play session when Enter Play Mode Options disable domain reload, plus the singleton reset
    ///     sweep that must survive a two-level singleton subclass.
    /// </summary>
    public sealed class AuditFixesStaticsTests
    {
        private readonly List<GameObject> _created = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _created.Count; i++)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();

            TryInvokeStaticReset(typeof(FieldGenerator));
            TryInvokeStaticReset(typeof(InventorySlotGridView));
            TryInvokeStaticReset(typeof(Money));
            TryInvokeStaticReset(typeof(GlobalSave));
            TryInvokeStaticReset(typeof(UIManager));
        }

        [Test]
        public void SingletonSweep_WithTwoLevelSubclass_DoesNotThrowAndKeepsResetting()
        {
            Assume.That(typeof(AuditFixesSingletonLeaf).BaseType, Is.EqualTo(typeof(AuditFixesSingletonRoot)));
            SetSearchFailed(true);

            Exception thrown = RunSingletonSweep();

            Assert.That(thrown, Is.Null,
                "A two-level singleton subclass must not abort the SubsystemRegistration sweep.");
            Assert.That(GetSearchFailed(), Is.False,
                "Singleton<T> statics must still be reset after an unresolvable subclass.");
        }

        [Test]
        public void FieldGeneratorReset_ClearsStaticInstance()
        {
            FieldGenerator generator = CreateFieldGenerator();
            SetFieldGeneratorInstance(generator);
            Assume.That(FieldGenerator.I, Is.Not.Null);

            InvokeStaticReset(typeof(FieldGenerator));

            Assert.That(FieldGenerator.I, Is.Null);
        }

        [Test]
        public void FieldGeneratorDestroy_ClearsStaticInstance()
        {
            FieldGenerator generator = CreateFieldGenerator();
            SetFieldGeneratorInstance(generator);

            // WHY: FieldGenerator is not [ExecuteAlways], so Unity never calls its OnDestroy in edit mode -
            // invoke the handler directly to assert the contract the play-mode teardown relies on.
            MethodInfo onDestroy = typeof(FieldGenerator).GetMethod(
                "OnDestroy", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(onDestroy, Is.Not.Null, "FieldGenerator must clear the static reference on destroy.");
            onDestroy.Invoke(generator, null);

            Assert.That(FieldGenerator.I, Is.Null,
                "A destroyed generator must not stay in the static reference (MissingReferenceException).");
        }

        [Test]
        public void InventorySlotGridViewReset_ClearsPendingSelection()
        {
            var owner = new GameObject("AuditFixesInventoryGrid");
            _created.Add(owner);
            InventorySlotGridView grid = owner.AddComponent<InventorySlotGridView>();
            SetPrivateStatic(typeof(InventorySlotGridView), "_selectedGrid", grid);
            SetPrivateStatic(typeof(InventorySlotGridView), "_selectedSlot", 3);

            InvokeStaticReset(typeof(InventorySlotGridView));

            Assert.That(GetPrivateStatic(typeof(InventorySlotGridView), "_selectedGrid"), Is.Null);
            Assert.That(GetPrivateStatic(typeof(InventorySlotGridView), "_selectedSlot"), Is.EqualTo(-1));
        }

        [Test]
        public void MoneyReset_ClearsWalletRegistry()
        {
            var owner = new GameObject("AuditFixesMoneyRegistry");
            _created.Add(owner);
            Money money = owner.AddComponent<Money>();
            List<Money> registry = (List<Money>)GetPrivateStatic(typeof(Money), "Registry");
            registry.Add(money);
            Assume.That(registry.Count, Is.GreaterThan(0));

            InvokeStaticReset(typeof(Money));

            Assert.That(registry.Count, Is.Zero);
        }

        [Test]
        public void GlobalSaveReset_ClearsCachedDataAndReadyFlag()
        {
            SetPrivateStatic(typeof(GlobalSave), "_data", new GlobalData());
            GlobalSave.IsReady = true;

            InvokeStaticReset(typeof(GlobalSave));

            Assert.That(GetPrivateStatic(typeof(GlobalSave), "_data"), Is.Null);
            Assert.That(GlobalSave.IsReady, Is.False);
        }

        [Test]
        public void UiReset_ClearsStaticInstance()
        {
            var owner = new GameObject("AuditFixesUi");
            _created.Add(owner);
            UIManager.I = owner.AddComponent<UIManager>();

            InvokeStaticReset(typeof(UIManager));

            Assert.That(UIManager.I, Is.Null);
        }

        private FieldGenerator CreateFieldGenerator()
        {
            var owner = new GameObject("AuditFixesFieldGenerator");
            _created.Add(owner);
            owner.AddComponent<Grid>();
            return owner.AddComponent<FieldGenerator>();
        }

        private static Exception RunSingletonSweep()
        {
            Type sweep = typeof(Singleton<>).Assembly.GetType("Neo.Tools.SingletonRuntimeReset");
            Assert.That(sweep, Is.Not.Null, "Neo.Tools.SingletonRuntimeReset not found.");

            MethodInfo method = sweep.GetMethod("ResetStaticState", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, "SingletonRuntimeReset.ResetStaticState not found.");

            try
            {
                method.Invoke(null, null);
                return null;
            }
            catch (TargetInvocationException ex)
            {
                return ex.InnerException;
            }
        }

        private static void InvokeStaticReset(Type type)
        {
            MethodInfo method = type.GetMethod("ResetStaticState", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(method, Is.Not.Null, $"{type.Name}.ResetStaticState not found.");

            RuntimeInitializeOnLoadMethodAttribute attribute =
                method.GetCustomAttribute<RuntimeInitializeOnLoadMethodAttribute>();
            Assert.That(attribute, Is.Not.Null,
                $"{type.Name}.ResetStaticState must be a RuntimeInitializeOnLoadMethod hook.");
            Assert.That(attribute.loadType, Is.EqualTo(RuntimeInitializeLoadType.SubsystemRegistration),
                $"{type.Name}.ResetStaticState must run on SubsystemRegistration.");

            method.Invoke(null, null);
        }

        private static void TryInvokeStaticReset(Type type)
        {
            type.GetMethod("ResetStaticState", BindingFlags.NonPublic | BindingFlags.Static)?.Invoke(null, null);
        }

        private static void SetFieldGeneratorInstance(FieldGenerator generator)
        {
            PropertyInfo property = typeof(FieldGenerator).GetProperty(
                nameof(FieldGenerator.I),
                BindingFlags.Public | BindingFlags.Static);
            MethodInfo setter = property?.GetSetMethod(true);
            Assert.That(setter, Is.Not.Null, "FieldGenerator.I setter not found.");
            setter.Invoke(null, new object[] { generator });
        }

        private static void SetSearchFailed(bool value)
        {
            SearchFailedField().SetValue(null, value);
        }

        private static bool GetSearchFailed()
        {
            return (bool)SearchFailedField().GetValue(null);
        }

        private static FieldInfo SearchFailedField()
        {
            FieldInfo field = typeof(Singleton<AuditFixesSingletonRoot>).GetField(
                "_searchFailed",
                BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, "Singleton<T>._searchFailed not found.");
            return field;
        }

        private static void SetPrivateStatic(Type type, string fieldName, object value)
        {
            StaticField(type, fieldName).SetValue(null, value);
        }

        private static object GetPrivateStatic(Type type, string fieldName)
        {
            return StaticField(type, fieldName).GetValue(null);
        }

        private static FieldInfo StaticField(Type type, string fieldName)
        {
            FieldInfo field = type.GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Static);
            Assert.That(field, Is.Not.Null, $"{type.Name}.{fieldName} not found.");
            return field;
        }

        private class AuditFixesSingletonRoot : Singleton<AuditFixesSingletonRoot>
        {
        }

        /// <summary>
        ///     Two-level subclass: <c>Singleton&lt;&gt;.MakeGenericType</c> on it violates the
        ///     <c>where T : Singleton&lt;T&gt;</c> constraint, which used to abort the whole sweep.
        /// </summary>
        private sealed class AuditFixesSingletonLeaf : AuditFixesSingletonRoot
        {
        }
    }
}
