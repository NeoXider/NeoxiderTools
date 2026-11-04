using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class LootSceneUnloadTests
    {
        private GameObject _prefab;
        private string _cloneName;

        [SetUp]
        public void SetUp()
        {
            _prefab = new GameObject("LootUnloadPrefab." + Guid.NewGuid().ToString("N"));
            _prefab.SetActive(false);
            _cloneName = _prefab.name + "(Clone)";
        }

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject clone in FindClones())
            {
                Object.DestroyImmediate(clone);
            }

            if (_prefab != null)
            {
                Object.DestroyImmediate(_prefab);
            }
        }

        [UnityTest]
        public IEnumerator SceneUnload_DoesNotSpawnLootIntoTheRemainingScenes()
        {
            Scene scene = SceneManager.CreateScene("LootUnload." + Guid.NewGuid().ToString("N"));
            GameObject owner = new("LootOwner");
            SceneManager.MoveGameObjectToScene(owner, scene);
            ConfigureLoot(owner);

            AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
            yield return unload;
            yield return null;

            Assert.That(FindClones(), Is.Empty,
                "Objects spawned from OnDestroy during unload leak into the next scene.");
        }

        [UnityTest]
        public IEnumerator GameplayDestroy_StillDropsLoot()
        {
            GameObject owner = new("LootOwner");
            ConfigureLoot(owner);

            Object.Destroy(owner);
            yield return null;

            Assert.That(FindClones(), Has.Count.EqualTo(1));
        }

        private void ConfigureLoot(GameObject owner)
        {
            Loot loot = owner.AddComponent<Loot>();
            loot.lootItems = new[] { _prefab };
            loot.minCount = 1;
            loot.maxCount = 1;
            loot.dropChance = 1f;
            loot.spawnOnDestroy = true;
        }

        private List<GameObject> FindClones()
        {
            List<GameObject> clones = new();
            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name == _cloneName)
                {
                    clones.Add(candidate);
                }
            }

            return clones;
        }
    }
}
