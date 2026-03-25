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
            Ready, // Ready to attack
            Attacking, // Wind-up / before hit
            Cooldown // Post-hit cooldown
        }

        [Header("Attack")] [SerializeField] private float _attackSpeed = 2;

        public float multiplayAttackSpeed = 1;
        public float delayTimeAttack = 0.2f; // Delay before hit
        [SerializeField] private bool _isAutoAttack;

        public UnityEvent OnStartAttack; // Wind-up start

        public UnityEvent OnAttack; // Hit moment
        public UnityEvent OnEndAttack; // Attack ready again
        private Coroutine _attackCoroutine;

        private bool _canAttackGlobal = true; // Master switch for attacking

        // --- Public properties ---
        public AttackState CurrentState { get; private set; } = AttackState.Ready;
        public float AttackCooldown { get; private set; } // Derived cooldown duration

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

        // --- Unity lifecycle ---
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
            // Stop attack when disabled
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

        // --- Public API ---

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

        // --- Private ---

        private IEnumerator AttackSequence()
        {
            // 1. Wind-up
            CurrentState = AttackState.Attacking;
            OnStartAttack?.Invoke();

            // 2. Delay before hit
            if (delayTimeAttack > 0)
            {
                yield return new WaitForSeconds(delayTimeAttack);
            }

            // 3. Hit
            OnAttack?.Invoke();

            // 4. Remaining cooldown
            CurrentState = AttackState.Cooldown;
            float cooldownDuration = AttackCooldown - delayTimeAttack;
            if (cooldownDuration > 0)
            {
                yield return new WaitForSeconds(cooldownDuration);
            }

            // 5. Ready for next attack
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
