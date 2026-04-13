using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Condition;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Edit
{
    [TestFixture]
    public class NeoConditionTests
    {
        private class TestComponent : MonoBehaviour
        {
            public int Health { get; set; } = 100;
            public float Speed = 5.5f;
            public bool IsAlive = true;
            public string Name = "Hero";

            public int GetDamage(int modifier)
            {
                return 10 + modifier;
            }
        }

        #region ReflectionCache

        [Test]
        public void ReflectionCache_CachesAndReturnsProperties()
        {
            Type type = typeof(TestComponent);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo prop1 = ReflectionCache.GetProperty(type, "Health", flags);
            PropertyInfo prop2 = ReflectionCache.GetProperty(type, "Health", flags);

            Assert.IsNotNull(prop1);
            Assert.AreSame(prop1, prop2, "ReflectionCache should return the exact same cached reference.");
        }

        [Test]
        public void ReflectionCache_CachesAndReturnsFields()
        {
            Type type = typeof(TestComponent);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            FieldInfo field1 = ReflectionCache.GetField(type, "Speed", flags);
            FieldInfo field2 = ReflectionCache.GetField(type, "Speed", flags);

            Assert.IsNotNull(field1);
            Assert.AreSame(field1, field2, "ReflectionCache should return the exact same cached reference.");
        }

        [Test]
        public void ReflectionCache_CachesAndReturnsMethods()
        {
            Type type = typeof(TestComponent);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            MethodInfo m1 = ReflectionCache.GetMethod(type, "GetDamage", ArgumentKind.Int, flags);
            MethodInfo m2 = ReflectionCache.GetMethod(type, "GetDamage", ArgumentKind.Int, flags);

            Assert.IsNotNull(m1);
            Assert.AreSame(m1, m2, "ReflectionCache should return the exact same cached reference.");
        }

        [Test]
        public void ReflectionCache_NonExistentProperty_ReturnsNull()
        {
            Type type = typeof(TestComponent);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            PropertyInfo prop = ReflectionCache.GetProperty(type, "NonExistent", flags);
            Assert.IsNull(prop);
        }

        [Test]
        public void ReflectionCache_NonExistentField_ReturnsNull()
        {
            Type type = typeof(TestComponent);
            BindingFlags flags = BindingFlags.Public | BindingFlags.Instance;

            FieldInfo field = ReflectionCache.GetField(type, "NonExistent", flags);
            Assert.IsNull(field);
        }

        #endregion

        #region ConditionEntry - ApplyCompareOp via reflection

        // ApplyCompareOp is private static — we test it through reflection since
        // it's the core comparison engine

        private static readonly MethodInfo IntCompareMethod = typeof(ConditionEntry)
            .GetMethod("ApplyCompareOp", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(int), typeof(int), typeof(CompareOp) }, null);

        private static readonly MethodInfo FloatCompareMethod = typeof(ConditionEntry)
            .GetMethod("ApplyCompareOp", BindingFlags.NonPublic | BindingFlags.Static,
                null, new[] { typeof(float), typeof(float), typeof(CompareOp) }, null);

        [Test]
        public void ConditionEntry_IntCompare_Equal()
        {
            Assert.IsNotNull(IntCompareMethod, "ApplyCompareOp(int,int,CompareOp) should exist.");
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 5, 5, CompareOp.Equal }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 5, 3, CompareOp.Equal }));
        }

        [Test]
        public void ConditionEntry_IntCompare_Greater()
        {
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 10, 5, CompareOp.Greater }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 5, 10, CompareOp.Greater }));
        }

        [Test]
        public void ConditionEntry_IntCompare_Less()
        {
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 3, 10, CompareOp.Less }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 10, 3, CompareOp.Less }));
        }

        [Test]
        public void ConditionEntry_IntCompare_NotEqual()
        {
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 5, 3, CompareOp.NotEqual }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 5, 5, CompareOp.NotEqual }));
        }

        [Test]
        public void ConditionEntry_IntCompare_GreaterOrEqual()
        {
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 5, 5, CompareOp.GreaterOrEqual }));
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 10, 5, CompareOp.GreaterOrEqual }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 3, 5, CompareOp.GreaterOrEqual }));
        }

        [Test]
        public void ConditionEntry_IntCompare_LessOrEqual()
        {
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 5, 5, CompareOp.LessOrEqual }));
            Assert.IsTrue((bool)IntCompareMethod.Invoke(null, new object[] { 3, 5, CompareOp.LessOrEqual }));
            Assert.IsFalse((bool)IntCompareMethod.Invoke(null, new object[] { 10, 5, CompareOp.LessOrEqual }));
        }

        [Test]
        public void ConditionEntry_FloatCompare_Equal()
        {
            Assert.IsNotNull(FloatCompareMethod, "ApplyCompareOp(float,float,CompareOp) should exist.");
            Assert.IsTrue((bool)FloatCompareMethod.Invoke(null, new object[] { 5.0f, 5.0f, CompareOp.Equal }));
            Assert.IsFalse((bool)FloatCompareMethod.Invoke(null, new object[] { 5.0f, 5.1f, CompareOp.Equal }));
        }

        [Test]
        public void ConditionEntry_FloatCompare_Greater()
        {
            Assert.IsTrue((bool)FloatCompareMethod.Invoke(null, new object[] { 10.5f, 5.0f, CompareOp.Greater }));
            Assert.IsFalse((bool)FloatCompareMethod.Invoke(null, new object[] { 5.0f, 10.0f, CompareOp.Greater }));
        }

        #endregion

        #region NeoCondition Component

        [Test]
        public void NeoCondition_EmptyConditions_EvaluatesTrue()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            Assert.IsTrue(nc.Evaluate(), "Empty conditions should evaluate as true.");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NeoCondition_AddAndRemoveCondition()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            var entry = new ConditionEntry();

            nc.AddCondition(entry);
            Assert.AreEqual(1, nc.Conditions.Count);

            nc.RemoveCondition(entry);
            Assert.AreEqual(0, nc.Conditions.Count);

            Object.DestroyImmediate(go);
        }

        [Test]
        public void NeoCondition_LogicMode_AND_DefaultIsAND()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            Assert.AreEqual(LogicMode.AND, nc.Logic);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NeoCondition_LogicMode_CanSetOR()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            nc.Logic = LogicMode.OR;
            Assert.AreEqual(LogicMode.OR, nc.Logic);
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NeoCondition_ResetState_ClearsLastResult()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();
            nc.Check(); // sets _lastResult
            Assert.IsTrue(nc.LastResult);
            nc.ResetState();
            Assert.IsFalse(nc.LastResult, "After reset, LastResult should be false (default).");
            Object.DestroyImmediate(go);
        }

        [Test]
        public void NeoCondition_Check_InvokesOnTrueEvent()
        {
            var go = new GameObject("CondObj");
            NeoCondition nc = go.AddComponent<NeoCondition>();

            bool fired = false;
            nc.OnTrue.AddListener(() => fired = true);

            // Disable onlyOnChange to always fire
            FieldInfo field =
                typeof(NeoCondition).GetField("_onlyOnChange", BindingFlags.NonPublic | BindingFlags.Instance);
            field?.SetValue(nc, false);

            nc.Check();
            Assert.IsTrue(fired, "OnTrue should fire when conditions evaluate to true.");
            Object.DestroyImmediate(go);
        }

        #endregion
    }
}
