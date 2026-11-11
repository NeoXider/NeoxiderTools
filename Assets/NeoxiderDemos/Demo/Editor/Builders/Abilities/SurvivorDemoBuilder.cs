using System;
using System.Collections.Generic;
using Neo.Abilities;
using Neo.Samples;
using Neo.Samples.Survivor;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Survivor demo (Scenes/SurvivorDemo.unity) from its <see cref="SurvivorConfig" />: camera, PoolManager,
    ///     the <see cref="AbilitySystemBehaviour" /> hub (library, projectile archetypes, spatial grid), the arena, the
    ///     player, inactive pooled templates (one per enemy unit template, the XP orb, one projectile per archetype)
    ///     and the HUD (<see cref="SurvivorHudView" /> with persistent card and restart wiring) plus the info card.
    ///     The hand-authored "Survivor Game" keeps its config; the builder assigns its scene references. Re-run the
    ///     builder after changing the config's enemy list or projectile archetypes.
    /// </summary>
    internal static class SurvivorDemoBuilder
    {
        internal const string ScenePath = "Scenes/SurvivorDemo.unity";
        internal const int PipSlots = 6;
        internal const int CardSlots = 3;

        private const string Scene = "SurvivorDemo.unity";
        private const string ConfigPath = "Survivor/Data/SurvivorConfig.asset";
        private const float SpatialCellSize = 3f;
        private const int HudSortingOrder = 100;

        private static readonly Color Accent = DemoPalette.Accent;
        private static readonly Color ArenaFrame = new(0.49f, 0.36f, 0.94f, 0.85f);
        private static readonly Color ArenaFloor = new(0.08f, 0.09f, 0.15f);
        private static readonly Color ProjectileGlow = new(0.25f, 0.79f, 0.94f, 0.55f);
        private static readonly Color ProjectileCore = new(0.85f, 0.98f, 1f);
        private static readonly Color OrbGlow = new(1f, 0.85f, 0.3f, 0.5f);
        private static readonly Color OrbCore = new(1f, 0.82f, 0.28f);
        private static readonly Color PipCell = new(0.12f, 0.12f, 0.17f, 0.96f);
        private static readonly Color PipReady = new(0.49f, 0.36f, 0.94f, 0.85f);
        private static readonly Color OverlayTint = new(0.04f, 0.04f, 0.07f, 0.82f);
        private static readonly Color GameOverTint = new(0.04f, 0.04f, 0.07f, 0.9f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            SurvivorGame game = DemoSceneKit.Step(Scene, "find or author the SurvivorGame",
                () => ctx.FindOrCreate<SurvivorGame>("Services", "Survivor Game"));
            SurvivorConfig config = DemoSceneKit.Step(Scene, "resolve the SurvivorConfig", () => ResolveConfig(ctx, game));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "find or author the ModuleDemoSceneInfo",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            Camera camera = DemoSceneKit.Step(Scene, "author the camera", () => SceneCamera(ctx, game, config));
            DemoSceneKit.Step(Scene, "author the EventSystem", () => DemoSceneKit.EnsureEventSystem(ctx, DemoInputModule.Auto, true));
            DemoSceneKit.Step(Scene, "author the PoolManager", () => Pool(ctx));
            World world = DemoSceneKit.Step(Scene, "author the arena, player and pooled templates",
                () => BuildWorld(ctx, config));
            AbilitySystemBehaviour hub = DemoSceneKit.Step(Scene, "author the ability hub",
                () => Hub(ctx, config, world));
            SurvivorHudView hud = DemoSceneKit.Step(Scene, "author the HUD", () => Hud(ctx, game, config, info));

            DemoSceneKit.Step(Scene, "assign the SurvivorGame references", () =>
            {
                ctx.Set(game, "_config", config);
                ctx.Set(game, "_hub", hub);
                ctx.Set(game, "_hud", hud);
                ctx.Set(game, "_camera", camera);
                ctx.Set(game, "_actors", world.Actors);
                ctx.Set(game, "_player", world.Player);
                ctx.Set(game, "_enemyTemplates", world.Enemies);
                ctx.Set(game, "_orbTemplate", world.Orb);
                ctx.Set(game, "_projectileTemplates", world.ProjectileObjects());
            });
        }

        private static SurvivorConfig ResolveConfig(DemoSceneBuildContext ctx, SurvivorGame game)
        {
            SurvivorConfig config = AbilitiesDemoBuild.ReadReference<SurvivorConfig>(game, "_config");
            if (config == null)
            {
                config = DemoSceneAssets.LoadDemoAsset<SurvivorConfig>(ConfigPath);
                ctx.Note("Survivor Game had no config; assigned " + ConfigPath + ".");
            }

            if (config.PlayerTemplate == null)
            {
                throw new InvalidOperationException("SurvivorConfig '" + config.name + "' has no PlayerTemplate.");
            }

            return config;
        }

        private static Camera SceneCamera(DemoSceneBuildContext ctx, SurvivorGame game, SurvivorConfig config)
        {
            Camera camera = DemoSceneKit.EnsureCamera(ctx, Accent);
            SerializedObject serialized = new(game);
            SerializedProperty background = serialized.FindProperty("_background");
            camera.orthographic = true;
            camera.orthographicSize = config.CameraSize;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = background != null ? background.colorValue : DemoPalette.CameraBackground(Accent);
            camera.transform.SetPositionAndRotation(new Vector3(0f, 0f, -10f), Quaternion.identity);
            ctx.MarkModified(camera);
            ctx.MarkModified(camera.transform);
            return camera;
        }

        private static PoolManager Pool(DemoSceneBuildContext ctx)
        {
            PoolManager existing = ctx.Find<PoolManager>();
            if (existing != null)
            {
                return existing;
            }

            // WHY: PoolManager is a DontDestroyOnLoad singleton; only a scene root can be promoted cleanly.
            GameObject root = ctx.Root("Pool Manager");
            return ctx.Ensure<PoolManager>(root);
        }

        private static World BuildWorld(DemoSceneBuildContext ctx, SurvivorConfig config)
        {
            Transform root = ctx.Root("World").transform;
            Arena(ctx, root, config.ArenaExtent);
            Transform actors = ctx.Child(root, "Actors").transform;
            SurvivorPlayerController player = Player(ctx, root, config);

            GameObject templates = ctx.Child(root, "Templates");
            List<KeyValuePair<string, GameObject>> projectiles = new();
            foreach (string archetype in CollectArchetypes(config))
            {
                projectiles.Add(new KeyValuePair<string, GameObject>(archetype, Projectile(ctx, templates.transform, archetype)));
            }

            List<SurvivorEnemy> enemies = new();
            HashSet<UnitTemplate> seen = new();
            for (int i = 0; i < config.Enemies.Count; i++)
            {
                SurvivorEnemyType type = config.Enemies[i];
                if (type == null || type.Template == null)
                {
                    ctx.Warn("SurvivorConfig enemy #" + i + " has no unit template; it never spawns.");
                    continue;
                }

                if (seen.Add(type.Template))
                {
                    enemies.Add(Enemy(ctx, templates.transform, type));
                }
            }

            if (enemies.Count == 0)
            {
                throw new InvalidOperationException("SurvivorConfig '" + config.name + "' has no enemy with a unit template.");
            }

            SurvivorXpOrb orb = Orb(ctx, templates.transform);

            // WHY: ctx.Child re-activates objects on every pass; templates must be saved inactive so pooled clones start
            // inactive and nothing on them runs in the scene.
            foreach (Transform template in templates.transform)
            {
                template.gameObject.SetActive(false);
            }

            templates.SetActive(false);
            return new World(actors, player, enemies.ToArray(), orb, projectiles);
        }

        private static void Arena(DemoSceneBuildContext ctx, Transform root, float extent)
        {
            Transform arena = ctx.Child(root, "Arena").transform;
            // WHY: the bright frame sits behind the darker floor so only a thin neon border shows.
            SpriteRenderer frame = DemoSceneKit.WorldSprite(ctx, arena, "Frame", DemoSceneAssets.RoundedRect, ArenaFrame,
                Vector3.zero, 1f, -21);
            frame.drawMode = SpriteDrawMode.Sliced;
            frame.size = new Vector2(extent * 2f + 0.9f, extent * 2f + 0.9f);
            SpriteRenderer floor = DemoSceneKit.WorldSprite(ctx, arena, "Floor", DemoSceneAssets.RoundedRect, ArenaFloor,
                Vector3.zero, 1f, -20);
            floor.drawMode = SpriteDrawMode.Sliced;
            floor.size = new Vector2(extent * 2f + 0.4f, extent * 2f + 0.4f);
        }

        private static SurvivorPlayerController Player(DemoSceneBuildContext ctx, Transform root, SurvivorConfig config)
        {
            GameObject player = ctx.Child(root, "Player");
            player.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Color color = config.PlayerColor;
            DemoSceneKit.WorldSprite(ctx, player.transform, "Glow", DemoSceneAssets.Glow,
                new Color(color.r, color.g, color.b, 0.4f), Vector3.zero, 1.4f, 4);
            DemoSceneKit.WorldSprite(ctx, player.transform, "Body", DemoSceneAssets.Disc, color, Vector3.zero,
                config.PlayerRadius * 2f, 5);
            AbilityUnitBehaviour unit = ctx.Ensure<AbilityUnitBehaviour>(player);
            ctx.Set(unit, "_template", config.PlayerTemplate);
            ctx.Set(unit, "_teamOverride", -1);
            return ctx.Ensure<SurvivorPlayerController>(player);
        }

        private static GameObject Projectile(DemoSceneBuildContext ctx, Transform templates, string archetype)
        {
            GameObject projectile = ctx.Child(templates, "Projectile_" + archetype);
            DemoSceneKit.WorldSprite(ctx, projectile.transform, "Glow", DemoSceneAssets.Glow, ProjectileGlow, Vector3.zero,
                0.9f, 3);
            DemoSceneKit.WorldSprite(ctx, projectile.transform, "Core", DemoSceneAssets.Disc, ProjectileCore, Vector3.zero,
                0.4f, 4);
            ctx.Ensure<AbilityProjectileBehaviour>(projectile);
            return projectile;
        }

        private static SurvivorEnemy Enemy(DemoSceneBuildContext ctx, Transform templates, SurvivorEnemyType type)
        {
            GameObject enemy = ctx.Child(templates, "Enemy_" + type.Template.name);
            DemoSceneKit.WorldSprite(ctx, enemy.transform, "Body", DemoSceneAssets.Disc, type.Color, Vector3.zero,
                type.Radius * 2f, 1);
            AbilityUnitBehaviour unit = ctx.Ensure<AbilityUnitBehaviour>(enemy);
            ctx.Set(unit, "_template", type.Template);
            ctx.Set(unit, "_teamOverride", -1);
            return ctx.Ensure<SurvivorEnemy>(enemy);
        }

        private static SurvivorXpOrb Orb(DemoSceneBuildContext ctx, Transform templates)
        {
            GameObject orb = ctx.Child(templates, "XpOrb");
            DemoSceneKit.WorldSprite(ctx, orb.transform, "Glow", DemoSceneAssets.Glow, OrbGlow, Vector3.zero, 0.55f, 1);
            DemoSceneKit.WorldSprite(ctx, orb.transform, "Core", DemoSceneAssets.Disc, OrbCore, Vector3.zero, 0.28f, 2);
            return ctx.Ensure<SurvivorXpOrb>(orb);
        }

        private static List<string> CollectArchetypes(SurvivorConfig config)
        {
            List<string> result = new();
            if (config.Library != null)
            {
                foreach (AbilityDefinition ability in config.Library.Abilities)
                {
                    AddArchetype(ability, result);
                }
            }

            // WHY: upgrade abilities may be missing from the library; without their archetypes a projectile upgrade
            // would cast but never spawn anything.
            foreach (SurvivorUpgrade upgrade in config.Upgrades)
            {
                if (upgrade != null)
                {
                    AddArchetype(upgrade.Ability, result);
                }
            }

            return result;
        }

        private static void AddArchetype(AbilityDefinition ability, List<string> result)
        {
            string archetype = ability != null && ability.Blueprint != null ? ability.Blueprint.ProjectileArchetypeId : null;
            if (!string.IsNullOrEmpty(archetype) && !result.Contains(archetype))
            {
                result.Add(archetype);
            }
        }

        private static AbilitySystemBehaviour Hub(DemoSceneBuildContext ctx, SurvivorConfig config, World world)
        {
            AbilitySystemBehaviour hub = ctx.FindOrCreate<AbilitySystemBehaviour>("Services", "Ability System");
            List<Object> libraries = new();
            if (config.Library != null)
            {
                libraries.Add(config.Library);
            }
            else
            {
                ctx.Warn("SurvivorConfig '" + config.name + "' has no ability library.");
            }

            ctx.Set(hub, "_libraries", libraries);
            AbilitiesDemoBuild.SetArchetypes(ctx, hub, world.Projectiles);
            ctx.Set(hub, "_autoTick", true);
            // WHY: showcases the opt-in spatial grid: projectile hits, area effects and auto-cast searches stay cheap
            // with hundreds of chasers; results are identical to the linear scan.
            ctx.Set(hub, "_spatialCellSize", SpatialCellSize);
            return hub;
        }

        private static SurvivorHudView Hud(DemoSceneBuildContext ctx, SurvivorGame game, SurvivorConfig config,
            ModuleDemoSceneInfo info)
        {
            GameObject root = ctx.Root("HUD", true);
            DemoSceneKit.OverlayCanvas(ctx, root, HudSortingOrder, new Vector2(1920f, 1080f));
            RectTransform canvas = (RectTransform)root.transform;
            SurvivorHudView view = ctx.Ensure<SurvivorHudView>(root);

            RectTransform xpFill = XpBar(ctx, canvas);
            TMP_Text level = DemoSceneKit.Text(ctx, canvas, "Level", "LV 1", 22f, DemoPalette.Text,
                TextAlignmentOptions.Left, FontStyles.Bold);
            DemoSceneKit.Anchor(level.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -20f), new Vector2(160f, 30f));
            TMP_Text timer = DemoSceneKit.Text(ctx, canvas, "Timer", "00:00", 40f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Anchor(timer.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -18f), new Vector2(220f, 48f));
            TMP_Text kills = DemoSceneKit.Text(ctx, canvas, "Kills", "0 kills", 20f, DemoPalette.Muted,
                TextAlignmentOptions.Right, FontStyles.Bold);
            DemoSceneKit.Anchor(kills.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-16f, -20f), new Vector2(160f, 30f));

            KitControls(ctx, canvas, game);
            HealthBar health = Health(ctx, canvas);
            Pips pips = AbilityPips(ctx, canvas, config);
            CanvasGroup tutorial = Tutorial(ctx, canvas);

            ctx.Set(info, "_showRuntimeOverlay", true);
            DemoSceneKit.InfoCard(ctx, canvas, info, InfoContent(), Accent);

            Cards cards = UpgradeOverlay(ctx, canvas, game, config);
            GameOver gameOver = GameOverOverlay(ctx, canvas, game);

            ctx.Set(view, "_xpFill", xpFill);
            ctx.Set(view, "_levelText", level);
            ctx.Set(view, "_timerText", timer);
            ctx.Set(view, "_killsText", kills);
            ctx.Set(view, "_hpFill", health.Fill);
            ctx.Set(view, "_hpText", health.Text);
            ctx.Set(view, "_healthFull", DemoPalette.Good);
            ctx.Set(view, "_healthEmpty", DemoPalette.Danger);
            ctx.Set(view, "_pips", pips.Roots);
            ctx.Set(view, "_pipFills", pips.Fills);
            ctx.Set(view, "_pipLabels", pips.Labels);
            ctx.Set(view, "_pipReady", PipReady);
            ctx.Set(view, "_pipCooling", DemoPalette.WithAlpha(PipReady, 0.4f));
            ctx.Set(view, "_tutorial", tutorial);
            ctx.Set(view, "_upgradeOverlay", cards.Overlay);
            ctx.Set(view, "_cards", cards.Roots);
            ctx.Set(view, "_cardTitles", cards.Titles);
            ctx.Set(view, "_cardDescriptions", cards.Descriptions);
            ctx.Set(view, "_cardStrips", cards.Strips);
            ctx.Set(view, "_cardDots", cards.Dots);
            ctx.Set(view, "_gameOverOverlay", gameOver.Overlay);
            ctx.Set(view, "_gameOverStats", gameOver.Stats);
            return view;
        }

        private static RectTransform XpBar(DemoSceneBuildContext ctx, RectTransform canvas)
        {
            Image track = DemoSceneKit.Image(ctx, canvas, "XPBar", DemoPalette.Track);
            RectTransform trackRect = track.rectTransform;
            DemoSceneKit.Anchor(trackRect, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -4f), new Vector2(0f, 10f));
            trackRect.offsetMin = new Vector2(10f, trackRect.offsetMin.y);
            trackRect.offsetMax = new Vector2(-10f, trackRect.offsetMax.y);
            Image fill = DemoSceneKit.Image(ctx, trackRect, "Fill", Accent);
            BarFill(fill.rectTransform, 0f);
            return fill.rectTransform;
        }

        private static void KitControls(DemoSceneBuildContext ctx, RectTransform canvas, SurvivorGame game)
        {
            RectTransform panel = ctx.UiChild(canvas, "Kit Controls");
            AbilitiesDemoBuild.Place(panel, Vector2.one, Vector2.one, Vector2.one, new Vector2(-16f, -58f),
                new Vector2(220f, 128f));
            DemoSceneKit.VerticalLayout(ctx, panel.gameObject, new RectOffset(0, 0, 0, 0), 8f);
            Button levelUp = DemoSceneKit.AccentButton(ctx, panel, "Level up now", Accent, DemoPalette.Text);
            AbilitiesDemoBuild.Size(ctx, levelUp, 220f, 44f);
            Button restart = DemoSceneKit.AccentButton(ctx, panel, "Restart run", DemoPalette.Panel, DemoPalette.Text);
            AbilitiesDemoBuild.Size(ctx, restart, 220f, 44f);
            TMP_Text note = DemoSceneKit.Text(ctx, panel, "Note", "SurvivorGame.GrantLevelUp / RestartRun", 13f,
                DemoPalette.Muted, TextAlignmentOptions.Right);
            LayoutElement noteSize = ctx.Ensure<LayoutElement>(note.gameObject);
            noteSize.preferredHeight = 20f;
            DemoWiring.AddVoid(levelUp.onClick, game.GrantLevelUp);
            DemoWiring.AddVoid(restart.onClick, game.RestartRun);
        }

        private static HealthBar Health(DemoSceneBuildContext ctx, RectTransform canvas)
        {
            Image track = DemoSceneKit.Image(ctx, canvas, "HPBar", DemoPalette.Track);
            DemoSceneKit.Anchor(track.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 28f), new Vector2(340f, 26f));
            Image fill = DemoSceneKit.Image(ctx, track.transform, "Fill", DemoPalette.Good);
            BarFill(fill.rectTransform, 1f);
            TMP_Text text = DemoSceneKit.Text(ctx, track.transform, "HPText", "100 / 100", 15f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(text.rectTransform);
            return new HealthBar(fill, text);
        }

        private static Pips AbilityPips(DemoSceneBuildContext ctx, RectTransform canvas, SurvivorConfig config)
        {
            RectTransform row = ctx.UiChild(canvas, "Pips");
            AbilitiesDemoBuild.Place(row, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(16f, 20f),
                new Vector2(PipSlots * 50f, 46f));
            GameObject[] roots = new GameObject[PipSlots];
            Image[] fills = new Image[PipSlots];
            TMP_Text[] labels = new TMP_Text[PipSlots];
            for (int i = 0; i < PipSlots; i++)
            {
                Image cell = DemoSceneKit.Image(ctx, row, "Pip " + (i + 1), PipCell);
                AbilitiesDemoBuild.Place(cell.rectTransform, Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                    new Vector2(i * 50f, 0f), new Vector2(44f, 0f));
                Image ready = DemoSceneKit.Image(ctx, cell.transform, "Ready", PipReady);
                DemoSceneKit.SetAnchors(ready.rectTransform, Vector2.zero, Vector2.one, new Vector2(2f, 2f),
                    new Vector2(-2f, -2f));
                string preview = i < config.StartingAbilities.Count && !string.IsNullOrEmpty(config.StartingAbilities[i])
                    ? config.StartingAbilities[i].Substring(0, 1).ToUpperInvariant()
                    : "?";
                TMP_Text label = DemoSceneKit.Text(ctx, cell.transform, "Label", preview, 15f, DemoPalette.Text,
                    TextAlignmentOptions.Center, FontStyles.Bold);
                DemoSceneKit.Stretch(label.rectTransform);
                // WHY: the saved scene previews the starting loadout; SurvivorHudView.SetAbilities shows one pip per slot.
                cell.gameObject.SetActive(i < Mathf.Max(1, config.StartingAbilities.Count));
                roots[i] = cell.gameObject;
                fills[i] = ready;
                labels[i] = label;
            }

            return new Pips(roots, fills, labels);
        }

        private static CanvasGroup Tutorial(DemoSceneBuildContext ctx, RectTransform canvas)
        {
            Image panel = DemoSceneKit.Image(ctx, canvas, "Tutorial", DemoPalette.Ink);
            DemoSceneKit.Anchor(panel.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -74f), new Vector2(660f, 40f));
            CanvasGroup group = ctx.Ensure<CanvasGroup>(panel.gameObject);
            group.alpha = 1f;
            group.interactable = false;
            group.blocksRaycasts = false;
            TMP_Text tip = DemoSceneKit.Text(ctx, panel.transform, "Tip",
                "WASD move   ·   weapons auto-fire   ·   collect XP   ·   level up to choose upgrades", 17f,
                DemoPalette.Text, TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Stretch(tip.rectTransform, 8f);
            return group;
        }

        private static Cards UpgradeOverlay(DemoSceneBuildContext ctx, RectTransform canvas, SurvivorGame game,
            SurvivorConfig config)
        {
            Image overlay = DemoSceneKit.Image(ctx, canvas, "UpgradeOverlay", OverlayTint, false, true);
            DemoSceneKit.Stretch(overlay.rectTransform);
            TMP_Text title = DemoSceneKit.Text(ctx, overlay.transform, "Title", "LEVEL UP", 44f, DemoPalette.Text,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Anchor(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -120f), new Vector2(600f, 56f));
            TMP_Text sub = DemoSceneKit.Text(ctx, overlay.transform, "Sub", "Choose one upgrade", 22f, DemoPalette.Muted,
                TextAlignmentOptions.Center);
            DemoSceneKit.Anchor(sub.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -172f), new Vector2(600f, 30f));

            RectTransform row = ctx.UiChild(overlay.transform, "Cards");
            AbilitiesDemoBuild.Place(row, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0f, -10f), new Vector2(830f, 300f));
            HorizontalLayoutGroup layout = ctx.Ensure<HorizontalLayoutGroup>(row.gameObject);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.spacing = 25f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            GameObject[] roots = new GameObject[CardSlots];
            TMP_Text[] titles = new TMP_Text[CardSlots];
            TMP_Text[] descriptions = new TMP_Text[CardSlots];
            Image[] strips = new Image[CardSlots];
            Image[] dots = new Image[CardSlots];
            for (int i = 0; i < CardSlots; i++)
            {
                SurvivorUpgrade preview = i < config.Upgrades.Count ? config.Upgrades[i] : null;
                Color accent = preview != null ? preview.Accent : Accent;
                RectTransform card = ctx.UiChild(row, "Btn_Upgrade " + (i + 1));
                Image background = ctx.Ensure<Image>(card.gameObject);
                DemoSceneKit.StyleImage(background, DemoPalette.Panel, true, true);
                Button button = ctx.Ensure<Button>(card.gameObject);
                button.interactable = true;
                button.transition = Selectable.Transition.ColorTint;
                button.targetGraphic = background;
                ColorBlock colors = ColorBlock.defaultColorBlock;
                colors.normalColor = Color.white;
                colors.highlightedColor = Color.white;
                colors.selectedColor = Color.white;
                colors.pressedColor = new Color(0.85f, 0.85f, 0.9f, 1f);
                button.colors = colors;
                AbilitiesDemoBuild.Size(ctx, button, 260f, 300f);

                Image strip = DemoSceneKit.Image(ctx, card, "Strip", accent);
                AbilitiesDemoBuild.Place(strip.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -16f), new Vector2(-24f, 8f));
                Image dot = DemoSceneKit.Image(ctx, card, "Dot", accent, false);
                dot.sprite = DemoSceneAssets.Disc;
                AbilitiesDemoBuild.Place(dot.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(54f, 54f));
                TMP_Text cardTitle = DemoSceneKit.Text(ctx, card, "Title", preview != null ? preview.Title : "Upgrade", 24f,
                    DemoPalette.Text, TextAlignmentOptions.Center, FontStyles.Bold);
                AbilitiesDemoBuild.Place(cardTitle.rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -108f), new Vector2(-24f, 34f));
                TMP_Text description = DemoSceneKit.Text(ctx, card, "Desc", preview != null ? preview.Description : "",
                    17f, DemoPalette.Muted, TextAlignmentOptions.Top);
                DemoSceneKit.SetAnchors(description.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 18f),
                    new Vector2(-18f, -150f));

                DemoWiring.AddInt(button.onClick, game.PickUpgrade, i);
                roots[i] = card.gameObject;
                titles[i] = cardTitle;
                descriptions[i] = description;
                strips[i] = strip;
                dots[i] = dot;
            }

            overlay.gameObject.SetActive(false);
            return new Cards(overlay.gameObject, roots, titles, descriptions, strips, dots);
        }

        private static GameOver GameOverOverlay(DemoSceneBuildContext ctx, RectTransform canvas, SurvivorGame game)
        {
            Image overlay = DemoSceneKit.Image(ctx, canvas, "GameOverOverlay", GameOverTint, false, true);
            DemoSceneKit.Stretch(overlay.rectTransform);
            TMP_Text title = DemoSceneKit.Text(ctx, overlay.transform, "Title", "YOU FELL", 52f, DemoPalette.Danger,
                TextAlignmentOptions.Center, FontStyles.Bold);
            DemoSceneKit.Anchor(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 110f), new Vector2(640f, 60f));
            TMP_Text stats = DemoSceneKit.Text(ctx, overlay.transform, "Stats", "Survived  00:00\n0 kills   ·   reached level 1",
                26f, DemoPalette.Text, TextAlignmentOptions.Center);
            DemoSceneKit.Anchor(stats.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(640f, 90f));
            Button again = DemoSceneKit.AccentButton(ctx, overlay.transform, "Play again", Accent, DemoPalette.Text);
            AbilitiesDemoBuild.Place((RectTransform)again.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f), new Vector2(0f, -90f), new Vector2(240f, 60f));
            DemoWiring.AddVoid(again.onClick, game.RestartRun);
            overlay.gameObject.SetActive(false);
            return new GameOver(overlay.gameObject, stats);
        }

        private static void BarFill(RectTransform fill, float amount)
        {
            fill.anchorMin = Vector2.zero;
            fill.anchorMax = new Vector2(Mathf.Clamp01(amount), 1f);
            fill.pivot = new Vector2(0f, 0.5f);
            fill.offsetMin = new Vector2(2f, 2f);
            fill.offsetMax = new Vector2(-2f, -2f);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Abilities · Survivor kit",
                Purpose = "A whole survivor loop as Neo.Abilities data: auto-cast weapons, a homing projectile, area " +
                          "bursts, slows, DoTs and level-up upgrades, all read from one SurvivorConfig.",
                RuntimeApi = "AbilitySystem.Cast(CastRequest.AtUnit / NoTarget), GrantAbility, ApplyModifier; " +
                             "AbilitySystemBehaviour.Paused freezes cooldowns and DoTs on the level-up screen.",
                SceneWorkflow = "Survivor Game holds the config; Ability System lists the library, the 'bolt' " +
                                "archetype and Spatial Cell Size 3 (grid queries for hundreds of units).",
                Verification = "WASD to move, weapons fire by themselves, XP orbs level you up; Level up now opens " +
                               "the card picker at once.",
                Hints = new[]
                {
                    "[Demo] World/Player: AbilityUnitBehaviour + SurvivorPlayerController",
                    "[Demo] World/Templates: pooled enemies, XP orb, projectile",
                    "[Demo] HUD: SurvivorHudView; cards -> SurvivorGame.PickUpgrade(i)"
                },
                Corner = ModuleDemoSceneInfo.Corner.BottomRight,
                PanelWidth = 400f,
                StartCollapsed = false
            };
        }

        private sealed class World
        {
            public World(Transform actors, SurvivorPlayerController player, SurvivorEnemy[] enemies, SurvivorXpOrb orb,
                List<KeyValuePair<string, GameObject>> projectiles)
            {
                Actors = actors;
                Player = player;
                Enemies = enemies;
                Orb = orb;
                Projectiles = projectiles;
            }

            public Transform Actors { get; }
            public SurvivorPlayerController Player { get; }
            public SurvivorEnemy[] Enemies { get; }
            public SurvivorXpOrb Orb { get; }
            public List<KeyValuePair<string, GameObject>> Projectiles { get; }

            public GameObject[] ProjectileObjects()
            {
                GameObject[] result = new GameObject[Projectiles.Count];
                for (int i = 0; i < Projectiles.Count; i++)
                {
                    result[i] = Projectiles[i].Value;
                }

                return result;
            }
        }

        private readonly struct HealthBar
        {
            public HealthBar(Image fill, TMP_Text text)
            {
                Fill = fill;
                Text = text;
            }

            public Image Fill { get; }
            public TMP_Text Text { get; }
        }

        private readonly struct Pips
        {
            public Pips(GameObject[] roots, Image[] fills, TMP_Text[] labels)
            {
                Roots = roots;
                Fills = fills;
                Labels = labels;
            }

            public GameObject[] Roots { get; }
            public Image[] Fills { get; }
            public TMP_Text[] Labels { get; }
        }

        private readonly struct Cards
        {
            public Cards(GameObject overlay, GameObject[] roots, TMP_Text[] titles, TMP_Text[] descriptions, Image[] strips,
                Image[] dots)
            {
                Overlay = overlay;
                Roots = roots;
                Titles = titles;
                Descriptions = descriptions;
                Strips = strips;
                Dots = dots;
            }

            public GameObject Overlay { get; }
            public GameObject[] Roots { get; }
            public TMP_Text[] Titles { get; }
            public TMP_Text[] Descriptions { get; }
            public Image[] Strips { get; }
            public Image[] Dots { get; }
        }

        private readonly struct GameOver
        {
            public GameOver(GameObject overlay, TMP_Text stats)
            {
                Overlay = overlay;
                Stats = stats;
            }

            public GameObject Overlay { get; }
            public TMP_Text Stats { get; }
        }
    }
}
