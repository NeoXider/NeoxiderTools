using System.Collections;
using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>Spawn Limit counts spawned objects only: a locked table waits without using the limit up.</summary>
    public sealed class SpawnerUnlockLimitPlayTests
    {
        [UnityTest]
        public IEnumerator LockedPrefab_WithSpawnLimitOne_StillSpawnsOnceItUnlocks()
        {
            GameObject prefab = new GameObject("Boss Template");
            prefab.SetActive(false);
            GameObject parent = new GameObject("Spawned");
            GameObject host = new GameObject("Spawner");
            host.SetActive(false);
            Spawner spawner = host.AddComponent<Spawner>();
            // WHY: plain Instantiate keeps this test free of shared pools that later pool tests would see.
            typeof(Spawner).GetField("_useObjectPool", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(spawner, false);
            spawner.prefabs = new[] { prefab };
            spawner.SetSelection(new[] { 1f }, new[] { 0.4f });
            spawner.spawnLimit = 1;
            spawner.minSpawnDelay = 0.05f;
            spawner.maxSpawnDelay = 0.05f;
            host.SetActive(true);
            spawner.StartSpawn();

            yield return new WaitForSeconds(0.2f);
            Assert.That(CountClones("Boss Template"), Is.Zero, "Nothing spawns while the prefab is locked.");

            yield return new WaitForSeconds(0.8f);
            Assert.That(CountClones("Boss Template"), Is.EqualTo(1),
                "The limit is spent on the spawned object, not on the waiting.");

            foreach (GameObject clone in FindClones("Boss Template"))
            {
                Object.Destroy(clone);
            }

            Object.Destroy(host);
            Object.Destroy(parent);
            Object.Destroy(prefab);
        }

        private static int CountClones(string templateName)
        {
            return FindClones(templateName).Count;
        }

        private static System.Collections.Generic.List<GameObject> FindClones(string templateName)
        {
            System.Collections.Generic.List<GameObject> clones = new System.Collections.Generic.List<GameObject>();
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name.StartsWith(templateName) && candidate.name != templateName)
                {
                    clones.Add(candidate.gameObject);
                }
            }

            return clones;
        }
    }
}
