using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    public class DialoguePlayModeTests
    {
        [UnityTest]
        public IEnumerator Test_Dummy()
        {
            // Placeholder: currently the dialogue system is strongly coupled to TextMeshPro
            // We use this dummy test to ensure the PlayMode assembly functions and runs.
            yield return null;
            Assert.Pass();
        }
    }
}
