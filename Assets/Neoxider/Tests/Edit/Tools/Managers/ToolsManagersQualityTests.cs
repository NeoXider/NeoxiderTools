using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Audio;
using Neo.Tools;
using Neo.Tools.Tests;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class ToolsManagersQualityTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<string> _trace = new();

        [TearDown]
        public void TearDown()
        {
            foreach (GameObject owner in _objects)
            {
                if (owner != null)
                {
                    Object.DestroyImmediate(owner);
                }
            }

            _objects.Clear();
            _trace.Clear();
        }

        [Test]
        public void Bootstrap_StartupInitThrows_StillCompletesAndInitializesLaterRegistrations()
        {
            Audit20261023NeoToolsManagersBootstrapProbe bootstrap = CreateBootstrap();
            Initializable failing = new("failing", 10, _trace)
            {
                OnInit = () => throw new InvalidOperationException("Expected startup failure")
            };
            Initializable low = new("low", 1, _trace);
            bootstrap.Register(failing);
            bootstrap.Register(low);

            Assert.Throws<InvalidOperationException>(bootstrap.InitializeForTest);

            Assert.That(bootstrap.IsBootstrapCompleted, Is.True,
                "A throwing startup target must not leave the bootstrap waiting forever.");
            Assert.That(low.InitCount, Is.Zero, "Startup stays fail-fast: lower priorities wait for the next drain.");
            Assert.That(bootstrap.HasFailed(failing), Is.True);

            bootstrap.Unregister(failing);
            bootstrap.InitializePending();
            Assert.That(low.InitCount, Is.EqualTo(1));
            Assert.That(bootstrap.HasFailed(failing), Is.False, "Unregister forgets the failure.");

            Initializable late = new("late", 0, _trace);
            bootstrap.Register(late);
            Assert.That(late.InitCount, Is.EqualTo(1), "Registrations after startup initialize immediately.");
            CollectionAssert.AreEqual(new[] { "failing", "low", "late" }, _trace);
        }

        [Test]
        public void Bootstrap_StartupInitThrows_LaterRegisterSkipsTheFailedTargetWithoutRethrowing()
        {
            Audit20261023NeoToolsManagersBootstrapProbe bootstrap = CreateBootstrap();
            Initializable failing = new("failing", 10, _trace)
            {
                OnInit = () => throw new InvalidOperationException("Expected startup failure")
            };
            Initializable low = new("low", 1, _trace);
            bootstrap.Register(failing);
            bootstrap.Register(low);
            Assert.Throws<InvalidOperationException>(bootstrap.InitializeForTest);

            Initializable late = new("late", 0, _trace);
            Assert.DoesNotThrow(() => bootstrap.Register(late),
                "An unrelated registration must not rethrow the quarantined target's old exception.");

            Assert.That(failing.InitCount, Is.EqualTo(1), "Automatic drains never re-run a failed Init.");
            Assert.That(low.InitCount, Is.EqualTo(1), "Targets the failed pass skipped run on the next drain.");
            Assert.That(late.InitCount, Is.EqualTo(1));
            CollectionAssert.AreEqual(new[] { "failing", "low", "late" }, _trace);

            Initializable later = new("later", 50, _trace);
            bootstrap.Register(later);
            Assert.That(failing.InitCount, Is.EqualTo(1), "A higher-priority newcomer does not retry it either.");
            Assert.That(later.InitCount, Is.EqualTo(1));

            Assert.Throws<InvalidOperationException>(bootstrap.InitializePending,
                "The explicit retry runs the failed target again and reports its exception.");
            Assert.That(failing.InitCount, Is.EqualTo(2));
            Assert.That(bootstrap.HasFailed(failing), Is.True);

            failing.OnInit = null;
            bootstrap.InitializePending();
            Assert.That(failing.InitCount, Is.EqualTo(3));
            Assert.That(bootstrap.HasFailed(failing), Is.False);
            Assert.That(low.InitCount, Is.EqualTo(1), "Initialized targets are never re-run by a retry.");
            CollectionAssert.AreEqual(new[] { "failing", "low", "late", "later", "failing", "failing" }, _trace);
        }

        [Test]
        public void Bootstrap_EqualPriorities_InitializeInRegistrationOrder()
        {
            Audit20261023NeoToolsManagersBootstrapProbe bootstrap = CreateBootstrap();
            bootstrap.Register(new Initializable("first", 5, _trace));
            bootstrap.Register(new Initializable("low", 1, _trace));
            bootstrap.Register(new Initializable("second", 5, _trace));
            bootstrap.Register(new Initializable("high", 9, _trace));
            bootstrap.Register(new Initializable("third", 5, _trace));

            bootstrap.InitializeForTest();

            CollectionAssert.AreEqual(new[] { "high", "first", "second", "third", "low" }, _trace);
        }

        [Test]
        public void SingletonReset_ScansOnlyAssembliesThatCanDeclareSingletons()
        {
            Type sweep = typeof(Singleton<>).Assembly.GetType("Neo.Tools.SingletonRuntimeReset");
            Assert.That(sweep, Is.Not.Null);
            MethodInfo find = sweep.GetMethod("FindDependentAssemblies", BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(find, Is.Not.Null);

            Assembly[] assemblies =
            {
                typeof(object).Assembly,
                typeof(GameObject).Assembly,
                typeof(Singleton<>).Assembly,
                typeof(ScoreManager).Assembly,
                typeof(AM).Assembly,
                typeof(AuditFixesSingletonRoot).Assembly
            };

            bool[] relevant = (bool[])find.Invoke(null, new object[] { assemblies, typeof(Singleton<>).Assembly });

            Assert.That(relevant[0], Is.False, "The BCL cannot declare a Neo singleton.");
            Assert.That(relevant[1], Is.False, "UnityEngine cannot declare a Neo singleton.");
            Assert.That(relevant[2], Is.True, "The owner assembly (GM, EM, Bootstrap) is always scanned.");
            Assert.That(relevant[3], Is.True, "ScoreManager's assembly references the owner.");
            Assert.That(relevant[4], Is.True, "AM's assembly references the owner.");
            Assert.That(relevant[5], Is.True, "Test fixture singletons are reset too.");
        }

        [Test]
        public void Singleton_DestroyInstance_InEditMode_DestroysTheObjectWithoutAnError()
        {
            Assume.That(AuditFixesSingletonRoot.HasInstance, Is.False, "This regression requires an isolated singleton.");
            Assume.That(Object.FindObjectsByType<AuditFixesSingletonRoot>(FindObjectsInactive.Include,
                FindObjectsSortMode.None), Is.Empty, "This regression requires a scene without another instance.");
            FieldInfo searchFailed = typeof(Singleton<AuditFixesSingletonRoot>).GetField("_searchFailed",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(searchFailed, Is.Not.Null, "Singleton<T>._searchFailed expected");
            object previousSearchFailed = searchFailed.GetValue(null);
            bool previousCreateInstance = AuditFixesSingletonRoot.CreateInstance;
            GameObject owner = new("Singleton edit-mode destroy");
            owner.SetActive(false);
            _objects.Add(owner);
            AuditFixesSingletonRoot instance = owner.AddComponent<AuditFixesSingletonRoot>();

            try
            {
                AuditFixesSingletonRoot.CreateInstance = false;
                searchFailed.SetValue(null, false);
                Assert.That(AuditFixesSingletonRoot.I, Is.SameAs(instance));

                // WHY: Object.Destroy logs "Destroy may not be called from edit mode", which fails the test as an
                // unhandled error log and used to leave the GameObject behind.
                AuditFixesSingletonRoot.DestroyInstance();

                Assert.That(owner == null, Is.True, "Edit Mode destroys the singleton's GameObject immediately.");
                Assert.That(AuditFixesSingletonRoot.HasInstance, Is.False);
            }
            finally
            {
                AuditFixesSingletonRoot.CreateInstance = previousCreateInstance;
                searchFailed.SetValue(null, previousSearchFailed);
            }
        }

        private Audit20261023NeoToolsManagersBootstrapProbe CreateBootstrap()
        {
            GameObject owner = new("Bootstrap quality");
            owner.SetActive(false);
            _objects.Add(owner);
            return owner.AddComponent<Audit20261023NeoToolsManagersBootstrapProbe>();
        }

        private sealed class Initializable : IInit
        {
            private readonly string _name;
            private readonly List<string> _trace;

            public Initializable(string name, int priority, List<string> trace)
            {
                _name = name;
                InitPriority = priority;
                _trace = trace;
            }

            public int InitPriority { get; }
            public int InitCount { get; private set; }
            public Action OnInit { get; set; }

            public void Init()
            {
                InitCount++;
                _trace.Add(_name);
                OnInit?.Invoke();
            }
        }
    }
}
