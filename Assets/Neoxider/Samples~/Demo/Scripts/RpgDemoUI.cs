using Neo.Core.Resources;
using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Rpg.Demo
{
    [AddComponentMenu("Neoxider/Samples/RPG Demo UI")]
    public sealed class RpgDemoUI : MonoBehaviour
    {
        [SerializeField] private RpgCharacter _player;
        [SerializeField] private RpgCharacter _enemy;
        [SerializeField] private int _attackDamage = 25;
        [SerializeField] private int _healAmount = 30;
        [SerializeField] private int _hpUpgradeAmount = 50;

        public void DamageEnemy() => Damage(_enemy);

        public void DamagePlayer() => Damage(_player);

        public void HealPlayer() => Heal(_player);

        public void HealEnemy() => Heal(_enemy);

        public void RestoreAll()
        {
            _player?.Restore();
            _enemy?.Restore();
        }

        public void UpgradePlayerMaxHp()
        {
            if (_player == null) return;

            _player.AddMaxResource(RpgResourceId.Hp, _hpUpgradeAmount);
            _player.RestoreResource(RpgResourceId.Hp);
        }

        private void Damage(RpgCharacter target)
        {
            if (target != null)
            {
                target.Damage(_attackDamage);
            }
        }

        private void Heal(RpgCharacter target)
        {
            if (target != null)
            {
                target.Heal(_healAmount);
            }
        }
    }
}
