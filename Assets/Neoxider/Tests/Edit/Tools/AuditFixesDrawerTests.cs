using System.Reflection;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Covers the audit fix for Drawer: with pooling enabled, a discarded stroke must be released
    ///     back to the pool instead of being destroyed, otherwise the pool later hands out a destroyed
    ///     instance and drawing throws MissingReferenceException.
    /// </summary>
    [TestFixture]
    public class AuditFixesDrawerTests
    {
        private GameObject _poolManagerGo;
        private GameObject _linePrefabGo;
        private GameObject _drawerGo;
        private LineRenderer _linePrefab;
        private Drawer _drawer;

        [SetUp]
        public void SetUp()
        {
            ResetPoolManagerStatics();

            _poolManagerGo = new GameObject("AuditFixesPoolManager");
            _poolManagerGo.AddComponent<PoolManager>();

            _linePrefabGo = new GameObject("AuditFixesLinePrefab");
            _linePrefabGo.SetActive(false);
            _linePrefab = _linePrefabGo.AddComponent<LineRenderer>();

            _drawerGo = new GameObject("AuditFixesDrawer");
            _drawer = _drawerGo.AddComponent<Drawer>();
            _drawer.usePooling = true;
            _drawer.poolPrefab = _linePrefab;
        }

        [TearDown]
        public void TearDown()
        {
            if (_drawerGo != null)
            {
                Object.DestroyImmediate(_drawerGo);
            }

            if (_poolManagerGo != null)
            {
                Object.DestroyImmediate(_poolManagerGo);
            }

            if (_linePrefabGo != null)
            {
                Object.DestroyImmediate(_linePrefabGo);
            }

            ResetPoolManagerStatics();
        }

        [Test]
        public void EndLine_DiscardedStroke_IsReleasedToPool_NotDestroyed()
        {
            _drawer.BeginLine(Vector3.zero);
            LineRenderer pooledLine = GetCurrentLine();
            PooledObjectInfo info = pooledLine.GetComponent<PooledObjectInfo>();
            Assert.IsNotNull(info, "BeginLine must take the line from PoolManager when pooling is on");

            // WHY: a single raw point is below minCountCreate, so EndLine discards the stroke.
            _drawer.EndLine();

            Assert.IsFalse(pooledLine == null, "a discarded pooled line must not be destroyed");
            Assert.IsTrue(info.IsInPool, "a discarded pooled line must be released back to the pool");
            Assert.IsFalse(pooledLine.gameObject.activeSelf, "the pool deactivates released instances");
        }

        [Test]
        public void DiscardedPooledLine_IsReusedByTheNextStroke()
        {
            _drawer.BeginLine(Vector3.zero);
            GameObject firstLine = GetCurrentLine().gameObject;

            _drawer.EndLine();
            _drawer.BeginLine(Vector3.zero);

            LineRenderer secondLine = GetCurrentLine();
            Assert.IsFalse(secondLine == null, "the pool must hand back a live instance");
            Assert.AreEqual(firstLine, secondLine.gameObject, "the released line must be re-issued by the pool");
        }

        [Test]
        public void DeleteAll_ReleasesTheInProgressLine_ToThePool()
        {
            _drawer.BeginLine(Vector3.zero);
            LineRenderer pooledLine = GetCurrentLine();
            PooledObjectInfo info = pooledLine.GetComponent<PooledObjectInfo>();
            Assert.IsNotNull(info);

            _drawer.DeleteAll();

            Assert.IsFalse(pooledLine == null, "DeleteAll must not destroy a pooled in-progress line");
            Assert.IsTrue(info.IsInPool, "DeleteAll must release the in-progress line back to the pool");
            Assert.IsNull(GetCurrentLineField().GetValue(_drawer), "DeleteAll must clear the in-progress line");
        }

        private LineRenderer GetCurrentLine()
        {
            var line = (LineRenderer)GetCurrentLineField().GetValue(_drawer);
            Assert.IsFalse(line == null, "Drawer must hold a current LineRenderer");
            return line;
        }

        private static FieldInfo GetCurrentLineField()
        {
            FieldInfo field = typeof(Drawer).GetField("_currentLR",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, "_currentLR");
            return field;
        }

        // WHY: the singleton caches its instance and a failed lookup statically across EditMode tests;
        // both must be cleared so this fixture resolves its own PoolManager.
        private static void ResetPoolManagerStatics()
        {
            SetStaticField("_instance", null);
            SetStaticField("_searchFailed", false);
        }

        private static void SetStaticField(string fieldName, object value)
        {
            FieldInfo field = typeof(Singleton<PoolManager>).GetField(fieldName,
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null, fieldName);
            field.SetValue(null, value);
        }
    }
}
