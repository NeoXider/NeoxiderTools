#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Neo.Abilities;
using Neo.Core.Level;
using Neo.Tools;
using NUnit.Framework;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play.Samples
{
    /// <summary>
    ///     Plays the Survivor demo (a NoCode game assembled from library components) with an autopilot: the player
    ///     kites around the arena and picks upgrade cards. Proves the module wiring end to end: the spawner rings
    ///     enemies, they chase and bite, die into pooled XP orbs that the magnet collects, levels open the card draft,
    ///     cards apply abilities and modifiers, and the survival timer wins the run. A second test reads the saved
    ///     scene: the wiring is persistent UnityEvents between library components, with no game script.
    ///     With <c>-neoSurvivorFrames &lt;dir&gt;</c> on the command line it also writes a JPEG per captured frame
    ///     (HUD included) and the mixed audio (<c>audio.f32</c>, format in <c>audio.txt</c>) for a gameplay video; <c>-neoSurvivorCards "Frost,Blades,..."</c> changes the card order
    ///     and <c>-neoSurvivorGrant "blades,lightning,..."</c> grants weapons at the start for a showcase run.
    /// </summary>
    public sealed class SurvivorDemoGameplayTests
    {
        private const string ScenePath = "Scenes/SurvivorDemo.unity";
        private const string FramesArgument = "-neoSurvivorFrames";
        private const string CardsArgument = "-neoSurvivorCards";
        private const string GrantArgument = "-neoSurvivorGrant";
        private const int SimulatedFps = 30;
        private const int CaptureEvery = 1;
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 720;

        private static readonly string[] DefaultCardPriority =
        {
            "Blades", "Aura", "Lightning", "Burning", "Spear", "Frost", "Bolt", "Might", "Haste", "Vitality", "Swift",
            "Regen", "Ward"
        };

        // WHY: a demo scene leaves DontDestroyOnLoad singletons behind (pool manager, audio manager); the next
        // test class must start without them.
        private readonly SamplePlaySceneIsolation _isolation = new SamplePlaySceneIsolation();

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            yield return _isolation.End();
        }

        [UnityTest]
        public IEnumerator SavedScene_IsWiredFromLibraryComponentsWithoutGameScripts()
        {
            string scene = SamplePlayScenes.RequireDemoScene(ScenePath);
            _isolation.Begin();
            yield return SamplePlayScenes.Load(scene);
            LogAssert.ignoreFailingMessages = false;

            AbilityUnitBehaviour player = FindByName<AbilityUnitBehaviour>("Player");
            Assert.That(player.GetComponent<AbilityAutoCaster>(), Is.Not.Null);
            Assert.That(Describe(player.OnMoveSpeedChanged), Is.EqualTo("KeyboardMover.SetSpeed"));
            Assert.That(Describe(player.OnDamaged),
                Is.EqualTo("TransformAnimator.Shake; TransformAnimator.Shake; SpriteFlash.Flash"),
                "a hit shakes the hero and the camera and flashes the hero white");
            MagneticField magnet = player.GetComponent<MagneticField>();
            Assert.That(magnet.Space, Is.EqualTo(MagneticField.PhysicsSpace.TwoD));
            Assert.That(magnet.Motion, Is.EqualTo(MagneticField.MoveMethod.Kinematic));
            Assert.That(Describe(magnet.OnObjectReached), Is.EqualTo("LevelComponent.AddXp; Despawner.DespawnOther"));

            Spawner spawner = FindByName<Spawner>("Enemy Spawner");
            Assert.That(spawner.AroundTarget, Is.SameAs(player.transform));
            Assert.That(spawner.MaxActive, Is.GreaterThan(0));
            Assert.That(spawner.prefabs.Length, Is.EqualTo(4), "one inactive template per enemy unit template");
            Assert.That(FindByName<Spawner>("Boss Spawner").prefabs.Length, Is.EqualTo(1), "the boss arrives once");
            Assert.That(player.GetComponentsInChildren<AbilityAutoCaster>(true).Length, Is.EqualTo(2),
                "one auto-caster aims at the nearest foe, one at a random foe");
            Assert.That(Object.FindFirstObjectByType<AbilityCuePlayer>(), Is.Not.Null,
                "ability cues play the weapon effects");
            Assert.That(FindInactive("Loadout").GetComponentsInChildren<AbilityCooldownSource>(true).Length,
                Is.EqualTo(7), "one loadout slot per weapon");
            int levelBadges = 0;
            foreach (AbilityNoCodeAction card in FindInactive("UpgradeOverlay")
                         .GetComponentsInChildren<AbilityNoCodeAction>(true))
            {
                Transform badge = card.transform.Find("Level");
                if (badge != null && badge.GetComponent("NoCodeFormattedText") != null)
                {
                    levelBadges++;
                }
            }

            Assert.That(levelBadges, Is.EqualTo(7), "every weapon card shows the level it takes the weapon to");
            foreach (GameObject template in spawner.prefabs)
            {
                AbilityUnitBehaviour enemy = template.GetComponent<AbilityUnitBehaviour>();
                Assert.That(Describe(enemy.OnDied), Is.EqualTo("Loot.DropLoot; Counter.Add; Despawner.Despawn"),
                    template.name);
                Assert.That(Describe(enemy.OnMoveSpeedChanged), Is.EqualTo("Follow.SetPositionSpeed"), template.name);
                Assert.That(Describe(enemy.OnDamaged), Is.EqualTo("TransformAnimator.Shake; SpriteFlash.Flash"),
                    template.name);
                Assert.That(template.GetComponent<Loot>().UseObjectPool, Is.True, template.name);
            }

            LevelComponent level = Object.FindFirstObjectByType<LevelComponent>();
            Assert.That(Describe(level.OnLevelUp), Does.StartWith(
                "GameObject.SetActive; SceneFlowController.Pause; ChanceSystemBehaviour.GenerateUniqueVoid"));
            ChanceSystemBehaviour draft = Object.FindFirstObjectByType<ChanceSystemBehaviour>();
            Assert.That(draft.Manager.Count, Is.EqualTo(13), "one draft entry per upgrade card");
            Assert.That(draft.DrawCount, Is.EqualTo(3));
            for (int i = 0; i < draft.Manager.Count; i++)
            {
                Assert.That(Describe(draft.EventsByIndex[i]), Is.EqualTo("GameObject.SetActive"), "card " + i);
            }

            Assert.That(FindInactive("UpgradeOverlay").activeSelf, Is.False, "Overlays are saved hidden.");
            Assert.That(FindInactive("DefeatOverlay").activeSelf, Is.False);
            Assert.That(FindInactive("VictoryOverlay").activeSelf, Is.False);

            List<string> gameScripts = new();
            foreach (MonoBehaviour behaviour in Object.FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (behaviour.gameObject.scene.path != scene)
                {
                    continue;
                }

                Type type = behaviour.GetType();
                string space = type.Namespace ?? string.Empty;
                bool demo = space.StartsWith("Neo.Samples", StringComparison.Ordinal) ||
                            space.StartsWith("Neo.Demo", StringComparison.Ordinal);
                bool library = !demo && (space == "Neo" || space.StartsWith("Neo.", StringComparison.Ordinal));
                bool engine = space.StartsWith("UnityEngine", StringComparison.Ordinal) ||
                              space.StartsWith("TMPro", StringComparison.Ordinal);
                // WHY: the info card is the only demo-shell component; everything else must be a library block.
                if (!library && !engine && type.Name != "ModuleDemoSceneInfo")
                {
                    gameScripts.Add(type.FullName);
                }
            }

            Assert.That(gameScripts, Is.Empty, "The Survivor demo is assembled from library components only.");
        }

        [UnityTest]
        [Timeout(900000)]
        public IEnumerator Autopilot_KillsLevelsUpTakesCardsAndWinsTheRun()
        {
            string scene = SamplePlayScenes.RequireDemoScene(ScenePath);
            _isolation.Begin();
            yield return SamplePlayScenes.Load(scene);
            LogAssert.ignoreFailingMessages = false;

            int previousCapture = Time.captureFramerate;
            float previousVolume = AudioListener.volume;
            Time.captureFramerate = SimulatedFps;
            Capture capture = Capture.FromCommandLine();
            // WHY: a plain test run must not play the demo music on the developer's speakers.
            AudioListener.volume = capture != null ? previousVolume : 0f;
            string[] priority = CardPriority();
            try
            {
                AbilityUnitBehaviour player = FindByName<AbilityUnitBehaviour>("Player");
                KeyboardMover mover = player.GetComponent<KeyboardMover>();
                LevelComponent level = Object.FindFirstObjectByType<LevelComponent>();
                Counter kills = Object.FindFirstObjectByType<Counter>();
                ChanceSystemBehaviour draft = Object.FindFirstObjectByType<ChanceSystemBehaviour>();
                TimerObject timer = Object.FindFirstObjectByType<TimerObject>();
                Transform actors = FindByName<Transform>("Actors");
                GameObject upgrade = FindInactive("UpgradeOverlay");
                GameObject victory = FindInactive("VictoryOverlay");
                GameObject defeat = FindInactive("DefeatOverlay");
                Assert.That(mover != null && level != null && kills != null && draft != null && timer != null);

                string grant = Argument(GrantArgument);
                if (!string.IsNullOrEmpty(grant))
                {
                    // WHY: "id" grants a weapon, "id:3" also sets its level, so a recording can show late upgrades.
                    foreach (string entry in grant.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        string[] pair = entry.Trim().Split(':');
                        player.Unit.System.GrantAbility(player.UnitId, pair[0]);
                        if (pair.Length > 1 && int.TryParse(pair[1], out int grantLevel))
                        {
                            player.Unit.System.SetAbilityLevel(player.UnitId, pair[0], grantLevel);
                        }
                    }
                }

                capture?.Attach();
                bool bossSeen = false;
                bool spitterSeen = false;
                int picks = 0;
                int frames = 0;
                int maxFrames = Mathf.CeilToInt((timer.duration + 20f) * SimulatedFps);
                List<string> taken = new();
                while (frames < maxFrames && !victory.activeSelf && !defeat.activeSelf)
                {
                    if (upgrade.activeSelf)
                    {
                        // WHY: let the cards show for a moment in the video before the pick.
                        for (int wait = 0; wait < SimulatedFps / 2; wait++)
                        {
                            capture?.Shoot();
                            yield return null;
                        }

                        Button card = PickCard(upgrade, priority);
                        Assert.That(card, Is.Not.Null, "an open draft shows at least one card");
                        taken.Add(card.name);
                        card.onClick.Invoke();
                        picks++;
                    }
                    else if (player.IsAlive)
                    {
                        Vector2 step = Steer(player.transform.position, actors) * (mover.Speed / SimulatedFps);
                        mover.MoveDelta(step);
                    }

                    if (frames % CaptureEvery == 0)
                    {
                        capture?.Shoot();
                    }

                    if (frames % 15 == 0)
                    {
                        for (int i = 0; i < actors.childCount; i++)
                        {
                            Transform actor = actors.GetChild(i);
                            if (!actor.gameObject.activeInHierarchy)
                            {
                                continue;
                            }

                            bossSeen |= actor.name.StartsWith("Enemy_unit_boss", StringComparison.Ordinal);
                            spitterSeen |= actor.name.StartsWith("Enemy_unit_spitter", StringComparison.Ordinal);
                        }
                    }

                    frames++;
                    yield return null;
                }

                for (int tail = 0; tail < SimulatedFps * 2; tail++)
                {
                    capture?.Shoot();
                    yield return null;
                }

                List<string> owned = new();
                foreach (AbilityCooldownSource slot in FindInactive("Loadout")
                             .GetComponentsInChildren<AbilityCooldownSource>(true))
                {
                    if (slot.Level > 0)
                    {
                        owned.Add(slot.AbilityId + " " + slot.Level);
                    }
                }

                string summary = "kills " + kills.ValueInt + ", level " + level.Level + ", weapons " +
                                 string.Join(", ", owned) + ", cards " + string.Join(", ", taken) + ", time " +
                                 timer.CurrentTime.ToString("0") + " s, spitter " + spitterSeen + ", boss " + bossSeen;
                Debug.Log("[SurvivorDemoGameplay] " + (victory.activeSelf ? "VICTORY" : "DEFEAT") + ": " + summary);
                Assert.That(kills.ValueInt, Is.GreaterThan(20), summary);
                Assert.That(level.Level, Is.GreaterThan(3), summary);
                Assert.That(picks, Is.GreaterThan(2), summary);
                Assert.That(owned.Count, Is.GreaterThan(2), "several different weapons are in play: " + summary);
                Assert.That(spitterSeen, Is.True, "the ranged enemy joins the waves: " + summary);
                Assert.That(bossSeen, Is.True, "the boss arrives at two minutes: " + summary);
                Assert.That(victory.activeSelf, Is.True, "the autopilot should survive the timer: " + summary);
            }
            finally
            {
                capture?.Dispose();
                AudioListener.volume = previousVolume;
                Time.captureFramerate = previousCapture;
                Time.timeScale = 1f;
            }
        }

        private static string Describe(UnityEventBase unityEvent)
        {
            List<string> calls = new();
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
            {
                Object target = unityEvent.GetPersistentTarget(i);
                calls.Add((target != null ? target.GetType().Name : "null") + "." +
                          unityEvent.GetPersistentMethodName(i));
            }

            return string.Join("; ", calls);
        }

        private static string Argument(string name)
        {
            string[] args = Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length - 1; i++)
            {
                if (args[i] == name)
                {
                    return args[i + 1];
                }
            }

            return null;
        }

        private static string[] CardPriority()
        {
            string custom = Argument(CardsArgument);
            return string.IsNullOrEmpty(custom)
                ? DefaultCardPriority
                : custom.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>Flees nearby enemies, orbits the arena centre and drifts toward XP orbs when it is safe.</summary>
        private static Vector2 Steer(Vector3 position, Transform actors)
        {
            Vector2 p = position;
            Vector2 flee = Vector2.zero;
            Vector2 orb = Vector2.zero;
            float nearestEnemy = float.MaxValue;
            float nearestOrb = float.MaxValue;
            for (int i = 0; i < actors.childCount; i++)
            {
                Transform child = actors.GetChild(i);
                if (!child.gameObject.activeInHierarchy)
                {
                    continue;
                }

                Vector2 offset = p - (Vector2)child.position;
                float distance = offset.magnitude;
                if (child.TryGetComponent(out AbilityUnitBehaviour unit) && unit.IsAlive)
                {
                    nearestEnemy = Mathf.Min(nearestEnemy, distance);
                    if (distance < 6f && distance > 0.001f)
                    {
                        flee += offset / (distance * distance);
                    }
                }
                else if (distance < nearestOrb && distance < 7f)
                {
                    nearestOrb = distance;
                    orb = -offset.normalized;
                }
            }

            Vector2 orbit = p.sqrMagnitude > 0.01f ? new Vector2(-p.y, p.x).normalized : Vector2.right;
            Vector2 home = p.magnitude > 6f ? -p.normalized * ((p.magnitude - 6f) * 0.25f) : Vector2.zero;
            Vector2 wish = flee * 3f + orbit * 0.6f + home;
            if (nearestEnemy > 2.5f && nearestOrb < 7f)
            {
                wish += orb * 0.8f;
            }

            return wish.sqrMagnitude > 0.0001f ? wish.normalized : orbit;
        }

        private static Button PickCard(GameObject overlay, string[] priority)
        {
            Button[] cards = overlay.GetComponentsInChildren<Button>(false);
            foreach (string preferred in priority)
            {
                foreach (Button card in cards)
                {
                    if (card.name.IndexOf(preferred, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return card;
                    }
                }
            }

            return cards.Length > 0 ? cards[0] : null;
        }

        private static T FindByName<T>(string name) where T : Component
        {
            foreach (T candidate in Object.FindObjectsByType<T>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (candidate.name == name)
                {
                    return candidate;
                }
            }

            Assert.Fail("No " + typeof(T).Name + " named '" + name + "' in the Survivor demo.");
            return null;
        }

        private static GameObject FindInactive(string name)
        {
            foreach (Transform candidate in Object.FindObjectsByType<Transform>(FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                if (candidate.name == name)
                {
                    return candidate.gameObject;
                }
            }

            Assert.Fail("No object named '" + name + "' in the Survivor demo.");
            return null;
        }

        /// <summary>Renders the main camera and the HUD (moved to screen-space camera) into numbered JPEGs.</summary>
        private sealed class Capture : IDisposable
        {
            private readonly string _folder;
            private RenderTexture _target;
            private Texture2D _readback;
            private Camera _camera;
            private FileStream _audio;
            private bool _audioRunning;
            private bool _masterMute;
            private int _index;

            private Capture(string folder)
            {
                _folder = folder;
            }

            public static Capture FromCommandLine()
            {
                string folder = Argument(FramesArgument);
                if (string.IsNullOrEmpty(folder))
                {
                    return null;
                }

                Directory.CreateDirectory(folder);
                return new Capture(folder);
            }

            public void Attach()
            {
                _camera = Camera.main;
                _target = new RenderTexture(CaptureWidth, CaptureHeight, 24);
                _readback = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
                _camera.targetTexture = _target;
                foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
                {
                    if (canvas.isRootCanvas && canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                    {
                        canvas.renderMode = RenderMode.ScreenSpaceCamera;
                        canvas.worldCamera = _camera;
                        canvas.planeDistance = 1f;
                    }
                }

                // WHY: the Game view Mute Audio toggle is an editor-wide preference and would record silence.
                _masterMute = UnityEditor.EditorUtility.audioMasterMute;
                UnityEditor.EditorUtility.audioMasterMute = false;
                _audioRunning = AudioRenderer.Start();
                if (_audioRunning)
                {
                    _audio = new FileStream(Path.Combine(_folder, "audio.f32"), FileMode.Create);
                    File.WriteAllText(Path.Combine(_folder, "audio.txt"),
                        AudioSettings.outputSampleRate + " " + ChannelCount());
                }
            }

            private static int ChannelCount()
            {
                switch (AudioSettings.speakerMode)
                {
                    case AudioSpeakerMode.Mono: return 1;
                    case AudioSpeakerMode.Quad: return 4;
                    case AudioSpeakerMode.Surround: return 5;
                    case AudioSpeakerMode.Mode5point1: return 6;
                    case AudioSpeakerMode.Mode7point1: return 8;
                    default: return 2;
                }
            }

            private void WriteAudio()
            {
                if (!_audioRunning)
                {
                    return;
                }

                int samples = AudioRenderer.GetSampleCountForCaptureFrame();
                NativeArray<float> buffer = new NativeArray<float>(samples * ChannelCount(), Allocator.Temp);
                if (AudioRenderer.Render(buffer))
                {
                    byte[] bytes = new byte[buffer.Length * sizeof(float)];
                    buffer.Reinterpret<byte>(sizeof(float)).CopyTo(bytes);
                    _audio.Write(bytes, 0, bytes.Length);
                }

                buffer.Dispose();
            }

            public void Shoot()
            {
                if (_camera == null)
                {
                    return;
                }

                Canvas.ForceUpdateCanvases();
                _camera.Render();
                RenderTexture previous = RenderTexture.active;
                RenderTexture.active = _target;
                _readback.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
                RenderTexture.active = previous;
                File.WriteAllBytes(Path.Combine(_folder, "frame_" + _index.ToString("00000") + ".jpg"),
                    _readback.EncodeToJPG(90));
                _index++;
                WriteAudio();
            }

            public void Dispose()
            {
                if (_audioRunning)
                {
                    AudioRenderer.Stop();
                    _audioRunning = false;
                }

                _audio?.Dispose();
                UnityEditor.EditorUtility.audioMasterMute = _masterMute;
                if (_camera != null)
                {
                    _camera.targetTexture = null;
                }

                if (_target != null)
                {
                    _target.Release();
                    Object.Destroy(_target);
                }

                if (_readback != null)
                {
                    Object.Destroy(_readback);
                }
            }
        }
    }
}
#endif
