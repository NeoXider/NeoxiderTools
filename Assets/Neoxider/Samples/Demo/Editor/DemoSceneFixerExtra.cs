using UnityEditor;
using UnityEngine;
using UnityEngine.AI;
using Neo.Rpg;
using Neo.Tools;

namespace Neo.Editor.Rpg
{
    public static class DemoSceneFixerExtra
    {
        [MenuItem("Tools/Neoxider/RPG/Fix All")]
        public static void FixAll()
        {
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

            AssetDatabase.SaveAssets();
            Debug.Log("All fixes applied successfully.");
        }
    }
}
