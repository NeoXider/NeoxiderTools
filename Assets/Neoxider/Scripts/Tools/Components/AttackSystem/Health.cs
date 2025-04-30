using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    /// Component that handles health system with damage, healing and auto-healing capabilities
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/" + nameof(Hp))]
    public class Health : MonoBehaviour, IHealable, IDamageable, IRestorable
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health points")]
        [SerializeField] private int maxHp = 10;

        [Tooltip("Current health points")]
        [SerializeField] private int hp;

        [Header("Auto-Heal Settings")]
        [Tooltip("Amount of health restored per auto-heal")]
        [SerializeField] private int healAmount = 0;

        [Tooltip("Delay between auto-heals in seconds")]
        [SerializeField] private float healDelay = 1f;

        [Tooltip("If true, can heal even when not alive")]
        [SerializeField] private bool ignoreIsAlive = false;

        [Header("Damage & Heal Limits")]
        [Tooltip("Maximum damage that can be taken at once (-1 for no limit)")]
        [Min(-1)]
        public int maxDamageAmount = -1;

        [Tooltip("Maximum healing that can be received at once (-1 for no limit)")]
        [Min(-1)]
        public int maxHealAmount = -1;

        [Header("Events")]
        [Tooltip("Called when health changes")]
        public UnityEvent<int> OnChange;

        [Tooltip("Called when health changes (0-1)")]
        public UnityEvent<float> OnChangePercent;

        [Tooltip("Called when taking damage")]
        public UnityEvent<int> OnDamage;

        [Tooltip("Called when receiving healing")]
        public UnityEvent<int> OnHeal;

        [Tooltip("Called when health reaches zero")]
        public UnityEvent OnDeath;

        [Tooltip("Called when maximum health changes")]
        public UnityEvent<int> OnChangeMaxHp;

        private Timer healTimer;

        /// <summary>
        /// Gets the maximum health points
        /// </summary>
        public int MaxHp => maxHp;

        /// <summary>
        /// Gets or sets current health points
        /// </summary>
        public int Hp
        {
            get => hp;
            set
            {
                hp = Mathf.Clamp(value, 0, maxHp);
                OnChange?.Invoke(hp);
                OnChangePercent?.Invoke(Mathf.Clamp01((float)hp / maxHp));
            }
        }

        /// <summary>
        /// Gets whether the entity is alive
        /// </summary>
        public bool IsAlive => hp > 0;

        /// <summary>
        /// Gets whether the entity can be healed
        /// </summary>
        public bool CanHeal => (IsAlive && !ignoreIsAlive) || ignoreIsAlive;

        /// <summary>
        /// Gets whether the entity needs healing
        /// </summary>
        public bool NeedHeal => hp < maxHp;

        private void Awake()
        {
            InitializeHealTimer();
            SetMaxHp(maxHp);
            Restore();
        }

        private void InitializeHealTimer()
        {
            healTimer = new Timer(healDelay, 0.1f, true);
            healTimer.OnTimerEnd.AddListener(OnHealTimerEnd);
        }

        private void OnDestroy()
        {
            if (healTimer != null)
            {
                healTimer.Stop();
            }
        }

        private void OnHealTimerEnd()
        {
            if (CanHeal && NeedHeal && healAmount > 0)
            {
                Heal(healAmount);
            }
        }

        /// <summary>
        /// Sets auto-heal parameters
        /// </summary>
        /// <param name="amount">Amount of health restored per auto-heal</param>
        /// <param name="delay">Delay between auto-heals in seconds (-1 to keep current)</param>
        [Button]
        public void SetHeal(int amount, float delay = -1)
        {
            healAmount = amount;

            if (delay != -1 && delay > 0)
            {
                healDelay = delay;
                healTimer.Duration = delay;
            }
        }

        /// <summary>
        /// Sets maximum health points
        /// </summary>
        /// <param name="count">New maximum health</param>
        /// <param name="restore">If true, restores health to maximum</param>
        [Button]
        public void SetMaxHp(int count, bool restore = false)
        {
            maxHp = count;

            if (restore)
                Restore();

            OnChangeMaxHp?.Invoke(count);
        }

        /// <summary>
        /// Sets current health points
        /// </summary>
        /// <param name="count">New health value</param>
        [Button]
        public void SetHp(int count)
        {
            Hp = count;
        }

        /// <summary>
        /// Restores health to maximum
        /// </summary>
        [Button]
        public void Restore()
        {
            Hp = maxHp;
        }

        /// <summary>
        /// Applies damage to the entity
        /// </summary>
        /// <param name="count">Amount of damage to take</param>
        [Button]
        public void TakeDamage(int count)
        {
            int damage = maxDamageAmount == -1 ? count : Mathf.Min(count, maxDamageAmount);
            Hp -= damage;
            OnDamage?.Invoke(count);

            if (!IsAlive) Die();
        }

        /// <summary>
        /// Heals the entity
        /// </summary>
        /// <param name="count">Amount of health to restore</param>
        [Button]
        public void Heal(int count)
        {
            int heal = maxHealAmount == -1 ? count : Mathf.Min(count, maxHealAmount);
            Hp += heal;

            OnHeal?.Invoke(count);
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }
    }
}