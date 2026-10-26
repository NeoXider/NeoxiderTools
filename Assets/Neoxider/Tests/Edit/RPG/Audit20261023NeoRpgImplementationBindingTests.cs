using System.Collections.Generic;
using Neo.Core.Resources;
using Neo.Reactive;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023NeoRpgImplementationBindingTests
    {
        private readonly List<Object> _objects = new();

        [TearDown]
        public void TearDown()
        {
            for (int i = _objects.Count - 1; i >= 0; i--)
            {
                if (_objects[i] != null) { Object.DestroyImmediate(_objects[i]); }
            }
            _objects.Clear();
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resource_InFlightSnapshotDropsOldHandlerAfterDisableOrRebind(bool rebind)
        {
            RpgCharacter first = Character(50f);
            RpgCharacter second = Character(7f);
            GameObject view = Track(new GameObject("AuditRpgResourceView"));
            view.SetActive(false);
            RpgResourceBinding binding = view.AddComponent<RpgResourceBinding>();
            ReactivePropertyFloat old = first.HpState;
            bool invalidate = false;
            int earlierCalls = 0;
            old.AddListener((float value) =>
            {
                earlierCalls++;
                if (!invalidate) { return; }
                if (rebind) { binding.Character = second; }
                else { binding.enabled = false; }
            });
            List<float> observed = new();
            binding.OnCurrent.AddListener((float value) => observed.Add(value));
            view.SetActive(true);
            Assert.That(binding.isActiveAndEnabled, Is.True);
            binding.Character = first; // Public binding admission; Edit activation alone does not run OnEnable.
            Assert.That(old.ListenerCount, Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(new[] { 50f }));
            first.Increase(RpgResourceId.Hp, 1f);
            Assert.That(earlierCalls, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f }));
            Assert.That(old.ListenerCount, Is.EqualTo(2), "actual old binding tail is captured before invalidation");
            observed.Clear();
            invalidate = true;

            first.Increase(RpgResourceId.Hp, 1f);

            Assert.That(earlierCalls, Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(rebind ? new[] { 7f } : new float[0]));
            old.Value = 88f;
            Assert.That(observed, Is.EqualTo(rebind ? new[] { 7f, 7f } : new float[0]),
                "only the explicit rebind's initial value may be published");
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Stat_InFlightSnapshotDropsOldHandlerAfterDisableOrRebind(bool rebind)
        {
            RpgCharacter first = Character(50f);
            RpgCharacter second = Character(7f);
            GameObject view = Track(new GameObject("AuditRpgStatView"));
            view.SetActive(false);
            RpgStatBinding binding = view.AddComponent<RpgStatBinding>();
            ReactivePropertyFloat old = first.GetStatState("S");
            bool invalidate = false;
            int earlierCalls = 0;
            old.AddListener((float value) =>
            {
                earlierCalls++;
                if (!invalidate) { return; }
                if (rebind) { binding.Character = second; }
                else { binding.enabled = false; }
            });
            binding.StatId = new RpgStatId("S");
            List<float> observed = new();
            binding.OnValue.AddListener((float value) => observed.Add(value));
            view.SetActive(true);
            Assert.That(binding.isActiveAndEnabled, Is.True);
            binding.Character = first;
            Assert.That(old.ListenerCount, Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(new[] { 50f }));
            first.SetStatBase("S", 51f);
            Assert.That(earlierCalls, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f }));
            Assert.That(old.ListenerCount, Is.EqualTo(2), "actual old binding tail is captured before invalidation");
            observed.Clear();
            invalidate = true;

            first.SetStatBase("S", 99f);

            Assert.That(earlierCalls, Is.EqualTo(2));
            Assert.That(observed, Is.EqualTo(rebind ? new[] { 7f } : new float[0]));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Resource_InitialCurrentObserverSuppressesOldMaxAndPercentContinuation(bool rebind)
        {
            RpgCharacter first = Character(50f);
            RpgCharacter second = Character(7f);
            GameObject view = Track(new GameObject("AuditRpgInitialView"));
            view.SetActive(false);
            RpgResourceBinding binding = view.AddComponent<RpgResourceBinding>();
            int initialCurrents = 0;
            int maxima = 0;
            int percents = 0;
            List<float> currentValues = new();
            binding.OnCurrent.AddListener((float value) =>
            {
                initialCurrents++;
                currentValues.Add(value);
                if (initialCurrents != 1) { return; }
                if (rebind) { binding.Character = second; }
                else { view.SetActive(false); }
            });
            binding.OnMax.AddListener((float value) => maxima++);
            binding.OnPercent.AddListener((float value) => percents++);

            view.SetActive(true);
            Assert.That(binding.isActiveAndEnabled, Is.True);
            Assert.That(initialCurrents, Is.Zero);
            binding.Character = first;

            Assert.That(initialCurrents, Is.EqualTo(rebind ? 2 : 1));
            Assert.That(currentValues, Is.EqualTo(rebind ? new[] { 50f, 7f } : new[] { 50f }));
            Assert.That(maxima, Is.EqualTo(rebind ? 1 : 0));
            Assert.That(percents, Is.EqualTo(rebind ? 1 : 0));
        }

        [Test]
        public void Rebuild_BindingIgnoresReplacedStateBeforeItsRuntimeRebuiltCallback()
        {
            RpgCharacter character = Character(50f);
            ReactivePropertyFloat old = character.HpState;
            character.RuntimeRebuilt += () => old.Value = 1f;
            RpgResourceBinding binding = Track(new GameObject("AuditRpgRebuiltView")).AddComponent<RpgResourceBinding>();
            binding.Character = character;
            List<float> observed = new();
            binding.OnCurrent.AddListener((float value) => observed.Add(value));

            character.RebuildRuntime();

            Assert.That(observed, Is.EqualTo(new[] { 50f }));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void Binding_SourceDestroyedByEarlierListenerSuppressesCapturedTailAndCanRebind(bool resource)
        {
            RpgCharacter source = Character(50f);
            GameObject view = Track(new GameObject("AuditRpgDestroyedSourceView"));
            view.SetActive(false);
            ReactivePropertyFloat old = resource ? source.HpState : source.GetStatState("S");
            bool destroyOnChange = false;
            int earlierCalls = 0;
            old.AddListener((float value) =>
            {
                earlierCalls++;
                if (destroyOnChange && source != null) { Object.DestroyImmediate(source.gameObject); }
            });
            List<float> observed = new();
            RpgResourceBinding resourceBinding = null;
            RpgStatBinding statBinding = null;
            if (resource)
            {
                resourceBinding = view.AddComponent<RpgResourceBinding>();
                resourceBinding.OnCurrent.AddListener((float value) => observed.Add(value));
            }
            else
            {
                statBinding = view.AddComponent<RpgStatBinding>();
                statBinding.StatId = new RpgStatId("S");
                statBinding.OnValue.AddListener((float value) => observed.Add(value));
            }
            view.SetActive(true);
            Assert.That(resource ? resourceBinding.isActiveAndEnabled : statBinding.isActiveAndEnabled, Is.True);
            if (resource) { resourceBinding.Character = source; }
            else { statBinding.Character = source; }
            Assert.That(source != null, Is.True);
            Assert.That(old.ListenerCount, Is.EqualTo(2), "destroying listener precedes the actual binding tail");
            Assert.That(observed, Is.EqualTo(new[] { 50f }));
            if (resource) { source.Increase(RpgResourceId.Hp, 1f); }
            else { source.SetStatBase("S", 51f); }
            Assert.That(earlierCalls, Is.EqualTo(1));
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f }), "live subscribed path must work before destruction");
            Assert.That(old.ListenerCount, Is.EqualTo(2));

            destroyOnChange = true;
            Assert.DoesNotThrow(() =>
            {
                if (resource) { source.Increase(RpgResourceId.Hp, 1f); }
                else { source.SetStatBase("S", 52f); }
            });

            Assert.That(earlierCalls, Is.EqualTo(2));
            Assert.That(source == null, Is.True, "actual source destruction invalidates Unity liveness");
            Assert.That(view.activeInHierarchy, Is.True);
            Assert.That(resource ? resourceBinding.isActiveAndEnabled : statBinding.isActiveAndEnabled, Is.True);
            Assert.That(resource ? resourceBinding.CurrentValue : statBinding.Value, Is.Zero);
            Assert.That(old.CurrentValue, Is.EqualTo(52f), "the destroyed source still has its old managed reactive value");
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f }), "captured tail must not publish 52 after destruction");

            RpgCharacter replacement = Character(7f);
            if (resource)
            {
                resourceBinding.Character = replacement;
                replacement.Increase(RpgResourceId.Hp, 1f);
            }
            else
            {
                statBinding.Character = replacement;
                replacement.SetStatBase("S", 8f);
            }
            Assert.That(old.ListenerCount, Is.EqualTo(1), "rebind detaches the exact cached old state even after destruction");
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f, 7f, 8f }));
            old.Value = 99f;
            Assert.That(observed, Is.EqualTo(new[] { 50f, 51f, 7f, 8f }));
        }

        private RpgCharacter Character(float value)
        {
            RpgCharacterTemplate template = Track(ScriptableObject.CreateInstance<RpgCharacterTemplate>());
            template.resources = new[] { new RpgResourceDefinition { id = new RpgStatId(RpgResourceId.Hp),
                startMax = 100f, startCurrent = value, restoreOnAwake = false } };
            template.stats = new[] { new RpgStatDefinition { id = new RpgStatId("S"), baseValue = value } };
            RpgCharacter character = Track(new GameObject("AuditRpgBindingActor")).AddComponent<RpgCharacter>();
            character.ApplyTemplate(template);
            return character;
        }

        private T Track<T>(T instance) where T : Object { _objects.Add(instance); return instance; }
    }
}
