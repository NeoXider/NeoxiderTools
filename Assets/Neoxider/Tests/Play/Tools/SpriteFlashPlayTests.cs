using System.Collections;
using Neo.Tools.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play
{
    /// <summary>A flash ends by itself, and an object switched off in the middle of one gets its material back.</summary>
    public sealed class SpriteFlashPlayTests
    {
        [UnityTest]
        public IEnumerator Flash_EndsAfterItsDuration_AndDisablingRestoresTheMaterial()
        {
            GameObject host = new GameObject("Flashing Sprite");
            SpriteRenderer body = host.AddComponent<SpriteRenderer>();
            Material original = new Material(Shader.Find("Sprites/Default"));
            body.sharedMaterial = original;
            SpriteFlash flash = host.AddComponent<SpriteFlash>();
            flash.Duration = 0.1f;

            flash.Flash();
            Assert.That(body.sharedMaterial, Is.Not.SameAs(original));

            yield return new WaitForSeconds(0.3f);
            Assert.That(flash.IsFlashing, Is.False, "Update ended the flash.");
            Assert.That(body.sharedMaterial, Is.SameAs(original));

            flash.FlashFor(10f);
            host.SetActive(false);
            Assert.That(flash.IsFlashing, Is.False, "A pooled object does not come back white.");
            Assert.That(body.sharedMaterial, Is.SameAs(original));

            Object.Destroy(host);
            Object.Destroy(original);
        }
    }
}
