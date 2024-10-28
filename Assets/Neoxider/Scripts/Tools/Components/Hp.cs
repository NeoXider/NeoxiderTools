using UnityEngine;
using UnityEngine.Events;

namespace Neoxider
{
    public class Hp : MonoBehaviour
    {
        [SerializeField] private int _maxHp = 10;
        private int _hp;

        public int maxHp => _maxHp;
        public int hp
        {
            get => _hp;
            set
            {
                _hp = Mathf.Clamp(value, 0, _maxHp);
                OnChange?.Invoke(hp);
            }
        }

        public UnityEvent<int> OnChange;
        public UnityEvent OnDeath;

        public UnityEvent<int> OnChangeMaxHp;

        private void Start()
        {
            Restore();
        }

        public void SetMaxHp(int count, bool restore = false)
        {
            _maxHp = count;

            if (restore)
                Restore();

            OnChangeMaxHp?.Invoke(count);
        }

        public void Restore()
        {
            hp = _maxHp;
        }

        public void TakeDamage(int damage)
        {
            hp -= damage;
            if (!IsAlive()) Die();
        }

        public void Heal(int amount)
        {
            hp += amount;
        }

        public void Die()
        {
            OnDeath.Invoke();
        }

        public bool IsAlive()
        {
            return _hp > 0;
        }
    }
}
