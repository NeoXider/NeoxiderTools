using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Neo.Save;
using Neo.Save.Examples;
using Neo.Tests.Performance;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;
using TestSaveable = global::Neo.Editor.Tests.Edit.Audit20261023FixtureNeoSaveManagerTestSaveable;

namespace Neo.Editor.Tests.Edit
{
    /// <summary>
    ///     <see cref="SaveManager" /> metadata caches: one field list per component type, an allocation-free cleanup
    ///     scan on every registration, collection fields serialized through a cached JSON wrapper, and an isolated
    ///     <see cref="ISaveableComponent.OnDataLoaded" /> callback.
    /// </summary>
    public sealed class SaveManagerQualityPassTests
    {
        private readonly List<GameObject> _objects = new();
        private Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> _previousRegistrations;
        private bool _previousIsLoad;

        [SetUp]
        public void SetUp()
        {
            _previousRegistrations =
                new Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)>(GetRegistrations());
            _previousIsLoad = SaveManager.IsLoad;
            SaveManager.ClearSubsystemCaches();
        }

        [TearDown]
        public void TearDown()
        {
            try
            {
                for (int i = _objects.Count - 1; i >= 0; i--)
                {
                    if (_objects[i] != null)
                    {
                        Object.DestroyImmediate(_objects[i]);
                    }
                }
            }
            finally
            {
                _objects.Clear();
                SaveManager.ClearSubsystemCaches();
                Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> registrations = GetRegistrations();
                foreach (KeyValuePair<string, (MonoBehaviour instance, List<FieldInfo> fields)> entry in
                         _previousRegistrations)
                {
                    registrations.Add(entry.Key, entry.Value);
                }

                typeof(SaveManager).GetProperty(nameof(SaveManager.IsLoad))?.SetValue(null, _previousIsLoad);
            }
        }

        [Test]
        public void Register_InstancesOfOneType_ShareTheCachedFieldList()
        {
            TestSaveable first = CreateSaveable("quality-pass-first");
            TestSaveable second = CreateSaveable("quality-pass-second");

            SaveManager.Register(first);
            SaveManager.Register(second);

            Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> registrations = GetRegistrations();
            List<FieldInfo> firstFields = registrations[SaveIdentityUtility.GetComponentKey(first)].fields;
            List<FieldInfo> secondFields = registrations[SaveIdentityUtility.GetComponentKey(second)].fields;
            Assert.That(firstFields, Is.SameAs(secondFields), "[SaveField] metadata is resolved once per type");
            Assert.That(firstFields, Has.Count.EqualTo(2));
        }

        [Test]
        public void CleanupScan_WithLiveRegistrations_AllocatesNothingAndDropsDestroyedOnes()
        {
            TestSaveable kept = CreateSaveable("quality-pass-kept");
            TestSaveable destroyed = CreateSaveable("quality-pass-destroyed");
            SaveManager.Register(kept);
            SaveManager.Register(destroyed);
            string destroyedKey = SaveIdentityUtility.GetComponentKey(destroyed);
            MethodInfo method = typeof(SaveManager).GetMethod("CleanupDestroyedRegistrations",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            Action cleanup = (Action)Delegate.CreateDelegate(typeof(Action), method);
            Action scan = () =>
            {
                for (int i = 0; i < 256; i++)
                {
                    cleanup();
                }
            };
            cleanup();

            long allocations = GcAlloc.TryCount(scan, out string notAdmitted);

            Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> registrations = GetRegistrations();
            Assert.That(registrations, Has.Count.EqualTo(2), "a scan without destroyed objects removes nothing");
            Object.DestroyImmediate(destroyed.gameObject);
            cleanup();
            Assert.That(registrations.ContainsKey(destroyedKey), Is.False);
            Assert.That(registrations.ContainsKey(SaveIdentityUtility.GetComponentKey(kept)), Is.True);
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocations, Is.Zero,
                "Every SaveableBehaviour.OnEnable runs this scan; it must not allocate while nothing was destroyed.");
        }

        [Test]
        public void CollectionValues_RoundTripThroughTheCachedWrapper()
        {
            Func<object, Type, string> toText = CreateValueToString();
            Func<string, Type, object> fromText = CreateStringToValue();
            List<int> list = new() { 3, 1, 2 };
            int[] array = { 7, 8 };
            List<string> names = new() { "a", "b" };

            string listJson = toText(list, typeof(List<int>));
            string arrayJson = toText(array, typeof(int[]));
            string namesJson = toText(names, typeof(List<string>));

            Assert.That(listJson, Is.EqualTo("{\"Items\":[3,1,2]}"));
            Assert.That(arrayJson, Is.EqualTo("{\"Items\":[7,8]}"));
            Assert.That(namesJson, Is.EqualTo("{\"Items\":[\"a\",\"b\"]}"));

            object listBack = fromText(listJson, typeof(List<int>));
            Assert.That(listBack, Is.TypeOf<List<int>>());
            Assert.That(listBack, Is.EqualTo(list));
            Assert.That(listBack, Is.Not.SameAs(list));
            Assert.That(fromText(arrayJson, typeof(int[])), Is.EqualTo(array));
            Assert.That(fromText(namesJson, typeof(List<string>)), Is.EqualTo(names));
            Assert.That(toText(new List<int>(), typeof(List<int>)), Is.EqualTo("{\"Items\":[]}"));
        }

        [Test]
        public void CollectionWrapper_IsResolvedOncePerCollectionType()
        {
            Func<object, Type, string> toText = CreateValueToString();
            Func<string, Type, object> fromText = CreateStringToValue();
            Dictionary<Type, FieldInfo> cache = GetWrapperCache();
            Assert.That(cache, Is.Empty, "SetUp cleared the subsystem caches");

            fromText(toText(new List<int> { 1 }, typeof(List<int>)), typeof(List<int>));
            fromText(toText(new[] { 1 }, typeof(int[])), typeof(int[]));
            FieldInfo listField = cache[typeof(List<int>)];
            FieldInfo arrayField = cache[typeof(int[])];
            for (int i = 0; i < 8; i++)
            {
                fromText(toText(new List<int> { i }, typeof(List<int>)), typeof(List<int>));
                fromText(toText(new[] { i }, typeof(int[])), typeof(int[]));
            }

            Assert.That(cache, Has.Count.EqualTo(2), "one wrapper entry per collection type");
            Assert.That(cache[typeof(List<int>)], Is.SameAs(listField), "later calls reuse the first resolution");
            Assert.That(cache[typeof(int[])], Is.SameAs(arrayField));
            Assert.That(listField.FieldType, Is.EqualTo(typeof(List<int>)));
            Assert.That(arrayField.FieldType, Is.EqualTo(typeof(int[])));
            Assert.That(listField.DeclaringType, Is.Not.EqualTo(arrayField.DeclaringType));
        }

        [Test]
        public void ListValueToString_AllocationsDoNotGrowWithTheElementCount()
        {
            Func<object, Type, string> toText = CreateValueToString();
            List<int> small = new() { 0, 1, 2 };
            List<int> large = new(300);
            for (int i = 0; i < 300; i++)
            {
                large.Add(i);
            }

            string smallJson = null;
            string largeJson = null;
            Action serializeSmall = () => smallJson = toText(small, typeof(List<int>));
            Action serializeLarge = () => largeJson = toText(large, typeof(List<int>));
            serializeSmall();
            serializeLarge();

            long smallCalls = GcAlloc.TryCount(serializeSmall, out string smallNotAdmitted);
            long largeCalls = GcAlloc.TryCount(serializeLarge, out string largeNotAdmitted);

            Assert.That(smallJson, Is.EqualTo("{\"Items\":[0,1,2]}"));
            Assert.That(largeJson, Does.StartWith("{\"Items\":[0,1,2,3,").And.EndWith(",298,299]}"));
            GcAlloc.IgnoreIfNotAdmitted(smallNotAdmitted ?? largeNotAdmitted);
            // WHY: the wrapper serializes the stored list itself; a per-element copy or reflective Add costs at least
            // one allocation per element, so 297 more elements would add hundreds of GC.Alloc calls.
            Assert.That(largeCalls, Is.LessThanOrEqualTo(smallCalls + 32),
                $"small list: {smallCalls} GC.Alloc calls, large list: {largeCalls}");
        }

        [Test]
        public void NotifyDataLoaded_WhenTheCallbackThrows_LogsTheExceptionInsteadOfPropagatingIt()
        {
            bool previousErrors = NeoDiagnostics.RuntimeErrorsEnabled;
            NeoDiagnostics.Configure(errors: true);
            try
            {
                GameObject host = new("SaveManagerQualityPass.throwing");
                _objects.Add(host);
                PlayerData destroyed = host.AddComponent<PlayerData>();
                Object.DestroyImmediate(destroyed);
                MethodInfo method = typeof(SaveManager).GetMethod("NotifyDataLoaded",
                    BindingFlags.Static | BindingFlags.NonPublic);
                Assert.That(method, Is.Not.Null);
                Action<MonoBehaviour> notify =
                    (Action<MonoBehaviour>)Delegate.CreateDelegate(typeof(Action<MonoBehaviour>), method);

                // WHY: PlayerData.OnDataLoaded reads transform, which throws on a destroyed component. A bulk or scene
                // load must log that and go on with the next component instead of aborting the load.
                LogAssert.Expect(LogType.Exception, new Regex("(MissingReference|NullReference)Exception"));
                Assert.DoesNotThrow(() => notify(destroyed));
            }
            finally
            {
                NeoDiagnostics.Configure(errors: previousErrors);
            }
        }

        private TestSaveable CreateSaveable(string identity)
        {
            GameObject host = new("SaveManagerQualityPass." + identity);
            _objects.Add(host);
            TestSaveable saveable = host.AddComponent<TestSaveable>();
            saveable.Identity = identity;
            return saveable;
        }

        private static Func<object, Type, string> CreateValueToString()
        {
            MethodInfo method = typeof(SaveManager).GetMethod("ValueToString",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Func<object, Type, string>)Delegate.CreateDelegate(typeof(Func<object, Type, string>), method);
        }

        private static Func<string, Type, object> CreateStringToValue()
        {
            MethodInfo method = typeof(SaveManager).GetMethod("StringToValue",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Func<string, Type, object>)Delegate.CreateDelegate(typeof(Func<string, Type, object>), method);
        }

        private static Dictionary<Type, FieldInfo> GetWrapperCache()
        {
            FieldInfo field = typeof(SaveManager).GetField("_wrapperItemsFields",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<Type, FieldInfo>)field.GetValue(null);
        }

        private static Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)> GetRegistrations()
        {
            FieldInfo field = typeof(SaveManager).GetField("_saveableComponents",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return (Dictionary<string, (MonoBehaviour instance, List<FieldInfo> fields)>)field.GetValue(null);
        }
    }
}
