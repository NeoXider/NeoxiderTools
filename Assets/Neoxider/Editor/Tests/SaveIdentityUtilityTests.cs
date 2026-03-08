using NUnit.Framework;
using UnityEngine;

namespace Neo.Save.Tests
{
    public class SaveIdentityUtilityTests
    {
        [Test]
        public void GetComponentKey_IsStableForSameComponent()
        {
            GameObject gameObject = new("StableIdentity");
            StableSaveable saveable = gameObject.AddComponent<StableSaveable>();

            try
            {
                string firstKey = SaveIdentityUtility.GetComponentKey(saveable);
                string secondKey = SaveIdentityUtility.GetComponentKey(saveable);

                Assert.That(firstKey, Is.EqualTo(secondKey));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GetComponentKey_DiffersForSameTypeOnSameGameObject()
        {
            GameObject gameObject = new("DuplicateComponents");
            StableSaveable first = gameObject.AddComponent<StableSaveable>();
            StableSaveable second = gameObject.AddComponent<StableSaveable>();

            try
            {
                string firstKey = SaveIdentityUtility.GetComponentKey(first);
                string secondKey = SaveIdentityUtility.GetComponentKey(second);

                Assert.That(firstKey, Is.Not.EqualTo(secondKey));
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void GetComponentKey_PrefersCustomIdentityProvider()
        {
            GameObject gameObject = new("CustomIdentity");
            CustomIdentitySaveable saveable = gameObject.AddComponent<CustomIdentitySaveable>();

            try
            {
                string key = SaveIdentityUtility.GetComponentKey(saveable);

                StringAssert.Contains("custom-save-id", key);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        private sealed class StableSaveable : MonoBehaviour, ISaveableComponent
        {
            public void OnDataLoaded()
            {
            }
        }

        private sealed class CustomIdentitySaveable : MonoBehaviour, ISaveableComponent, ISaveIdentityProvider
        {
            public string SaveIdentity => "custom-save-id";

            public void OnDataLoaded()
            {
            }
        }
    }
}
