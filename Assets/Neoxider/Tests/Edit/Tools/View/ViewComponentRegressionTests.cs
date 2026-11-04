using System;
using System.Collections.Generic;
using System.Reflection;
using DG.Tweening;
using DG.Tweening.Core;
using DG.Tweening.Plugins.Options;
using Neo.Tests.Performance;
using Neo.Tools;
using Neo.Tools.View;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests
{
    /// <summary>
    ///     Regressions and frame-cost guards for the Tools/View light, emission and depth components. Unity
    ///     callbacks that EditMode does not run (Awake, Update) are invoked explicitly.
    /// </summary>
    public sealed class ViewComponentRegressionTests
    {
        private const int Iterations = 200;

        private readonly List<Object> _owned = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            for (int index = _owned.Count - 1; index >= 0; index--)
            {
                if (_owned[index] != null)
                {
                    Object.DestroyImmediate(_owned[index]);
                }
            }

            _owned.Clear();
        }

        [Test]
        public void LightAnimator_DestroyedLight_StopsInsteadOfThrowingEveryFrame()
        {
            GameObject host = CreateObject("Light");
            Light light = host.AddComponent<Light>();
            light.intensity = 1f;
            LightAnimator animator = host.AddComponent<LightAnimator>();
            animator.minIntensity = 2f;
            animator.maxIntensity = 2f;
            InvokePrivate(animator, "Awake");
            animator.Play();
            animator.Tick(0.1f);
            Assert.That(light.intensity, Is.EqualTo(2f).Within(1e-4f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(2f).Within(1e-4f));

            Object.DestroyImmediate(light);

            Assert.DoesNotThrow(() => animator.Tick(0.1f), "A destroyed light must not throw on every frame.");
            Assert.That(animator.IsPlaying, Is.False);
            Assert.That(animator.LightComponent == null, Is.True);
            Assert.DoesNotThrow(animator.ResetToOriginal, "OnDisable's reset must survive a destroyed light too.");
        }

        [Test]
        public void LightAnimator_ManualTickWhileDisabled_KeepsTheRestoredBaseline()
        {
            GameObject host = CreateObject("Disabled light");
            Light light = host.AddComponent<Light>();
            light.intensity = 1f;
            LightAnimator animator = host.AddComponent<LightAnimator>();
            animator.minIntensity = 2f;
            animator.maxIntensity = 2f;
            animator.ManualUpdate = true;
            InvokePrivate(animator, "Awake");
            InvokePrivate(animator, "OnEnable");
            animator.Play();
            animator.Tick(0.1f);
            Assert.That(light.intensity, Is.EqualTo(2f).Within(1e-4f));

            InvokePrivate(animator, "OnDisable");
            Assert.That(light.intensity, Is.EqualTo(1f).Within(1e-4f), "OnDisable restores the baseline.");
            animator.Tick(0.1f);
            Assert.That(light.intensity, Is.EqualTo(1f).Within(1e-4f),
                "A manual clock must not overwrite the baseline of a disabled animator.");

            InvokePrivate(animator, "OnEnable");
            animator.Tick(0.1f);
            Assert.That(light.intensity, Is.EqualTo(2f).Within(1e-4f), "Re-enabling resumes the animation.");
        }

        [Test]
        public void LightAnimator_WarmTicks_DoNotAllocate()
        {
            GameObject host = CreateObject("Allocation light");
            host.AddComponent<Light>();
            LightAnimator animator = host.AddComponent<LightAnimator>();
            animator.changeColor = true;
            animator.targetColor = Color.cyan;
            InvokePrivate(animator, "Awake");
            animator.Play();
            for (int i = 0; i < 3; i++)
            {
                animator.Tick(0.016f);
            }

            using GcAllocCounter allocationCounter = GcAllocCounter.CreateOrIgnore();
            allocationCounter.Begin();
            for (int i = 0; i < Iterations; i++)
            {
                animator.Tick(0.016f);
            }

            long allocated = allocationCounter.End();

            Assert.That(allocated, Is.EqualTo(0L));
        }

        [Test]
        public void MeshEmission_ResetToOriginal_RestoresTheBaselineWhateverTheCutoff()
        {
            Material original = CreateEmissiveMaterial();
            Color authored = new Color(0.2f, 0.4f, 0.6f, 1f);
            original.SetColor("_EmissionColor", authored);
            GameObject host = CreateObject("Emission cutoff");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            emission.syncWithLight = false;
            emission.emissionCutoff = 2f;
            InvokePrivate(emission, "Awake");
            Material clone = renderer.sharedMaterial;
            _owned.Add(clone);
            Assert.That(clone, Is.Not.SameAs(original));
            Color baseline = clone.GetColor("_EmissionColor");

            emission.ResetToOriginal();

            AssertColor(clone.GetColor("_EmissionColor"), baseline, "A cutoff above 1 must not black out the restore.");
            Assert.That(clone.IsKeywordEnabled("_EMISSION"), Is.True);
            AssertColor(emission.CurrentColor, baseline, "The published color matches the material.");
        }

        [Test]
        public void MeshEmission_Update_FollowsLightChangesAndCutoffTransitions()
        {
            Material original = CreateEmissiveMaterial();
            GameObject host = CreateObject("Emission sync");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            Light light = host.AddComponent<Light>();
            light.color = Color.red;
            light.intensity = 4f;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            emission.targetLight = light;
            emission.intensityMultiplier = 1f;
            emission.emissionCutoff = 0.3f;
            InvokePrivate(emission, "Awake");
            Material clone = renderer.sharedMaterial;
            _owned.Add(clone);
            Action update = UpdateOf(emission);

            update();
            update();
            AssertColor(clone.GetColor("_EmissionColor"), Color.red * 4f, "Synced emission.");

            light.color = Color.green;
            update();
            AssertColor(clone.GetColor("_EmissionColor"), Color.green * 4f, "A light change must reach the material.");

            light.intensity = 0.1f;
            update();
            Assert.That(clone.IsKeywordEnabled("_EMISSION"), Is.False, "Below the cutoff emission switches off.");
            AssertColor(clone.GetColor("_EmissionColor"), Color.black, "Below the cutoff emission is black.");

            light.intensity = 2f;
            update();
            Assert.That(clone.IsKeywordEnabled("_EMISSION"), Is.True, "Above the cutoff emission switches back on.");
            AssertColor(clone.GetColor("_EmissionColor"), Color.green * 2f, "Re-enabled emission.");
        }

        [Test]
        public void ZPositionAdjuster_Adjust_DerivesDepthAndSkipsUnchangedWrites()
        {
            GameObject host = CreateObject("Depth");
            host.transform.position = new Vector3(1f, 500000f, 3f);
            ZPositionAdjuster adjuster = host.AddComponent<ZPositionAdjuster>();
            adjuster.Ratio = 2f;

            adjuster.Adjust();

            Assert.That(ZPositionAdjuster.ComputeDepth(500000f, true, 2f), Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(host.transform.position.z, Is.EqualTo(1.5f).Within(1e-5f));
            Assert.That(host.transform.position.x, Is.EqualTo(1f));
            host.transform.hasChanged = false;

            adjuster.Adjust();
            Assert.That(host.transform.hasChanged, Is.False, "A resting object must not be rewritten every frame.");

            host.transform.position = new Vector3(1f, -500000f, 1.5f);
            adjuster.UseNormalizeToUnit = false;
            adjuster.Adjust();
            Assert.That(host.transform.position.z, Is.EqualTo(-1f).Within(1e-5f), "-1..1 normalization x 2.");

            adjuster.Ratio = -3f;
            Assert.That(adjuster.Ratio, Is.Zero, "The ratio keeps its authored minimum of zero.");
        }

        [Test]
        public void BillboardDepthAndEmission_WarmFramesWhileMoving_DoNotAllocate()
        {
            Material original = CreateEmissiveMaterial();
            GameObject emissive = CreateObject("Allocation emission");
            emissive.AddComponent<MeshRenderer>().sharedMaterial = original;
            Light light = emissive.AddComponent<Light>();
            light.intensity = 4f;
            MeshEmission emission = emissive.AddComponent<MeshEmission>();
            emission.targetLight = light;
            emission.intensityMultiplier = 1f;
            InvokePrivate(emission, "Awake");
            _owned.Add(emissive.GetComponent<MeshRenderer>().sharedMaterial);
            Action emissionUpdate = UpdateOf(emission);

            Camera camera = CreateObject("Allocation camera").AddComponent<Camera>();
            camera.transform.position = new Vector3(0f, 2f, -10f);
            GameObject sprite = CreateObject("Allocation billboard");
            BillboardUniversal billboard = sprite.AddComponent<BillboardUniversal>();
            billboard.SetTargetCamera(camera);
            billboard.SetBillboardMode(BillboardUniversal.BillboardMode.TowardsCamera);
            Action billboardLateUpdate = LateUpdateOf(billboard);
            ZPositionAdjuster depth = sprite.AddComponent<ZPositionAdjuster>();
            Transform spriteTransform = sprite.transform;

            Action frames = () =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    spriteTransform.position = new Vector3(i * 0.01f, i * 0.02f, 0f);
                    light.intensity = 1f + (i & 7);
                    emissionUpdate();
                    billboardLateUpdate();
                    depth.Adjust();
                }
            };
            frames();

            long allocationCalls = GcAlloc.TryCount(frames, out string notAdmitted);

            Assert.That(spriteTransform.position.z,
                Is.EqualTo(ZPositionAdjuster.ComputeDepth(spriteTransform.position.y, true, 1f)).Within(1e-5f),
                "positive control: the depth follows the last move");
            Assert.That(emission.CurrentIntensity, Is.GreaterThan(0f), "positive control: emission follows the light");
            GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
            Assert.That(allocationCalls, Is.Zero,
                "MeshEmission.Update, BillboardUniversal.LateUpdate and ZPositionAdjuster.Adjust must not allocate");
        }

        [Test]
        public void ImageFillAmountAnimator_RepeatedTarget_KeepsTheRunningTweenWithoutAllocating()
        {
            GameObject host = CreateObject("Fill repeat");
            Image image = host.AddComponent<Image>();
            ImageFillAmountAnimator animator = host.AddComponent<ImageFillAmountAnimator>();
            animator.SetValueImmediate(0.1f);
            animator.SetValue(0.9f);
            Tween running = ReadFillTween(animator);
            try
            {
                Assert.That(running != null && running.IsActive() && running.IsPlaying(), Is.True,
                    "positive control: an enabled animator starts a tween");
                animator.SetValue(0.9f);

                long allocationCalls = GcAlloc.TryCount(() =>
                {
                    for (int i = 0; i < Iterations; i++)
                    {
                        animator.SetValue(0.9f);
                    }
                }, out string notAdmitted);

                Assert.That(ReadFillTween(animator), Is.SameAs(running),
                    "repeating the running target keeps the tween, so its ease is not restarted");
                Assert.That(running.IsActive(), Is.True);

                animator.SetValue(0.4f);
                Tween replaced = ReadFillTween(animator);
                Assert.That(replaced, Is.Not.SameAs(running), "a new target still replaces the tween");
                // WHY: outside Play Mode DOTween's Kill leaves the tween flagged active, so IsActive cannot show the
                // retirement here; the animator's ownership guard must stop a replaced tween from writing instead.
                float shown = image.fillAmount;
                ((TweenerCore<float, float, FloatOptions>)running).setter(0.77f);
                Assert.That(image.fillAmount, Is.EqualTo(shown).Within(1e-6f), "a replaced tween no longer writes");
                if (Application.isPlaying)
                {
                    Assert.That(running.IsActive(), Is.False);
                }

                replaced.Kill();

                animator.SetValueImmediate(0.3f);
                animator.SetValue(0.3f);
                Assert.That(ReadFillTween(animator), Is.Null, "the value already shown starts no tween");
                Assert.That(image.fillAmount, Is.EqualTo(0.3f).Within(1e-5f));

                GcAlloc.IgnoreIfNotAdmitted(notAdmitted);
                Assert.That(allocationCalls, Is.Zero, "a repeated SetValue must not build a tween or closures");
            }
            finally
            {
                if (running != null && running.IsActive())
                {
                    running.Kill();
                }

                Tween left = ReadFillTween(animator);
                if (left != null && left.IsActive())
                {
                    left.Kill();
                }
            }
        }

        [Test]
        public void StarView_ShowStars_DrivesSlotsWithoutAScoreManager()
        {
            StarView view = CreateObject("Stars").AddComponent<StarView>();
            GameObject first = CreateObject("Star 1");
            GameObject second = CreateObject("Star 2");
            GameObject third = CreateObject("Star 3");
            view.starObjects = new[] { first, null, second, third };

            view.ShowStars(3);
            Assert.That(first.activeSelf && second.activeSelf, Is.True, "Slots keep their index; a gap is skipped.");
            Assert.That(third.activeSelf, Is.False);

            view.ShowStars(0);
            Assert.That(first.activeSelf || second.activeSelf || third.activeSelf, Is.False);

            view.Bind(null);
            Assert.That(view.scoreManager == null, Is.True, "Binding null only unbinds.");
            Assert.That(first.activeSelf, Is.False, "Unbinding leaves the visuals as they are.");
        }

        private Material CreateEmissiveMaterial()
        {
            Shader shader = Shader.Find("Standard");
            if (shader == null)
            {
                shader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                Assert.Ignore("No shader with _EmissionColor is available in this project.");
            }

            Material material = new Material(shader);
            _owned.Add(material);
            if (!material.HasProperty("_EmissionColor"))
            {
                Assert.Ignore("The available shader has no _EmissionColor property.");
            }

            return material;
        }

        private GameObject CreateObject(string name)
        {
            GameObject host = new GameObject("ViewComponentRegressionTests." + name);
            _owned.Add(host);
            return host;
        }

        private static void InvokePrivate(object target, string methodName)
        {
            MethodInfo method = target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null, methodName);
            method.Invoke(target, null);
        }

        private static Action UpdateOf(MeshEmission emission)
        {
            MethodInfo method = typeof(MeshEmission).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Action)Delegate.CreateDelegate(typeof(Action), emission, method);
        }

        private static Tween ReadFillTween(ImageFillAmountAnimator animator)
        {
            FieldInfo field = typeof(ImageFillAmountAnimator).GetField("_anim",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            return field.GetValue(animator) as Tween;
        }

        private static Action LateUpdateOf(BillboardUniversal billboard)
        {
            MethodInfo method = typeof(BillboardUniversal).GetMethod("LateUpdate",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(method, Is.Not.Null);
            return (Action)Delegate.CreateDelegate(typeof(Action), billboard, method);
        }

        private static void AssertColor(Color actual, Color expected, string stage)
        {
            // WHY 1e-4: HDR material colors round-trip through the project's color-space conversion.
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(1e-4f), stage + " red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(1e-4f), stage + " green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(1e-4f), stage + " blue");
        }
    }
}
