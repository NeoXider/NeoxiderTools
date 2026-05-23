using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Core.Resources;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;
#if MIRROR
using Mirror;
#endif

namespace Neo.Tests.Edit.RPG
{
    [TestFixture]
    public class RpgCharacterTests
    {
        [Test]
        public void ApplyTemplate_UsesCanonicalHpAndCustomResourceIds()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();

            try
            {
                template.resources = new[]
                {
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId(RpgStatPreset.Hp),
                        startCurrent = 75f,
                        startMax = 100f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    },
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId("DarkMana"),
                        startCurrent = 10f,
                        startMax = 30f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    }
                };

                character.ApplyTemplate(template);

                Assert.That(character.HpValue, Is.EqualTo(75f));
                Assert.That(character.MaxHpValue, Is.EqualTo(100f));
                Assert.That(character.GetResource("DarkMana"), Is.EqualTo(10f));
                Assert.That(character.GetResourceMax("DarkMana"), Is.EqualTo(30f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void DamageAndHeal_UpdateReactiveHp()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();

            try
            {
                character.ApplyTemplate(template);

                float dealt = character.Damage(25f);
                float healed = character.Heal(10f);

                Assert.That(dealt, Is.EqualTo(25f));
                Assert.That(healed, Is.EqualTo(10f));
                Assert.That(character.HpValue, Is.EqualTo(85f));
                Assert.That(character.HpPercentState.CurrentValue, Is.EqualTo(0.85f).Within(0.001f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SpendAndRefill_CustomResource_WorkThroughUniversalApi()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();

            try
            {
                template.resources = new[]
                {
                    template.resources[0],
                    new RpgResourceDefinition
                    {
                        id = new RpgStatId("BloodMana"),
                        startCurrent = 40f,
                        startMax = 80f,
                        restoreOnAwake = true,
                        restoreToFull = false
                    }
                };
                character.ApplyTemplate(template);

                bool spent = character.Spend("BloodMana", 15f);
                float refilled = character.Refill("BloodMana", 5f);

                Assert.That(spent, Is.True);
                Assert.That(refilled, Is.EqualTo(5f));
                Assert.That(character.GetResource("BloodMana"), Is.EqualTo(30f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void ManualUpgrade_CanIncreaseStatAndDerivedResourceMax()
        {
            GameObject go = new("RpgCharacter");
            RpgCharacter character = AddCharacter(go);
            RpgCharacterTemplate template = CreateBasicTemplate();
            RpgProgressionDefinition progression = ScriptableObject.CreateInstance<RpgProgressionDefinition>();

            try
            {
                progression.growthMode = RpgLevelGrowthMode.ManualUpgradePoints;
                progression.upgradePointsPerLevel = 1;
                progression.upgradeRules = new[]
                {
                    new RpgStatUpgradeRule
                    {
                        statId = new RpgStatId(RpgStatPreset.Vitality),
                        increasePerPoint = 1f,
                        costPerUpgrade = 1,
                        maxUpgradeCount = -1,
                        derivedResourceModifiers = new[]
                        {
                            new RpgResourceModifier
                            {
                                resourceId = new RpgStatId(RpgStatPreset.Hp),
                                kind = RpgResourceModifierKind.AddMaxFlat,
                                value = 15f
                            }
                        }
                    }
                };
                template.progression = progression;
                template.stats = new[]
                {
                    new RpgStatDefinition
                    {
                        id = new RpgStatId(RpgStatPreset.Vitality),
                        baseValue = 10f
                    }
                };

                character.ApplyTemplate(template);
                character.AddUpgradePoints(1);
                bool upgraded = character.UpgradeStat(nameof(RpgStatPreset.Vitality));

                Assert.That(upgraded, Is.True);
                Assert.That(character.GetStat(nameof(RpgStatPreset.Vitality)), Is.EqualTo(11f));
                Assert.That(character.MaxHpValue, Is.EqualTo(115f));
            }
            finally
            {
                Object.DestroyImmediate(progression);
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(go);
            }
        }

        [Test]
        public void SaveAndLoadProfile_UsesSaveProvider()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            const string saveKey = "RpgCharacterTests.Profile";
            RpgCharacterTemplate template = CreateBasicTemplate();

            GameObject firstGo = new("RpgCharacter_First");
            RpgCharacter first = AddCharacter(firstGo);
            SetPrivateField(first, "_saveKey", saveKey);

            try
            {
                first.ApplyTemplate(template);
                first.Damage(40f);
                first.SaveProfile();

                Assert.That(provider.HasKey(saveKey), Is.True);
                Assert.That(provider.SaveCallCount, Is.GreaterThan(0));

                GameObject secondGo = new("RpgCharacter_Second");
                RpgCharacter second = AddCharacter(secondGo);
                SetPrivateField(second, "_saveKey", saveKey);

                try
                {
                    second.ApplyTemplate(template);
                    second.LoadProfile();

                    Assert.That(second.HpValue, Is.EqualTo(60f));
                    Assert.That(second.MaxHpValue, Is.EqualTo(100f));
                }
                finally
                {
                    Object.DestroyImmediate(secondGo);
                }
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(firstGo);
            }
        }

        [Test]
        public void NetworkSnapshot_RoundTripsCustomIdsWithSeparators()
        {
            const string resourceId = "Dark;Mana=1/2|x:y";
            RpgCharacterTemplate template = CreateBasicTemplate();
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = 100f,
                    startMax = 100f,
                    restoreOnAwake = true,
                    restoreToFull = true
                },
                new RpgResourceDefinition
                {
                    id = new RpgStatId(resourceId),
                    startCurrent = 10f,
                    startMax = 30f,
                    restoreOnAwake = true,
                    restoreToFull = false
                }
            };

            GameObject firstGo = new("RpgCharacter_First");
            GameObject secondGo = new("RpgCharacter_Second");
            RpgCharacter first = AddCharacter(firstGo);
            RpgCharacter second = AddCharacter(secondGo);

            try
            {
                first.ApplyTemplate(template);
                second.ApplyTemplate(template);
                first.Spend(resourceId, 3f);

                MethodInfo buildSnapshot = typeof(RpgCharacter).GetMethod(
                    "BuildSnapshot", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo applySnapshot = typeof(RpgCharacter).GetMethod(
                    "ApplySnapshot", BindingFlags.Instance | BindingFlags.NonPublic);

                Assert.That(buildSnapshot, Is.Not.Null, "Mirror snapshot API should exist in this project.");
                Assert.That(applySnapshot, Is.Not.Null, "Mirror snapshot API should exist in this project.");

                string snapshot = (string)buildSnapshot.Invoke(first, Array.Empty<object>());
                applySnapshot.Invoke(second, new object[] { snapshot });

                Assert.That(second.GetResource(resourceId), Is.EqualTo(7f));
            }
            finally
            {
                Object.DestroyImmediate(template);
                Object.DestroyImmediate(firstGo);
                Object.DestroyImmediate(secondGo);
            }
        }

        private static RpgCharacterTemplate CreateBasicTemplate()
        {
            RpgCharacterTemplate template = ScriptableObject.CreateInstance<RpgCharacterTemplate>();
            template.resources = new[]
            {
                new RpgResourceDefinition
                {
                    id = new RpgStatId(RpgStatPreset.Hp),
                    startCurrent = 100f,
                    startMax = 100f,
                    restoreOnAwake = true,
                    restoreToFull = true
                }
            };
            return template;
        }

        private static RpgCharacter AddCharacter(GameObject go)
        {
#if MIRROR
            go.AddComponent<NetworkIdentity>();
#endif
            return go.AddComponent<RpgCharacter>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            FieldInfo fieldInfo = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(fieldInfo, Is.Not.Null,
                $"Field '{fieldName}' was not found on type '{target.GetType().Name}'.");
            fieldInfo.SetValue(target, value);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new(StringComparer.Ordinal);

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public int SaveCallCount { get; private set; }
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
                return _values.TryGetValue(key, out object value) && value is float floatValue
                    ? floatValue
                    : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _values.TryGetValue(key, out object value) && value is string stringValue
                    ? stringValue
                    : defaultValue;
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
                SaveCallCount++;
                OnDataSaved?.Invoke();
            }

            public void Load()
            {
                OnDataLoaded?.Invoke();
            }
        }
    }
}
