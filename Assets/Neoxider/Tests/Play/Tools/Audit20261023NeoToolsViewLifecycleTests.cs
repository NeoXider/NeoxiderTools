using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using Neo.Tools.View;
using Neo.UI;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    public sealed class Audit20261023NeoToolsViewLifecycleTests
    {
        private readonly List<GameObject> _objects = new();
        private readonly List<Material> _materials = new();
        private ISaveProvider _previousProvider;

        [SetUp]
        public void SetUp()
        {
            _previousProvider = SaveProvider.CurrentProvider;
            SaveProvider.SetProvider(new MemorySaveProvider());
        }

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null)
                {
                    Object.DestroyImmediate(_objects[i]);
                }
            }

            for (int i = _materials.Count - 1; i >= 0; i--)
            {
                if (_materials[i] != null)
                {
                    Object.DestroyImmediate(_materials[i]);
                }
            }

            _objects.Clear();
            _materials.Clear();
            SaveProvider.SetProvider(_previousProvider);
        }

        [UnityTest]
        public IEnumerator Billboard_CustomDirection_RotatesWithoutCameraAndIgnoresZeroDirection()
        {
            GameObject host = CreateObject("Billboard");
            BillboardUniversal billboard = host.AddComponent<BillboardUniversal>();
            SetField(billboard, "useMainCameraFallback", false);
            billboard.SetTargetCamera(null);
            billboard.SetBillboardMode(BillboardUniversal.BillboardMode.TowardsDirection);
            billboard.SetIgnoreY(false);
            Vector3 direction = new(2f, 3f, 4f);
            billboard.SetCustomDirection(direction);

            yield return null;

            Assert.That(Vector3.Angle(host.transform.forward, direction), Is.LessThan(0.01f));
            billboard.SetIgnoreY(true);
            yield return null;
            Assert.That(Vector3.Angle(host.transform.forward, new Vector3(2f, 0f, 4f)), Is.LessThan(0.01f));

            Quaternion previous = host.transform.rotation;
            billboard.SetCustomDirection(Vector3.zero);
            yield return null;
            Assert.That(Quaternion.Angle(host.transform.rotation, previous), Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator Selector_FirstEnable_LoadsSavedIndexAndExclusionsBeforeWriting()
        {
            const string key = "Audit20261023NeoToolsView.Startup";
            SaveProvider.SetInt(key + "_Index", 1);
            SaveProvider.SetString(key + "_Excluded", "0");
            GameObject root = CreateObject("SavedSelector");
            root.SetActive(false);
            GameObject first = CreateObject("First");
            GameObject second = CreateObject("Second");
            first.transform.SetParent(root.transform);
            second.transform.SetParent(root.transform);
            Selector selector = root.AddComponent<Selector>();
            SetField(selector, "_saveEnabled", true);
            SetField(selector, "_saveKey", key);

            root.SetActive(true);

            Assert.That(SaveProvider.GetInt(key + "_Index"), Is.EqualTo(1), "OnEnable must preserve the stored value.");
            Assert.That(SaveProvider.GetString(key + "_Excluded"), Is.EqualTo("0"));
            yield return null;

            Assert.That(selector.Value, Is.EqualTo(1));
            Assert.That(selector.IsExcluded(0), Is.True);
            Assert.That(first.activeSelf, Is.False);
            Assert.That(second.activeSelf, Is.True);
            Assert.That(SaveProvider.GetInt(key + "_Index"), Is.EqualTo(1));

            selector.Set(0);
            selector.ExcludeIndex(1);
            Assert.That(SaveProvider.GetInt(key + "_Index"), Is.Zero);
            Assert.That(SaveProvider.GetString(key + "_Excluded"), Is.EqualTo("0,1"));
        }

        [Test]
        public void StarView_MissingManagerAndSlots_AllowsExplicitRebindingAndUpdatesRemainingSlots()
        {
            StarView view = CreateObject("Stars").AddComponent<StarView>();
            ScoreManager manager = CreateObject("Score").AddComponent<ScoreManager>();
            GameObject star = CreateObject("Star");
            VisualToggle toggle = CreateObject("Toggle").AddComponent<VisualToggle>();
            view.starObjects = new[] { null, star };
            view.stars = new[] { null, toggle };
            view.scoreManager = manager;

            view.Awake();
            view.Awake();
            manager.CountStars = 1;
            Assert.That(star.activeSelf, Is.False);
            Assert.That(toggle.IsActive, Is.False);
            manager.CountStars = 2;
            Assert.That(star.activeSelf, Is.True);
            Assert.That(toggle.IsActive, Is.True);
            Assert.That(manager.CountStarsReactive.ListenerCount, Is.EqualTo(1));
        }

        [Test]
        public void StarView_Destroy_RemovesOriginalSubscriptionAndLeavesExternalViewsUntouched()
        {
            ScoreManager manager = CreateObject("Score").AddComponent<ScoreManager>();
            GameObject star = CreateObject("ExternalStar");
            GameObject host = CreateObject("Stars");
            host.SetActive(false);
            StarView view = host.AddComponent<StarView>();
            view.scoreManager = manager;
            view.starObjects = new[] { star };
            host.SetActive(true);
            manager.CountStars = 1;
            Assert.That(star.activeSelf, Is.True);
            Assert.That(manager.CountStarsReactive.ListenerCount, Is.EqualTo(1));

            // A public field can be changed before teardown; unsubscribe the property actually used at bind time.
            view.scoreManager = null;
            Object.DestroyImmediate(view);
            Assert.That(manager.CountStarsReactive.ListenerCount, Is.Zero);
            manager.CountStars = 0;
            Assert.That(star.activeSelf, Is.True);
        }

        [UnityTest]
        public IEnumerator Light_ResetAndDisable_KeepReactiveIntensityConsistentWithLight()
        {
            GameObject host = CreateObject("Light");
            Light light = host.AddComponent<Light>();
            light.intensity = 1f;
            light.color = Color.red;
            LightAnimator animator = host.AddComponent<LightAnimator>();
            animator.playOnStart = false;
            animator.minIntensity = animator.maxIntensity = 2f;
            yield return null;
            animator.Play();
            yield return null;
            Assert.That(light.intensity, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(2f).Within(0.0001f));

            animator.ResetToOriginal();
            Assert.That(light.intensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(1f).Within(0.0001f));
            yield return null;
            Assert.That(light.intensity, Is.EqualTo(2f).Within(0.0001f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(2f).Within(0.0001f),
                "The same animated value must be published again after an explicit reset.");

            animator.enabled = false;
            Assert.That(light.intensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(animator.CurrentColor, Is.EqualTo(Color.red));
        }

        [UnityTest]
        public IEnumerator Light2D_WhenUrpIsAvailable_AnimatesAndRestoresRealComponent()
        {
            Type lightType = Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.2D.Runtime", false)
                ?? Type.GetType(
                "UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime", false);
            if (lightType == null)
            {
                Assert.Ignore("URP is optional; run this scenario in the central URP compatibility project.");
            }

            GameObject host = CreateObject("Light2D");
            Component light = host.AddComponent(lightType);
            PropertyInfo intensity = lightType.GetProperty("intensity");
            PropertyInfo color = lightType.GetProperty("color");
            intensity.SetValue(light, 3f);
            color.SetValue(light, Color.green);
            LightAnimator animator = host.AddComponent<LightAnimator>();
            animator.playOnStart = false;
            animator.minIntensity = animator.maxIntensity = 2f;
            yield return null;
            animator.Play();
            yield return null;

            Assert.That((float)intensity.GetValue(light), Is.EqualTo(2f).Within(0.0001f));
            Assert.That(animator.CurrentIntensity, Is.EqualTo(2f).Within(0.0001f));
            animator.enabled = false;
            Assert.That((float)intensity.GetValue(light), Is.EqualTo(3f).Within(0.0001f));
            Assert.That((Color)color.GetValue(light), Is.EqualTo(Color.green));
        }

        [UnityTest]
        public IEnumerator MeshEmission_DestroyComponent_RestoresSurvivingRendererAndReleasesOwnedClone()
        {
            // Normalized float channels: 1e-6 is one millionth of full scale (about 8 float epsilons at 1).
            // Compare native material roundtrips to their captured baseline, not Color's rounded display.
            const float materialColorTolerance = 0.000001f;
            Material original = CreateMaterial("Standard");
            Color authored = new(0.2f, 0.4f, 0.6f, 1f);
            original.SetColor("_EmissionColor", authored);
            Color originalBaseline = original.GetColor("_EmissionColor");
            AssertEmissionColor(originalBaseline, authored, materialColorTolerance, "Original native baseline");
            GameObject host = CreateObject("Emission");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            emission.syncWithLight = false;
            Material clone = renderer.sharedMaterial;
            Assert.That(clone, Is.Not.SameAs(original));
            Color cloneBaseline = clone.GetColor("_EmissionColor");
            AssertEmissionColor(cloneBaseline, originalBaseline, materialColorTolerance, "Clone native baseline");
            clone.SetColor("_EmissionColor", Color.magenta);
            Assert.That(clone.GetColor("_EmissionColor"), Is.Not.EqualTo(cloneBaseline),
                "The owned material must differ before restoration is exercised.");
            emission.ResetToOriginal();
            AssertEmissionColor(emission.CurrentColor, cloneBaseline, 0f, "Published captured baseline");
            AssertEmissionColor(clone.GetColor("_EmissionColor"), cloneBaseline, materialColorTolerance,
                "Restored clone material");

            Object.DestroyImmediate(emission);

            Assert.That(renderer.sharedMaterial, Is.SameAs(original));
            yield return null;
            Assert.That(clone == null, Is.True, "The component must release only its own instance.");
            Assert.That(original != null, Is.True);
            AssertEmissionColor(original.GetColor("_EmissionColor"), originalBaseline, 0f, "Untouched original");
        }

        [UnityTest]
        public IEnumerator MeshEmission_DestroyComponent_PreservesMaterialAssignedByAnotherOwner()
        {
            Material original = CreateMaterial("Standard");
            Material replacement = CreateMaterial("Standard");
            GameObject host = CreateObject("EmissionReplacement");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            Material clone = renderer.sharedMaterial;
            renderer.sharedMaterial = replacement;

            Object.DestroyImmediate(emission);

            Assert.That(renderer.sharedMaterial, Is.SameAs(replacement));
            yield return null;
            Assert.That(clone == null, Is.True);
            Assert.That(original != null, Is.True);
        }

        [Test]
        public void MeshEmission_WithoutMaterial_DisablesItself()
        {
            GameObject host = CreateObject("EmissionWithoutMaterial");
            host.AddComponent<MeshRenderer>();
            if (NeoDiagnostics.RuntimeErrorsEnabled)
            {
                LogAssert.Expect(LogType.Error, "MeshEmission: No material assigned to the MeshRenderer!");
            }

            MeshEmission emission = host.AddComponent<MeshEmission>();

            Assert.That(emission.enabled, Is.False);
            Assert.That(host.GetComponent<MeshRenderer>().sharedMaterial, Is.Null);
        }

        [UnityTest]
        public IEnumerator MeshEmission_Disable_PublishesRestoredEmissionState()
        {
            Material original = CreateMaterial("Standard");
            original.SetColor("_EmissionColor", Color.green);
            GameObject host = CreateObject("EmissionDisable");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            Light light = host.AddComponent<Light>();
            light.intensity = 4f;
            light.color = Color.red;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            emission.targetLight = light;
            emission.intensityMultiplier = 1f;
            yield return null;
            Assert.That(emission.CurrentIntensity, Is.EqualTo(4f).Within(0.0001f));
            Assert.That(emission.CurrentColor, Is.EqualTo(Color.red));

            emission.enabled = false;

            Assert.That(emission.CurrentIntensity, Is.EqualTo(1f).Within(0.0001f));
            Assert.That(emission.CurrentColor, Is.EqualTo(Color.green));
            Assert.That(renderer.sharedMaterial.GetColor("_EmissionColor"), Is.EqualTo(Color.green));
        }

        [Test]
        public void MeshEmission_HdrpWhenAvailable_CapturesEmissiveColorWithoutBuiltinProperty()
        {
            Material original = CreateMaterial("HDRP/Lit");
            if (original.HasProperty("_EmissionColor") || !original.HasProperty("_EmissiveColor"))
            {
                Assert.Ignore("This scenario requires the HDRP-only emissive property.");
            }

            Color authored = new(0.2f, 0.4f, 0.6f, 1f);
            original.SetColor("_EmissiveColor", authored);
            GameObject host = CreateObject("HdrpEmission");
            MeshRenderer renderer = host.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = original;
            MeshEmission emission = host.AddComponent<MeshEmission>();
            emission.ResetToOriginal();

            Assert.That(emission.CurrentColor, Is.EqualTo(authored));
            Assert.That(renderer.sharedMaterial.GetColor("_EmissiveColor"), Is.EqualTo(authored));
        }

        private GameObject CreateObject(string name)
        {
            GameObject instance = new("Audit20261023NeoToolsView." + name);
            _objects.Add(instance);
            return instance;
        }

        private Material CreateMaterial(string shaderName)
        {
            Shader shader = Shader.Find(shaderName);
            if (shader == null)
            {
                Assert.Ignore("Shader unavailable in this compatibility project: " + shaderName);
            }

            Material material = new(shader);
            _materials.Add(material);
            return material;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }

        private static void AssertEmissionColor(Color actual, Color expected, float tolerance, string stage)
        {
            TestContext.WriteLine($"{stage}: actual=({actual.r:R}, {actual.g:R}, {actual.b:R}, {actual.a:R}); " +
                $"expected=({expected.r:R}, {expected.g:R}, {expected.b:R}, {expected.a:R}); tolerance={tolerance:R}");
            Assert.That(actual.r, Is.EqualTo(expected.r).Within(tolerance), stage + " red");
            Assert.That(actual.g, Is.EqualTo(expected.g).Within(tolerance), stage + " green");
            Assert.That(actual.b, Is.EqualTo(expected.b).Within(tolerance), stage + " blue");
            Assert.That(actual.a, Is.EqualTo(expected.a).Within(tolerance), stage + " alpha");
        }

        private sealed class MemorySaveProvider : ISaveProvider
        {
            private readonly Dictionary<string, object> _values = new();
            public SaveProviderType ProviderType => SaveProviderType.PlayerPrefs;
            public event Action OnDataSaved;
            public event Action OnDataLoaded;
            public event Action<string> OnKeyChanged;
            public int GetInt(string key, int defaultValue = 0) => Read(key, defaultValue);
            public float GetFloat(string key, float defaultValue = 0f) => Read(key, defaultValue);
            public string GetString(string key, string defaultValue = "") => Read(key, defaultValue);
            public bool GetBool(string key, bool defaultValue = false) => Read(key, defaultValue);
            public void SetInt(string key, int value) => Write(key, value);
            public void SetFloat(string key, float value) => Write(key, value);
            public void SetString(string key, string value) => Write(key, value);
            public void SetBool(string key, bool value) => Write(key, value);
            public bool HasKey(string key) => _values.ContainsKey(key);
            public void DeleteAll() => _values.Clear();
            public void Save() => OnDataSaved?.Invoke();
            public void Load() => OnDataLoaded?.Invoke();

            public void DeleteKey(string key)
            {
                _values.Remove(key);
                OnKeyChanged?.Invoke(key);
            }

            private T Read<T>(string key, T defaultValue)
            {
                return _values.TryGetValue(key, out object value) && value is T typed ? typed : defaultValue;
            }

            private void Write(string key, object value)
            {
                _values[key] = value;
                OnKeyChanged?.Invoke(key);
            }
        }
    }
}
