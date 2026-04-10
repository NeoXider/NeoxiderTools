using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public class PhysicsToolsTests
    {
        private GameObject _testObj;
        private ExplosiveForce _explosive;

        [SetUp]
        public void SetUp()
        {
            _testObj = new GameObject("Explosive");
            _explosive = _testObj.AddComponent<ExplosiveForce>();
        }

        [TearDown]
        public void TearDown()
        {
            if (_testObj != null)
            {
                Object.DestroyImmediate(_testObj);
            }
        }

        [Test]
        public void SetForce_ClampsToZero()
        {
            _explosive.SetForce(-10f);
            Assert.AreEqual(0f, _explosive.CurrentForce, 0.001f);
        }

        [Test]
        public void SetForce_SetsCorrectValue()
        {
            _explosive.SetForce(250f);
            Assert.AreEqual(250f, _explosive.CurrentForce, 0.001f);
        }

        [Test]
        public void SetRadius_ClampsToZero()
        {
            _explosive.SetRadius(-5f);
            Assert.AreEqual(0f, _explosive.CurrentRadius, 0.001f);
        }

        [Test]
        public void SetRadius_SetsCorrectValue()
        {
            _explosive.SetRadius(15f);
            Assert.AreEqual(15f, _explosive.CurrentRadius, 0.001f);
        }

        [Test]
        public void ResetExplosion_ClearsHasExploded()
        {
            // Force manual mode through reflection so we can call Explode
            FieldInfo field = typeof(ExplosiveForce).GetField("activationMode",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            field.SetValue(_explosive, ExplosiveForce.ActivationMode.Manual);

            _explosive.Explode();
            Assert.IsTrue(_explosive.HasExploded);

            _explosive.ResetExplosion();
            Assert.IsFalse(_explosive.HasExploded);
        }

        [Test]
        public void HasExploded_InitiallyFalse()
        {
            Assert.IsFalse(_explosive.HasExploded);
        }
    }
}
