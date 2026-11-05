using System.Collections.Generic;
using Neo.Abilities;
using Neo.Samples;
using Neo.Tools;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the "Abilities: triggers and modifiers" tutorial (Scenes/Abilities/AbilitiesTriggersDemo.unity): the
    ///     content assets from <see cref="AbilityTemplates" /> (attack enhancer, mark, stacking poison, auto-defense
    ///     shield, thorns, formation zone) plus a strike and a golem smash, three unit templates, the formation ring
    ///     prefab, the hub with its library, archetype and trace, the 2D arena with world labels and indicators, the
    ///     lesson card, the "why did this fire" panel and the info card. Re-running it updates everything in place.
    /// </summary>
    internal static class AbilitiesTriggersDemoBuilder
    {
        internal const string ScenePath = "Scenes/Abilities/AbilitiesTriggersDemo.unity";

        private const string Scene = "AbilitiesTriggersDemo.unity";
        private const string Folder = "Scenes/Abilities/Assets/Triggers/";
        private const string RingPrefabPath = Folder + "FormationRing.prefab";
        private const string FormationArchetype = "thunder_formation_zone";

        private static readonly Color Accent = new(1f, 0.55f, 0.26f);
        private static readonly Color HeroColor = new(0.3f, 0.78f, 1f);
        private static readonly Color GolemColor = new(0.95f, 0.36f, 0.42f);
        private static readonly Color ImpColor = new(1f, 0.66f, 0.3f);
        private static readonly Color ShieldColor = new(0.55f, 0.9f, 1f, 0.75f);
        private static readonly Color MarkColor = new(1f, 0.86f, 0.25f);
        private static readonly Color PoisonColor = new(0.45f, 1f, 0.45f, 0.65f);
        private static readonly Color EmpowerColor = new(1f, 0.6f, 0.2f, 0.6f);
        private static readonly Color FormationColor = new(0.65f, 0.55f, 1f, 0.9f);

        private static readonly Vector3 HeroPosition = new(-3.6f, -0.9f, 0f);
        private static readonly Vector3 GolemPosition = new(1.4f, -0.4f, 0f);
        private static readonly Vector3 ImpPosition = new(2.9f, -1.7f, 0f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Content content = DemoSceneKit.Step(Scene, "author the content assets", AuthorContent);
            AbilitiesTriggersDemoGuide guide = DemoSceneKit.Step(Scene, "find or author the guide",
                () => ctx.FindOrCreate<AbilitiesTriggersDemoGuide>("Services", "Demo Guide"));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "find or author the info card host",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            DemoSceneKit.Step(Scene, "author the hub and the pool", () => Hub(ctx, content));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Abilities · Triggers & Modifiers", Accent)
                {
                    Backdrop = DemoBackdrop.None,
                    Placement = DemoContentPlacement.Right,
                    ContentWidth = 540f,
                    ContentOffsetY = 0f,
                    LogLines = 5
                }));
            DemoSceneKit.Step(Scene, "tint the camera", () => TintCamera(ctx, frame));

            Arena arena = DemoSceneKit.Step(Scene, "author the arena and the units", () => AuthorArena(ctx, content));
            DemoPanel card = frame.Content;
            TMP_Text status = DemoSceneKit.Step(Scene, "author the lessons", () => Lessons(ctx, card, guide));
            TMP_Text why = DemoSceneKit.Step(Scene, "author the why panel", () => WhyPanel(ctx, frame));
            DemoSceneKit.Step(Scene, "author the info card", () =>
            {
                ctx.Set(info, "_showRuntimeOverlay", true);
                frame.InfoCard(info, InfoContent());
            });
            DemoSceneKit.Step(Scene, "assign the guide references", () =>
            {
                ctx.Set(guide, "_hero", arena.Hero);
                ctx.Set(guide, "_golem", arena.Golem);
                ctx.Set(guide, "_imp", arena.Imp);
                ctx.Set(guide, "_heroCaster", arena.HeroCaster);
                ctx.Set(guide, "_golemCaster", arena.GolemCaster);
                ctx.Set(guide, "_heroLabel", arena.HeroLabel);
                ctx.Set(guide, "_golemLabel", arena.GolemLabel);
                ctx.Set(guide, "_impLabel", arena.ImpLabel);
                ctx.Set(guide, "_status", status);
                ctx.Set(guide, "_why", why);
                ctx.Set(guide, "_log", frame.Log);
                ctx.Set(guide, "_heroShield", arena.HeroShield);
                ctx.Set(guide, "_golemMark", arena.GolemMark);
                ctx.Set(guide, "_golemPoison", arena.GolemPoison);
                ctx.Set(guide, "_heroEmpower", arena.HeroEmpower);
            });
        }

        private static Content AuthorContent()
        {
            AbilityTemplateBundle empower = AbilityTemplates.AttackEnhancer("empower", 1.5f, 3, 10f, 6f);
            AbilityTemplateBundle mark = AbilityTemplates.Mark("hunter_mark", 15f, 3, 12f, 14f, 4f);
            AbilityTemplateBundle poison = AbilityTemplates.StackingPoison("venom", 4f, 5, 6f, 6f, 14f);
            AbilityTemplateBundle shield = AbilityTemplates.AutoDefenseShield("guardian_barrier", 0.4f, 120f, 6f, 10f);
            AbilityTemplateBundle thorns = AbilityTemplates.Thorns("thorns", 0.3f);
            AbilityTemplateBundle formation = AbilityTemplates.FormationZone("thunder_formation", 2.4f, 6f, 1f, 12f,
                0.6f, 14f, 8f);
            formation.FindAbility("thunder_formation").CastEffects[0].ArchetypeId = FormationArchetype;

            AbilityBlueprint strike = Ability("strike", "Strike", "A basic attack: 20 physical damage.", 20f, 0.6f);
            AbilityBlueprint smash = Ability("golem_smash", "Golem Smash", "The golem hits back: 70 physical damage.",
                70f, 1f);

            List<AbilityDefinition> abilities = new();
            List<ModifierDefinition> modifiers = new();
            Content content = new()
            {
                Strike = AbilityAsset(strike, abilities),
                Smash = AbilityAsset(smash, abilities),
                EmpowerCast = AbilityAsset(empower.FindAbility("empower_cast"), abilities),
                Mark = AbilityAsset(mark.FindAbility("hunter_mark"), abilities),
                Dart = AbilityAsset(poison.FindAbility("venom_dart"), abilities),
                Formation = AbilityAsset(formation.FindAbility("thunder_formation"), abilities)
            };
            AbilityAsset(formation.FindAbility("thunder_formation_pulse"), abilities);
            ModifierAsset(empower.FindModifier("empower"), modifiers);
            ModifierAsset(mark.FindModifier("hunter_mark_mark"), modifiers);
            ModifierAsset(poison.FindModifier("venom"), modifiers);
            ModifierAsset(shield.FindModifier("guardian_barrier"), modifiers);
            ModifierAsset(formation.FindModifier("thunder_formation_slow"), modifiers);
            content.ShieldPassive = ModifierAsset(shield.FindModifier("guardian_barrier_passive"), modifiers);
            content.Thorns = ModifierAsset(thorns.FindModifier("thorns"), modifiers);

            content.Library = DemoSceneAssets.EnsureAsset<AbilityLibrary>(Folder + "TriggersDemoLibrary.asset",
                library =>
                {
                    SerializedObject serialized = new(library);
                    Fill(serialized.FindProperty("_abilities"), abilities);
                    Fill(serialized.FindProperty("_modifiers"), modifiers);
                    serialized.ApplyModifiedPropertiesWithoutUndo();
                });

            content.HeroTemplate = Template("TriggersHero.asset", "Hero", 1, 300f, 0f,
                new List<AbilityDefinition> { content.Strike, content.EmpowerCast, content.Mark, content.Dart, content.Formation },
                new List<ModifierDefinition> { content.ShieldPassive });
            content.GolemTemplate = Template("TriggersGolem.asset", "Golem", 2, 600f, 2f,
                new List<AbilityDefinition> { content.Smash }, new List<ModifierDefinition> { content.Thorns });
            content.ImpTemplate = Template("TriggersImp.asset", "Imp", 2, 180f, 0f, new List<AbilityDefinition>(),
                new List<ModifierDefinition>());
            content.Ring = DemoSceneAssets.EnsurePrefab(RingPrefabPath, BuildRing);
            return content;
        }

        private static AbilityBlueprint Ability(string id, string name, string description, float damage, float cooldown)
        {
            AbilityBlueprint ability = new()
            {
                Id = id,
                DisplayName = name,
                Description = description,
                Targeting = TargetingMode.Unit,
                TeamFilter = AbilityTeamFilter.Enemies,
                Cooldown = cooldown
            };
            ability.ImpactEffects.Add(new EffectNodeData
            {
                OpId = AbilityEffectOps.Damage,
                Target = EffectTargetSelector.Target,
                Amount = damage,
                DamageType = AbilityDamageTypes.Physical
            });
            return ability;
        }

        private static AbilityDefinition AbilityAsset(AbilityBlueprint blueprint, List<AbilityDefinition> all)
        {
            AbilityDefinition asset = DemoSceneAssets.EnsureAsset<AbilityDefinition>(Folder + blueprint.Id + ".asset",
                definition => CopyInto(blueprint, definition.Blueprint));
            all.Add(asset);
            return asset;
        }

        private static ModifierDefinition ModifierAsset(ModifierBlueprint blueprint, List<ModifierDefinition> all)
        {
            ModifierDefinition asset = DemoSceneAssets.EnsureAsset<ModifierDefinition>(Folder + blueprint.Id + ".asset",
                definition => CopyInto(blueprint, definition.Blueprint));
            all.Add(asset);
            return asset;
        }

        private static void CopyInto(object source, object target)
        {
            string json = JsonUtility.ToJson(source);
            if (JsonUtility.ToJson(target) != json)
            {
                JsonUtility.FromJsonOverwrite(json, target);
            }
        }

        private static UnitTemplate Template(string file, string displayName, int team, float health, float armor,
            List<AbilityDefinition> abilities, List<ModifierDefinition> innate)
        {
            return DemoSceneAssets.EnsureAsset<UnitTemplate>(Folder + file, template =>
            {
                SerializedObject serialized = new(template);
                serialized.FindProperty("_displayName").stringValue = displayName;
                serialized.FindProperty("_team").intValue = team;
                SerializedProperty pools = serialized.FindProperty("_resources");
                pools.arraySize = 1;
                SerializedProperty pool = pools.GetArrayElementAtIndex(0);
                pool.FindPropertyRelative(nameof(UnitResourceConfig.ResourceId)).stringValue = AbilityResourceIds.Health;
                pool.FindPropertyRelative(nameof(UnitResourceConfig.Max)).floatValue = health;
                pool.FindPropertyRelative(nameof(UnitResourceConfig.RegenPerSecond)).floatValue = 0f;
                SerializedProperty properties = serialized.FindProperty("_baseProperties");
                properties.arraySize = 1;
                SerializedProperty property = properties.GetArrayElementAtIndex(0);
                property.FindPropertyRelative(nameof(UnitPropertyDefault.PropertyId)).stringValue = AbilityProperties.Armor;
                property.FindPropertyRelative(nameof(UnitPropertyDefault.Value)).floatValue = armor;
                Fill(serialized.FindProperty("_abilities"), abilities);
                Fill(serialized.FindProperty("_innateModifiers"), innate);
                serialized.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static void Fill<T>(SerializedProperty list, List<T> items) where T : Object
        {
            list.arraySize = items.Count;
            for (int i = 0; i < items.Count; i++)
            {
                list.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
            }
        }

        private static void BuildRing(GameObject root)
        {
            SpriteRenderer ring = root.AddComponent<SpriteRenderer>();
            ring.sprite = DemoSceneAssets.Ring;
            ring.color = FormationColor;
            ring.sortingOrder = -2;
            GameObject fill = new("Fill");
            fill.transform.SetParent(root.transform, false);
            SpriteRenderer disc = fill.AddComponent<SpriteRenderer>();
            disc.sprite = DemoSceneAssets.Disc;
            disc.color = new Color(FormationColor.r, FormationColor.g, FormationColor.b, 0.18f);
            disc.sortingOrder = -3;
            float meshRadius = ring.sprite != null ? Mathf.Max(0.01f, ring.sprite.bounds.extents.x) : 0.5f;
            AbilityZoneView view = root.AddComponent<AbilityZoneView>();
            SerializedObject serialized = new(view);
            serialized.FindProperty("_meshRadius").floatValue = meshRadius;
            serialized.FindProperty("_heightScale").floatValue = 1f;
            serialized.FindProperty("_offset").vector3Value = Vector3.zero;
            serialized.FindProperty("_scaleToRadius").boolValue = true;
            serialized.FindProperty("_plane").enumValueIndex = (int)AbilityZonePlane.ScreenXY;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Hub(DemoSceneBuildContext ctx, Content content)
        {
            AbilitySystemBehaviour hub = ctx.FindOrCreate<AbilitySystemBehaviour>("Services", "Ability System");
            ctx.Set(hub, "_libraries", new List<Object> { content.Library });
            ctx.Set(hub, "_traceEnabled", true);
            ctx.Set(hub, "_traceCapacity", 512);
            AbilitiesDemoBuild.SetArchetypes(ctx, hub, new[]
            {
                new KeyValuePair<string, GameObject>(FormationArchetype, content.Ring)
            });
            ctx.FindOrCreate<PoolManager>("Services", "Pool Manager");
        }

        private static void TintCamera(DemoSceneBuildContext ctx, DemoFrame frame)
        {
            if (frame.Camera == null)
            {
                return;
            }

            frame.Camera.orthographic = true;
            frame.Camera.orthographicSize = 5.4f;
            frame.Camera.transform.position = new Vector3(0f, 0f, -10f);
            frame.Camera.clearFlags = CameraClearFlags.SolidColor;
            frame.Camera.backgroundColor = DemoPalette.CameraBackground(Accent);
            ctx.MarkModified(frame.Camera);
            ctx.MarkModified(frame.Camera.transform);
        }

        private static Arena AuthorArena(DemoSceneBuildContext ctx, Content content)
        {
            Transform world = ctx.Root("World").transform;
            DemoSceneKit.WorldSprite(ctx, world, "Arena Glow", DemoSceneAssets.Glow,
                DemoPalette.WithAlpha(Accent, 0.14f), new Vector3(-1f, -0.8f, 0f), 13f, -20);
            DemoSceneKit.WorldSprite(ctx, world, "Arena Floor", DemoSceneAssets.Disc,
                DemoPalette.WithAlpha(new Color(0.14f, 0.12f, 0.2f), 0.9f), new Vector3(-1f, -1.1f, 0f), 11f, -15);

            Arena arena = new();
            arena.Hero = Unit(ctx, world, "Hero", content.HeroTemplate, HeroPosition, HeroColor, 1.4f);
            arena.Golem = Unit(ctx, world, "Golem", content.GolemTemplate, GolemPosition, GolemColor, 2.1f);
            arena.Imp = Unit(ctx, world, "Imp", content.ImpTemplate, ImpPosition, ImpColor, 1.0f);
            arena.HeroCaster = Caster(ctx, arena.Hero, content.HeroTemplate);
            arena.GolemCaster = Caster(ctx, arena.Golem, content.GolemTemplate);
            arena.HeroLabel = Label(ctx, arena.Hero.transform, "Hero Label", 1.75f, HeroColor);
            arena.GolemLabel = Label(ctx, arena.Golem.transform, "Golem Label", 1.7f, GolemColor);
            arena.ImpLabel = Label(ctx, arena.Imp.transform, "Imp Label", 1.0f, ImpColor);
            Indicator(ctx, arena.Hero.transform, "Outline", DemoSceneAssets.Ring, Outline(HeroColor), Vector3.zero, 1.6f,
                3).SetActive(true);
            Indicator(ctx, arena.Golem.transform, "Outline", DemoSceneAssets.Ring, Outline(GolemColor), Vector3.zero,
                2.4f, 3).SetActive(true);
            Indicator(ctx, arena.Imp.transform, "Outline", DemoSceneAssets.Ring, Outline(ImpColor), Vector3.zero, 1.15f,
                3).SetActive(true);
            arena.HeroShield = Indicator(ctx, arena.Hero.transform, "Shield Bubble", DemoSceneAssets.Ring, ShieldColor,
                Vector3.zero, 2.1f, 5);
            arena.HeroEmpower = Indicator(ctx, arena.Hero.transform, "Empower Glow", DemoSceneAssets.Glow, EmpowerColor,
                Vector3.zero, 2.6f, -1);
            arena.GolemMark = Indicator(ctx, arena.Golem.transform, "Mark", DemoSceneAssets.WhiteSquare, MarkColor,
                new Vector3(0f, 1.45f, 0f), 0.42f, 6);
            arena.GolemMark.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            arena.GolemPoison = Indicator(ctx, arena.Golem.transform, "Venom Glow", DemoSceneAssets.Glow, PoisonColor,
                Vector3.zero, 3f, -1);
            return arena;
        }

        private static Color Outline(Color color)
        {
            return Color.Lerp(color, Color.white, 0.55f);
        }

        private static AbilityUnitBehaviour Unit(DemoSceneBuildContext ctx, Transform world, string name,
            UnitTemplate template, Vector3 position, Color color, float size)
        {
            GameObject host = ctx.Child(world, name);
            host.transform.localPosition = position;
            SpriteRenderer body = ctx.Ensure<SpriteRenderer>(host);
            body.sprite = DemoSceneAssets.Disc;
            body.color = color;
            body.sortingOrder = 2;
            host.transform.localScale = new Vector3(size, size, 1f);
            ctx.MarkModified(host.transform);
            ctx.MarkModified(body);
            AbilityUnitBehaviour unit = ctx.Ensure<AbilityUnitBehaviour>(host);
            ctx.Set(unit, "_template", template);
            ctx.Set(unit, "_teamOverride", -1);
            return unit;
        }

        private static AbilityCasterBehaviour Caster(DemoSceneBuildContext ctx, AbilityUnitBehaviour unit,
            UnitTemplate template)
        {
            AbilityCasterBehaviour caster = ctx.Ensure<AbilityCasterBehaviour>(unit.gameObject);
            List<Object> abilities = new();
            foreach (AbilityDefinition ability in template.Abilities)
            {
                abilities.Add(ability);
            }

            ctx.Set(caster, "_abilities", abilities);
            return caster;
        }

        private static TMP_Text Label(DemoSceneBuildContext ctx, Transform unit, string name, float height, Color color)
        {
            Transform world = unit.parent;
            return DemoSceneKit.WorldLabel(ctx, world, name, unit.name, unit.localPosition + new Vector3(0f, height, 0f),
                3.2f, color, new Vector2(5f, 2.4f), 8);
        }

        private static GameObject Indicator(DemoSceneBuildContext ctx, Transform unit, string name, Sprite sprite,
            Color color, Vector3 offset, float size, int order)
        {
            GameObject host = ctx.Child(unit, name);
            host.transform.localPosition = offset;
            float inverse = 1f / Mathf.Max(0.01f, unit.localScale.x);
            host.transform.localScale = new Vector3(size * inverse, size * inverse, 1f);
            SpriteRenderer renderer = ctx.Ensure<SpriteRenderer>(host);
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = order;
            ctx.MarkModified(renderer);
            ctx.MarkModified(host.transform);
            host.SetActive(false);
            return host;
        }

        private static TMP_Text Lessons(DemoSceneBuildContext ctx, DemoPanel card, AbilitiesTriggersDemoGuide guide)
        {
            card.TextRow("Intro", "Hero (blue) fights a Golem with thorns and a small Imp. Press the lessons in " +
                                  "order and watch the arena, the labels, the log and the why panel.", 17f, 44f);

            card.Section("1 · Strike + reflect (golem thorns 30%)");
            Lesson(ctx, card, "Strike golem (20)", guide.Strike,
                "The golem's thorns reflect 30% of each hit: the hero loses 6.");

            card.Section("2 · Attack enhancer · next 3 hits x1.5");
            Lesson(ctx, card, "Empower", guide.Empower, "before_deal_damage + modify_damage multiply, 3 charges.");

            card.Section("3 · Mark · next 3 hits on the golem +15");
            Lesson(ctx, card, "Mark golem", guide.Mark, "before_take_damage + modify_damage add, removed when spent.");

            card.Section("4 · Stacking poison · 4 per second per stack");
            Lesson(ctx, card, "Venom dart", guide.PoisonDart, "Stack policy + AmountPerStack; up to 5 stacks.");

            card.Section("5 · Formation zone · pulses every second");
            Lesson(ctx, card, "Place formation", guide.Formation,
                "zone op: 12 damage + slow to enemies inside for 6 s; the ring is AbilityZoneView.");

            card.Section("6 · Auto-defense shield · below 40% HP");
            Lesson(ctx, card, "Golem smashes hero (70)", guide.GolemSmash,
                "HealthBelow trigger: crossing 40% grants a 120 shield (10 s internal cooldown).");

            Button reset = card.Button("Reset demo");
            AbilitiesDemoBuild.LightLabels(reset);
            DemoWiring.AddVoid(reset.onClick, guide.ResetDemo);

            card.Section("Live state");
            return card.TextRow("Live status", "Empower off · Mark off · Venom off\nShield off · Zones 0", 17f, 52f);
        }

        private static void Lesson(DemoSceneBuildContext ctx, DemoPanel card, string label,
            UnityEngine.Events.UnityAction action, string note)
        {
            Button button = card.Button(label);
            AbilitiesDemoBuild.LightLabels(button);
            DemoWiring.AddVoid(button.onClick, action);
            card.Note(note, 22f);
        }

        private static TMP_Text WhyPanel(DemoSceneBuildContext ctx, DemoFrame frame)
        {
            DemoPanel panel = DemoSceneKit.FloatingPanel(frame, "Why Panel", DemoPanelPlacement.BottomLeft, 560f,
                Vector2.zero);
            panel.Section("Why did this fire? · AbilitySystem.Trace.Explain");
            return panel.TextRow("Why text", "Use a lesson button: the latest reaction and its causes appear here.",
                15f, 150f);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Abilities · triggers and modifiers",
                Purpose = "Six reactive mechanics built only from data: thorns (reflect), an attack enhancer, a mark " +
                          "for the next hits, a stacking poison, a formation zone and an auto-defense shield. The " +
                          "assets come from AbilityTemplates; the trace explains every reaction.",
                RuntimeApi = "AbilityCasterBehaviour.TryCastAtUnit / TryCast / TryCastAtPoint; AbilitySystem.Trace " +
                             "(Explain); ModifierInstance.GetRemainingCharges / Stacks; AbilityTemplates.*.",
                SceneWorkflow = "Hub: library, formation ring archetype, Trace Enabled. Units: UnitTemplate assets " +
                                "with abilities and innate modifiers (shield passive, thorns). Buttons -> " +
                                "AbilitiesTriggersDemoGuide (persistent).",
                Verification = "Strike: golem -20, hero -6 (thorns). Empower: three hits of 30. Mark: three hits " +
                               "+15. Dart: venom stacks tick 4 per stack. Formation: ring, pulses, slow. Smash x3: " +
                               "the shield appears below 40% HP.",
                Hints = new[]
                {
                    "Assets: Scenes/Abilities/Assets/Triggers (abilities, modifiers, templates, library, ring prefab)",
                    "Modifier reactions: Trigger, Max Triggers, Remove When Spent, Hits Only, Cooldown",
                    "Why panel: AbilitySystem.Trace.Explain(latest reaction)"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 430f,
                StartCollapsed = false
            };
        }

        private sealed class Content
        {
            public AbilityDefinition Strike;
            public AbilityDefinition Smash;
            public AbilityDefinition EmpowerCast;
            public AbilityDefinition Mark;
            public AbilityDefinition Dart;
            public AbilityDefinition Formation;
            public ModifierDefinition ShieldPassive;
            public ModifierDefinition Thorns;
            public AbilityLibrary Library;
            public UnitTemplate HeroTemplate;
            public UnitTemplate GolemTemplate;
            public UnitTemplate ImpTemplate;
            public GameObject Ring;
        }

        private sealed class Arena
        {
            public AbilityUnitBehaviour Hero;
            public AbilityUnitBehaviour Golem;
            public AbilityUnitBehaviour Imp;
            public AbilityCasterBehaviour HeroCaster;
            public AbilityCasterBehaviour GolemCaster;
            public TMP_Text HeroLabel;
            public TMP_Text GolemLabel;
            public TMP_Text ImpLabel;
            public GameObject HeroShield;
            public GameObject HeroEmpower;
            public GameObject GolemMark;
            public GameObject GolemPoison;
        }
    }
}
