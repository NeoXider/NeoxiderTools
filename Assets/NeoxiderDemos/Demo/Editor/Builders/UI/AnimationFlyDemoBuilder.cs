using System;
using System.Collections.Generic;
using System.IO;
using Neo.Samples;
using Neo.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the complete Neo.UI AnimationFly demo into AnimationFlyDemo.unity: the configured
    ///     <see cref="AnimationFly" /> service (spawn parent, camera, Bonus Prefab List), world pickup and chest
    ///     markers, the wallet and gem targets, play buttons and tuning sliders wired to
    ///     <see cref="AnimationFlyDemoController" /> through persistent listeners, the log feed and the info card.
    ///     Coin, gem and chest art are PNG assets; the pooled visual is a prefab asset created once.
    /// </summary>
    internal static class AnimationFlyDemoBuilder
    {
        private const string Scene = "Scenes/UI/AnimationFlyDemo.unity";
        private const string CoinSpritePath = "Sprites/UI/AnimationFlyCoin.png";
        private const string GemSpritePath = "Sprites/UI/AnimationFlyGem.png";
        private const string ChestSpritePath = "Sprites/UI/AnimationFlyChest.png";
        private const string SampleSpritePath = "Sprites/Sphere item inventory_Icon.png";
        private const string GlowGemPrefabPath = "Prefabs/UI/AnimationFlyGlowGem.prefab";
        private const string MutedHex = "#9EA3BD";
        private const int UiLayer = 5;

        private static readonly Color Accent = new(1f, 0.76f, 0.24f);
        private static readonly Color GemTint = new(0.36f, 0.84f, 1f);
        private static readonly Color ChestGlow = new(1f, 0.55f, 0.25f);
        private static readonly Vector3 PickupPosition = new(-2.3f, -0.75f, 0f);
        private static readonly Vector3 ChestPosition = new(1.7f, -0.75f, 0f);

        [DemoSceneBuilder(Scene)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Sprite coin = LoadSprite(CoinSpritePath, "coin sprite");
            Sprite gem = LoadSprite(GemSpritePath, "gem sprite");
            Sprite chest = LoadSprite(ChestSpritePath, "chest sprite");
            Sprite sample = LoadSprite(SampleSpritePath, "sample sprite");
            GameObject glowGem = EnsureGlowGemPrefab(gem);

            // WHY: the scene's hand-authored "AnimationFly Demo" object keeps its controller and info component.
            ModuleDemoSceneInfo info = ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info");
            AnimationFlyDemoController controller =
                ctx.FindOrCreate<AnimationFlyDemoController>("Services", "Demo Controller");
            AnimationFly fly = ctx.FindOrCreate<AnimationFly>("Services", "Animation Fly");

            // WHY: no backdrop - the world pickup and chest behind the overlay canvas must stay visible.
            DemoFrame frame = DemoSceneKit.Frame(ctx, new DemoFrameOptions("Neo.UI · AnimationFly", Accent)
            {
                Backdrop = DemoBackdrop.None,
                Placement = DemoContentPlacement.Right,
                ContentWidth = 500f,
                ContentOffsetY = 0f,
                LogLines = 5
            });

            GameObject world = ctx.Root("World");
            DemoSceneKit.WorldSprite(ctx, world.transform, "Stage Glow", DemoSceneAssets.Glow,
                DemoPalette.WithAlpha(Accent, 0.12f), new Vector3(-0.3f, -0.6f, 0f), 7f, -10);
            Transform pickup = WorldMarker(ctx, world.transform, "World Pickup", coin, PickupPosition, Accent,
                "World-space start");
            Transform chestMarker = WorldMarker(ctx, world.transform, "World Chest", chest, ChestPosition, ChestGlow,
                "Fountain and prefab start");

            DemoPanel card = frame.Content;
            card.Section("Targets · Canvas RectTransforms");
            CounterRowParts wallet = CounterRow(ctx, card, "Wallet", coin, Accent, "Coins", "EndTransform of coin flights");
            CounterRowParts gems = CounterRow(ctx, card, "Gems", gem, GemTint, "Gems", "EndTransform of gem flights");

            card.Section("Live · request callbacks");
            TMP_Text rewards = card.ValueLabel("OnReward calls", "0");
            TMP_Text started = card.ValueLabel("OnItemStarted", "0");
            TMP_Text owned = card.ValueLabel("OwnedVisualCount", "0");
            Button reset = card.Button("Reset counters");

            card.Section("Tune · AnimationFly fields");
            DemoSliderRow count = card.SliderRow("Count · request.Count", 1f, 30f, 8f, true, "0");
            DemoSliderRow duration = card.SliderRow("Duration · flyDuration", 0.15f, 2f, 0.65f, false, "0.00");
            DemoSliderRow delay = card.SliderRow("Delay · delayBetweenBonuses", 0f, 0.2f, 0.035f, false, "0.000");
            DemoSliderRow arc = card.SliderRow("Arc · arcStrength", 0f, 400f, 160f, true, "0");
            DemoSliderRow scale = card.SliderRow("Scale · scaleMult", 0.5f, 2.5f, 1f, false, "0.0");
            DemoSliderRow rotation = card.SliderRow("Rotation · rotationDegrees", 0f, 720f, 220f, true, "0");
            Suffix(ctx, duration, " s");
            Suffix(ctx, delay, " s");
            Suffix(ctx, arc, " px");
            Suffix(ctx, scale, "x");
            Suffix(ctx, rotation, "°");
            DemoWiring.AddDynamic(duration.Slider.onValueChanged, controller.SetDuration);
            DemoWiring.AddDynamic(delay.Slider.onValueChanged, controller.SetDelay);
            DemoWiring.AddDynamic(arc.Slider.onValueChanged, controller.SetArc);
            DemoWiring.AddDynamic(scale.Slider.onValueChanged, controller.SetScale);
            DemoWiring.AddDynamic(rotation.Slider.onValueChanged, controller.SetRotation);

            DemoPanel play = frame.Panel("Play Panel", DemoPanelPlacement.BottomLeft, 540f);
            play.Section("Play · AnimationFly.Play(request)");
            Button[] spaces = play.ButtonRow("World -> Wallet", "Screen point -> Wallet");
            Button[] bursts = play.ButtonRow("UI burst -> Gems", "Scatter -> Gems");
            Button[] presets = play.ButtonRow("Fountain", "Magnet");
            Button[] visuals = play.ButtonRow("Fountain + Magnet", "Sample sprite");
            Button[] pooling = play.ButtonRow("Pooled prefab", "By type 0 (list)");
            DemoWiring.AddVoid(spaces[0].onClick, controller.PlayWorldToWallet);
            DemoWiring.AddVoid(spaces[1].onClick, controller.PlayScreenToUi);
            DemoWiring.AddVoid(bursts[0].onClick, controller.PlayUiToUiBurst);
            DemoWiring.AddVoid(bursts[1].onClick, controller.PlayScatterBurst);
            DemoWiring.AddVoid(presets[0].onClick, controller.PlayChestFountain);
            DemoWiring.AddVoid(presets[1].onClick, controller.PlayMagnetToWallet);
            DemoWiring.AddVoid(visuals[0].onClick, controller.PlayFountainMagnet);
            DemoWiring.AddVoid(visuals[1].onClick, controller.PlaySampleSpriteToUi);
            DemoWiring.AddVoid(pooling[0].onClick, controller.PlayPrefabPooled);
            DemoWiring.AddVoid(pooling[1].onClick, controller.PlayByTypeFromList);
            DemoWiring.AddVoid(reset.onClick, controller.ResetCounters);

            play.Section("Time · pause menus");
            Toggle pause = play.Toggle("Pause game · Time.timeScale = 0", false);
            Toggle unscaled = play.Toggle("AnimationFly · Use Unscaled Time", true);
            DemoWiring.AddDynamic(pause.onValueChanged, controller.SetGamePaused);
            DemoWiring.AddDynamic(unscaled.onValueChanged, controller.SetUnscaledTime);

            List<Button> buttons = new();
            buttons.AddRange(spaces);
            buttons.AddRange(bursts);
            buttons.AddRange(presets);
            buttons.AddRange(visuals);
            buttons.AddRange(pooling);
            buttons.Add(reset);
            foreach (Button button in buttons)
            {
                AddPressScale(ctx, button);
            }

            ctx.Set(info, "_showRuntimeOverlay", true);
            frame.InfoCard(info, new DemoInfoContent
            {
                Title = "Neo.UI · AnimationFly",
                Purpose = "Reward fly effects: coins, gems and icons fly from world, screen or UI points into UI " +
                          "counters, with pooling and reward callbacks.",
                RuntimeApi = "AnimationFly.I.Play(new AnimationFlyRequest { Sprite or Prefab, Count, " +
                             "StartTransform, EndTransform, OnReward }).",
                SceneWorkflow = "[Demo] Services/Animation Fly holds the defaults (Spawn Parent = Fly Layer, " +
                                "Bonus Prefab List). Select a button to see its onClick wiring.",
                Verification = "Coins land in the wallet, gems in the gem counter; pause time and the flights " +
                               "still finish on unscaled time.",
                Hints = new[]
                {
                    "Sliders write AnimationFly fields; Count goes into the request",
                    "Pooled prefab: DisableAndPool reuses instances (watch OwnedVisualCount)",
                    "Grant rewards in OnReward, not right after Play"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                StartCollapsed = false,
                PanelWidth = 460f
            });

            // WHY: created last so flying visuals render above every card, the info card included.
            RectTransform flyLayer = frame.Layer("Fly Layer");
            ConfigureFly(ctx, fly, frame, flyLayer, glowGem, gems.Icon);

            ctx.Set(controller, "_fly", fly);
            ctx.Set(controller, "_sampleSprite", sample);
            ctx.Set(controller, "_coinSprite", coin);
            ctx.Set(controller, "_gemSprite", gem);
            ctx.Set(controller, "_prefabVisual", glowGem);
            ctx.Set(controller, "_flyRoot", flyLayer);
            ctx.Set(controller, "_worldPickup", pickup);
            ctx.Set(controller, "_worldChest", chestMarker);
            ctx.Set(controller, "_sourceButton", bursts[0].transform);
            ctx.Set(controller, "_scatterSource", bursts[1].transform);
            ctx.Set(controller, "_walletTarget", wallet.Icon);
            ctx.Set(controller, "_gemTarget", gems.Icon);
            ctx.Set(controller, "_countSlider", count.Slider);
            ctx.Set(controller, "_durationSlider", duration.Slider);
            ctx.Set(controller, "_delaySlider", delay.Slider);
            ctx.Set(controller, "_arcSlider", arc.Slider);
            ctx.Set(controller, "_scaleSlider", scale.Slider);
            ctx.Set(controller, "_rotationSlider", rotation.Slider);
            ctx.Set(controller, "_pauseToggle", pause);
            ctx.Set(controller, "_unscaledToggle", unscaled);
            ctx.Set(controller, "_coinValue", wallet.Value);
            ctx.Set(controller, "_gemValue", gems.Value);
            ctx.Set(controller, "_rewardValue", rewards);
            ctx.Set(controller, "_startedValue", started);
            ctx.Set(controller, "_ownedValue", owned);
            ctx.Set(controller, "_log", frame.Log);
            ctx.Set(controller, "_demoButtons", buttons);
            ctx.Set(controller, "_playOnStart", true);
        }

        private static void ConfigureFly(DemoSceneBuildContext ctx, AnimationFly fly, DemoFrame frame,
            RectTransform flyLayer, GameObject glowGem, RectTransform gemTarget)
        {
            fly.parentCanvas = frame.Canvas;
            fly.spawnParent = flyLayer;
            fly.animationCamera = frame.Camera;
            fly.spawnSpace = AnimationFlySpawnSpace.Canvas;
            fly.defaultStartSpace = AnimationFlyCoordinateSpace.Auto;
            fly.defaultEndSpace = AnimationFlyCoordinateSpace.Auto;
            fly.defaultCompletionMode = AnimationFlyCompletionMode.DisableAndPool;
            fly.useAnchoredPositionForUI = true;
            fly.setAsLastSibling = true;
            fly.destroyOnComplete = true;
            fly.maxPoolPerKey = 64;
            fly.motionPreset = AnimationFlyMotionPreset.Arc;
            fly.flyDuration = 0.65f;
            fly.delayBetweenBonuses = 0.035f;
            // WHY: canvas spawn space works in canvas pixels, so arc and spread are pixel distances.
            fly.arcStrength = 160f;
            fly.middlePoint = 0.4f;
            fly.multY = 0.5f;
            fly.startRandomOffset = new Vector3(18f, 18f, 0f);
            fly.endRandomOffset = new Vector3(6f, 6f, 0f);
            fly.middleRandomOffset = new Vector3(40f, 60f, 0f);
            fly.scaleMult = 1f;
            fly.rotateDuringFlight = true;
            fly.rotationDegrees = 220f;
            fly.useUnscaledTime = true;
            fly.bonusPrefabList = new List<AnimationFly.BonusPrefabData>
            {
                new()
                {
                    bonusType = 0,
                    prefab = glowGem,
                    endPos = gemTarget,
                    isWorldSpace = false,
                    endSpace = AnimationFlyCoordinateSpace.Canvas
                }
            };
            fly.RefreshPrefabCache();
            ctx.MarkModified(fly);
        }

        private static Transform WorldMarker(DemoSceneBuildContext ctx, Transform parent, string name, Sprite sprite,
            Vector3 position, Color glow, string subtitle)
        {
            GameObject marker = ctx.Child(parent, name);
            marker.transform.localPosition = position;
            marker.transform.localRotation = Quaternion.identity;
            marker.transform.localScale = Vector3.one;
            DemoSceneKit.WorldSprite(ctx, marker.transform, "Glow", DemoSceneAssets.Glow,
                DemoPalette.WithAlpha(glow, 0.45f), Vector3.zero, 2.4f, 1);
            DemoSceneKit.WorldSprite(ctx, marker.transform, "Icon", sprite, Color.white, Vector3.zero, 1.1f, 2);

            RectTransform captionRect = ctx.UiChild(marker.transform, "Caption");
            TextMeshPro caption = ctx.Ensure<TextMeshPro>(captionRect.gameObject);
            DemoSceneKit.StyleText(caption, name + "\n<size=75%><color=" + MutedHex + ">" + subtitle + "</color></size>",
                2.2f, DemoPalette.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            caption.textWrappingMode = TextWrappingModes.NoWrap;
            caption.sortingOrder = 5;
            captionRect.localPosition = new Vector3(0f, -1.05f, 0f);
            captionRect.localRotation = Quaternion.identity;
            captionRect.localScale = Vector3.one;
            captionRect.sizeDelta = new Vector2(3.6f, 0.9f);
            ctx.MarkModified(caption);
            return marker.transform;
        }

        private static CounterRowParts CounterRow(DemoSceneBuildContext ctx, DemoPanel panel, string key, Sprite icon,
            Color tint, string title, string subtitle)
        {
            RectTransform row = panel.Row("Target · " + key, 64f);
            Image background = DemoSceneKit.Image(ctx, row, "Background", DemoPalette.WithAlpha(tint, 0.12f));
            DemoSceneKit.Stretch(background.rectTransform);

            Image iconImage = DemoSceneKit.Image(ctx, row, "Icon", Color.white, false);
            iconImage.sprite = icon;
            iconImage.preserveAspect = true;
            DemoSceneKit.Anchor(iconImage.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(38f, 0f), new Vector2(44f, 44f));

            TMP_Text caption = DemoSceneKit.Text(ctx, row, "Caption",
                title + "\n<size=70%><color=" + MutedHex + ">" + subtitle + "</color></size>", 20f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.SetAnchors(caption.rectTransform, Vector2.zero, new Vector2(0.75f, 1f), new Vector2(74f, 4f),
                new Vector2(0f, -4f));

            TMP_Text value = DemoSceneKit.Text(ctx, row, "Value", "0", 34f, tint, TextAlignmentOptions.Right,
                FontStyles.Bold);
            DemoSceneKit.SetAnchors(value.rectTransform, new Vector2(0.6f, 0f), Vector2.one, Vector2.zero,
                new Vector2(-16f, 0f));
            return new CounterRowParts(iconImage.rectTransform, value);
        }

        private static void Suffix(DemoSceneBuildContext ctx, DemoSliderRow row, string suffix)
        {
            if (row.Readout == null)
            {
                return;
            }

            ctx.Set(row.Readout, "_suffix", suffix);
            if (row.Value != null)
            {
                row.Value.text += suffix;
            }
        }

        private static void AddPressScale(DemoSceneBuildContext ctx, Button button)
        {
            ButtonScale press = ctx.Ensure<ButtonScale>(button.gameObject);
            ctx.Set(press, "_rectTransform", button.transform);
            ctx.Set(press, "_pressedSize", new Vector2(0.94f, 0.94f));
            ctx.Set(press, "resizeDuration", 0.08f);
            ctx.Set(press, "_useUnscaledTime", true);
        }

        private static Sprite LoadSprite(string relativePath, string what)
        {
            string path = DemoSampleRoots.Demo + "/" + relativePath;
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null)
            {
                throw new InvalidOperationException(Scene + " (load " + what + "): no Sprite at '" + path +
                                                    "'. Restore the PNG or set its Texture Type to Sprite.");
            }

            return sprite;
        }

        /// <summary>
        ///     The pooled-prefab flight needs a real prefab asset (an inactive scene template would spawn inactive
        ///     clones). Created once by <see cref="DemoSceneAssets.EnsurePrefab" />; an existing asset is kept as is,
        ///     so hand edits survive rebuilds.
        /// </summary>
        private static GameObject EnsureGlowGemPrefab(Sprite gem)
        {
            return DemoSceneAssets.EnsurePrefab(GlowGemPrefabPath, root => BuildGlowGem(root, gem), true);
        }

        private static void BuildGlowGem(GameObject root, Sprite gem)
        {
            RectTransform rect = (RectTransform)root.transform;
            rect.sizeDelta = new Vector2(64f, 64f);
            Image glow = root.AddComponent<Image>();
            glow.sprite = DemoSceneAssets.Glow;
            glow.color = DemoPalette.WithAlpha(GemTint, 0.8f);
            glow.raycastTarget = false;

            GameObject gemObject = new("Gem", typeof(RectTransform));
            gemObject.layer = UiLayer;
            gemObject.transform.SetParent(root.transform, false);
            RectTransform gemRect = (RectTransform)gemObject.transform;
            gemRect.anchorMin = Vector2.zero;
            gemRect.anchorMax = Vector2.one;
            gemRect.offsetMin = new Vector2(12f, 12f);
            gemRect.offsetMax = new Vector2(-12f, -12f);
            Image gemImage = gemObject.AddComponent<Image>();
            gemImage.sprite = gem;
            gemImage.preserveAspect = true;
            gemImage.raycastTarget = false;
        }

        private readonly struct CounterRowParts
        {
            public CounterRowParts(RectTransform icon, TMP_Text value)
            {
                Icon = icon;
                Value = value;
            }

            public RectTransform Icon { get; }
            public TMP_Text Value { get; }
        }
    }
}
