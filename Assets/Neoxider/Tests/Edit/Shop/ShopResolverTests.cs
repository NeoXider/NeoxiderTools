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
        private GameObject _parent;

        [TearDown]
        public void TearDown()
        {
            if (_parent != null) Object.DestroyImmediate(_parent);
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
            _parent = new GameObject("Parent");
            ShopResolverProbe parentProbe = _parent.AddComponent<ShopResolverProbe>();
            _contextObject = new GameObject("Context");
            _contextObject.transform.SetParent(_parent.transform);
            ShopResolverContext context = _contextObject.AddComponent<ShopResolverContext>();

            ShopResolverProbe result = ShopResolver.Resolve<ShopResolverProbe>(context);

            Assert.That(result, Is.SameAs(parentProbe));
            Assert.That(result, Is.Not.SameAs(sceneProbe));
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




}
