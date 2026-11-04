using System;
using System.Collections.Generic;
using System.Reflection;
using Neo.Abilities;
using Neo.Samples;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace Neo.Demo.Editor.Builders
{
    /// <summary>
    ///     Authors the Neo.Abilities NoCode demo (Scenes/Abilities/AbilitiesNoCodeDemo.unity). The hand-authored units
    ///     (Player with caster, auto-caster and cooldown source; TrainingDummy; AbilitySystem) are kept and their key
    ///     settings re-asserted; the builder owns the whole UI: kit frame, live <c>SetProgress</c> bars, NoCode buttons
    ///     hosting their own <see cref="AbilityNoCodeAction" />, the C# API buttons of <see cref="AbilitiesDemoGuide" />,
    ///     the Auto Zap toggle bound to <c>AbilityAutoCaster.enabled</c>, the live result panel and the info card.
    ///     It replaces the earlier hand-made canvas and the Player-hosted NoCode actions it used.
    /// </summary>
    internal static class AbilitiesNoCodeDemoBuilder
    {
        internal const string ScenePath = "Scenes/Abilities/AbilitiesNoCodeDemo.unity";
        internal const string HowToPlayName = "How to play";
        internal const string HitLabel = "Hit Player 20";
        internal const string HealLabel = "Cast Heal";

        private const string Scene = "AbilitiesNoCodeDemo.unity";
        private const string PlayerName = "Player";
        private const string DummyName = "TrainingDummy";
        private const string LegacyCanvasName = "Canvas";
        private const string AssetFolder = "Scenes/Abilities/Assets/";
        private const string ZapId = "zap";
        private const string HealId = "heal_pulse";
        private const float HitAmount = 20f;

        private static readonly Color Accent = DemoPalette.Accent;
        private static readonly Color ResultColor = new(0.45f, 0.9f, 0.8f);

        [DemoSceneBuilder(ScenePath)]
        public static void Build(DemoSceneBuildContext ctx)
        {
            Units units = DemoSceneKit.Step(Scene, "find or author the units", () => EnsureUnits(ctx));
            AbilitiesDemoGuide guide = DemoSceneKit.Step(Scene, "find or author the AbilitiesDemoGuide",
                () => ctx.FindOrCreate<AbilitiesDemoGuide>("Services", "Demo Guide"));
            ModuleDemoSceneInfo info = DemoSceneKit.Step(Scene, "find or author the ModuleDemoSceneInfo",
                () => ctx.FindOrCreate<ModuleDemoSceneInfo>("Services", "Demo Info"));
            DemoSceneKit.Step(Scene, "remove the legacy hand-made UI", () => RemoveLegacyUi(ctx, units));
            DemoSceneKit.Step(Scene, "use the kit input module", () => DemoSceneKit.EnsureEventSystem(ctx, DemoInputModule.Auto, true));

            DemoFrame frame = DemoSceneKit.Step(Scene, "author the frame", () => DemoSceneKit.Frame(ctx,
                new DemoFrameOptions("Neo.Abilities · NoCode", Accent) { ContentWidth = 920f }));
            DemoSceneKit.Step(Scene, "tint the hand-authored camera", () => TintCamera(ctx, frame));

            DemoPanel card = frame.Content;
            DemoSceneKit.Step(Scene, "author the steps", () => card.Paragraph(
                "1 · Hit Player 20, then Cast Heal - and try Heal again during its 3 s cooldown.\n" +
                "2 · Zap the dummy, raise the level (12 / 20 / 30 damage) and Reset to repeat.", 64f));
            DemoSceneKit.Step(Scene, "author the live bars", () => LiveBars(ctx, card, units));
            DemoSceneKit.Step(Scene, "author the NoCode buttons", () => NoCodeButtons(ctx, card, units, frame.Log));
            Toggle autoZap = DemoSceneKit.Step(Scene, "author the C# API buttons and Auto Zap",
                () => ApiControls(ctx, card, units, guide));
            Result result = DemoSceneKit.Step(Scene, "author the live result panel", () => ResultPanel(ctx, card));
            DemoSceneKit.Step(Scene, "wire unit and caster events to the guide", () => WireUnitEvents(units, guide));
            DemoSceneKit.Step(Scene, "author the info card", () =>
            {
                ctx.Set(info, "_showRuntimeOverlay", true);
                frame.InfoCard(info, InfoContent());
            });
            DemoSceneKit.Step(Scene, "assign the guide references", () =>
            {
                ctx.Set(guide, "_canvas", frame.Canvas);
                ctx.Set(guide, "_player", units.Player);
                ctx.Set(guide, "_dummy", units.Dummy);
                ctx.Set(guide, "_caster", units.Caster);
                ctx.Set(guide, "_autoCaster", units.AutoCaster);
                ctx.Set(guide, "_zapAbilityId", ZapId);
                ctx.Set(guide, "_healAbilityId", HealId);
                ctx.Set(guide, "_status", result.Status);
                ctx.Set(guide, "_feedback", result.Feedback);
                ctx.Set(guide, "_autoZapToggle", autoZap);
                ctx.Set(guide, "_log", frame.Log);
            });
        }

        private static Units EnsureUnits(DemoSceneBuildContext ctx)
        {
            ctx.FindOrCreate<AbilitySystemBehaviour>("Services", "Ability System");
            AbilityDefinition zap = DemoSceneAssets.LoadDemoAsset<AbilityDefinition>(AssetFolder + "Zap.asset");
            AbilityDefinition heal = DemoSceneAssets.LoadDemoAsset<AbilityDefinition>(AssetFolder + "HealPulse.asset");
            UnitTemplate playerTemplate = DemoSceneAssets.LoadDemoAsset<UnitTemplate>(AssetFolder + "DemoUnit.asset");
            UnitTemplate dummyTemplate = DemoSceneAssets.LoadDemoAsset<UnitTemplate>(AssetFolder + "TrainingDummyUnit.asset");

            AbilityUnitBehaviour player = FindOrCreateUnit(ctx, PlayerName, Vector3.zero);
            ctx.Set(player, "_template", playerTemplate);
            ctx.Set(player, "_teamOverride", 0);

            AbilityUnitBehaviour dummy = FindOrCreateUnit(ctx, DummyName, new Vector3(6f, 0f, 0f));
            ctx.Set(dummy, "_template", dummyTemplate);
            ctx.Set(dummy, "_teamOverride", 1);

            AbilityCasterBehaviour caster = ctx.Ensure<AbilityCasterBehaviour>(player.gameObject);
            ctx.Set(caster, "_abilities", new List<Object> { zap, heal });

            AbilityAutoCaster autoCaster = ctx.Ensure<AbilityAutoCaster>(player.gameObject);
            ctx.Set(autoCaster, "_caster", caster);
            ctx.Set(autoCaster, "_abilityIds", new List<string> { ZapId });
            // WHY: Auto Zap starts off so the dummy survives while the person reads; the toggle enables it.
            autoCaster.enabled = false;
            ctx.MarkModified(autoCaster);

            AbilityCooldownSource cooldown = ctx.Ensure<AbilityCooldownSource>(player.gameObject);
            ctx.Set(cooldown, "_caster", caster);
            ctx.Set(cooldown, "_abilityId", ZapId);

            return new Units(player, dummy, caster, autoCaster, heal);
        }

        private static AbilityUnitBehaviour FindOrCreateUnit(DemoSceneBuildContext ctx, string objectName, Vector3 position)
        {
            AbilityUnitBehaviour unit = AbilitiesDemoBuild.FindNamed<AbilityUnitBehaviour>(ctx, objectName);
            if (unit != null)
            {
                return unit;
            }

            GameObject host = ctx.Child(ctx.Root("World").transform, objectName);
            host.transform.localPosition = position;
            ctx.Note("Authored the missing unit '" + objectName + "' under [Demo] World.");
            return ctx.Ensure<AbilityUnitBehaviour>(host);
        }

        private static void RemoveLegacyUi(DemoSceneBuildContext ctx, Units units)
        {
            foreach (GameObject root in ctx.Scene.GetRootGameObjects())
            {
                if (root.name != LegacyCanvasName || DemoSceneBuildContext.IsGenerated(root) ||
                    root.GetComponent<Canvas>() == null || root.transform.Find("Btn_" + HealLabel) == null)
                {
                    continue;
                }

                ctx.Retire(root, "earlier hand-made '" + LegacyCanvasName + "'; its NoCode bars and buttons are " +
                                 "authored in [Demo] Frame now");
            }

            // WHY: the NoCode actions now live on their buttons; the Player-hosted pair only served the removed canvas.
            foreach (AbilityNoCodeAction action in units.Player.GetComponents<AbilityNoCodeAction>())
            {
                if (!DemoSceneBuildContext.IsGenerated(action.gameObject))
                {
                    ctx.Note("Removed the Player-hosted AbilityNoCodeAction (moved onto its button).");
                    Object.DestroyImmediate(action);
                }
            }

            ctx.MarkModified(units.Player.gameObject);
        }

        private static void TintCamera(DemoSceneBuildContext ctx, DemoFrame frame)
        {
            if (frame.Camera == null)
            {
                return;
            }

            frame.Camera.clearFlags = CameraClearFlags.SolidColor;
            frame.Camera.backgroundColor = DemoPalette.CameraBackground(Accent);
            ctx.MarkModified(frame.Camera);
        }

        private static void LiveBars(DemoSceneBuildContext ctx, DemoPanel card, Units units)
        {
            card.Section("Units · SetProgress bound to AbilityUnitBehaviour (NoCode)");
            DemoPanel[] columns = card.Columns("Units", 2);
            AbilitiesDemoBuild.ProgressRow(ctx, columns[0], "Player HP", DemoPalette.Good, units.Player.gameObject,
                nameof(AbilityUnitBehaviour), nameof(AbilityUnitBehaviour.HealthNormalized), 1f, 0.1f);
            AbilitiesDemoBuild.ProgressRow(ctx, columns[1], "Training Dummy HP", DemoPalette.Danger, units.Dummy.gameObject,
                nameof(AbilityUnitBehaviour), nameof(AbilityUnitBehaviour.HealthNormalized), 1f, 0.1f);
            AbilitiesDemoBuild.ProgressRow(ctx, card, "Zap cooldown · AbilityCooldownSource", DemoPalette.Cyan,
                units.Player.gameObject, nameof(AbilityCooldownSource), nameof(AbilityCooldownSource.CooldownNormalized),
                0f, 0.05f);
        }

        private static void NoCodeButtons(DemoSceneBuildContext ctx, DemoPanel card, Units units, DemoLogFeed log)
        {
            card.Section("1 · NoCode · Button -> AbilityNoCodeAction.Execute");
            Button[] buttons = card.ButtonRow(HitLabel, HealLabel);
            AbilitiesDemoBuild.LightLabels(buttons);

            AbilityNoCodeAction hit = ctx.Ensure<AbilityNoCodeAction>(buttons[0].gameObject);
            ctx.Set(hit, "_actionType", (int)AbilityNoCodeActionType.ApplyDamage);
            ctx.Set(hit, "_caster", null);
            ctx.Set(hit, "_unit", units.Player);
            ctx.Set(hit, "_targetUnit", units.Player);
            ctx.Set(hit, "_amount", HitAmount);

            AbilityNoCodeAction heal = ctx.Ensure<AbilityNoCodeAction>(buttons[1].gameObject);
            ctx.Set(heal, "_actionType", (int)AbilityNoCodeActionType.CastById);
            ctx.Set(heal, "_caster", units.Caster);
            ctx.Set(heal, "_unit", null);
            ctx.Set(heal, "_targetUnit", null);
            ctx.Set(heal, "_ability", units.Heal);
            ctx.Set(heal, "_abilityId", HealId);

            DemoWiring.AddVoid(buttons[0].onClick, hit.Execute);
            DemoWiring.AddVoid(buttons[1].onClick, heal.Execute);
            DemoWiring.AddDynamic(hit.OnResultMessage, log.Log);
            DemoWiring.AddDynamic(heal.OnResultMessage, log.Log);
            card.Note("Select a button: its AbilityNoCodeAction holds the action, target and amount. Results go to the log.");
        }

        private static Toggle ApiControls(DemoSceneBuildContext ctx, DemoPanel card, Units units, AbilitiesDemoGuide guide)
        {
            card.Section("2 · C# API · AbilitiesDemoGuide -> AbilityCasterBehaviour");
            Button[] buttons = card.ButtonRow("Zap dummy", "Next level", "Reset demo");
            AbilitiesDemoBuild.LightLabels(buttons);
            DemoWiring.AddVoid(buttons[0].onClick, guide.Zap);
            DemoWiring.AddVoid(buttons[1].onClick, guide.NextLevel);
            DemoWiring.AddVoid(buttons[2].onClick, guide.ResetDemo);

            Toggle autoZap = card.Toggle("Auto Zap · Toggle -> AbilityAutoCaster.enabled (NoCode)", false);
            // WHY: a property setter has no method group in C#; the persistent call shows as "bool enabled".
            DemoWiring.AddSetter(autoZap.onValueChanged, units.AutoCaster, nameof(Behaviour.enabled));
            DemoWiring.AddDynamic(autoZap.onValueChanged, guide.OnAutoZapChanged);
            return autoZap;
        }

        private static Result ResultPanel(DemoSceneBuildContext ctx, DemoPanel card)
        {
            card.Section("Live result");
            RectTransform panel = card.Row(HowToPlayName, 112f);
            TMP_Text status = DemoSceneKit.Text(ctx, panel, "Live status",
                "Player 100/100 HP   ·   Dummy 100/100 HP   ·   Level 1\nHeal: READY   ·   Zap: READY", 20f,
                DemoPalette.Text, TextAlignmentOptions.TopLeft, FontStyles.Bold);
            DemoSceneKit.SetAnchors(status.rectTransform, new Vector2(0f, 0.45f), Vector2.one, Vector2.zero, Vector2.zero);
            TMP_Text feedback = DemoSceneKit.Text(ctx, panel, "Result", "Start with Hit Player 20 above. Watch HP change.",
                20f, ResultColor, TextAlignmentOptions.TopLeft);
            DemoSceneKit.SetAnchors(feedback.rectTransform, Vector2.zero, new Vector2(1f, 0.45f), Vector2.zero,
                Vector2.zero);
            return new Result(status, feedback);
        }

        private static void WireUnitEvents(Units units, AbilitiesDemoGuide guide)
        {
            DemoWiring.AddDynamic(units.Player.OnDamaged, guide.OnPlayerDamaged);
            DemoWiring.AddDynamic(units.Player.OnHealed, guide.OnPlayerHealed);
            DemoWiring.AddDynamic(units.Dummy.OnDamaged, guide.OnDummyDamaged);
            DemoWiring.AddDynamic(units.Caster.OnCastFailed, guide.OnCastFailed);
        }

        private static DemoInfoContent InfoContent()
        {
            return new DemoInfoContent
            {
                Title = "Neo.Abilities · NoCode",
                Purpose = "Data-driven abilities without code: two AbilityUnitBehaviour units built from UnitTemplate " +
                          "assets, a caster with Zap and HealPulse, and NoCode bridges on the buttons.",
                RuntimeApi = "AbilityCasterBehaviour.TryCastAtUnit / Cast(CastRequest) -> CastResult, " +
                             "GetCooldownNormalized; AbilityUnitBehaviour.SetLevel / ApplyDamage / ApplyHeal.",
                SceneWorkflow = "Buttons -> AbilityNoCodeAction.Execute; bars -> SetProgress on HealthNormalized and " +
                                "AbilityCooldownSource; Auto Zap toggle -> AbilityAutoCaster.enabled.",
                Verification = "Hit 20 then Heal (+15), a second Heal is rejected by the cooldown, Zap deals " +
                               "12 / 20 / 30 at levels 1 / 2 / 3, Reset restores both units.",
                Hints = new[]
                {
                    "Btn_Hit Player 20 / Btn_Cast Heal: Button + AbilityNoCodeAction",
                    "Player: unit, caster, auto-caster, cooldown source; TrainingDummy: unit",
                    "Player / TrainingDummy On Damaged -> AbilitiesDemoGuide (persistent)",
                    "Assets: Scenes/Abilities/Assets (DemoUnit, TrainingDummyUnit, Zap, HealPulse)"
                },
                Corner = ModuleDemoSceneInfo.Corner.TopLeft,
                PanelWidth = 460f,
                StartCollapsed = false
            };
        }

        private readonly struct Units
        {
            public Units(AbilityUnitBehaviour player, AbilityUnitBehaviour dummy, AbilityCasterBehaviour caster,
                AbilityAutoCaster autoCaster, AbilityDefinition heal)
            {
                Player = player;
                Dummy = dummy;
                Caster = caster;
                AutoCaster = autoCaster;
                Heal = heal;
            }

            public AbilityUnitBehaviour Player { get; }
            public AbilityUnitBehaviour Dummy { get; }
            public AbilityCasterBehaviour Caster { get; }
            public AbilityAutoCaster AutoCaster { get; }
            public AbilityDefinition Heal { get; }
        }

        private readonly struct Result
        {
            public Result(TMP_Text status, TMP_Text feedback)
            {
                Status = status;
                Feedback = feedback;
            }

            public TMP_Text Status { get; }
            public TMP_Text Feedback { get; }
        }
    }
}
