using System.Collections.Generic;
using Neo.Tools.View;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Editor.Tests
{
    public sealed class SpriteFlashTests
    {
        private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
        private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");

        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _owned.Count; i++)
            {
                if (_owned[i] != null)
                {
                    Object.DestroyImmediate(_owned[i]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void Flash_DrawsWithTheFlashMaterial_AndGivesTheOriginalBackWhenTheTimeIsUp()
        {
            SpriteRenderer body = NewSprite("body", null, out Material original);
            SpriteFlash flash = body.gameObject.AddComponent<SpriteFlash>();
            flash.Duration = 0.2f;
            int started = 0;
            int finished = 0;
            flash.OnFlashStarted.AddListener(() => started++);
            flash.OnFlashFinished.AddListener(() => finished++);

            flash.Flash();

            Assert.That(flash.IsFlashing, Is.True);
            Assert.That(body.sharedMaterial, Is.Not.SameAs(original));
            Assert.That(body.sharedMaterial.shader.name, Is.EqualTo(SpriteFlash.ShaderName));
            Assert.That(started, Is.EqualTo(1));

            flash.Tick(0.1f);
            Assert.That(flash.IsFlashing, Is.True, "Half of the duration has passed.");

            flash.Tick(0.11f);
            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(body.sharedMaterial, Is.SameAs(original));
            Assert.That(body.HasPropertyBlock(), Is.False, "A renderer without a property block gets none left behind.");
            Assert.That(finished, Is.EqualTo(1));
        }

        [Test]
        public void Flash_WhileFlashing_RestartsTheTimer_AndKeepsTheOriginalMaterial()
        {
            SpriteRenderer body = NewSprite("body", null, out Material original);
            SpriteFlash flash = body.gameObject.AddComponent<SpriteFlash>();
            flash.Duration = 0.2f;
            int started = 0;
            flash.OnFlashStarted.AddListener(() => started++);

            flash.Flash();
            flash.Tick(0.15f);
            flash.Flash();
            flash.Tick(0.15f);

            Assert.That(flash.IsFlashing, Is.True, "The second call restarted the 0.2 s timer.");
            Assert.That(started, Is.EqualTo(1), "A restart is not a new flash.");

            flash.Tick(0.1f);
            Assert.That(body.sharedMaterial, Is.SameAs(original), "The flash material was never taken for the original.");
        }

        [Test]
        public void FadeOut_LowersTheAmount_AndHoldKeepsItFull()
        {
            SpriteRenderer fading = NewSprite("fading", null, out Material _);
            SpriteFlash fade = fading.gameObject.AddComponent<SpriteFlash>();
            fade.Duration = 0.4f;
            fade.Color = Color.red;
            fade.Flash();
            fade.Tick(0.3f);

            MaterialPropertyBlock block = new MaterialPropertyBlock();
            fading.GetPropertyBlock(block);
            Assert.That(block.GetFloat(FlashAmountId), Is.EqualTo(0.25f).Within(0.001f));
            Assert.That(block.GetColor(FlashColorId), Is.EqualTo(Color.red));

            SpriteRenderer holding = NewSprite("holding", null, out Material _);
            SpriteFlash hold = holding.gameObject.AddComponent<SpriteFlash>();
            hold.Duration = 0.4f;
            hold.FadeOut = false;
            hold.Flash();
            hold.Tick(0.3f);

            holding.GetPropertyBlock(block);
            Assert.That(block.GetFloat(FlashAmountId), Is.EqualTo(1f));
        }

        [Test]
        public void EmptyRendererList_FlashesEverySpriteUnderTheObject()
        {
            GameObject root = new GameObject("unit");
            _owned.Add(root);
            SpriteRenderer first = NewSprite("first", root.transform, out Material firstMaterial);
            SpriteRenderer second = NewSprite("second", root.transform, out Material secondMaterial);
            SpriteFlash flash = root.AddComponent<SpriteFlash>();

            flash.Flash();

            Assert.That(first.sharedMaterial, Is.SameAs(second.sharedMaterial), "Both draw with the one flash material.");
            Assert.That(first.sharedMaterial, Is.Not.SameAs(firstMaterial));

            flash.Stop();

            Assert.That(first.sharedMaterial, Is.SameAs(firstMaterial));
            Assert.That(second.sharedMaterial, Is.SameAs(secondMaterial));
        }

        [Test]
        public void Stop_LeavesAMaterialAssignedDuringTheFlash_AndAnExistingPropertyBlock()
        {
            SpriteRenderer body = NewSprite("body", null, out Material _);
            MaterialPropertyBlock own = new MaterialPropertyBlock();
            own.SetFloat("_Custom", 3f);
            body.SetPropertyBlock(own);
            SpriteFlash flash = body.gameObject.AddComponent<SpriteFlash>();
            flash.Flash();

            Material replacement = new Material(Shader.Find("Sprites/Default"));
            _owned.Add(replacement);
            body.sharedMaterial = replacement;
            flash.Stop();

            Assert.That(body.sharedMaterial, Is.SameAs(replacement), "Another owner's material is not undone.");
            MaterialPropertyBlock after = new MaterialPropertyBlock();
            body.GetPropertyBlock(after);
            Assert.That(after.GetFloat("_Custom"), Is.EqualTo(3f), "The renderer's own block survives.");
            Assert.That(after.GetFloat(FlashAmountId), Is.Zero);
        }

        [Test]
        public void InvalidDuration_DoesNotStartAFlash()
        {
            SpriteRenderer body = NewSprite("body", null, out Material original);
            SpriteFlash flash = body.gameObject.AddComponent<SpriteFlash>();

            flash.FlashFor(0f);
            flash.FlashFor(-1f);
            flash.FlashFor(float.NaN);
            flash.FlashFor(float.PositiveInfinity);

            Assert.That(flash.IsFlashing, Is.False);
            Assert.That(body.sharedMaterial, Is.SameAs(original));
        }

        [Test]
        public void AssignedFlashMaterial_IsUsedInsteadOfTheSharedOne()
        {
            SpriteRenderer body = NewSprite("body", null, out Material _);
            Material custom = new Material(Shader.Find(SpriteFlash.ShaderName));
            _owned.Add(custom);
            SpriteFlash flash = body.gameObject.AddComponent<SpriteFlash>();
            flash.FlashMaterial = custom;

            flash.Flash();

            Assert.That(body.sharedMaterial, Is.SameAs(custom));
        }

        private SpriteRenderer NewSprite(string name, Transform parent, out Material original)
        {
            GameObject host = new GameObject(name);
            if (parent != null)
            {
                host.transform.SetParent(parent, false);
            }
            else
            {
                _owned.Add(host);
            }

            SpriteRenderer renderer = host.AddComponent<SpriteRenderer>();
            original = new Material(Shader.Find("Sprites/Default"));
            _owned.Add(original);
            renderer.sharedMaterial = original;
            return renderer;
        }
    }
}
