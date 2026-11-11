using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>Despawn After Seconds: a timed self-despawn that restarts on every enable.</summary>
    public sealed class DespawnerLifetimePlayTests
    {
        [UnityTest]
        public IEnumerator DespawnAfterSeconds_DespawnsOnceWhenTheLifetimeElapses()
        {
            GameObject effect = new GameObject("Timed Effect");
            effect.SetActive(false);
            Despawner despawner = effect.AddComponent<Despawner>();
            despawner.DespawnAfterSeconds = 0.2f;
            int despawned = 0;
            despawner.OnDespawn.AddListener(() => despawned++);
            effect.SetActive(true);

            yield return new WaitForSeconds(0.05f);
            Assert.That(despawned, Is.Zero, "The object lives until its lifetime elapses.");

            yield return new WaitForSeconds(0.4f);
            Assert.That(despawned, Is.EqualTo(1));
            Assert.That(effect == null, Is.True, "An object without a pool is destroyed.");
        }

        [UnityTest]
        public IEnumerator DespawnAfterSeconds_DisabledBeforeTheLifetime_DoesNotDespawnLater()
        {
            GameObject effect = new GameObject("Interrupted Effect");
            effect.SetActive(false);
            Despawner despawner = effect.AddComponent<Despawner>();
            despawner.DespawnAfterSeconds = 0.15f;
            int despawned = 0;
            despawner.OnDespawn.AddListener(() => despawned++);
            effect.SetActive(true);
            effect.SetActive(false);

            yield return new WaitForSeconds(0.35f);
            Assert.That(despawned, Is.Zero, "Disabling the object cancels the pending despawn.");
            Assert.That(effect != null, Is.True);
            Object.Destroy(effect);
        }
    }
}
