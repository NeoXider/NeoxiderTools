using UnityEngine;
using UnityEngine.Events;

namespace Neo
{
    namespace Tools
    {
        [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Hp))]
        public class Hp : MonoBehaviour, IHealable, IDamageable, IRestorable
        {
            [SerializeField] private int _maxHp = 10;
            [SerializeField] private int _hp;
            [SerializeField] private int _healAmount = 0;
            [SerializeField] private bool _ignoreIsAlive = false;
            [SerializeField] private float _healDelay = 1;

            [Space]
            [Header("Other Settings")]
            [Min(-1)]
            public int maxDamageAmount = -1;
            [Min(-1)]
            public int maxHealAmount = -1;

            public int maxHp => _maxHp;
            public int hp
            {
                get => _hp;
                set
                {
                    _hp = Mathf.Clamp(value, 0, _maxHp);
                    OnChange?.Invoke(hp);
                    OnChangePercent?.Invoke(Mathf.Clamp01((float)_hp / _maxHp));
                }
            }

            public bool IsAlive => hp > 0;
            public bool CanHeal => (IsAlive && !_ignoreIsAlive) || _ignoreIsAlive;
            public bool NeedHeal => hp < maxHp;

            public UnityEvent<int> OnChange;
            public UnityEvent<float> OnChangePercent;
            public UnityEvent<int> OnDamage;
            public UnityEvent<int> OnHeal;
            public UnityEvent OnDeath;

            public UnityEvent<int> OnChangeMaxHp;

            private float _timerHeal = 0;

            private void Awake()
            {
                SetMaxHp(_maxHp);
                Restore();
            }

            private void Update()
            {
                HealUpdate();
            }

            private void HealUpdate()
            {
                if (CanHeal)
                {
                    if (_healAmount <= 0 || !NeedHeal)
                    {
                        _timerHeal = _healDelay;
                    }
                    else if (_timerHeal <= 0)
                    {
                        _timerHeal = _healDelay;
                        Heal(_healAmount);
                    }
                    else
                    {
                        _timerHeal -= Time.deltaTime;
                    }
                }
            }

            public void SetHeal(int amount, float healDelay = -1)
            {
                _healAmount = amount;

                if (healDelay != -1 && healDelay > 0)
                {
                    _healDelay = healDelay;
                }
            }

            public void SetMaxHp(int count, bool restore = false)
            {
                _maxHp = count;

                if (restore)
                    Restore();

                OnChangeMaxHp?.Invoke(count);
            }

            public void SetHp(int count)
            {
                hp = count;
            }

            public void Restore()
            {
                hp = _maxHp;
            }

            public void TakeDamage(int count)
            {
                int damage = maxDamageAmount == -1 ? count : Mathf.Min(count, maxDamageAmount);
                hp -= damage;
                OnDamage?.Invoke(count);

                if (!IsAlive) Die();
            }

            public void Heal(int count)
            {
                int heal = maxHealAmount == -1 ? count : Mathf.Min(count, maxHealAmount);
                hp += heal;

                OnHeal?.Invoke(count);
            }

            private void Die()
            {
                OnDeath.Invoke();
            }
        }
    }
}