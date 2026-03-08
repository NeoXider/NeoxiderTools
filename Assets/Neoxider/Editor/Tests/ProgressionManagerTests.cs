using System;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Neo.Progression;
using Neo.Save;
using UnityEngine;

namespace Neo.Progression.Tests
{
    public class ProgressionManagerTests
    {
        [Test]
        public void AddXp_UpdatesLevelAndPerkPoints()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            LevelCurveDefinition levelCurve = CreateLevelCurve(
                CreateLevel(1, 0, 0),
                CreateLevel(2, 100, 2),
                CreateLevel(3, 250, 1));

            GameObject managerObject = new("ProgressionManager");
            ProgressionManager manager = managerObject.AddComponent<ProgressionManager>();
            manager.SetDefinitions(levelCurve, null, null);
            manager.SaveKey = "ProgressionTests.Levels";

            try
            {
                manager.ResetProgression();
                manager.AddXp(120);

                Assert.That(manager.TotalXp, Is.EqualTo(120));
                Assert.That(manager.CurrentLevel, Is.EqualTo(2));
                Assert.That(manager.AvailablePerkPoints, Is.EqualTo(2));
            }
            finally
            {
                ProgressionManager.DestroyInstance();
                if (levelCurve != null)
                {
                    UnityEngine.Object.DestroyImmediate(levelCurve);
                }
            }
        }

        [Test]
        public void SaveAndLoad_RestoresUnlockedNodesAndPurchasedPerks()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            LevelCurveDefinition levelCurve = CreateLevelCurve(
                CreateLevel(1, 0, 0),
                CreateLevel(2, 100, 1));
            UnlockTreeDefinition unlockTree = CreateUnlockTree(CreateNode("starter-node", false, 2));
            PerkTreeDefinition perkTree = CreatePerkTree(CreatePerk("starter-perk", 1, 2, new[] { "starter-node" }));

            GameObject firstManagerObject = new("ProgressionManager_First");
            ProgressionManager firstManager = firstManagerObject.AddComponent<ProgressionManager>();
            firstManager.SetDefinitions(levelCurve, unlockTree, perkTree);
            firstManager.SaveKey = "ProgressionTests.Persistence";

            try
            {
                firstManager.ResetProgression();
                firstManager.AddXp(150);

                Assert.That(firstManager.TryUnlockNode("starter-node", out string unlockError), Is.True, unlockError);
                Assert.That(firstManager.TryBuyPerk("starter-perk", out string perkError), Is.True, perkError);

                ProgressionManager.DestroyInstance();

                GameObject secondManagerObject = new("ProgressionManager_Second");
                ProgressionManager secondManager = secondManagerObject.AddComponent<ProgressionManager>();
                secondManager.SetDefinitions(levelCurve, unlockTree, perkTree);
                secondManager.SaveKey = "ProgressionTests.Persistence";
                secondManager.LoadProfile();

                try
                {
                    Assert.That(secondManager.TotalXp, Is.EqualTo(150));
                    Assert.That(secondManager.CurrentLevel, Is.EqualTo(2));
                    Assert.That(secondManager.HasUnlockedNode("starter-node"), Is.True);
                    Assert.That(secondManager.HasPurchasedPerk("starter-perk"), Is.True);
                    Assert.That(secondManager.AvailablePerkPoints, Is.EqualTo(0));
                }
                finally
                {
                    ProgressionManager.DestroyInstance();
                }
            }
            finally
            {
                if (levelCurve != null)
                {
                    UnityEngine.Object.DestroyImmediate(levelCurve);
                }

                if (unlockTree != null)
                {
                    UnityEngine.Object.DestroyImmediate(unlockTree);
                }

                if (perkTree != null)
                {
                    UnityEngine.Object.DestroyImmediate(perkTree);
                }
            }
        }

        private static LevelCurveDefinition CreateLevelCurve(params ProgressionLevelDefinition[] levels)
        {
            LevelCurveDefinition definition = ScriptableObject.CreateInstance<LevelCurveDefinition>();
            SetPrivateField(definition, "_levels", new List<ProgressionLevelDefinition>(levels));
            return definition;
        }

        private static ProgressionLevelDefinition CreateLevel(int level, int requiredXp, int grantedPerkPoints)
        {
            ProgressionLevelDefinition definition = new();
            SetPrivateField(definition, "_level", level);
            SetPrivateField(definition, "_requiredXp", requiredXp);
            SetPrivateField(definition, "_grantedPerkPoints", grantedPerkPoints);
            SetPrivateField(definition, "_rewards", new List<ProgressionReward>());
            return definition;
        }

        private static UnlockTreeDefinition CreateUnlockTree(params UnlockNodeDefinition[] nodes)
        {
            UnlockTreeDefinition definition = ScriptableObject.CreateInstance<UnlockTreeDefinition>();
            SetPrivateField(definition, "_nodes", new List<UnlockNodeDefinition>(nodes));
            return definition;
        }

        private static UnlockNodeDefinition CreateNode(string id, bool unlockedByDefault, int requiredLevel)
        {
            UnlockNodeDefinition definition = new();
            SetPrivateField(definition, "_id", id);
            SetPrivateField(definition, "_displayName", id);
            SetPrivateField(definition, "_unlockedByDefault", unlockedByDefault);
            SetPrivateField(definition, "_requiredLevel", requiredLevel);
            SetPrivateField(definition, "_prerequisiteNodeIds", new List<string>());
            SetPrivateField(definition, "_conditions", CreateEmptyPrivateList(definition, "_conditions"));
            SetPrivateField(definition, "_rewards", new List<ProgressionReward>());
            return definition;
        }

        private static PerkTreeDefinition CreatePerkTree(params PerkDefinition[] perks)
        {
            PerkTreeDefinition definition = ScriptableObject.CreateInstance<PerkTreeDefinition>();
            SetPrivateField(definition, "_perks", new List<PerkDefinition>(perks));
            return definition;
        }

        private static PerkDefinition CreatePerk(string id, int cost, int requiredLevel, IReadOnlyList<string> requiredUnlockNodes)
        {
            PerkDefinition definition = new();
            SetPrivateField(definition, "_id", id);
            SetPrivateField(definition, "_displayName", id);
            SetPrivateField(definition, "_cost", cost);
            SetPrivateField(definition, "_requiredLevel", requiredLevel);
            SetPrivateField(definition, "_prerequisitePerkIds", new List<string>());
            SetPrivateField(definition, "_requiredUnlockNodeIds", new List<string>(requiredUnlockNodes));
            SetPrivateField(definition, "_conditions", CreateEmptyPrivateList(definition, "_conditions"));
            SetPrivateField(definition, "_rewards", new List<ProgressionReward>());
            return definition;
        }

        private static object CreateEmptyPrivateList(object target, string fieldName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");

            Type fieldType = fieldInfo.FieldType;
            object list = Activator.CreateInstance(fieldType);
            Assert.That(list, Is.Not.Null, $"Could not create list for field '{fieldName}'.");
            return list;
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null, $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            fieldInfo.SetValue(target, value);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _values.TryGetValue(key, out object value) && value is int intValue ? intValue : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _values.TryGetValue(key, out object value) && value is float floatValue ? floatValue : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string stringValue ? stringValue : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _values.TryGetValue(key, out object value) && value is bool boolValue ? boolValue : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _values.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _values.Clear();
            }

            public void Save()
            {
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
