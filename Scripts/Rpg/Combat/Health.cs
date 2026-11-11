using System;
using Neo.Reactive;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Component that handles health system with damage, healing and auto-healing capabilities.
    ///     Implements IDamageable, IHealable, IRestorable for use with AttackExecution and other combat components.
    /// </summary>
    [LegacyComponent("Neo.Rpg.Components.RpgCharacter")]
    [Obsolete(
        "Use Neo.Rpg.Components.RpgCharacter (universal resource/stat/buff system, multiplayer-ready).")]
    [NeoDoc("Rpg/Combat/README.md")]
    [CreateFromMenu("Neoxider/Tools/Components/Health")]
    [AddComponentMenu("Neoxider/Tools/Components/AttackSystem/Health (Legacy)")]
    public class Health : MonoBehaviour, IHealable, IDamageable, IRestorable
    {
        [Header("Health Settings")] [Tooltip("Maximum health points")]
        public int maxHp = 10;

        [Tooltip("Current health points")] [SerializeField]
        private int hp;

        [Tooltip("If true, health will be restored to maximum on Awake")] [SerializeField]
        private bool restoreOnAwake = true;

        [Header("Auto-Heal Settings")] [Tooltip("Amount of health restored per auto-heal")]
        public int healAmount;

        [Tooltip("Delay between auto-heals in seconds")]
        public float healDelay = 1f;

        [Tooltip("If true, can heal even when not alive")] [SerializeField]
        private bool ignoreIsAlive;

        [Header("Damage & Heal Limits")]
        [Tooltip("Maximum damage that can be taken at once (-1 for no limit)")]
        [Min(-1)]
        public int maxDamageAmount = -1;

        [Tooltip("Maximum healing that can be received at once (-1 for no limit)")] [Min(-1)]
        public int maxHealAmount = -1;

        [Tooltip("Reactive current HP; subscribe via Hp.OnChanged")]
        public ReactivePropertyInt Hp = new();

        [Tooltip("Reactive HP percent (0-1); subscribe via HpPercent.OnChanged")]
        public ReactivePropertyFloat HpPercent = new();

        public UnityEvent<int> OnDamage;
        public UnityEvent<int> OnHeal;
        public UnityEvent OnDeath;
        public UnityEvent<int> OnChangeMaxHp;

        private Timer healTimer;

        public int MaxHp => maxHp;

        /// <summary>Current HP (for NeoCondition and reflection).</summary>
        public int HpValue => Hp.CurrentValue;

        /// <summary>HP fraction 0–1 (for NeoCondition and reflection).</summary>
        public float HpPercentValue => HpPercent.CurrentValue;

        public bool IsAlive => HpValue > 0;
        public bool CanHeal => (IsAlive && !ignoreIsAlive) || ignoreIsAlive;
        public bool NeedHeal => HpValue < maxHp;

        private void Awake()
        {
            int initialHp = hp;
            InitializeHealTimer();
            SetMaxHp(maxHp);
            SetHpValue(restoreOnAwake ? maxHp : initialHp);
        }

        private void OnEnable()
        {
            UpdateHealTimerState();
        }

        private void OnDisable()
        {
            healTimer?.Stop();
        }

        private void OnDestroy()
        {
            healTimer?.Dispose();
            healTimer = null;
        }

        [Button]
        public void TakeDamage(int count)
        {
            if (count <= 0 || !IsAlive)
            {
                return;
            }

            int damage = maxDamageAmount == -1 ? count : Mathf.Min(count, Mathf.Max(0, maxDamageAmount));
            bool died = damage >= HpValue;
            SetHpValue((long)HpValue - damage);
            OnDamage?.Invoke(damage);

            if (died)
            {
                Die();
            }
        }

        [Button]
        public void Heal(int count)
        {
            if (count <= 0)
            {
                return;
            }

            int heal = maxHealAmount == -1 ? count : Mathf.Min(count, Mathf.Max(0, maxHealAmount));
            SetHpValue((long)HpValue + heal);
            OnHeal?.Invoke(heal);
        }

        [Button]
        public void Restore()
        {
            SetHpValue(maxHp);
        }

        private void SetHpValue(long value)
        {
            hp = (int)Math.Max(0L, Math.Min(value, Mathf.Max(0, maxHp)));
            Hp.Value = hp;
            HpPercent.Value = maxHp > 0 ? Mathf.Clamp01((float)HpValue / maxHp) : 0f;
        }

        private void InitializeHealTimer()
        {
            healTimer = new Timer(Mathf.Max(0.01f, healDelay), 0.1f, true);
            healTimer.OnTimerEnd.AddListener(OnHealTimerEnd);
        }

        private void UpdateHealTimerState()
        {
            if (healTimer == null)
            {
                return;
            }

            if (Application.isPlaying && isActiveAndEnabled && healAmount > 0)
            {
                healTimer.Start();
            }
            else
            {
                healTimer.Stop();
            }
        }

        private void OnHealTimerEnd()
        {
            if (isActiveAndEnabled && CanHeal && NeedHeal && healAmount > 0)
            {
                Heal(healAmount);
            }
        }

        [Button]
        public void SetHeal(int amount, float delay = -1)
        {
            healAmount = amount;

            if (delay != -1 && delay > 0)
            {
                healDelay = delay;
                if (healTimer != null)
                {
                    healTimer.Duration = delay;
                }
            }

            UpdateHealTimerState();
        }

        [Button]
        public void SetMaxHp(int count, bool restore = false)
        {
            maxHp = Mathf.Max(0, count);

            if (restore)
            {
                Restore();
            }
            else
            {
                SetHpValue(HpValue);
            }

            OnChangeMaxHp?.Invoke(maxHp);
        }

        [Button]
        public void SetHp(int count)
        {
            SetHpValue(count);
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }
    }
}
