using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(AttackExecution))]
    public class AttackExecution : MonoBehaviour
    {
        [SerializeField] private float _attackSpeed = 2;

        public float AttackSpeed
        {
            get => _attackSpeed;
            set
            {
                _attackSpeed = value;
                UpdateAttackTime(false);
            }
        }

        public Vector2 minMaxSpeedAttack = new(0, 10);
        public float multiplayAttackSpeed = 1;

        public float attackСooldown;

        [Header("Delay before the actual attack")]
        public float delayTimeAttack = 0.2f;

        [SerializeField] private bool _isAutoAttack = false;

        [SerializeField]
        public bool isAutoAttack
        {
            get => _isAutoAttack;
            set => _isAutoAttack = value;
        }

        [Space] [SerializeField] private bool _canAttackTime = true;
        [SerializeField] private bool _canAttack = true;
        public bool canAttackTime => canAttackTime;
        public bool canAttack => _canAttack && _canAttackTime;

        [Space] public UnityEvent OnStartAttack;
        public UnityEvent OnAttack;
        public UnityEvent OnEndAttack;

        private float time = -100;

        private void Start()
        {
            UpdateAttackTime();
        }

        private void Update()
        {
            if (!_canAttackTime)
            {
                var canAttackTime = Time.time > time;

                if (canAttackTime)
                {
                    SetCanAttackTimer(canAttackTime);
                    OnEndAttack?.Invoke();
                }
            }
            else
            {
                if (_isAutoAttack) Attack();
            }
        }

        public void Reset()
        {
            CancelInvoke();
            UpdateAttackTime();
            SetCanAttack(true, true);
        }

        public void SetCanAttack(bool active)
        {
            _canAttack = active;
        }

        public void SetCanAttack(bool canAttack, bool canAttackTime)
        {
            _canAttack = canAttack;
            _canAttackTime = canAttackTime;
        }

        public void SetCanAttackTimer(bool active)
        {
            _canAttackTime = active;
        }

        public void UpdateAttackTime(bool updateTime = false)
        {
            attackСooldown = GetTimeAttack(_attackSpeed);
            SetCanAttackTimer(updateTime);
        }

        public bool Attack()
        {
            if (_canAttackTime && enabled && _canAttack)
            {
                SetCanAttackTimer(false);
                time = Time.time + attackСooldown;
                Invoke(nameof(AttackComplete), delayTimeAttack);
                OnStartAttack?.Invoke();
                return true;
            }

            return false;
        }

        public void AttackInvoke()
        {
            Attack();
        }

        public void AttackComplete()
        {
            OnAttack?.Invoke();
        }

        private void OnValidate()
        {
            UpdateAttackTime();
        }

        public float GetTimeAttack(float attackSpeed)
        {
            return 1 / (attackSpeed * (1 / multiplayAttackSpeed));
        }
    }
}