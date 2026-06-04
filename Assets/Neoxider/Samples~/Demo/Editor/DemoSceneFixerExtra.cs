using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Tools;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Rpg
{
    public static class DemoSceneFixerExtra
    {
        [MenuItem("Tools/Neoxider/RPG/Fix All")]
        public static void FixAll()
        {
            const string meleeTemplatePath = "Assets/Neoxider/Samples/Demo/Data/RpgCombatNpcDemo/Assets/MeleeNpcCharacterTemplate.asset";
            const string rangedTemplatePath = "Assets/Neoxider/Samples/Demo/Data/RpgCombatNpcDemo/Assets/RangedNpcCharacterTemplate.asset";

            // 1. Definition
            var def = AssetDatabase.LoadAssetAtPath<RpgAttackDefinition>("Assets/Neoxider/Samples/Demo/Data/EnemyRangedAttack.asset");
            var proj = AssetDatabase.LoadAssetAtPath<RpgProjectile>("Assets/Neoxider/Samples/Demo/Prefabs/EnemyEnergySphere.prefab");
            if (def != null && proj != null)
            {
                var so = new SerializedObject(def);
                so.FindProperty("_projectilePrefab").objectReferenceValue = proj;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(def);
                Debug.Log("Fixed Definition Projectile");
            }

            // 2. Ranged Enemy
            var rangedPath = "Assets/Neoxider/Samples/Demo/Prefabs/SphereEnemy_Ranged.prefab";
            using (var scope = new PrefabUtility.EditPrefabContentsScope(rangedPath))
            {
                var r = scope.prefabContentsRoot;
                RemoveMissingScripts(r);
                EnsureRpgCharacter(r, rangedTemplatePath);

                var ac = r.GetComponent<RpgAttackController>();
                if (ac != null && def != null)
                {
                    var so = new SerializedObject(ac);
                    var arr = so.FindProperty("_attacks");
                    arr.arraySize = 1;
                    arr.GetArrayElementAtIndex(0).objectReferenceValue = def;
                    so.ApplyModifiedProperties();
                }

                var auto = r.GetComponent<RpgAutoAttackController>();
                if (auto != null)
                {
                    var so = new SerializedObject(auto);
                    so.FindProperty("attackRange").floatValue = 15f;
                    so.FindProperty("attackInterval").floatValue = 1.0f;
                    so.FindProperty("targetTag").stringValue = "Player";
                    so.ApplyModifiedProperties();
                }

                var nav = r.GetComponent<NavMeshAgent>();
                if (nav) nav.stoppingDistance = 10f;
                
                // Add contact damage to ranged as well just in case player gets close!
                var cd = r.GetComponent<RpgContactDamage>();
                if (cd == null) cd = r.AddComponent<RpgContactDamage>();
                var cdSo = new SerializedObject(cd);
                cdSo.FindProperty("damageRange").floatValue = 2.5f;
                cdSo.FindProperty("damage").intValue = 3;
                cdSo.FindProperty("cooldown").floatValue = 1f;
                cdSo.FindProperty("targetTag").stringValue = "Player";
                cdSo.ApplyModifiedProperties();
                Debug.Log("Fixed Ranged Enemy");
            }

            // 3. Melee Enemy
            var meleePath = "Assets/Neoxider/Samples/Demo/Prefabs/CubeEnemy_New.prefab";
            using (var scope = new PrefabUtility.EditPrefabContentsScope(meleePath))
            {
                var r = scope.prefabContentsRoot;
                RemoveMissingScripts(r);
                EnsureRpgCharacter(r, meleeTemplatePath);

                var nav = r.GetComponent<NavMeshAgent>();
                if (nav) nav.stoppingDistance = 1.5f;

                var cd = r.GetComponent<RpgContactDamage>();
                if (cd)
                {
                    var so = new SerializedObject(cd);
                    so.FindProperty("damageRange").floatValue = 2.5f;
                    so.FindProperty("damage").intValue = 10;
                    so.FindProperty("cooldown").floatValue = 1f;
                    so.FindProperty("targetTag").stringValue = "Player";
                    so.ApplyModifiedProperties();
                }
                Debug.Log("Fixed Melee Enemy");
            }

            // 4. In Scene Spawners
            var spawners = Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None);
            var rangedGo = AssetDatabase.LoadAssetAtPath<GameObject>(rangedPath);
            var meleeGo = AssetDatabase.LoadAssetAtPath<GameObject>(meleePath);
            if (rangedGo != null && meleeGo != null)
            {
                foreach (var s in spawners)
                {
                    var so = new SerializedObject(s);
                    var variants = so.FindProperty("variants");
                    if (variants != null)
                    {
                        variants.arraySize = 2;
                        var v0 = variants.GetArrayElementAtIndex(0);
                        v0.FindPropertyRelative("GameObject").objectReferenceValue = meleeGo;
                        v0.FindPropertyRelative("Probability").floatValue = 0.5f;

                        var v1 = variants.GetArrayElementAtIndex(1);
                        v1.FindPropertyRelative("GameObject").objectReferenceValue = rangedGo;
                        v1.FindPropertyRelative("Probability").floatValue = 0.5f;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(s);
                    }
                    else 
                    {
                        // fallback to old prefabs array
                        var p = so.FindProperty("_prefabs");
                        if (p != null)
                        {
                            p.arraySize = 2;
                            p.GetArrayElementAtIndex(0).objectReferenceValue = meleeGo;
                            p.GetArrayElementAtIndex(1).objectReferenceValue = rangedGo;
                            so.ApplyModifiedProperties();
                            EditorUtility.SetDirty(s);
                        }
                    }
                }
                Debug.Log("Fixed Spawners");
            }

            // 5. Sample asset validation fixes
            RepairRpgEnemyPrefab("Assets/Neoxider/Samples/Demo/Prefabs/CubeEnemy.prefab", meleeTemplatePath);
            RepairRpgEnemyPrefab(meleePath, meleeTemplatePath);
            RepairRpgEnemyPrefab(rangedPath, rangedTemplatePath);
#if MIRROR
            EnsureNetworkIdentity(rangedPath);
            EnsureNetworkIdentity(meleePath);
#endif
            var cleanedTerrainCount = CleanMissingTreePrototypes();
            if (cleanedTerrainCount > 0)
            {
                Debug.Log($"Cleaned missing terrain tree prototypes in {cleanedTerrainCount} TerrainData asset(s).");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("All fixes applied successfully.");
        }

        private static void RepairRpgEnemyPrefab(string prefabPath, string templatePath)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var root = scope.prefabContentsRoot;
            if (root == null) return;

            RemoveMissingScripts(root);
            EnsureRpgCharacter(root, templatePath);
#if MIRROR
            EnsureNetworkIdentity(root, prefabPath);
#endif
            EditorUtility.SetDirty(root);
        }

        private static void RemoveMissingScripts(GameObject root)
        {
            if (root == null) return;

            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                int removed = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(transform.gameObject);
                if (removed > 0)
                {
                    Debug.Log($"Removed {removed} missing script(s) from {transform.gameObject.name}.");
                    EditorUtility.SetDirty(transform.gameObject);
                }
            }
        }

        private static void EnsureRpgCharacter(GameObject root, string templatePath)
        {
            if (root == null) return;

            var character = root.GetComponent<RpgCharacter>();
            if (character == null)
            {
                character = root.AddComponent<RpgCharacter>();
                Debug.Log($"Added RpgCharacter to {root.name}.");
            }

            var template = AssetDatabase.LoadAssetAtPath<RpgCharacterTemplate>(templatePath);
            if (template != null && character.Template != template)
            {
                character.Template = template;
            }

            character.isNetworked = false;
            EditorUtility.SetDirty(character);
        }

#if MIRROR
        private static void EnsureNetworkIdentity(string prefabPath)
        {
            using var scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            var root = scope.prefabContentsRoot;
            if (root == null) return;

            RemoveMissingScripts(root);
            EnsureNetworkIdentity(root, prefabPath);
            EditorUtility.SetDirty(root);
        }

        private static void EnsureNetworkIdentity(GameObject root, string prefabPath)
        {
            var identity = root.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                identity = root.AddComponent<NetworkIdentity>();
                Debug.Log($"Added NetworkIdentity to {prefabPath}");
            }

            _ = identity.assetId;
            EditorUtility.SetDirty(root);
            EditorUtility.SetDirty(identity);
        }
#endif

        private static int CleanMissingTreePrototypes()
        {
            var cleanedCount = 0;
            string[] terrainGuids = AssetDatabase.FindAssets("t:TerrainData", new[] { "Assets/Neoxider/Samples" });

            foreach (string guid in terrainGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrain == null) continue;

                TreePrototype[] prototypes = terrain.treePrototypes ?? new TreePrototype[0];
                TreeInstance[] instances = terrain.treeInstances ?? new TreeInstance[0];
                var validPrototypes = new List<TreePrototype>(prototypes.Length);
                var indexMap = new Dictionary<int, int>(prototypes.Length);
                var changed = false;

                for (int i = 0; i < prototypes.Length; i++)
                {
                    if (prototypes[i].prefab == null)
                    {
                        changed = true;
                        continue;
                    }

                    indexMap[i] = validPrototypes.Count;
                    validPrototypes.Add(prototypes[i]);
                }

                if (!changed)
                {
                    foreach (TreeInstance instance in instances)
                    {
                        if (instance.prototypeIndex < 0 || instance.prototypeIndex >= prototypes.Length)
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (!changed) continue;

                var validInstances = new List<TreeInstance>(instances.Length);
                foreach (TreeInstance instance in instances)
                {
                    if (!indexMap.TryGetValue(instance.prototypeIndex, out int newIndex))
                    {
                        continue;
                    }

                    TreeInstance remapped = instance;
                    remapped.prototypeIndex = newIndex;
                    validInstances.Add(remapped);
                }

                terrain.treeInstances = validInstances.ToArray();
                terrain.treePrototypes = validPrototypes.ToArray();
                EditorUtility.SetDirty(terrain);
                cleanedCount++;
            }

            return cleanedCount;
        }
    }
}
