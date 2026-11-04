using System;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Neo.Demo.Editor.Tests
{
    /// <summary>
    ///     VampireSurvivorDemoController no longer adds RpgDeathHandler at runtime, so every enemy prefab the
    ///     VampireSurvivorMCP scene spawns must carry an authored one next to its RpgCharacter.
    /// </summary>
    public sealed class VampireSurvivorEnemyPrefabTests
    {
        private const string CharacterType = "Neo.Rpg.Components.RpgCharacter";
        private const string DeathHandlerType = "Neo.Rpg.RpgDeathHandler";

        [Test]
        public void VampireSurvivorScene_EnemyPrefabs_CarryAuthoredDeathHandler()
        {
            string[] guids = AssetDatabase.FindAssets("VampireSurvivorMCP t:Scene");
            Assume.That(guids.Length, Is.GreaterThan(0), "VampireSurvivorMCP.unity is not visible in this layout.");
            string scenePath = AssetDatabase.GUIDToAssetPath(guids[0]);

            int enemies = 0;
            foreach (string dependency in AssetDatabase.GetDependencies(scenePath, false))
            {
                if (!dependency.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(dependency);
                if (prefab == null)
                {
                    continue;
                }

                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component.GetType().FullName != CharacterType ||
                        !component.CompareTag("Enemy"))
                    {
                        continue;
                    }

                    enemies++;
                    Assert.That(HasComponent(component.gameObject, DeathHandlerType), Is.True,
                        dependency + ": '" + component.name + "' needs an authored RpgDeathHandler.");
                }
            }

            Assert.That(enemies, Is.GreaterThanOrEqualTo(2), "The scene spawns CubeEnemy_New and SphereEnemy_Ranged.");
        }

        private static bool HasComponent(GameObject host, string fullName)
        {
            foreach (Component component in host.GetComponents<Component>())
            {
                if (component != null && component.GetType().FullName == fullName)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
