using System;
using System.Collections.Generic;
using UnityEngine;
using Neo.Rpg;
using Neo.Rpg.Components;
using Neo.Rpg.Runtime;

namespace Neo.Tests.Edit.RPG
{
    public sealed class Audit20261023FixtureRpgCombatEdgeTestCombatReceiver : MonoBehaviour, IRpgCombatReceiver
    {
        private float _currentHp = 10f;

        public int SpendCalls { get; private set; }
        public int DamageCalls { get; private set; }
        public float CurrentHp => _currentHp;
        public float MaxHp => 10f;
        public int Level => 1;
        public bool IsDead => _currentHp <= 0f;
        public bool IsInvulnerable => false;
        public bool CanPerformActions => true;

        public float TakeDamage(RpgDamageInfo info)
        {
            DamageCalls++;
            float amount = Mathf.Max(0f, info.Amount);
            _currentHp = Mathf.Max(0f, _currentHp - amount);
            return amount;
        }

        public float Heal(float amount)
        {
            float healed = Mathf.Max(0f, amount);
            _currentHp = Mathf.Min(MaxHp, _currentHp + healed);
            return healed;
        }

        public bool TrySpendResource(string resourceId, float amount, out string failReason)
        {
            SpendCalls++;
            failReason = null;
            return true;
        }

        public bool TryApplyBuff(string buffId, out string failReason)
        {
            failReason = null;
            return true;
        }

        public bool TryApplyStatus(string statusId, out string failReason)
        {
            failReason = null;
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
