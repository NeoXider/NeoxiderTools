using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Inspector-only auto-caster (the Survivor demo pattern as a reusable component): fires the
    ///     configured abilities as soon as they are ready, or on a fixed interval. Unit/point/direction
    ///     abilities lock onto the nearest valid unit through the scene hub; self and no-target
    ///     abilities just cast. Disable the component to stop casting.
    /// </summary>
    [NeoDoc("Abilities/AbilityAutoCaster.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Auto Caster")]
    [AddComponentMenu("Neoxider/Abilities/Ability Auto Caster")]
    public sealed class AbilityAutoCaster : MonoBehaviour
    {
        public enum AutoCastMode
        {
            WhenReady,
            Interval
        }

        [Tooltip("Caster driven by this component. Empty = the AbilityCasterBehaviour on this GameObject or its parents.")]
        [SerializeField] private AbilityCasterBehaviour _caster;

        [Tooltip("Ability ids to auto-cast, attempted in list order. Empty = every granted ability (sorted by id).")]
        [SerializeField] private List<string> _abilityIds = new List<string>();

        [Tooltip("WhenReady casts each ability the moment its cooldown ends; Interval runs one cast pass every Interval seconds.")]
        [SerializeField] private AutoCastMode _mode = AutoCastMode.WhenReady;

        [Tooltip("Seconds between cast passes in Interval mode.")]
        [SerializeField] [Min(0.02f)] private float _interval = 0.5f;

        [Tooltip("Seconds before a failed ability is retried. Prevents per-frame failure spam (e.g. while out of mana). 0 = retry immediately.")]
        [SerializeField] [Min(0f)] private float _failedRetryDelay = 0.5f;

        [Tooltip("Target search radius for unit/point/direction abilities whose Range is 0 (unlimited).")]
        [SerializeField] [Min(0f)] private float _targetSearchRange = 20f;

        [Header("Events")]
        [SerializeField] private UnityEvent<string> _onCast = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onCastFailed = new UnityEvent<string>();

        private static readonly Comparison<AbilitySlot> SlotByIdComparison =
            (a, b) => string.CompareOrdinal(a.Blueprint.Id, b.Blueprint.Id);

        private readonly List<AbilitySlot> _slotScratch = new List<AbilitySlot>(8);
        private readonly List<AbilitySlot> _sortedSlotCache = new List<AbilitySlot>(8);
        private readonly List<UnitId> _unitScratch = new List<UnitId>(16);

        private readonly Dictionary<string, float> _nextAttemptAt =
            new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        private float _intervalTimer;
        private AbilityCasterBehaviour _resolvedCaster;
        private bool _casterSearched;
        private uint _cachedSlotsUnit;
        private int _cachedSlotsVersion = -1;

        public UnityEvent<string> OnCast => _onCast;
        public UnityEvent<string> OnCastFailed => _onCastFailed;

        private void OnEnable()
        {
            _intervalTimer = 0f;
            _nextAttemptAt.Clear();
            // WHY: caster resolution (found or not) is cached; re-enable is the re-search point.
            _casterSearched = false;
            _resolvedCaster = null;
            _cachedSlotsVersion = -1;
        }

        private void Update()
        {
            if (_mode == AutoCastMode.Interval)
            {
                _intervalTimer += Time.deltaTime;
                if (_intervalTimer < _interval)
                {
                    return;
                }

                _intervalTimer = 0f;
            }

            CastReadyAbilities();
        }

        /// <summary>
        ///     Runs one cast pass immediately: every configured (or granted) ability that is ready is
        ///     attempted once, in deterministic order. Update calls this on the configured schedule.
        /// </summary>
        [Button]
        public void CastReadyAbilities()
        {
            AbilityCasterBehaviour caster = ResolveCaster();
            AbilityUnit unit = caster != null ? caster.UnitBehaviour.Unit : null;
            if (unit == null || !unit.IsAlive)
            {
                return;
            }

            AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
            if (hub == null || hub.Paused)
            {
                return;
            }

            List<AbilitySlot> slots = CollectAttemptSlots(unit);
            float now = Time.time;
            // WHY: casts validate range from the caster unit, so target search must too — this
            // component may sit on a manager object far from the unit it drives.
            Vector3 origin = caster.UnitBehaviour.transform.position;
            QuerySharedCandidates(hub, slots, origin, now);

            for (int i = 0; i < slots.Count; i++)
            {
                TryAutoCast(caster, unit, hub, slots[i], origin, now);
            }
        }

        private List<AbilitySlot> CollectAttemptSlots(AbilityUnit unit)
        {
            if (_abilityIds.Count > 0)
            {
                _slotScratch.Clear();
                for (int i = 0; i < _abilityIds.Count; i++)
                {
                    // WHY: ungranted ids are skipped silently — an upgrade may grant them later.
                    if (!string.IsNullOrEmpty(_abilityIds[i]) &&
                        unit.System.TryGetSlot(unit.Id, _abilityIds[i], out AbilitySlot slot))
                    {
                        _slotScratch.Add(slot);
                    }
                }

                return _slotScratch;
            }

            // WHY: slot storage order is unspecified; the ordinal sort keeps cast order deterministic.
            // The sorted list is cached — grant/revoke bumps SlotsVersion, so it re-sorts only on change.
            if (_cachedSlotsUnit != unit.Id.Value || _cachedSlotsVersion != unit.System.SlotsVersion)
            {
                unit.System.GetSlots(unit.Id, _sortedSlotCache);
                _sortedSlotCache.Sort(SlotByIdComparison);
                _cachedSlotsUnit = unit.Id.Value;
                _cachedSlotsVersion = unit.System.SlotsVersion;
            }

            return _sortedSlotCache;
        }

        private void QuerySharedCandidates(AbilitySystemBehaviour hub, List<AbilitySlot> slots, Vector3 origin,
            float now)
        {
            float maxRange = 0f;
            for (int i = 0; i < slots.Count; i++)
            {
                AbilityBlueprint blueprint = slots[i].Blueprint;
                if (blueprint.Targeting == TargetingMode.NoTarget || blueprint.Targeting == TargetingMode.Self ||
                    !IsAttemptEligible(slots[i], now))
                {
                    continue;
                }

                float range = blueprint.Range > 0f ? blueprint.Range : _targetSearchRange;
                maxRange = Mathf.Max(maxRange, range);
            }

            _unitScratch.Clear();
            if (maxRange > 0f)
            {
                // WHY: one radius query per pass at the widest needed range; per-ability range and team
                // filters then run over this shared candidate list instead of re-querying the scene.
                hub.QueryUnitsInRadius(origin, maxRange, _unitScratch);
            }
        }

        private bool IsAttemptEligible(AbilitySlot slot, float now)
        {
            if (!slot.IsReady)
            {
                return false;
            }

            return _failedRetryDelay <= 0f ||
                   !_nextAttemptAt.TryGetValue(slot.Blueprint.Id, out float nextAt) || now >= nextAt;
        }

        private void TryAutoCast(AbilityCasterBehaviour caster, AbilityUnit unit, AbilitySystemBehaviour hub,
            AbilitySlot slot, Vector3 origin, float now)
        {
            if (!IsAttemptEligible(slot, now))
            {
                return;
            }

            AbilityBlueprint blueprint = slot.Blueprint;
            string abilityId = blueprint.Id;

            bool success;
            if (blueprint.Targeting == TargetingMode.NoTarget || blueprint.Targeting == TargetingMode.Self)
            {
                success = caster.TryCast(abilityId);
            }
            else
            {
                float range = blueprint.Range > 0f ? blueprint.Range : _targetSearchRange;
                AbilityUnitBehaviour target = FindNearestTarget(unit, hub, blueprint.TeamFilter, range, origin);
                if (target == null)
                {
                    // WHY: no valid unit in range is not a failure — wait silently, like the Survivor auto-caster.
                    return;
                }

                switch (blueprint.Targeting)
                {
                    case TargetingMode.Unit:
                        success = caster.TryCastAtUnit(abilityId, target);
                        break;
                    case TargetingMode.Point:
                        success = caster.TryCastAtPoint(abilityId, target.transform.position);
                        break;
                    default:
                        success = caster.TryCastTowards(abilityId, target.transform.position - origin);
                        break;
                }
            }

            if (success)
            {
                _nextAttemptAt.Remove(abilityId);
                _onCast.Invoke(abilityId);
            }
            else
            {
                if (_failedRetryDelay > 0f)
                {
                    _nextAttemptAt[abilityId] = now + _failedRetryDelay;
                }

                _onCastFailed.Invoke(abilityId);
            }
        }

        private AbilityUnitBehaviour FindNearestTarget(AbilityUnit unit, AbilitySystemBehaviour hub,
            AbilityTeamFilter filter, float range, Vector3 origin)
        {
            float sqrRange = range * range;
            AbilityUnitBehaviour best = null;
            float bestSqr = float.MaxValue;
            uint bestId = 0;
            for (int i = 0; i < _unitScratch.Count; i++)
            {
                UnitId id = _unitScratch[i];
                AbilityUnit candidate = unit.System.GetUnit(id);
                // WHY: liveness re-checks per ability — an earlier cast in the same pass may have
                // killed a shared-query candidate.
                if (candidate == null || !candidate.IsAlive ||
                    !AbilitySystem.MatchesTeamFilter(unit, candidate, filter))
                {
                    continue;
                }

                AbilityUnitBehaviour behaviour = hub.GetBehaviour(id);
                if (behaviour == null)
                {
                    continue;
                }

                float sqr = (behaviour.transform.position - origin).sqrMagnitude;
                // WHY: the shared query used the widest range of the pass; each ability re-applies its own.
                if (sqr > sqrRange)
                {
                    continue;
                }

                // WHY: unit-id tie-break keeps equidistant target choice deterministic.
                if (sqr < bestSqr || (sqr == bestSqr && best != null && id.Value < bestId))
                {
                    best = behaviour;
                    bestSqr = sqr;
                    bestId = id.Value;
                }
            }

            return best;
        }

        private AbilityCasterBehaviour ResolveCaster()
        {
            return AbilityCasterBehaviour.Resolve(this, _caster, ref _resolvedCaster, ref _casterSearched);
        }
    }
}
