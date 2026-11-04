using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Save;
using Neo.Tools;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Neo.Editor.Tests.Tools
{
    public sealed class ToolsComponentsQualityTests
    {
        private const BindingFlags PrivateInstance = BindingFlags.Instance | BindingFlags.NonPublic;

        private readonly List<Object> _created = new();
        private readonly List<RenderTexture> _textures = new();
        private string _lootCloneName;
        private string _saveKey;

        [TearDown]
        public void TearDown()
        {
            foreach (Object created in _created)
            {
                if (created is GameObject gameObject && gameObject != null)
                {
                    Camera camera = gameObject.GetComponent<Camera>();
                    if (camera != null)
                    {
                        camera.targetTexture = null;
                    }
                }
            }

            for (int i = _created.Count - 1; i >= 0; i--)
            {
                if (_created[i] != null)
                {
                    Object.DestroyImmediate(_created[i]);
                }
            }

            _created.Clear();
            foreach (RenderTexture texture in _textures)
            {
                Object.DestroyImmediate(texture);
            }

            _textures.Clear();
            if (_lootCloneName != null)
            {
                foreach (GameObject clone in FindByName(_lootCloneName))
                {
                    Object.DestroyImmediate(clone);
                }

                _lootCloneName = null;
            }

            if (_saveKey != null)
            {
                SaveProvider.DeleteKey(_saveKey);
                _saveKey = null;
            }
        }

        [Test]
        public void CameraScaler_ReinitializedFromAScaledCamera_KeepsTheAuthoredReference()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitWidth,
                out GameObject root, out Camera camera);
            camera.targetTexture = CreateTexture(100, 200);
            Invoke(scaler, "ApplyCameraScale", true);
            float scaled = 5f * (16f / 9f) / 0.5f;
            Assert.That(camera.orthographicSize, Is.EqualTo(scaled).Within(0.0001f));

            // WHY: Instantiate copies serialized state exactly like a scene reopen or domain reload after an
            // Edit Mode preview saved the scaled camera size.
            GameObject reopened = Object.Instantiate(root);
            _created.Add(reopened);
            Camera reopenedCamera = reopened.GetComponent<Camera>();
            CameraAspectRatioScaler reopenedScaler = reopened.GetComponent<CameraAspectRatioScaler>();
            Assert.That(reopenedCamera.orthographicSize, Is.EqualTo(scaled).Within(0.0001f));

            Invoke(reopenedScaler, "Awake");
            Invoke(reopenedScaler, "ApplyCameraScale", true);

            Assert.That(reopenedScaler.ReferenceOrthographicSize, Is.EqualTo(5f));
            Assert.That(reopenedCamera.orthographicSize, Is.EqualTo(scaled).Within(0.0001f),
                "The scaled camera value must not become the next reference (compounded scaling).");
        }

        [Test]
        public void CameraScaler_ReferenceProperty_AppliesOnRefresh()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitHeight,
                out _, out Camera camera);
            camera.targetTexture = CreateTexture(160, 90);

            scaler.ReferenceOrthographicSize = 3f;
            scaler.Refresh();

            Assert.That(camera.orthographicSize, Is.EqualTo(3f).Within(0.0001f));

            camera.orthographicSize = 9f;
            scaler.CaptureReferenceFromCamera();
            Assert.That(scaler.ReferenceOrthographicSize, Is.EqualTo(9f));
        }

        [Test]
        public void CameraScaler_CameraInspectorEdit_BecomesTheReferenceOnTheNextRefresh()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitWidth,
                out _, out Camera camera);
            camera.targetTexture = CreateTexture(100, 200);
            scaler.Refresh();
            Assert.That(camera.orthographicSize, Is.EqualTo(5f * (16f / 9f) / 0.5f).Within(0.0001f));

            camera.orthographicSize = 4f;
            scaler.Refresh();

            float scaled = 4f * (16f / 9f) / 0.5f;
            Assert.That(scaler.ReferenceOrthographicSize, Is.EqualTo(4f),
                "A size the scaler did not write is an authoring edit and becomes the reference.");
            Assert.That(camera.orthographicSize, Is.EqualTo(scaled).Within(0.0001f));

            scaler.Refresh();
            Assert.That(scaler.ReferenceOrthographicSize, Is.EqualTo(4f), "The scaler's own output is never adopted.");
            Assert.That(camera.orthographicSize, Is.EqualTo(scaled).Within(0.0001f));
        }

        [Test]
        public void CameraScaler_PerspectiveFovEdit_BecomesTheReferenceOnTheNextRefresh()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitHeight,
                out _, out Camera camera);
            camera.orthographic = false;
            camera.targetTexture = CreateTexture(160, 90);
            scaler.Refresh();
            Assert.That(camera.fieldOfView, Is.EqualTo(60f).Within(0.0001f));

            camera.fieldOfView = 45f;
            scaler.Refresh();

            Assert.That(scaler.ReferenceFieldOfView, Is.EqualTo(45f).Within(0.0001f));
            Assert.That(camera.fieldOfView, Is.EqualTo(45f).Within(0.0001f));
            Assert.That(scaler.ReferenceOrthographicSize, Is.EqualTo(5f), "The inactive projection is untouched.");
        }

        [Test]
        public void CameraScaler_EditWithPreviewDisabled_IsAdoptedOnTheNextInitialization()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitWidth,
                out GameObject root, out Camera camera);
            SetField(scaler, "updateInEditor", false);
            camera.orthographicSize = 7f;

            // WHY: Instantiate copies serialized state like entering Play Mode or reopening the scene.
            GameObject copy = Object.Instantiate(root);
            _created.Add(copy);
            CameraAspectRatioScaler copyScaler = copy.GetComponent<CameraAspectRatioScaler>();
            Invoke(copyScaler, "Awake");

            Assert.That(copyScaler.ReferenceOrthographicSize, Is.EqualTo(7f),
                "The authored camera size is the composition when the scaler never wrote the camera.");
        }

        [Test]
        public void CameraScaler_SettingProperties_ApplyOnRefreshAndClampLikeTheInspector()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitHeight,
                out _, out Camera camera);
            camera.targetTexture = CreateTexture(160, 90);
            scaler.Refresh();
            Assert.That(camera.orthographicSize, Is.EqualTo(5f).Within(0.0001f));

            scaler.ScaleMultiplier = 2f;
            scaler.Refresh();
            Assert.That(camera.orthographicSize, Is.EqualTo(10f).Within(0.0001f));

            scaler.MaxSize = 8f;
            scaler.Refresh();
            Assert.That(camera.orthographicSize, Is.EqualTo(8f).Within(0.0001f));

            scaler.MinSize = 12f;
            Assert.That(scaler.MaxSize, Is.EqualTo(12f), "Raising the minimum above the maximum raises the maximum.");
            scaler.Refresh();
            Assert.That(camera.orthographicSize, Is.EqualTo(12f).Within(0.0001f));

            scaler.MaxSize = 1f;
            scaler.ScaleMultiplier = -3f;
            scaler.MinSize = -1f;
            Assert.That(scaler.MaxSize, Is.EqualTo(12f), "The maximum never drops below the minimum.");
            Assert.That(scaler.ScaleMultiplier, Is.EqualTo(0.01f));
            Assert.That(scaler.MinSize, Is.EqualTo(0.01f));

            scaler.UseTargetResolution = false;
            scaler.UseViewportRectInFitBoth = false;
            scaler.UpdateInRuntime = false;
            scaler.UpdateInEditor = false;
            scaler.ResetViewportRectOnDisable = false;
            Assert.That(scaler.UseTargetResolution || scaler.UseViewportRectInFitBoth || scaler.UpdateInRuntime
                        || scaler.UpdateInEditor || scaler.ResetViewportRectOnDisable, Is.False);
        }

        [Test]
        public void CameraScaler_SizeLimitChange_IsPickedUpByTheRuntimeChangeCheck()
        {
            CameraAspectRatioScaler scaler = CreateScaler(5f, CameraAspectRatioScaler.ScaleMode.FitHeight,
                out _, out Camera camera);
            camera.targetTexture = CreateTexture(160, 90);
            scaler.Refresh();

            scaler.MaxSize = 3f;
            Invoke(scaler, "ApplyCameraScale", false);

            Assert.That(camera.orthographicSize, Is.EqualTo(3f).Within(0.0001f),
                "The per-frame Update path must re-apply when only a size limit changed.");
        }

        [TestCase(true, true, false, true)]
        [TestCase(false, true, false, false)]
        [TestCase(true, false, false, false)]
        [TestCase(true, true, true, false)]
        public void Loot_DropsOnDestroy_OnlyForGameplayDestruction(bool isPlaying, bool sceneLoaded, bool quitting,
            bool expected)
        {
            MethodInfo decision = typeof(Loot).GetMethod("ShouldDropOnDestroy",
                BindingFlags.Static | BindingFlags.NonPublic);
            Assert.That(decision, Is.Not.Null);

            bool drops = (bool)decision.Invoke(null, new object[] { isPlaying, sceneLoaded, quitting });

            Assert.That(drops, Is.EqualTo(expected));
        }

        [Test]
        public void Loot_XYScatter_KeepsDepthAndStaysInsideTheRadius()
        {
            GameObject prefab = CreateObject("QualityLoot." + Guid.NewGuid().ToString("N"));
            _lootCloneName = prefab.name + "(Clone)";
            GameObject owner = CreateObject("QualityLootOwner");
            owner.transform.position = new Vector3(1f, 2f, 3f);
            Loot loot = owner.AddComponent<Loot>();
            loot.lootItems = new[] { prefab };
            loot.minCount = 8;
            loot.maxCount = 8;
            loot.dropChance = 1f;
            loot.dropRadius = 2f;
            loot.ScatterPlane = LootScatterPlane.XY;

            loot.DropLoot();

            List<GameObject> clones = FindByName(_lootCloneName);
            Assert.That(clones, Has.Count.EqualTo(8));
            foreach (GameObject clone in clones)
            {
                Vector3 position = clone.transform.position;
                Assert.That(position.z, Is.EqualTo(3f));
                Assert.That(Vector2.Distance(new Vector2(position.x, position.y), new Vector2(1f, 2f)),
                    Is.LessThanOrEqualTo(2.0001f));
            }
        }

        [Test]
        public void Counter_RuntimeReset_ClearsTheSaveKeyRegistry()
        {
            Dictionary<string, List<Counter>> snapshot = new(Counter.Registry);
            MethodInfo reset = typeof(Counter).GetMethod("ResetStaticState",
                BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
            Assert.That(reset, Is.Not.Null);

            try
            {
                Counter.Registry["Quality.Stale." + Guid.NewGuid().ToString("N")] = new List<Counter>();

                reset.Invoke(null, null);

                Assert.That(Counter.Registry, Is.Empty,
                    "With domain reload disabled, the previous session's counters must not stay registered.");
            }
            finally
            {
                Counter.Registry.Clear();
                foreach (KeyValuePair<string, List<Counter>> entry in snapshot)
                {
                    Counter.Registry[entry.Key] = entry.Value;
                }
            }
        }

        [Test]
        public void ScoreManager_Init_DerivesStarsLikeResetScore()
        {
            _saveKey = "Quality.ScoreManager." + Guid.NewGuid().ToString("N");
            GameObject owner = CreateObject("QualityScore");
            ScoreManager score = owner.AddComponent<ScoreManager>();
            SetField(score, "_keySave", _saveKey);
            score.useProgress = false;
            score.starScores = new[] { 0f, 5f };

            Invoke(score, "Init");

            Assert.That(score.CountStarsValue, Is.EqualTo(1), "A threshold of zero is met at score zero.");
            score.Add(10, false);
            score.ResetScore();
            Assert.That(score.CountStarsValue, Is.EqualTo(1));
            Assert.That(score.SaveKey, Is.EqualTo(_saveKey));
        }

        private CameraAspectRatioScaler CreateScaler(float size, CameraAspectRatioScaler.ScaleMode mode,
            out GameObject root, out Camera camera)
        {
            root = CreateObject("QualityScaler");
            camera = root.AddComponent<Camera>();
            camera.orthographic = true;
            camera.orthographicSize = size;
            camera.fieldOfView = 60f;
            CameraAspectRatioScaler scaler = root.AddComponent<CameraAspectRatioScaler>();
            SetField(scaler, "scaleMode", mode);
            SetField(scaler, "minSize", 0.01f);
            SetField(scaler, "maxSize", 1000f);
            Invoke(scaler, "Awake");
            return scaler;
        }

        private GameObject CreateObject(string name)
        {
            GameObject gameObject = new(name);
            gameObject.SetActive(false);
            _created.Add(gameObject);
            return gameObject;
        }

        private RenderTexture CreateTexture(int width, int height)
        {
            RenderTexture texture = new(width, height, 0);
            _textures.Add(texture);
            return texture;
        }

        private static List<GameObject> FindByName(string name)
        {
            List<GameObject> matches = new();
            foreach (GameObject candidate in Object.FindObjectsByType<GameObject>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name == name)
                {
                    matches.Add(candidate);
                }
            }

            return matches;
        }

        private static void SetField(object target, string name, object value)
        {
            FieldInfo field = target.GetType().GetField(name, PrivateInstance);
            Assert.That(field, Is.Not.Null, name);
            field.SetValue(target, value);
        }

        private static void Invoke(object target, string name, params object[] arguments)
        {
            MethodInfo method = target.GetType().GetMethod(name, PrivateInstance);
            Assert.That(method, Is.Not.Null, name);
            method.Invoke(target, arguments);
        }
    }
}
