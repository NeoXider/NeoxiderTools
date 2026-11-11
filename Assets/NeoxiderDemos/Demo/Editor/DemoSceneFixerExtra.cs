using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Tools;
using Neo.Demo.Editor;
#if MIRROR
using Mirror;
#endif

namespace Neo.Editor.Rpg
{
    /// <summary>One-shot repair of the RPG demo prefabs, data and spawners (menu Neoxider/Samples/RPG/Fix All).</summary>
    public static class DemoSceneFixerExtra
    {
        private const string FallbackDemoRoot = "Assets/Neoxider/Samples/Demo";

        [MenuItem("Neoxider/Samples/RPG/Fix All", false, 320)]
        public static void FixAll()
        {
            string demoRoot = DemoSampleRoots.TryResolveDemoRoot(out string resolved, out _) ? resolved : FallbackDemoRoot;
            string meleeTemplatePath = demoRoot + "/Data/RpgCombatNpcDemo/Assets/MeleeNpcCharacterTemplate.asset";
            string rangedTemplatePath = demoRoot + "/Data/RpgCombatNpcDemo/Assets/RangedNpcCharacterTemplate.asset";

            RpgAttackDefinition def = AssetDatabase.LoadAssetAtPath<RpgAttackDefinition>(demoRoot + "/Data/EnemyRangedAttack.asset");
            RpgProjectile proj = AssetDatabase.LoadAssetAtPath<RpgProjectile>(demoRoot + "/Prefabs/EnemyEnergySphere.prefab");
            if (def != null && proj != null)
            {
                SerializedObject so = new SerializedObject(def);
                so.FindProperty("_projectilePrefab").objectReferenceValue = proj;
                so.ApplyModifiedProperties();
                EditorUtility.SetDirty(def);
                Debug.Log("Fixed Definition Projectile");
            }

            string rangedPath = demoRoot + "/Prefabs/SphereEnemy_Ranged.prefab";
            using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(rangedPath))
            {
                GameObject r = scope.prefabContentsRoot;
                RemoveMissingScripts(r);
                EnsureRpgCharacter(r, rangedTemplatePath);

                RpgAttackController ac = r.GetComponent<RpgAttackController>();
                if (ac != null && def != null)
                {
                    SerializedObject so = new SerializedObject(ac);
                    SerializedProperty arr = so.FindProperty("_attacks");
                    arr.arraySize = 1;
                    arr.GetArrayElementAtIndex(0).objectReferenceValue = def;
                    so.ApplyModifiedProperties();
                }

                RpgAutoAttackController auto = r.GetComponent<RpgAutoAttackController>();
                if (auto != null)
                {
                    SerializedObject so = new SerializedObject(auto);
                    so.FindProperty("attackRange").floatValue = 15f;
                    so.FindProperty("attackInterval").floatValue = 1.0f;
                    so.FindProperty("targetTag").stringValue = "Player";
                    so.ApplyModifiedProperties();
                }

                NavMeshAgent nav = r.GetComponent<NavMeshAgent>();
                if (nav) nav.stoppingDistance = 10f;

                // WHY: ranged enemies also deal contact damage so a player who closes in still gets hit.
                RpgContactDamage cd = r.GetComponent<RpgContactDamage>();
                if (cd == null) cd = r.AddComponent<RpgContactDamage>();
                SerializedObject cdSo = new SerializedObject(cd);
                cdSo.FindProperty("damageRange").floatValue = 2.5f;
                cdSo.FindProperty("damage").intValue = 3;
                cdSo.FindProperty("cooldown").floatValue = 1f;
                cdSo.FindProperty("targetTag").stringValue = "Player";
                cdSo.ApplyModifiedProperties();
                Debug.Log("Fixed Ranged Enemy");
            }

            string meleePath = demoRoot + "/Prefabs/CubeEnemy_New.prefab";
            using (PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(meleePath))
            {
                GameObject r = scope.prefabContentsRoot;
                RemoveMissingScripts(r);
                EnsureRpgCharacter(r, meleeTemplatePath);

                NavMeshAgent nav = r.GetComponent<NavMeshAgent>();
                if (nav) nav.stoppingDistance = 1.5f;

                RpgContactDamage cd = r.GetComponent<RpgContactDamage>();
                if (cd)
                {
                    SerializedObject so = new SerializedObject(cd);
                    so.FindProperty("damageRange").floatValue = 2.5f;
                    so.FindProperty("damage").intValue = 10;
                    so.FindProperty("cooldown").floatValue = 1f;
                    so.FindProperty("targetTag").stringValue = "Player";
                    so.ApplyModifiedProperties();
                }
                Debug.Log("Fixed Melee Enemy");
            }

            Spawner[] spawners = Object.FindObjectsByType<Spawner>(FindObjectsSortMode.None);
            GameObject rangedGo = AssetDatabase.LoadAssetAtPath<GameObject>(rangedPath);
            GameObject meleeGo = AssetDatabase.LoadAssetAtPath<GameObject>(meleePath);
            if (rangedGo != null && meleeGo != null)
            {
                foreach (Spawner s in spawners)
                {
                    SerializedObject so = new SerializedObject(s);
                    SerializedProperty variants = so.FindProperty("variants");
                    if (variants != null)
                    {
                        variants.arraySize = 2;
                        SerializedProperty v0 = variants.GetArrayElementAtIndex(0);
                        v0.FindPropertyRelative("GameObject").objectReferenceValue = meleeGo;
                        v0.FindPropertyRelative("Probability").floatValue = 0.5f;

                        SerializedProperty v1 = variants.GetArrayElementAtIndex(1);
                        v1.FindPropertyRelative("GameObject").objectReferenceValue = rangedGo;
                        v1.FindPropertyRelative("Probability").floatValue = 0.5f;
                        so.ApplyModifiedProperties();
                        EditorUtility.SetDirty(s);
                    }
                    else 
                    {
                        // WHY: older Spawner versions serialize a plain _prefabs array instead of variants.
                        SerializedProperty p = so.FindProperty("_prefabs");
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

            RepairRpgEnemyPrefab(demoRoot + "/Prefabs/CubeEnemy.prefab", meleeTemplatePath);
            RepairRpgEnemyPrefab(meleePath, meleeTemplatePath);
            RepairRpgEnemyPrefab(rangedPath, rangedTemplatePath);
#if MIRROR
            EnsureNetworkIdentity(rangedPath);
            EnsureNetworkIdentity(meleePath);
#endif
            int cleanedTerrainCount = CleanMissingTreePrototypes(System.IO.Path.GetDirectoryName(demoRoot)?.Replace('\\', '/'));
            if (cleanedTerrainCount > 0)
            {
                Debug.Log($"Cleaned missing terrain tree prototypes in {cleanedTerrainCount} TerrainData asset(s).");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("All fixes applied successfully.");
        }

        private static void RepairRpgEnemyPrefab(string prefabPath, string templatePath)
        {
            using PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            GameObject root = scope.prefabContentsRoot;
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

            RpgCharacter character = root.GetComponent<RpgCharacter>();
            if (character == null)
            {
                character = root.AddComponent<RpgCharacter>();
                Debug.Log($"Added RpgCharacter to {root.name}.");
            }

            RpgCharacterTemplate template = AssetDatabase.LoadAssetAtPath<RpgCharacterTemplate>(templatePath);
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
            using PrefabUtility.EditPrefabContentsScope scope = new PrefabUtility.EditPrefabContentsScope(prefabPath);
            GameObject root = scope.prefabContentsRoot;
            if (root == null) return;

            RemoveMissingScripts(root);
            EnsureNetworkIdentity(root, prefabPath);
            EditorUtility.SetDirty(root);
        }

        private static void EnsureNetworkIdentity(GameObject root, string prefabPath)
        {
            NetworkIdentity identity = root.GetComponent<NetworkIdentity>();
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

        private static int CleanMissingTreePrototypes(string samplesRoot)
        {
            int cleanedCount = 0;
            string searchRoot = string.IsNullOrEmpty(samplesRoot) ? "Assets/Neoxider/Samples" : samplesRoot;
            string[] terrainGuids = AssetDatabase.FindAssets("t:TerrainData", new[] { searchRoot });

            foreach (string guid in terrainGuids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TerrainData terrain = AssetDatabase.LoadAssetAtPath<TerrainData>(path);
                if (terrain == null) continue;

                TreePrototype[] prototypes = terrain.treePrototypes ?? new TreePrototype[0];
                TreeInstance[] instances = terrain.treeInstances ?? new TreeInstance[0];
                List<TreePrototype> validPrototypes = new List<TreePrototype>(prototypes.Length);
                Dictionary<int, int> indexMap = new Dictionary<int, int>(prototypes.Length);
                bool changed = false;

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

                List<TreeInstance> validInstances = new List<TreeInstance>(instances.Length);
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
