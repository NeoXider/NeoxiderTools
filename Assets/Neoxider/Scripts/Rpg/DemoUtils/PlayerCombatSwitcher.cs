using UnityEngine;
using UnityEngine.Events;

namespace Neo.Rpg.DemoUtils
{
    /// <summary>
    /// Simple utility to swap between attacks in a demo scene.
    /// </summary>
    [AddComponentMenu("Neoxider/RPG/Demo/PlayerCombatSwitcher")]
    public sealed class PlayerCombatSwitcher : MonoBehaviour
    {
        public RpgAttackController AttackController;
        public RpgAttackPreset[] AttackPresets;
        public KeyCode SwitchKey = KeyCode.Q;

        public UnityEvent<string> OnSwitched = new();

        private int _currentIndex = 0;

        private void Update()
        {
            if (Input.GetKeyDown(SwitchKey))
            {
                CycleAttack();
            }

            if (AttackController != null && AttackPresets.Length > 0)
            {
                if (Input.GetMouseButtonDown(0))
                {
                    AttackController.TryUsePreset(AttackPresets[_currentIndex], out _);
                }
            }
        }

        public void CycleAttack()
        {
            if (AttackPresets == null || AttackPresets.Length == 0) return;

            _currentIndex = (_currentIndex + 1) % AttackPresets.Length;
            OnSwitched?.Invoke($"Equipped: {AttackPresets[_currentIndex].name}");
        }
    }
}
