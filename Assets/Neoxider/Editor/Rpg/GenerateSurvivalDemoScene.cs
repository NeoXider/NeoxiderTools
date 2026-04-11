#if UNITY_EDITOR
using System.IO;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using Neo.Core.Level;
using Neo.Rpg;
using Neo.Rpg.DemoUtils;
using Neo.NPC;
using Neo.NPC.Combat;
using Neo.Progression;

namespace Neo.Rpg.DemoUtils.Editor
{
    public static class GenerateSurvivalDemoScene
    {
        [MenuItem("Neoxider/Tools/Generate RPG Survival Demo Scene")]
        public static void GenerateScene()
        {
            if (!EditorUtility.DisplayDialog("Generate Survival Demo Scene",
                    "This will create a new scene replacing the current one. Unsaved changes will be lost. Proceed?",
                    "Yes", "Cancel")) return;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);
            scene.name = "Survival_Demo_Generated";

            // 1. Environment
            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Environment_Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            
            // Obstacles
            for (int i = 0; i < 4; i++)
            {
                var obs = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obs.name = $"Obstacle_{i}";
                obs.transform.position = new Vector3(Random.Range(-10f, 10f), 0.5f, Random.Range(-10f, 10f));
                obs.transform.localScale = new Vector3(2f, 2f, 2f);
                obs.transform.SetParent(ground.transform);
            }

            // NavMesh via Reflection
            System.Type navSurfaceType = System.Type.GetType("Unity.AI.Navigation.NavMeshSurface, Unity.AI.Navigation");
            if (navSurfaceType != null)
            {
                var navSurface = ground.AddComponent(navSurfaceType);
                var buildMethod = navSurfaceType.GetMethod("BuildNavMesh");
                if (buildMethod != null) buildMethod.Invoke(navSurface, null);
            }

            // 2. Player
            GameObject player = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            player.name = "Player_Hero";
            player.transform.position = new Vector3(0, 1f, 0);
            player.GetComponent<Renderer>().sharedMaterial.color = Color.blue;

            var levelComp = player.AddComponent<LevelComponent>();
            var progression = player.AddComponent<ProgressionManager>();
            var stats = player.AddComponent<RpgStatsManager>();
            var combatSwitcher = player.AddComponent<PlayerCombatSwitcher>();
            var attackCtrl = player.AddComponent<RpgAttackController>();

            // 3. NPC Prefab Generation
            string prefabDir = "Assets/Neoxider/Samples~/Demo/Prefabs";
            if (!Directory.Exists(prefabDir)) Directory.CreateDirectory(prefabDir);

            GameObject npcBase = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            npcBase.name = "Survival_NPC";
            npcBase.GetComponent<Renderer>().sharedMaterial.color = Color.red;
            npcBase.AddComponent<NavMeshAgent>();
            npcBase.AddComponent<NpcNavigation>();
            var npcCombatant = npcBase.AddComponent<RpgCombatant>();
            npcBase.tag = "Enemy";

            string prefabPath = $"{prefabDir}/Survival_NPC_Generated.prefab";
            GameObject npcPrefab = PrefabUtility.SaveAsPrefabAsset(npcBase, prefabPath);
            Object.DestroyImmediate(npcBase);

            // 4. Spawner
            GameObject spawner = new GameObject("Wave_Spawner");
            var waveSpawner = spawner.AddComponent<RpgWaveSpawner>();
            waveSpawner.NpcPrefab = npcPrefab;
            waveSpawner.PlayerLevelContext = levelComp;
            
            Transform p1 = new GameObject("SpawnPoint_1").transform; p1.SetParent(spawner.transform); p1.position = new Vector3(15, 1, 15);
            Transform p2 = new GameObject("SpawnPoint_2").transform; p2.SetParent(spawner.transform); p2.position = new Vector3(-15, 1, -15);
            Transform p3 = new GameObject("SpawnPoint_3").transform; p3.SetParent(spawner.transform); p3.position = new Vector3(15, 1, -15);
            waveSpawner.SpawnPoints = new Transform[] { p1, p2, p3 };

            // 5. Camera Follow (Simple Child for Demo API brevity)
            Camera.main.transform.SetParent(player.transform);
            Camera.main.transform.localPosition = new Vector3(0, 15, -15);
            Camera.main.transform.localRotation = Quaternion.Euler(45, 0, 0);

            EditorUtility.DisplayDialog("Success", "RPG Survival demo generated successfully. Look at the Hierarchy, save the scene, and press Play!", "OK");
        }
    }
}
#endif
