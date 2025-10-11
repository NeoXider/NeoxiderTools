using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(AttackExecution))]
    public class AttackExecution : MonoBehaviour
    {
        public enum AttackState
        {
            Ready,      // Готов к атаке
            Attacking,  // В процессе атаки (до удара)
            Cooldown    // Перезарядка после удара
        }

        [Header("Настройки атаки")]
        [SerializeField] private float _attackSpeed = 2;
        public float multiplayAttackSpeed = 1;
        public float delayTimeAttack = 0.2f; // Задержка перед ударом
        [SerializeField] private bool _isAutoAttack;

        [Header("События")]
        public UnityEvent OnStartAttack; // Вызывается в начале атаки (замах)
        public UnityEvent OnAttack;      // Вызывается в момент удара
        public UnityEvent OnEndAttack;   // Вызывается, когда атака снова готова

        private bool _canAttackGlobal = true; // Глобальный флаг, разрешающий атаковать
        private Coroutine _attackCoroutine;

        // --- Публичные свойства ---
        public AttackState CurrentState { get; private set; } = AttackState.Ready;
        public float AttackCooldown { get; private set; } // Рассчитанное время перезарядки

        public float AttackSpeed
        {
            get => _attackSpeed;
            set
            {
                _attackSpeed = value;
                UpdateAttackCooldown();
            }
        }

        public bool IsAutoAttack
        {
            get => _isAutoAttack;
            set => _isAutoAttack = value;
        }

        public bool CanAttack => CurrentState == AttackState.Ready && _canAttackGlobal;

        // --- Жизненный цикл Unity ---
        private void Start()
        {
            UpdateAttackCooldown();
        }

        private void Update()
        {
            if (_isAutoAttack && CanAttack)
            {
                Attack();
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying) return;
            UpdateAttackCooldown();
        }

        private void OnDisable()
        {
            // Прерываем атаку, если объект отключается
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                CurrentState = AttackState.Ready;
            }
        }

        // --- Публичные методы ---

        /// <summary>
        /// Пытается начать атаку, если это возможно.
        /// </summary>
        /// <returns>True, если атака началась успешно.</returns>
        public bool Attack()
        {
            if (!CanAttack) return false;

            _attackCoroutine = StartCoroutine(AttackSequence());
            return true;
        }

        /// <summary>
        /// Разрешает или запрещает атаковать.
        /// </summary>
        public void SetCanAttack(bool canAttack)
        {
            _canAttackGlobal = canAttack;
        }

        /// <summary>
        /// Сбрасывает состояние атаки, прерывая текущий цикл и делая ее снова доступной.
        /// </summary>
        public void ResetAttack()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
            }
            CurrentState = AttackState.Ready;
            OnEndAttack?.Invoke();
        }

        // --- Приватные методы ---

        private IEnumerator AttackSequence()
        {
            // 1. Начало атаки (замах)
            CurrentState = AttackState.Attacking;
            OnStartAttack?.Invoke();

            // 2. Задержка перед ударом
            if (delayTimeAttack > 0)
            {
                yield return new WaitForSeconds(delayTimeAttack);
            }

            // 3. Момент удара
            OnAttack?.Invoke();

            // 4. Оставшееся время перезарядки
            CurrentState = AttackState.Cooldown;
            float cooldownDuration = AttackCooldown - delayTimeAttack;
            if (cooldownDuration > 0)
            {
                yield return new WaitForSeconds(cooldownDuration);
            }

            // 5. Готовность к следующей атаке
            CurrentState = AttackState.Ready;
            OnEndAttack?.Invoke();
            _attackCoroutine = null;
        }

        private void UpdateAttackCooldown()
        {
            if (_attackSpeed <= 0) _attackSpeed = 0.01f;
            if (multiplayAttackSpeed <= 0) multiplayAttackSpeed = 0.01f;
            AttackCooldown = 1 / (_attackSpeed * multiplayAttackSpeed);
        }
    }
}