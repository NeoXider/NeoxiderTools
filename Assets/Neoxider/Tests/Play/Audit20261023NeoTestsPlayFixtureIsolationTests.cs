using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Save;
using Neo.Tests.Play.RPG;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoTestsPlayFixtureIsolationTests
    {
        [Test]
        public void InventoryFixture_PreservesAmbientSaveDataAndRestoresTheSameProvider()
        {
            ISaveProvider original = SaveProvider.CurrentProvider;
            ShopPurchasePlayModeTests.MemorySaveProvider ambient = new ShopPurchasePlayModeTests.MemorySaveProvider();
            ambient.SetInt("unrelated-profile", 73);
            SaveProvider.SetProvider(ambient);
            InventoryPlayModeTests fixture = new InventoryPlayModeTests();
            try
            {
                fixture.SetUp();
                Assert.That(ambient.GetInt("unrelated-profile"), Is.EqualTo(73));
                SaveProvider.SetInt("inventory-test-only", 12);
                fixture.TearDown();

                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(ambient));
                Assert.That(ambient.GetInt("unrelated-profile"), Is.EqualTo(73));
                Assert.That(ambient.HasKey("inventory-test-only"), Is.False);
            }
            finally
            {
                fixture.TearDown();
                SaveProvider.SetProvider(original);
            }
        }

        [Test]
        public void ShopFixture_DestroyCallbacksKeepUsingTheIsolatedProvider()
        {
            ISaveProvider original = SaveProvider.CurrentProvider;
            ShopPurchasePlayModeTests.MemorySaveProvider ambient = new ShopPurchasePlayModeTests.MemorySaveProvider();
            SaveProvider.SetProvider(ambient);
            ShopPurchasePlayModeTests fixture = new ShopPurchasePlayModeTests();
            GameObject owned = null;
            try
            {
                fixture.SetUp();
                ISaveProvider isolated = SaveProvider.CurrentProvider;
                owned = fixture.Track(new GameObject("ShopFixtureSaveOnDisable"));
                owned.AddComponent<SaveOnDisable>();
                fixture.TearDown();

                Assert.That(owned == null, Is.True);
                Assert.That(isolated.GetString(SaveOnDisable.Key), Is.EqualTo("cleanup"));
                Assert.That(ambient.HasKey(SaveOnDisable.Key), Is.False);
                Assert.That(SaveProvider.CurrentProvider, Is.SameAs(ambient));
            }
            finally
            {
                fixture.TearDown();
                if (owned != null)
                {
                    Object.DestroyImmediate(owned);
                }

                SaveProvider.SetProvider(original);
            }
        }

        [TestCase("fr-FR")]
        [TestCase("ru-RU")]
        public void MemoryProvider_FloatRoundTripsInCommaDecimalCultures(string culture)
        {
            CultureInfo previousCulture = CultureInfo.CurrentCulture;
            try
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
                ShopPurchasePlayModeTests.MemorySaveProvider provider = new ShopPurchasePlayModeTests.MemorySaveProvider();
                provider.SetFloat("fraction", 1.25f);
                Assert.That(provider.GetFloat("fraction", -1f), Is.EqualTo(1.25f).Within(0.0001f));
            }
            finally
            {
                CultureInfo.CurrentCulture = previousCulture;
            }
        }

        [UnityTest]
        public IEnumerator InteractiveFixture_PreservesUnrelatedObjectsAndRestoresTheEventSystem()
        {
            EventSystem originalEventSystem = EventSystem.current;
            GameObject ambientRoot = new GameObject("AmbientActiveRoot");
            GameObject ambientInactiveRoot = new GameObject("AmbientInactiveRoot");
            ambientInactiveRoot.SetActive(false);
            GameObject ambientEventSystemObject = new GameObject("AmbientEventSystem", typeof(EventSystem));
            EventSystem ambientEventSystem = ambientEventSystemObject.GetComponent<EventSystem>();
            EventSystem.current = ambientEventSystem;
            HashSet<Camera> previousCameras = new HashSet<Camera>(
                Object.FindObjectsByType<Camera>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            InteractiveObjectPlayTests fixture = new InteractiveObjectPlayTests();
            try
            {
                fixture.SetUp();
                GameObject ownedCamera = null;
                foreach (Camera camera in Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))
                {
                    if (!previousCameras.Contains(camera))
                    {
                        ownedCamera = camera.gameObject;
                    }
                }

                Assert.That(ownedCamera, Is.Not.Null);
                Assert.That(ambientEventSystem != null, Is.True);
                Assert.That(ambientEventSystem.enabled, Is.False);
                yield return fixture.PublicAPI_InteractionDistance_GetSet();
                yield return fixture.TearDown();

                Assert.That(ownedCamera == null, Is.True, "Fixture-created objects must be released.");
                Assert.That(ambientRoot != null && ambientRoot.activeSelf, Is.True);
                Assert.That(ambientInactiveRoot != null && !ambientInactiveRoot.activeSelf, Is.True);
                Assert.That(ambientEventSystem != null && ambientEventSystem.enabled, Is.True);
                Assert.That(EventSystem.current, Is.SameAs(ambientEventSystem));
            }
            finally
            {
                // Cleanup is synchronous; the fixture yields once after restoring owned state.
                IEnumerator cleanup = fixture.TearDown();
                while (cleanup.MoveNext()) { }
                Object.DestroyImmediate(ambientRoot);
                Object.DestroyImmediate(ambientInactiveRoot);
                Object.DestroyImmediate(ambientEventSystemObject);
                if (originalEventSystem != null)
                {
                    EventSystem.current = originalEventSystem;
                }
            }
        }

        [UnityTest]
        public IEnumerator AudioFixture_InterruptedTestReleasesItsClipsAndKeepsUnrelatedClips()
        {
            AudioClip ambient = AudioClip.Create("AmbientClip", 64, 1, 44100, false);
            HashSet<AudioClip> previousClips = new HashSet<AudioClip>(Resources.FindObjectsOfTypeAll<AudioClip>());
            AmTeardownPlayModeTests fixture = new AmTeardownPlayModeTests();
            IEnumerator scenario = fixture.DestroyingTheAmGameObject_WithCreatedVoices_LogsNothing();
            try
            {
                Assert.That(scenario.MoveNext(), Is.True);
                yield return scenario.Current;
                Assert.That(scenario.MoveNext(), Is.True);
                List<AudioClip> createdClips = new List<AudioClip>();
                foreach (AudioClip clip in Resources.FindObjectsOfTypeAll<AudioClip>())
                {
                    if (!previousClips.Contains(clip) && (clip.name == "hit" || clip.name == "click" || clip.name == "tap"))
                    {
                        createdClips.Add(clip);
                    }
                }

                Assert.That(createdClips.Count, Is.EqualTo(3));
                fixture.StopCapturing();
                foreach (AudioClip clip in createdClips)
                {
                    Assert.That(clip == null, Is.True, "An interrupted test must release native AudioClip objects.");
                }

                Assert.That(ambient != null, Is.True);
            }
            finally
            {
                (scenario as IDisposable)?.Dispose();
                fixture.StopCapturing();
                Object.DestroyImmediate(ambient);
            }
        }

        [Test]
        public void RpgFixture_ReleasesCharacterTemplatesAndKeepsUnrelatedProjectiles()
        {
            GameObject ambientProjectile = new GameObject("AmbientProjectile");
            ambientProjectile.SetActive(false);
            ambientProjectile.AddComponent<RpgProjectile>();
            HashSet<RpgCharacterTemplate> previousTemplates =
                new HashSet<RpgCharacterTemplate>(Resources.FindObjectsOfTypeAll<RpgCharacterTemplate>());
            RpgCombatPlayModeTests fixture = new RpgCombatPlayModeTests();
            IEnumerator scenario = fixture.PlayerDirectAttack_DealsDamageToNpc();
            try
            {
                Assert.That(scenario.MoveNext(), Is.True);
                List<RpgCharacterTemplate> createdTemplates = new List<RpgCharacterTemplate>();
                foreach (RpgCharacterTemplate template in Resources.FindObjectsOfTypeAll<RpgCharacterTemplate>())
                {
                    if (!previousTemplates.Contains(template) &&
                        (template.name == "PlayerTemplate" || template.name == "MeleeDummyTemplate"))
                    {
                        createdTemplates.Add(template);
                    }
                }

                Assert.That(createdTemplates.Count, Is.EqualTo(2));
                fixture.TearDown();
                foreach (RpgCharacterTemplate template in createdTemplates)
                {
                    Assert.That(template == null, Is.True);
                }

                Assert.That(ambientProjectile != null && !ambientProjectile.activeSelf, Is.True);
            }
            finally
            {
                (scenario as IDisposable)?.Dispose();
                fixture.TearDown();
                Object.DestroyImmediate(ambientProjectile);
            }
        }

        private sealed class SaveOnDisable : MonoBehaviour
        {
            public const string Key = "Audit20261023NeoTestsPlay_cleanup";

            private void OnDisable()
            {
                SaveProvider.SetString(Key, "cleanup");
            }
        }
    }
}
