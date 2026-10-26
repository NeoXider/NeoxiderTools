using System.Collections;
using Neo.Tools;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TestTools;

#pragma warning disable CS0618
namespace Neo.Editor.Tests
{
    [TestFixture]
    public sealed class Audit20261023NeoRpgCombatLifecycleTests
    {
        private GameObject _source;
        private bool _ownsPlayMode;
        private bool _hasTimeScaleSnapshot;
        private float _previousTimeScale;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            Assert.That(Application.isPlaying, Is.False, "This EditMode fixture must own its Play Mode transition.");
            yield return new EnterPlayMode();
            _ownsPlayMode = true;
            Assert.That(Application.isPlaying, Is.True);
            // Allow ordinary host startup without resetting its scenes, managers or providers.
            yield return null;
            _previousTimeScale = Time.timeScale;
            _hasTimeScaleSnapshot = true;
            Time.timeScale = 1f;
            double scaledStart = Time.timeAsDouble;
            double deadline = Time.realtimeSinceStartupAsDouble + 3d;
            while (Time.timeAsDouble <= scaledStart && Time.realtimeSinceStartupAsDouble < deadline)
            {
                yield return null;
            }

            Assert.That(Time.timeScale, Is.EqualTo(1f), "The fixture's explicit interval control must remain admitted.");
            Assert.That(Time.timeAsDouble, Is.GreaterThan(scaledStart),
                "Scaled time must advance before interpreting Invoke completion as a runtime result.");
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            try
            {
                if (_source != null)
                {
                    Object.DestroyImmediate(_source);
                    _source = null;
                }
            }
            finally
            {
                if (_hasTimeScaleSnapshot)
                {
                    Time.timeScale = _previousTimeScale;
                    _hasTimeScaleSnapshot = false;
                }
            }

            if (_ownsPlayMode && Application.isPlaying)
            {
                _ownsPlayMode = false;
                yield return new ExitPlayMode();
            }
        }

        [UnityTest]
        public IEnumerator Attack_ResetFromStartEvent_CancelsHitAndAllowsNextAttack()
        {
            _source = new GameObject("ResetAttackFromStart");
            AttackExecution attack = _source.AddComponent<AttackExecution>();
            attack.delayTimeAttack = 0f;
            int hits = 0;
            attack.OnStartAttack = new UnityEvent();
            attack.OnStartAttack.AddListener(attack.ResetAttack);
            attack.OnAttack = new UnityEvent();
            attack.OnAttack.AddListener(() => hits++);

            Assert.That(attack.Attack(), Is.True);
            yield return null;

            Assert.That(hits, Is.Zero);
            Assert.That(attack.CurrentState, Is.EqualTo(AttackExecution.AttackState.Ready));
            attack.OnStartAttack.RemoveListener(attack.ResetAttack);
            Assert.That(attack.Attack(), Is.True);
            Assert.That(hits, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Attack_DisabledFromStartEvent_DoesNotApplyHit()
        {
            _source = new GameObject("DisableAttackFromStart");
            AttackExecution attack = _source.AddComponent<AttackExecution>();
            attack.delayTimeAttack = 0f;
            int hits = 0;
            attack.OnStartAttack = new UnityEvent();
            attack.OnStartAttack.AddListener(() => attack.enabled = false);
            attack.OnAttack = new UnityEvent();
            attack.OnAttack.AddListener(() => hits++);

            attack.Attack();
            yield return null;

            Assert.That(hits, Is.Zero);
            Assert.That(attack.CurrentState, Is.EqualTo(AttackExecution.AttackState.Ready));
            Assert.That(attack.CanAttack, Is.False);
            Assert.That(attack.Attack(), Is.False);
            attack.enabled = true;
            Assert.That(attack.CanAttack, Is.True);
        }

        [UnityTest]
        public IEnumerator Attack_ResetFromHitEvent_StaysReady()
        {
            _source = new GameObject("ResetAttackFromHit");
            AttackExecution attack = _source.AddComponent<AttackExecution>();
            attack.delayTimeAttack = 0f;
            attack.OnAttack = new UnityEvent();
            attack.OnAttack.AddListener(attack.ResetAttack);

            Assert.That(attack.Attack(), Is.True);

            Assert.That(attack.CurrentState, Is.EqualTo(AttackExecution.AttackState.Ready));
            Assert.That(attack.CanAttack, Is.True);
            yield return null;
            Assert.That(attack.CurrentState, Is.EqualTo(AttackExecution.AttackState.Ready));
        }

        [UnityTest]
        public IEnumerator Attack_StartedFromEndEvent_CanStillBeCancelled()
        {
            _source = new GameObject("RestartAttackFromEnd");
            AttackExecution attack = _source.AddComponent<AttackExecution>();
            attack.AttackSpeed = 1000f;
            attack.delayTimeAttack = 0f;
            int hits = 0;
            int completions = 0;
            attack.OnAttack = new UnityEvent();
            attack.OnAttack.AddListener(() => hits++);
            attack.OnEndAttack = new UnityEvent();
            attack.OnEndAttack.AddListener(() =>
            {
                completions++;
                if (completions == 1)
                {
                    attack.delayTimeAttack = 0.3f;
                    attack.Attack();
                }
            });

            attack.Attack();
            float deadline = Time.realtimeSinceStartup + 2f;
            while (completions == 0 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(completions, Is.EqualTo(1));
            attack.ResetAttack();
            yield return ObserveScaledDuration(0.5f);

            Assert.That(hits, Is.EqualTo(1), "The second wind-up was cancelled before its hit.");
            Assert.That(attack.CurrentState, Is.EqualTo(AttackExecution.AttackState.Ready));
        }

        [UnityTest]
        public IEnumerator ManagedHitbox_RestartExtendsWindow_AndDisableCancelsPendingEnd()
        {
            _source = new GameObject("RestartManagedHitbox");
            _source.SetActive(false);
            BoxCollider hitbox = _source.AddComponent<BoxCollider>();
            hitbox.isTrigger = true;
            AdvancedAttackCollider attack = _source.AddComponent<AdvancedAttackCollider>();
            using (SerializedObject settings = new(attack))
            {
                settings.FindProperty("autoManageColliders").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            attack.triggerDuration = 0.4f;
            int completedWindows = 0;
            attack.OnDeactivateTrigger = new UnityEvent();
            attack.OnDeactivateTrigger.AddListener(() => completedWindows++);
            _source.SetActive(true);
            Assert.That(attack.isActiveAndEnabled, Is.True);
            Assert.That(hitbox.enabled, Is.False);

            attack.ActivateTrigger();
            Assert.That(hitbox.enabled, Is.True);
            Assert.That(attack.IsInvoking(), Is.True, "The native trigger-end invocation must be scheduled.");
            yield return ObserveScaledDuration(0.25f);
            attack.ActivateTrigger();
            Assert.That(hitbox.enabled, Is.True);
            Assert.That(attack.IsInvoking(), Is.True);
            yield return ObserveScaledDuration(0.2f);

            Assert.That(hitbox.enabled, Is.True, "The previous window must not close the current window.");
            Assert.That(completedWindows, Is.Zero);
            yield return ObserveScaledDuration(0.5f);
            Assert.That(hitbox.enabled, Is.False);
            Assert.That(completedWindows, Is.EqualTo(1));
            Assert.That(attack.IsInvoking(), Is.False);

            attack.ActivateTrigger();
            Assert.That(hitbox.enabled, Is.True);
            Assert.That(attack.IsInvoking(), Is.True);
            _source.SetActive(false);
            Assert.That(hitbox.enabled, Is.False);
            Assert.That(attack.IsInvoking(), Is.False);
            _source.SetActive(true);
            yield return ObserveScaledDuration(0.5f);

            Assert.That(hitbox.enabled, Is.False);
            Assert.That(completedWindows, Is.EqualTo(1), "An interrupted window has no delayed completion.");
            Assert.That(attack.IsInvoking(), Is.False);
        }

        [UnityTest]
        public IEnumerator Evade_CompletedEvent_ObservesDeferredCooldownBeforeRestartAttempt()
        {
            _source = new GameObject("DeferredEvadeCooldown");
            Evade evade = _source.AddComponent<Evade>();
            evade.evadeDuration = 0.05f;
            evade.cooldownDuration = 100f;
            evade.cooldownStartsWithEvade = false;
            int completions = 0;
            bool restarted = false;
            bool cooldownObservedBeforeReentry = false;
            evade.OnEvadeCompleted = new UnityEvent();
            evade.OnEvadeCompleted.AddListener(() =>
            {
                completions++;
                cooldownObservedBeforeReentry = evade.IsOnCooldown;
                restarted = evade.TryStartEvade();
            });

            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(evade.isActiveAndEnabled, Is.True);
            Assert.That(evade.IsEvading, Is.True);
            Assert.That(evade.IsOnCooldown, Is.False);
            Assert.That(evade.IsInvoking(), Is.True, "The native action-end invocation must be scheduled.");
            yield return ObserveScaledDuration(0.15f);

            Assert.That(completions, Is.EqualTo(1));
            Assert.That(cooldownObservedBeforeReentry, Is.True);
            Assert.That(restarted, Is.False);
            Assert.That(evade.IsOnCooldown, Is.True);
        }

        [UnityTest]
        public IEnumerator Evade_DurationChangeAppliesNextCycle_AndDisableRestoresReadyState()
        {
            _source = new GameObject("EvadeCooldownConfiguration");
            Evade evade = _source.AddComponent<Evade>();
            evade.cooldownDuration = 100f;
            evade.evadeDuration = 100f;
            int cooldownCompletions = 0;
            evade.OnCooldownCompleted = new UnityEvent();
            evade.OnCooldownCompleted.AddListener(() => cooldownCompletions++);
            Assert.That(evade.TryStartEvade(), Is.True);
            float remaining = evade.GetRemainingCooldown();

            evade.SetCooldownDuration(200f);
            Assert.That(evade.GetRemainingCooldown(), Is.EqualTo(remaining).Within(0.0001f));
            _source.SetActive(false);
            Assert.That(evade.CanEvade, Is.False);
            Assert.That(evade.TryStartEvade(), Is.False);
            _source.SetActive(true);
            yield return null;

            Assert.That(evade.IsEvading, Is.False);
            Assert.That(evade.IsOnCooldown, Is.False);
            Assert.That(evade.CanEvade, Is.True);
            Assert.That(evade.CooldownProgress, Is.EqualTo(1f));
            Assert.That(evade.ReloadProgressValue, Is.EqualTo(1f));
            Assert.That(evade.RemainingCooldownTimeValue, Is.Zero);
            Assert.That(cooldownCompletions, Is.Zero);
            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(evade.GetRemainingCooldown(), Is.GreaterThan(190f));
        }

        [UnityTest]
        public IEnumerator Evade_DisabledFromStartedEvent_LeavesNoCooldownOrDelayedCompletion()
        {
            _source = new GameObject("DisableEvadeFromStarted");
            Evade evade = _source.AddComponent<Evade>();
            evade.evadeDuration = 0.05f;
            int completions = 0;
            evade.OnEvadeStarted = new UnityEvent();
            evade.OnEvadeStarted.AddListener(() => _source.SetActive(false));
            evade.OnEvadeCompleted = new UnityEvent();
            evade.OnEvadeCompleted.AddListener(() => completions++);

            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(evade.IsEvading, Is.False);
            Assert.That(evade.IsOnCooldown, Is.False);
            _source.SetActive(true);
            yield return ObserveScaledDuration(0.15f);

            Assert.That(completions, Is.Zero);
            Assert.That(evade.CanEvade, Is.True);
        }

        [UnityTest]
        public IEnumerator Evade_ResetFromCooldownStarted_CancelsCooldownWithoutDelayedUpdates()
        {
            yield return VerifyCooldownStartCancellation(false);
        }

        [UnityTest]
        public IEnumerator Evade_DisabledFromCooldownStarted_CancelsCooldownWithoutDelayedUpdates()
        {
            yield return VerifyCooldownStartCancellation(true);
        }

        private IEnumerator VerifyCooldownStartCancellation(bool disableActor)
        {
            _source = new GameObject("CancelEvadeFromCooldownStarted");
            Evade evade = _source.AddComponent<Evade>();
            evade.evadeDuration = 100f;
            evade.cooldownDuration = 0.1f;
            evade.cooldownUpdateInterval = 0.015f;
            evade.useUnscaledTimeForCooldown = true;
            int cooldownStarts = 0;
            int cooldownCompletions = 0;
            int actionCompletions = 0;
            int outputChangesAfterCancellation = 0;
            bool cancelled = false;
            evade.OnCooldownStarted = new UnityEvent();
            evade.OnCooldownStarted.AddListener(() =>
            {
                cooldownStarts++;
                if (disableActor)
                {
                    _source.SetActive(false);
                }
                else
                {
                    evade.ResetCooldown();
                }

                cancelled = true;
            });
            evade.OnCooldownCompleted = new UnityEvent();
            evade.OnCooldownCompleted.AddListener(() => cooldownCompletions++);
            evade.OnEvadeCompleted = new UnityEvent();
            evade.OnEvadeCompleted.AddListener(() => actionCompletions++);
            evade.ReloadProgress.OnChanged.AddListener(_ =>
            {
                if (cancelled)
                {
                    outputChangesAfterCancellation++;
                }
            });
            evade.RemainingCooldownTime.OnChanged.AddListener(_ =>
            {
                if (cancelled)
                {
                    outputChangesAfterCancellation++;
                }
            });

            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(cooldownStarts, Is.EqualTo(1), "The cancellation must run inside a real timer-start dispatch.");
            Assert.That(evade.IsEvading, Is.EqualTo(!disableActor));
            if (disableActor)
            {
                Assert.That(evade.CanEvade, Is.False);
                _source.SetActive(true);
            }

            double observationDeadline = Time.realtimeSinceStartupAsDouble + 0.35d;
            do
            {
                Assert.That(evade.IsOnCooldown, Is.False);
                Assert.That(evade.GetRemainingCooldown(), Is.Zero);
                Assert.That(evade.RemainingCooldownTimeValue, Is.Zero);
                Assert.That(evade.ReloadProgressValue, Is.EqualTo(1f));
                Assert.That(evade.CooldownProgress, Is.EqualTo(1f));
                Assert.That(evade.IsEvading, Is.EqualTo(!disableActor));
                Assert.That(evade.CanEvade, Is.EqualTo(disableActor));
                Assert.That(cooldownCompletions, Is.Zero);
                Assert.That(actionCompletions, Is.Zero);
                Assert.That(outputChangesAfterCancellation, Is.Zero);
                yield return null;
            } while (Time.realtimeSinceStartupAsDouble < observationDeadline);

            // Remove cancellation only after observing beyond the cancelled cooldown's original duration.
            cancelled = false;
            evade.OnCooldownStarted.RemoveAllListeners();
            evade.OnCooldownStarted.AddListener(() => cooldownStarts++);
            _source.SetActive(false);
            _source.SetActive(true);
            evade.evadeDuration = 0.05f;
            Assert.That(evade.TryStartEvade(), Is.True);
            Assert.That(evade.IsOnCooldown, Is.True);
            double completionDeadline = Time.realtimeSinceStartupAsDouble + 2d;
            while ((!evade.CanEvade || cooldownCompletions == 0 || actionCompletions == 0) &&
                   Time.realtimeSinceStartupAsDouble < completionDeadline)
            {
                yield return null;
            }

            Assert.That(cooldownStarts, Is.EqualTo(2));
            Assert.That(cooldownCompletions, Is.EqualTo(1));
            Assert.That(actionCompletions, Is.EqualTo(1));
            Assert.That(evade.CanEvade, Is.True);
            Assert.That(evade.IsEvading, Is.False);
            Assert.That(evade.IsOnCooldown, Is.False);
            Assert.That(evade.RemainingCooldownTimeValue, Is.Zero);
            Assert.That(evade.ReloadProgressValue, Is.EqualTo(1f));
        }

        [UnityTest]
        public IEnumerator Health_AwakePreservesAuthoredHpWhenRestoreIsDisabled()
        {
            _source = new GameObject("AuthoredCombatHealth");
            _source.SetActive(false);
            Health health = _source.AddComponent<Health>();
            using (SerializedObject settings = new(health))
            {
                settings.FindProperty("restoreOnAwake").boolValue = false;
                settings.FindProperty("hp").intValue = 7;
                settings.ApplyModifiedPropertiesWithoutUndo();
            }

            _source.SetActive(true);

            Assert.That(health.HpValue, Is.EqualTo(7));
            Assert.That(health.HpPercentValue, Is.EqualTo(0.7f).Within(0.0001f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Health_AutoHealStarts_StopsWhileDisabled_AndRestartsOnEnable()
        {
            _source = new GameObject("AutoHealCombatHealth");
            Health health = _source.AddComponent<Health>();
            health.SetMaxHp(100);
            health.SetHp(3);
            health.SetHeal(2, 0.1f);
            float deadline = Time.realtimeSinceStartup + 2f;
            while (health.HpValue == 3 && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            // Observe looping Timer completion through Health's public behavior.
            Assert.That(health.HpValue, Is.GreaterThan(3));
            _source.SetActive(false);
            int pausedHp = health.HpValue;
            yield return ObserveScaledDuration(0.3f);
            Assert.That(health.HpValue, Is.EqualTo(pausedHp));
            _source.SetActive(true);
            deadline = Time.realtimeSinceStartup + 2f;
            while (health.HpValue == pausedHp && Time.realtimeSinceStartup < deadline)
            {
                yield return null;
            }

            Assert.That(health.HpValue, Is.GreaterThan(pausedHp));
        }

        private static IEnumerator ObserveScaledDuration(float seconds)
        {
            Assert.That(Application.isPlaying, Is.True, "Native invocation checks require admitted Play Mode.");
            double targetScaledTime = Time.timeAsDouble + seconds;
            double deadline = Time.realtimeSinceStartupAsDouble + seconds + 3d;
            while (Time.timeAsDouble < targetScaledTime && Time.realtimeSinceStartupAsDouble < deadline)
            {
                Assert.That(Time.timeScale, Is.EqualTo(1f), "The captured/restored interval control must remain unchanged.");
                yield return null;
            }

            Assert.That(Time.timeAsDouble, Is.GreaterThanOrEqualTo(targetScaledTime),
                "The required scaled interval must elapse before interpreting callback state.");
        }
    }
}
#pragma warning restore CS0618
