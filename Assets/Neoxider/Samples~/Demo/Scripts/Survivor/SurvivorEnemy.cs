using Neo.Abilities;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     A pooled chaser. Reads its move speed from the ability unit (so slows/roots from modifiers
    ///     apply automatically), touches the player for continuous damage, and drops XP on death.
    ///     The domain owns combat; this component is pure presentation + steering.
    /// </summary>
    [RequireComponent(typeof(AbilityUnitBehaviour))]
    public sealed class SurvivorEnemy : MonoBehaviour
    {
        private AbilityUnitBehaviour _unit;
        private SpriteRenderer _body;
        private SurvivorEnemyType _type;
        private SurvivorGame _game;
        private float _contactAccum;
        private bool _dead;

        public AbilityUnitBehaviour UnitBehaviour => _unit;
        public SurvivorEnemyType Type => _type;

        private void Awake()
        {
            _unit = GetComponent<AbilityUnitBehaviour>();
            _body = GetComponentInChildren<SpriteRenderer>();
        }

        /// <summary>Called by the spawner right after the pool activates the object (all OnEnable have run).</summary>
        public void Spawned(SurvivorEnemyType type, SurvivorGame game, float healthMultiplier)
        {
            _type = type;
            _game = game;
            _dead = false;
            _contactAccum = 0f;

            if (_body != null)
            {
                _body.color = type.Color;
                _body.transform.localScale = Vector3.one * (type.Radius * 2f);
            }

            AbilityUnit unit = _unit.Unit;
            if (unit != null && healthMultiplier > 1f)
            {
                float newMax = unit.MaxHealth * healthMultiplier;
                unit.Resources.SetMax(AbilityResourceIds.Health, newMax);
                unit.Resources.Restore(AbilityResourceIds.Health);
            }
        }

        private void Update()
        {
            AbilityUnit unit = _unit.Unit;
            if (_dead || _game == null || unit == null)
            {
                return;
            }

            if (!unit.IsAlive)
            {
                Die();
                return;
            }

            SurvivorPlayerController player = _game.Player;
            if (player == null || !player.IsAlive)
            {
                return;
            }

            Vector3 toPlayer = player.transform.position - transform.position;
            float distance = toPlayer.magnitude;

            bool immobile = unit.HasState(AbilityStates.Stunned) || unit.HasState(AbilityStates.Rooted) ||
                            unit.HasState(AbilityStates.Frozen);
            if (!immobile && distance > 0.001f)
            {
                float speed = unit.GetProperty(AbilityProperties.MoveSpeed, 2.5f);
                transform.position += toPlayer / distance * speed * Time.deltaTime;
            }

            float touch = _type.Radius + _game.Config.PlayerRadius;
            if (distance <= touch)
            {
                _contactAccum += Time.deltaTime;
                float tick = 0.2f;
                while (_contactAccum >= tick)
                {
                    _contactAccum -= tick;
                    DamageService.ApplyDamage(unit.System, unit.Id, player.UnitId,
                        _type.ContactDps * tick, AbilityDamageTypes.Physical);
                }
            }
            else
            {
                _contactAccum = 0f;
            }
        }

        private void Die()
        {
            if (_dead)
            {
                return;
            }

            _dead = true;
            _game.HandleEnemyDeath(this);
        }
    }
}
