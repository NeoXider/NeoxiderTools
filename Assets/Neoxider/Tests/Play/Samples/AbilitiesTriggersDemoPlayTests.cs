#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Neo.Abilities;
using NUnit.Framework;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Neo.Tests.Play
{
    /// <summary>
    ///     Plays the "Abilities: triggers and modifiers" tutorial live: every lesson button through its authored
    ///     onClick, the resulting domain state (thorns reflection, empowered and marked hits, poison stacks, the
    ///     formation zone and the auto-defense shield) and the "why did this fire" text.
    /// </summary>
    public sealed class AbilitiesTriggersDemoPlayTests
    {
        private const string ScenePath = "Scenes/Abilities/AbilitiesTriggersDemo.unity";
        private const string GuideType = "Neo.Samples.AbilitiesTriggersDemoGuide";

        [UnityTest]
        public IEnumerator TriggersDemo_EveryLessonWorksLive()
        {
            LogAssert.ignoreFailingMessages = false;
            string scene = SamplePlayScenes.RequireDemoScene(ScenePath);
            yield return SamplePlayScenes.Load(scene);
            yield return SamplePlayScenes.WaitFrames(10);

            Component guide = SamplePlayScenes.RequireActiveComponent(GuideType);
            Assert.That(SamplePlayScenes.GetProperty(guide, "IsSceneBuilt"), Is.True,
                "AbilitiesTriggersDemo must carry its authored references.");
            AbilitySystem system = AbilitySystemBehaviour.I.System;
            Assert.That(system.Trace.Enabled, Is.True, "the hub records the trace for the why panel");
            AbilityUnitBehaviour hero = Unit(guide, "_hero");
            AbilityUnitBehaviour golem = Unit(guide, "_golem");
            AbilityUnitBehaviour imp = Unit(guide, "_imp");
            Assert.That(system.Modifiers.CountActive(hero.UnitId, "guardian_barrier_passive"), Is.EqualTo(1),
                "the hero template carries the auto-defense passive");
            Assert.That(system.Modifiers.CountActive(golem.UnitId, "thorns"), Is.EqualTo(1),
                "the golem template carries thorns");

            SamplePlayScenes.ClickButton("Btn_Strike golem (20)");
            yield return SamplePlayScenes.WaitFrames(3);
            Assert.That(golem.CurrentHealth, Is.EqualTo(600f - Physical(20f, 2f)).Within(0.01f));
            Assert.That(hero.CurrentHealth, Is.EqualTo(300f - Physical(20f, 2f) * 0.3f).Within(0.01f),
                "thorns reflected 30% of the mitigated hit");

            yield return Wait(0.7f);
            float beforeEmpower = golem.CurrentHealth;
            SamplePlayScenes.ClickButton("Btn_Empower");
            SamplePlayScenes.ClickButton("Btn_Strike golem (20)");
            yield return SamplePlayScenes.WaitFrames(3);
            Assert.That(beforeEmpower - golem.CurrentHealth, Is.EqualTo(Physical(30f, 2f)).Within(0.01f),
                "an empowered hit deals x1.5");

            yield return Wait(0.7f);
            float beforeMark = golem.CurrentHealth;
            SamplePlayScenes.ClickButton("Btn_Mark golem");
            SamplePlayScenes.ClickButton("Btn_Strike golem (20)");
            yield return SamplePlayScenes.WaitFrames(3);
            Assert.That(beforeMark - golem.CurrentHealth, Is.EqualTo(Physical(52.5f, 2f)).Within(0.01f),
                "(20 + 15 mark) x 1.5 empower, then armor");
            Assert.That(system.Modifiers.TryGetActive(golem.UnitId, "hunter_mark_mark", out ModifierInstance mark),
                Is.True);
            Assert.That(mark.GetRemainingCharges(0), Is.EqualTo(2));

            SamplePlayScenes.ClickButton("Btn_Venom dart");
            yield return Wait(1.1f);
            SamplePlayScenes.ClickButton("Btn_Venom dart");
            yield return Wait(1.2f);
            Assert.That(system.Modifiers.TryGetActive(golem.UnitId, "venom", out ModifierInstance venom), Is.True);
            Assert.That(venom.Stacks, Is.EqualTo(2));

            SamplePlayScenes.ClickButton("Btn_Place formation");
            yield return SamplePlayScenes.WaitFrames(3);
            Assert.That(system.ZoneCount, Is.EqualTo(1));
            Assert.That(imp.CurrentHealth, Is.LessThan(180f), "the imp stands inside the formation");
            Assert.That(imp.Unit.GetProperty(AbilityProperties.MoveSpeed, 1f), Is.EqualTo(0.6f).Within(0.001f),
                "the formation slows enemies inside");
            yield return Wait(1.2f);

            for (int i = 0; i < 3; i++)
            {
                SamplePlayScenes.ClickButton("Btn_Golem smashes hero (70)");
                yield return Wait(1.05f);
            }

            Assert.That(system.Modifiers.CountActive(hero.UnitId, "guardian_barrier"), Is.EqualTo(1),
                "crossing 40% health raised the auto-defense shield");

            TMP_Text why = (TMP_Text)SamplePlayScenes.GetField(guide, "_why");
            Assert.That(why.text, Does.Contain("because"), "the why panel explains the latest reaction");

            SamplePlayScenes.ClickButton("Btn_Reset demo");
            yield return SamplePlayScenes.WaitFrames(3);
            Assert.That(golem.CurrentHealth, Is.EqualTo(600f));
            Assert.That(hero.CurrentHealth, Is.EqualTo(300f));
            Assert.That(system.ZoneCount, Is.Zero);
            Assert.That(system.Modifiers.CountActive(hero.UnitId, "guardian_barrier_passive"), Is.EqualTo(1),
                "reset keeps the innate passives");
        }

        private static float Physical(float amount, float armor)
        {
            return amount * (1f - 0.06f * armor / (1f + 0.06f * armor));
        }

        private static AbilityUnitBehaviour Unit(Component guide, string field)
        {
            AbilityUnitBehaviour unit = SamplePlayScenes.GetField(guide, field) as AbilityUnitBehaviour;
            Assert.That(unit, Is.Not.Null, field);
            Assert.That(unit.Unit, Is.Not.Null, field + " registered");
            return unit;
        }

        private static IEnumerator Wait(float seconds)
        {
            float end = Time.time + seconds;
            while (Time.time < end)
            {
                yield return null;
            }
        }
    }
}
#endif
