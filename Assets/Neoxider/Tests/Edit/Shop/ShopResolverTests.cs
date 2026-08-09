using Neo.Shop;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class ShopResolverTests
    {
        private GameObject _sceneRoot;
        private GameObject _contextObject;

        [TearDown]
        public void TearDown()
        {
            if (_contextObject != null)
            {
                Object.DestroyImmediate(_contextObject);
            }

            if (_sceneRoot != null)
            {
                Object.DestroyImmediate(_sceneRoot);
            }
        }

        [Test]
        public void Resolve_NullContext_ReturnsNull()
        {
            ShopResolverProbe result = ShopResolver.Resolve<ShopResolverProbe>(null);

            Assert.That(result, Is.Null);
        }

        [Test]
        public void Resolve_PrefersParentHierarchyOverSceneFallback()
        {
            _sceneRoot = new GameObject("Scene fallback");
            ShopResolverProbe sceneProbe = _sceneRoot.AddComponent<ShopResolverProbe>();
            GameObject parent = new GameObject("Parent");
            ShopResolverProbe parentProbe = parent.AddComponent<ShopResolverProbe>();
            _contextObject = new GameObject("Context");
            _contextObject.transform.SetParent(parent.transform);
            ShopResolverContext context = _contextObject.AddComponent<ShopResolverContext>();

            ShopResolverProbe result = ShopResolver.Resolve<ShopResolverProbe>(context);

            Assert.That(result, Is.SameAs(parentProbe));
            Assert.That(result, Is.Not.SameAs(sceneProbe));
            Object.DestroyImmediate(parent);
            _contextObject = null;
        }

        [Test]
        public void TryResolve_UsesSceneFallbackWhenHierarchyHasNoMatch()
        {
            _sceneRoot = new GameObject("Scene fallback");
            ShopResolverProbe sceneProbe = _sceneRoot.AddComponent<ShopResolverProbe>();
            _contextObject = new GameObject("Context");
            ShopResolverContext context = _contextObject.AddComponent<ShopResolverContext>();

            bool found = ShopResolver.TryResolve(context, out ShopResolverProbe result);

            Assert.That(found, Is.True);
            Assert.That(result, Is.SameAs(sceneProbe));
        }
    }

    public sealed class ShopResolverContext : MonoBehaviour
    {
    }

    public sealed class ShopResolverProbe : MonoBehaviour
    {
    }
}
