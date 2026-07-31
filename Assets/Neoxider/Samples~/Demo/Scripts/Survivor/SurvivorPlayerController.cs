using System.Collections.Generic;
using Neo.Abilities;
using UnityEngine;

namespace Neo.Samples.Survivor
{
    /// <summary>
    ///     The player avatar: WASD movement scaled by the unit's move_speed property (so Swift upgrades
    ///     and slows apply automatically) and an auto-caster that fires every granted ability the moment
    ///     it comes off cooldown — unit-targeted abilities lock onto the nearest enemy, the rest self-cast.
    /// </summary>
    [RequireComponent(typeof(AbilityUnitBehaviour))]
    public sealed class SurvivorPlayerController : MonoBehaviour
    {
        private readonly List<AbilitySlot> _slots = new List<AbilitySlot>(8);

        private AbilityUnitBehaviour _unit;
        private SurvivorGame _game;

        public AbilityUnit Unit => _unit != null ? _unit.Unit : null;
        public UnitId UnitId => Unit?.Id ?? UnitId.None;
        public bool IsAlive => _unit != null && _unit.IsAlive;

        private void Awake()
        {
            _unit = GetComponent<AbilityUnitBehaviour>();
        }

        public void Initialize(SurvivorGame game)
        {
            _game = game;
            AbilityUnit unit = Unit;
            if (unit == null)
            {
                return;
            }

            for (int i = 0; i < game.Config.StartingAbilities.Count; i++)
            {
                Grant(game.Config.StartingAbilities[i]);
            }
        }

        /// <summary>Grants an auto-cast ability by id (called by upgrades). Safe to call repeatedly.</summary>
        public void Grant(string abilityId)
        {
            AbilityUnit unit = Unit;
            if (unit != null && !string.IsNullOrEmpty(abilityId))
            {
                unit.System.GrantAbility(unit.Id, abilityId);
            }
        }

        private void Update()
        {
            AbilityUnit unit = Unit;
            if (_game == null || unit == null || !unit.IsAlive || _game.IsPaused)
            {
                return;
            }

            Move(unit);
            AutoCast(unit);
        }

        private void Move(AbilityUnit unit)
        {
            var input = new Vector3(Input.GetAxisRaw("Horizontal"), Input.GetAxisRaw("Vertical"), 0f);
            if (input.sqrMagnitude > 1f)
            {
                input.Normalize();
            }

            float speed = unit.GetProperty(AbilityProperties.MoveSpeed, 5f);
            Vector3 pos = transform.position + input * speed * Time.deltaTime;

            float e = _game.Config.ArenaExtent;
            pos.x = Mathf.Clamp(pos.x, -e, e);
            pos.y = Mathf.Clamp(pos.y, -e, e);
            pos.z = 0f;
            transform.position = pos;
        }

        private void AutoCast(AbilityUnit unit)
        {
            unit.System.GetSlots(unit.Id, _slots);
            for (int i = 0; i < _slots.Count; i++)
            {
                AbilitySlot slot = _slots[i];
                if (!slot.IsReady)
                {
                    continue;
                }

                AbilityBlueprint bp = slot.Blueprint;
                if (bp.Targeting == TargetingMode.Unit)
                {
                    float range = bp.Range > 0f ? bp.Range : 999f;
                    SurvivorEnemy target = _game.FindNearestEnemy(transform.position, range);
                    if (target != null)
                    {
                        unit.System.Cast(CastRequest.AtUnit(unit.Id, bp.Id, target.UnitBehaviour.UnitId));
                    }
                }
                else
                {
                    unit.System.Cast(CastRequest.NoTarget(unit.Id, bp.Id));
                }
            }
        }
    }
}
