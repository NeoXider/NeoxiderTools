using Neo.Core.Level;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Scene presence of one ability unit: registers into the hub on enable, applies its
    ///     <see cref="UnitTemplate" />, and surfaces gameplay receipts as UnityEvents for UI/VFX wiring.
    ///     The domain unit is available via <see cref="Unit" />.
    /// </summary>
    [NeoDoc("Abilities/AbilityUnitBehaviour.md")]
    [AddComponentMenu("Neoxider/Abilities/Ability Unit")]
    public sealed class AbilityUnitBehaviour : MonoBehaviour
    {
        [Tooltip("Archetype applied on registration (pools, properties, abilities).")]
        [SerializeField] private UnitTemplate _template;

        [Tooltip("Team override. -1 = use the template team.")]
        [SerializeField] private int _teamOverride = -1;

        [Tooltip("Optional Core level source: bridges its level into the unit and follows its level-ups for leveled ability values.")]
        [SerializeField] private LevelComponent _levelSource;

        [Header("Events")]
        [SerializeField] private UnityEvent<float> _onDamaged = new UnityEvent<float>();
        [SerializeField] private UnityEvent<float> _onHealed = new UnityEvent<float>();
        [SerializeField] private UnityEvent _onDied = new UnityEvent();
        [SerializeField] private UnityEvent<string> _onModifierApplied = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onModifierRemoved = new UnityEvent<string>();
        [SerializeField] private UnityEvent<string> _onAbilityCast = new UnityEvent<string>();

        public AbilityUnit Unit { get; private set; }

        public UnitId UnitId => Unit?.Id ?? UnitId.None;

        public UnityEvent<float> OnDamaged => _onDamaged;
        public UnityEvent<float> OnHealed => _onHealed;
        public UnityEvent OnDied => _onDied;
        public UnityEvent<string> OnModifierApplied => _onModifierApplied;
        public UnityEvent<string> OnModifierRemoved => _onModifierRemoved;
        public UnityEvent<string> OnAbilityCast => _onAbilityCast;

        public UnitTemplate Template => _template;

        /// <summary>
        ///     Assigns the template used when the unit registers. Only takes effect before the component
        ///     is enabled — set it on an inactive object (e.g. a pooled/spawned prefab) before activating.
        /// </summary>
        public void SetTemplate(UnitTemplate template)
        {
            _template = template;
        }

        /// <summary>Overrides the team applied on registration. Same timing rules as <see cref="SetTemplate" />.</summary>
        public void SetTeamOverride(int team)
        {
            _teamOverride = team;
        }

        /// <summary>
        ///     Sets the domain unit's level directly (clamped to at least 1). Drives per-unit-level leveled
        ///     ability values. Prefer wiring a <see cref="LevelComponent" /> when you have one.
        /// </summary>
        public void SetLevel(int level)
        {
            Unit?.SetLevel(level);
        }

        /// <summary>Assigns (or clears) the Core level source used to bridge unit level. Takes effect on next enable.</summary>
        public void SetLevelSource(LevelComponent levelSource)
        {
            _levelSource = levelSource;
        }

        public float CurrentHealth => Unit?.Health ?? 0f;
        public float MaxHealth => Unit?.MaxHealth ?? 0f;
        public float HealthNormalized => MaxHealth > 0f ? CurrentHealth / MaxHealth : 0f;
        public bool IsAlive => Unit?.IsAlive ?? false;

        private void OnEnable()
        {
            AbilitySystemBehaviour hub = AbilitySystemBehaviour.I;
            AbilitySystem system = hub.System;

            TeamId team = _teamOverride >= 0 ? new TeamId(_teamOverride) : _template != null
                ? _template.Team
                : TeamId.Neutral;

            Unit = system.CreateUnit(team, _template != null ? _template.DisplayName : name);
            if (_template != null)
            {
                _template.ApplyTo(Unit);
            }

            if (_levelSource != null)
            {
                Unit.SetLevel(_levelSource.Level);
                _levelSource.OnLevelUp.AddListener(HandleLevelSourceChanged);
            }

            hub.RegisterBehaviour(this);
            system.Events.SubscribeAny(HandleEvent);
        }

        private void OnDisable()
        {
            if (Unit == null)
            {
                return;
            }

            if (_levelSource != null)
            {
                _levelSource.OnLevelUp.RemoveListener(HandleLevelSourceChanged);
            }

            AbilitySystemBehaviour hub = AbilitySystemBehaviour.I;
            hub.System.Events.UnsubscribeAny(HandleEvent);
            hub.UnregisterBehaviour(this);
            hub.System.DestroyUnit(Unit.Id);
            Unit = null;
        }

        private void HandleLevelSourceChanged(int newLevel)
        {
            Unit?.SetLevel(newLevel);
        }

        /// <summary>Convenience damage entry (routes through the full pipeline).</summary>
        public void ApplyDamage(float amount)
        {
            if (Unit != null)
            {
                DamageService.ApplyDamage(Unit.System, UnitId.None, Unit.Id, amount,
                    AbilityDamageTypes.Pure);
            }
        }

        [Button]
        public void DebugDamage25()
        {
            ApplyDamage(25f);
        }

        private void HandleEvent(AbilityEventArgs args)
        {
            if (Unit == null || args.Target != Unit.Id)
            {
                return;
            }

            switch (args.EventId)
            {
                case AbilityEvents.TakeDamage:
                    _onDamaged.Invoke(args.Amount);
                    break;
                case AbilityEvents.HealReceived:
                    _onHealed.Invoke(args.Amount);
                    break;
                case AbilityEvents.Death:
                    _onDied.Invoke();
                    break;
                case AbilityEvents.ModifierApplied:
                    _onModifierApplied.Invoke(args.ModifierId);
                    break;
                case AbilityEvents.ModifierRemoved:
                    _onModifierRemoved.Invoke(args.ModifierId);
                    break;
                case AbilityEvents.AbilityCast:
                    _onAbilityCast.Invoke(args.AbilityId);
                    break;
            }
        }
    }
}
