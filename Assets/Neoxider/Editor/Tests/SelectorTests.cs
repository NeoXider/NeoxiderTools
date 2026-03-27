using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Tools.Tests
{
    public class SelectorTests
    {
        [Test]
        public void Set_LoopsIndex_WhenLoopEnabled_AndUpdatesActiveItem()
        {
            GameObject root = new("SelectorRoot");
            GameObject a = new("A");
            GameObject b = new("B");
            a.transform.SetParent(root.transform);
            b.transform.SetParent(root.transform);

            Selector selector = root.AddComponent<Selector>();

            try
            {
                selector.startOnAwake = false;
                selector.FillMode = false;
                // By default _loop = true, so Set(10) should wrap within [0,1]
                selector.Set(10);

                Assert.That(selector.Value, Is.EqualTo(0));
                Assert.That(a.activeSelf, Is.True);
                Assert.That(b.activeSelf, Is.False);
                Assert.That(selector.CountActive, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void UniqueSelection_DoesNotRepeatUntilReset()
        {
            GameObject root = new("SelectorUniqueRoot");
            GameObject a = new("A");
            GameObject b = new("B");
            GameObject c = new("C");
            a.transform.SetParent(root.transform);
            b.transform.SetParent(root.transform);
            c.transform.SetParent(root.transform);

            Selector selector = root.AddComponent<Selector>();

            try
            {
                selector.startOnAwake = false;
                SetPrivateBool(selector, "_useRandomSelection", true);
                SetPrivateBool(selector, "_uniqueSelectionMode", true);
                SetPrivateBool(selector, "_resetUniqueWhenCycleComplete", false);

                HashSet<int> seen = new();
                for (int i = 0; i < 3; i++)
                {
                    selector.SetRandom();
                    seen.Add(selector.Value);
                }

                Assert.That(seen.Count, Is.EqualTo(3), "Every index must be picked exactly once");

                int before = selector.Value;
                selector.SetRandom(); // all indices used, no auto-reset
                Assert.That(selector.Value, Is.EqualTo(before), "Without auto-reset the index must not change");

                selector.ResetUnique();
                selector.SetRandom();
                Assert.That(seen.Contains(selector.Value), Is.True, "After ResetUnique() indices are available again");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExcludeIndex_RemovesIndexFromRandomPool()
        {
            GameObject root = new("SelectorExcludeRoot");
            GameObject a = new("A");
            GameObject b = new("B");
            a.transform.SetParent(root.transform);
            b.transform.SetParent(root.transform);

            Selector selector = root.AddComponent<Selector>();

            try
            {
                selector.startOnAwake = false;
                SetPrivateBool(selector, "_useRandomSelection", true);

                selector.ExcludeIndex(0);

                for (int i = 0; i < 10; i++)
                {
                    selector.SetRandom();
                    Assert.That(selector.Value, Is.EqualTo(1),
                        "Index 0 is excluded; random selection must yield only 1");
                }

                selector.IncludeAllIndices();
                bool seenZero = false;
                bool seenOne = false;
                for (int i = 0; i < 20; i++)
                {
                    selector.SetRandom();
                    if (selector.Value == 0)
                    {
                        seenZero = true;
                    }

                    if (selector.Value == 1)
                    {
                        seenOne = true;
                    }
                }

                Assert.That(seenZero && seenOne, Is.True, "After IncludeAllIndices() both indices are available again");
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SaveAndLoad_PersistsIndexAndExcludedIndices()
        {
            DictionarySaveProvider provider = new();
            SaveProvider.SetProvider(provider);

            const string saveKey = "SelectorTests.Persistence";

            GameObject firstRoot = new("SelectorSave_First");
            GameObject a = new("A");
            GameObject b = new("B");
            a.transform.SetParent(firstRoot.transform);
            b.transform.SetParent(firstRoot.transform);

            Selector first = firstRoot.AddComponent<Selector>();
            first.startOnAwake = false;

            SetPrivateBool(first, "_useRandomSelection", true);
            SetPrivateBool(first, "_saveEnabled", true);
            SetPrivateString(first, "_saveKey", saveKey);

            first.Set(1);
            first.ExcludeIndex(0);

            // Force-save state
            InvokePrivate(first, "SaveState");

            Object.DestroyImmediate(firstRoot);

            GameObject secondRoot = new("SelectorSave_Second");
            GameObject a2 = new("A2");
            GameObject b2 = new("B2");
            a2.transform.SetParent(secondRoot.transform);
            b2.transform.SetParent(secondRoot.transform);

            Selector second = secondRoot.AddComponent<Selector>();
            second.startOnAwake = false;

            try
            {
                SetPrivateBool(second, "_useRandomSelection", true);
                SetPrivateBool(second, "_saveEnabled", true);
                SetPrivateString(second, "_saveKey", saveKey);

                InvokePrivate(second, "LoadState");
                second.Set(second.Value); // apply loaded index

                Assert.That(second.Value, Is.EqualTo(1), "Index must be restored from save");
                Assert.That(second.IsExcluded(0), Is.True, "Excluded index must be restored");
            }
            finally
            {
                Object.DestroyImmediate(secondRoot);
            }
        }

        [Test]
        public void SetRandomUnique_DoesNotToggleSelectorItems_WhenControlGameObjectActiveIsDisabled()
        {
            GameObject root = new("SelectorNotifyOnlyDisabledRoot");
            GameObject a = new("A");
            GameObject b = new("B");
            GameObject c = new("C");
            a.transform.SetParent(root.transform);
            b.transform.SetParent(root.transform);
            c.transform.SetParent(root.transform);

            SelectorItem itemA = a.AddComponent<SelectorItem>();
            SelectorItem itemB = b.AddComponent<SelectorItem>();
            SelectorItem itemC = c.AddComponent<SelectorItem>();
            Selector selector = root.AddComponent<Selector>();

            try
            {
                selector.startOnAwake = false;
                SetPrivateBool(selector, "_useRandomSelection", true);
                SetPrivateBool(selector, "_uniqueSelectionMode", true);
                SetPrivateBool(selector, "_resetUniqueWhenCycleComplete", false);
                SetPrivateBool(selector, "_notifySelectorItemsOnly", true);
                SetPrivateBool(selector, "_controlGameObjectActive", false);

                selector.SetRandom();

                Assert.That(a.activeSelf, Is.True);
                Assert.That(b.activeSelf, Is.True);
                Assert.That(c.activeSelf, Is.True);
                Assert.That(itemA.ActiveValue, Is.False);
                Assert.That(itemB.ActiveValue, Is.False);
                Assert.That(itemC.ActiveValue, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Set_DoesNotToggleGameObjects_WhenControlGameObjectActiveIsDisabled()
        {
            GameObject root = new("SelectorNoSetActiveRoot");
            GameObject a = new("A");
            GameObject b = new("B");
            a.transform.SetParent(root.transform);
            b.transform.SetParent(root.transform);

            Selector selector = root.AddComponent<Selector>();

            try
            {
                selector.startOnAwake = false;
                SetPrivateBool(selector, "_controlGameObjectActive", false);
                SetPrivateBool(selector, "_notifySelectorItemsOnly", false);

                selector.Set(1);

                Assert.That(selector.Value, Is.EqualTo(1));
                Assert.That(a.activeSelf, Is.True);
                Assert.That(b.activeSelf, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void SetPrivateBool(object target, string fieldName, bool value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field `{fieldName}` was not found.");
            field.SetValue(target, value);
        }

        private static void SetPrivateString(object target, string fieldName, string value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, $"Field `{fieldName}` was not found.");
            field.SetValue(target, value);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, $"Method `{methodName}` was not found.");
            method.Invoke(target, null);
        }

        private sealed class DictionarySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, bool> _bools = new();
            private readonly Dictionary<string, float> _floats = new();
            private readonly Dictionary<string, int> _ints = new();
            private readonly Dictionary<string, string> _strings = new();

            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;

            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;

            public int GetInt(string key, int defaultValue = 0)
            {
                return _ints.TryGetValue(key, out int v) ? v : defaultValue;
            }

            public void SetInt(string key, int value)
            {
                _ints[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public float GetFloat(string key, float defaultValue = 0f)
            {
                return _floats.TryGetValue(key, out float v) ? v : defaultValue;
            }

            public void SetFloat(string key, float value)
            {
                _floats[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public string GetString(string key, string defaultValue = "")
            {
                return _strings.TryGetValue(key, out string v) ? v : defaultValue;
            }

            public void SetString(string key, string value)
            {
                _strings[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool GetBool(string key, bool defaultValue = false)
            {
                return _bools.TryGetValue(key, out bool v) ? v : defaultValue;
            }

            public void SetBool(string key, bool value)
            {
                _bools[key] = value;
                OnKeyChanged?.Invoke(key);
            }

            public bool HasKey(string key)
            {
                return _ints.ContainsKey(key) || _floats.ContainsKey(key) || _strings.ContainsKey(key) ||
                       _bools.ContainsKey(key);
            }

            public void DeleteKey(string key)
            {
                _ints.Remove(key);
                _floats.Remove(key);
                _strings.Remove(key);
                _bools.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            public void DeleteAll()
            {
                _ints.Clear();
                _floats.Clear();
                _strings.Clear();
                _bools.Clear();
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
