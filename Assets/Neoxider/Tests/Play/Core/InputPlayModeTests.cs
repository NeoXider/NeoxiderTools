using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class InputPlayModeTests
    {
        private GameObject _go;

        [SetUp]
        public void Setup()
        {
            _go = new GameObject();
        }

        [TearDown]
        public void Teardown()
        {
            Object.DestroyImmediate(_go);
        }

        [UnityTest]
        public IEnumerator MultiKeyEventTrigger_Creation_DoesNotThrow()
        {
            MultiKeyEventTrigger trigger = _go.AddComponent<MultiKeyEventTrigger>();
            Assert.IsNotNull(trigger);
            yield return null;
        }
    }
}
