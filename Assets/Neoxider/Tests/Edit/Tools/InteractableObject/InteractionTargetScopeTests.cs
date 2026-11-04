using System.Collections.Generic;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests.Tools
{
    /// <summary>
    ///     Target membership and source-rig exclusion extracted from InteractiveObject. Both projection strategies
    ///     and every automatic query rely on these answers, so they are pinned scene-free here.
    /// </summary>
    [TestFixture]
    public sealed class InteractionTargetScopeTests
    {
        private readonly List<GameObject> _owned = new();

        [TearDown]
        public void DestroyOwned()
        {
            for (int i = _owned.Count - 1; i >= 0; i--)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void Contains_LiveHierarchyUntilAnExplicitColliderOverridesIt()
        {
            GameObject target = Create("Target", null);
            BoxCollider root = target.AddComponent<BoxCollider>();
            GameObject child = Create("Child", target.transform);
            BoxCollider childCollider = child.AddComponent<BoxCollider>();
            BoxCollider foreign = Create("Foreign", null).AddComponent<BoxCollider>();
            BoxCollider2D root2D = Create("Target2D", target.transform).AddComponent<BoxCollider2D>();

            InteractionTargetScope hierarchy = new(target.transform, null, null, true, null, null);
            Assert.That(hierarchy.Contains(root), Is.True);
            Assert.That(hierarchy.Contains(childCollider), Is.True);
            Assert.That(hierarchy.Contains(root2D), Is.True);
            Assert.That(hierarchy.Contains(foreign), Is.False);
            Assert.That(hierarchy.Contains(null), Is.False);

            foreign.transform.SetParent(child.transform, true);
            Assert.That(hierarchy.Contains(foreign), Is.True, "Membership is evaluated live, after reparenting.");

            InteractionTargetScope explicit3D = new(target.transform, childCollider, null, true, null, null);
            Assert.That(explicit3D.Contains(childCollider), Is.True);
            Assert.That(explicit3D.Contains(root), Is.False, "An explicit 3D collider replaces the hierarchy rule.");
            Assert.That(explicit3D.Contains(root2D), Is.True, "The 2D rule stays hierarchical without a 2D override.");
        }

        [Test]
        public void IsInIgnoredSourceRig_IgnoresThePlayerRigButNotASharedLevel()
        {
            GameObject level = Create("Level", null);
            GameObject target = Create("Target", level.transform);
            target.AddComponent<BoxCollider>();
            GameObject player = Create("Player", level.transform);
            BoxCollider capsule = Create("Capsule", player.transform).AddComponent<BoxCollider>();
            BoxCollider wall = Create("Wall", level.transform).AddComponent<BoxCollider>();

            InteractionTargetScope scope = new(target.transform, null, null, true, player.transform, null);
            Assert.That(scope.IsInIgnoredSourceRig(capsule), Is.True, "A collider inside the distance source rig.");
            Assert.That(scope.IsInIgnoredSourceRig(wall), Is.False, "A shared level root is not the player rig.");

            InteractionTargetScope disabled = new(target.transform, null, null, false, player.transform, null);
            Assert.That(disabled.IsInIgnoredSourceRig(capsule), Is.False);

            GameObject separatePlayer = Create("SeparatePlayer", null);
            GameObject eyes = Create("Eyes", separatePlayer.transform);
            BoxCollider body = Create("Body", separatePlayer.transform).AddComponent<BoxCollider>();
            InteractionTargetScope viewRig = new(target.transform, null, null, true, null, eyes.transform);
            Assert.That(viewRig.IsInIgnoredSourceRig(body), Is.True,
                "Siblings under a rig root other than the target's root belong to the view source rig.");
            Assert.That(viewRig.IsInIgnoredSourceRig(null), Is.False);
        }

        private GameObject Create(string name, Transform parent)
        {
            GameObject instance = new(name);
            _owned.Add(instance);
            if (parent != null)
            {
                instance.transform.SetParent(parent, false);
            }

            return instance;
        }
    }
}
