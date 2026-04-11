using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg
{
    /// <summary>
    ///     Handles death of an <see cref="RpgCombatant"/> by performing a configurable action.
    ///     Attach alongside any RpgCombatant — fully NoCode, no additional scripts required.
    /// </summary>
    [NeoDoc("Rpg/RpgDeathHandler.md")]
    [RequireComponent(typeof(RpgCombatant))]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgDeathHandler))]
    public sealed class RpgDeathHandler : MonoBehaviour
    {
        public enum DeathAction
        {
            /// <summary>Deactivate the GameObject (can be pooled/reactivated).</summary>
            Deactivate,
            /// <summary>Destroy the GameObject immediately.</summary>
            Destroy,
            /// <summary>Destroy the GameObject after a delay (for death animations).</summary>
            DestroyDelayed
        }

        [Header("Death Settings")]
        [SerializeField] private DeathAction action = DeathAction.Deactivate;

        [Tooltip("Delay before destruction (only used with DestroyDelayed).")]
        [SerializeField] [Min(0f)] private float destroyDelay = 2f;

        [Header("Debug")]
        [SerializeField] private bool debugLog;

        [Header("Events")]
        [Tooltip("Raised when death handling begins (before action is performed).")]
        [SerializeField] private UnityEvent _onDeathBegin = new();
        
        [Tooltip("Raised after the death action is performed.")]
        [SerializeField] private UnityEvent _onDeathComplete = new();

        /// <summary>Raised when death handling begins.</summary>
        public UnityEvent OnDeathBegin => _onDeathBegin;
        /// <summary>Raised after death action is performed.</summary>
        public UnityEvent OnDeathComplete => _onDeathComplete;

        private RpgCombatant _combatant;

        private void Awake()
        {
            _combatant = GetComponent<RpgCombatant>();
            _combatant.OnDeath.AddListener(HandleDeath);
        }

        private void OnDestroy()
        {
            if (_combatant != null)
                _combatant.OnDeath.RemoveListener(HandleDeath);
        }

        private void HandleDeath()
        {
            if (debugLog) Debug.Log($"[RpgDeathHandler] {name} died — action: {action}");
            _onDeathBegin?.Invoke();

            switch (action)
            {
                case DeathAction.Deactivate:
                    gameObject.SetActive(false);
                    _onDeathComplete?.Invoke();
                    break;
                case DeathAction.Destroy:
                    _onDeathComplete?.Invoke();
                    Destroy(gameObject);
                    break;
                case DeathAction.DestroyDelayed:
                    _onDeathComplete?.Invoke();
                    Destroy(gameObject, destroyDelay);
                    break;
            }
        }
    }
}
