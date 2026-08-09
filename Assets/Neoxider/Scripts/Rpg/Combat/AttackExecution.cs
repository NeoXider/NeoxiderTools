using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Tools
{
    [LegacyComponent("Neo.Rpg.RpgAttackController")]
    [Obsolete("Use Neo.Rpg.RpgAttackController with RpgAttackDefinition for melee, ranged, and area attacks.")]
    [NeoDoc("Tools/Components/AttackSystem/AttackExecution.md")]
    [CreateFromMenu("Neoxider/Tools/Components/AttackExecution")]
    [AddComponentMenu("Neoxider/Tools/Components/AttackExecution (Legacy)")]
    public class AttackExecution : MonoBehaviour
    {
        public enum AttackState
        {
            Ready,
            Attacking, // WHY: Wind-up / before hit
            Cooldown // WHY: Post-hit cooldown
        }

        [Header("Attack")] [SerializeField] private float _attackSpeed = 2;

        public float multiplayAttackSpeed = 1;
        public float delayTimeAttack = 0.2f; // WHY: Delay before hit
        [SerializeField] private bool _isAutoAttack;

        public UnityEvent OnStartAttack;

        public UnityEvent OnAttack; // WHY: Hit moment
        public UnityEvent OnEndAttack; // WHY: Attack ready again
        private Coroutine _attackCoroutine;

        private bool _canAttackGlobal = true;

        public AttackState CurrentState { get; private set; } = AttackState.Ready;
        public float AttackCooldown { get; private set; }

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

        private void OnDisable()
        {
            if (_attackCoroutine != null)
            {
                StopCoroutine(_attackCoroutine);
                CurrentState = AttackState.Ready;
            }
        }

        private void OnValidate()
        {
            if (Application.isPlaying)
            {
                return;
            }

            UpdateAttackCooldown();
        }

        /// <summary>
        ///     Tries to start an attack when allowed.
        /// </summary>
        /// <returns>True if attack started.</returns>
        public bool Attack()
        {
            if (!CanAttack)
            {
                return false;
            }

            _attackCoroutine = StartCoroutine(AttackSequence());
            return true;
        }

        /// <summary>
        ///     Enables or disables attacking.
        /// </summary>
        public void SetCanAttack(bool canAttack)
        {
            _canAttackGlobal = canAttack;
        }

        /// <summary>
        ///     Resets attack state, cancels the current cycle, and returns to ready.
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

        private IEnumerator AttackSequence()
        {
            CurrentState = AttackState.Attacking;
            OnStartAttack?.Invoke();

            if (delayTimeAttack > 0)
            {
                yield return new WaitForSeconds(delayTimeAttack);
            }

            OnAttack?.Invoke();

            CurrentState = AttackState.Cooldown;
            float cooldownDuration = AttackCooldown - delayTimeAttack;
            if (cooldownDuration > 0)
            {
                yield return new WaitForSeconds(cooldownDuration);
            }

            CurrentState = AttackState.Ready;
            OnEndAttack?.Invoke();
            _attackCoroutine = null;
        }

        private void UpdateAttackCooldown()
        {
            if (_attackSpeed <= 0)
            {
                _attackSpeed = 0.01f;
            }

            if (multiplayAttackSpeed <= 0)
            {
                multiplayAttackSpeed = 0.01f;
            }

            AttackCooldown = 1 / (_attackSpeed * multiplayAttackSpeed);
        }
    }
}
