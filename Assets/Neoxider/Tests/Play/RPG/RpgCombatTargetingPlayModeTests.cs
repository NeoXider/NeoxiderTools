using System.Collections;
using System.Reflection;
using Neo.Rpg;
using Neo.Rpg.Components;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Neo.Tests.Play.RPG
{
    public class RpgCombatTargetingPlayModeTests
    {
        private static T GetPrivateField<T>(object target, string fieldName) where T : class
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            return field.GetValue(target) as T;
        }

        private static void SetPrivateField<T>(object target, string fieldName, T value)
        {
            FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.NotNull(field);
            field.SetValue(target, value);
        }

        [UnityTest]
        public IEnumerator RpgAutoAttackController_FindsTaggedTargetAfterInterval()
        {
            var attacker = new GameObject("RpgAutoAttackTargetFinder");
            attacker.AddComponent<RpgAttackController>();
            RpgAutoAttackController autoAttack = attacker.AddComponent<RpgAutoAttackController>();
            SetPrivateField(autoAttack, "targetTag", "Player");
            SetPrivateField(autoAttack, "targetFindInterval", 0.05f);
            SetPrivateField(autoAttack, "attackInterval", 1000f);

            yield return null;

            var player = new GameObject("PlayerTarget");
            player.tag = "Player";
            player.transform.position = Vector3.zero;
            attacker.transform.position = Vector3.zero;

            yield return new WaitForSeconds(0.03f);
            yield return null;
            Assert.IsNull(GetPrivateField<Transform>(autoAttack, "_target"));

            yield return new WaitForSeconds(0.08f);
            yield return null;
            Assert.AreEqual(player.transform, GetPrivateField<Transform>(autoAttack, "_target"));

            Object.Destroy(attacker);
            Object.Destroy(player);
            yield return null;
        }

        [UnityTest]
        public IEnumerator RpgContactDamage_TargetReceiver_DealsDamageOverCooldown()
        {
            var targetObject = new GameObject("DamageTarget");
            TestCombatReceiver targetReceiver = targetObject.AddComponent<TestCombatReceiver>();

            var sourceObject = new GameObject("DamageSource");
            RpgContactDamage contact = sourceObject.AddComponent<RpgContactDamage>();
            SetPrivateField(contact, "cooldown", 0.05f);

            int attackCount = 0;
            float lastDamage = -1f;
            contact.OnAttack.AddListener(() => attackCount++);
            contact.OnDamageDealt.AddListener(v => lastDamage = v);

            contact.SetTargetReceiver(targetReceiver);

            yield return null;
            yield return null;

            Assert.AreEqual(1, attackCount);
            Assert.AreEqual(targetReceiver.StartingDamage, targetReceiver.CurrentHp, 0.001f);

            yield return new WaitForSeconds(0.06f);
            yield return null;

            Assert.AreEqual(2, attackCount);
            Assert.AreEqual(contact.Damage, lastDamage);

            Object.Destroy(targetObject);
            Object.Destroy(sourceObject);
            yield return null;
        }

        private sealed class TestCombatReceiver : MonoBehaviour, IRpgCombatReceiver
        {
            private float _maxHp = 10f;
            private float _currentHp = 10f;

            public float StartingDamage => _currentHp;
            public float CurrentHp => _currentHp;
            public float MaxHp => _maxHp;
            public int Level => 1;
            public bool IsDead => _currentHp <= 0f;
            public bool IsInvulnerable => false;
            public bool CanPerformActions => !_isBusy;

            private bool _isBusy;

            public float TakeDamage(RpgDamageInfo info)
            {
                _currentHp = Mathf.Max(0f, _currentHp - Mathf.Max(0f, info.Amount));
                return info.Amount;
            }

            public float Heal(float amount)
            {
                _currentHp = Mathf.Min(_maxHp, _currentHp + amount);
                return _currentHp;
            }

            public bool TrySpendResource(string resourceId, float amount, out string failReason)
            {
                failReason = string.Empty;
                return true;
            }

            public bool TryApplyBuff(string buffId, out string failReason)
            {
                failReason = string.Empty;
                return true;
            }

            public bool TryApplyStatus(string statusId, out string failReason)
            {
                failReason = string.Empty;
                return true;
            }

            public void AddInvulnerabilityLock()
            {
            }

            public void RemoveInvulnerabilityLock()
            {
            }

            public float GetOutgoingDamageMultiplier()
            {
                return 1f;
            }

            public float GetMovementSpeedMultiplier()
            {
                return 1f;
            }
        }
    }
}
