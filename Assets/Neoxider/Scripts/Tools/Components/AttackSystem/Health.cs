#if ODIN_INSPECTOR
using Sirenix.OdinInspector;
#endif
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    /// <summary>
    ///     Component that handles health system with damage, healing and auto-healing capabilities
    /// </summary>
    [AddComponentMenu("Neoxider/Tools/" + nameof(Health))]
    public class Health : MonoBehaviour, IHealable, IDamageable, IRestorable
    {
        [Header("Health Settings")]
        [Tooltip("Maximum health points")]
        public int maxHp = 10;

        [Tooltip("Current health points")]
        [SerializeField] private int hp;

        [Tooltip("If true, health will be restored to maximum on Awake")]
        [SerializeField] private bool restoreOnAwake = true;

        [Header("Auto-Heal Settings")]
        [Tooltip("Amount of health restored per auto-heal")]
        public int healAmount;

        [Tooltip("Delay between auto-heals in seconds")]
        public float healDelay = 1f;

        [Tooltip("If true, can heal even when not alive")]
        [SerializeField] private bool ignoreIsAlive;

        [Header("Damage & Heal Limits")]
        [Tooltip("Maximum damage that can be taken at once (-1 for no limit)")]
        [Min(-1)]
        public int maxDamageAmount = -1;

        [Tooltip("Maximum healing that can be received at once (-1 for no limit)")]
        [Min(-1)]
        public int maxHealAmount = -1;

        [Header("Events")]
        public UnityEvent<int> OnChange;
        public UnityEvent<float> OnChangePercent;
        public UnityEvent<int> OnDamage;
        public UnityEvent<int> OnHeal;
        public UnityEvent OnDeath;
        public UnityEvent<int> OnChangeMaxHp;

        private Timer healTimer;

        public int MaxHp => maxHp;

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

        public bool IsAlive => hp > 0;
        public bool CanHeal => (IsAlive && !ignoreIsAlive) || ignoreIsAlive;
        public bool NeedHeal => hp < maxHp;

        private void Awake()
        {
            InitializeHealTimer();
            SetMaxHp(maxHp);

            if (restoreOnAwake)
            {
                Restore();
            }
            else
            {
                Hp = hp; // Применяем значение из инспектора, вызывая логику сеттера
            }
        }

        private void OnDestroy()
        {
            if (healTimer != null) healTimer.Stop();
        }

        [Button]
        public void TakeDamage(int count)
        {
            var damage = maxDamageAmount == -1 ? count : Mathf.Min(count, maxDamageAmount);
            Hp -= damage;
            OnDamage?.Invoke(damage); // Передаем фактический урон

            if (!IsAlive) Die();
        }

        [Button]
        public void Heal(int count)
        {
            var heal = maxHealAmount == -1 ? count : Mathf.Min(count, maxHealAmount);
            Hp += heal;
            OnHeal?.Invoke(heal); // Передаем фактическое лечение
        }

        [Button]
        public void Restore()
        {
            Hp = maxHp;
        }

        private void InitializeHealTimer()
        {
            healTimer = new Timer(healDelay, 0.1f, true);
            healTimer.OnTimerEnd.AddListener(OnHealTimerEnd);
        }

        private void OnHealTimerEnd()
        {
            if (CanHeal && NeedHeal && healAmount > 0) Heal(healAmount);
        }

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

        [Button]
        public void SetMaxHp(int count, bool restore = false)
        {
            maxHp = count;

            if (restore)
                Restore();

            OnChangeMaxHp?.Invoke(count);
        }

        [Button]
        public void SetHp(int count)
        {
            Hp = count;
        }

        private void Die()
        {
            OnDeath?.Invoke();
        }
    }
}