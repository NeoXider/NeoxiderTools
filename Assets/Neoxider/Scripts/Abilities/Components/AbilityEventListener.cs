using System;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Subscribes to one ability system event and relays it as UnityEvents and a C# event: "when this ability is
    ///     cast", "when this unit takes damage", "when anything is killed". Use it to call your own code, analytics,
    ///     quests or achievements without touching ability data. Optional filters narrow it to one ability and to
    ///     the units on either side of the event.
    /// </summary>
    [NeoDoc("Abilities/AbilityEventListener.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Event Listener")]
    [AddComponentMenu("Neoxider/Abilities/Ability Event Listener")]
    public sealed class AbilityEventListener : MonoBehaviour
    {
        [Tooltip("Event id: ability_cast, take_damage, deal_damage, kill, death, heal_received, modifier_applied, " +
                 "modifier_removed, critical_hit, zone_spawned, zone_expired, ... (constants in AbilityEvents).")]
        [SerializeField] private string _eventId = AbilityEvents.AbilityCast;

        [Tooltip("Only events of this ability. Empty = any ability.")]
        [SerializeField] private string _abilityId = string.Empty;

        [Tooltip("Only events about this unit: the caster of ability_cast, the damaged unit of take_damage, the " +
                 "dealer of deal_damage and kill, the dead unit of death. Empty = any unit.")]
        [SerializeField] private AbilityUnitBehaviour _unit;

        [Tooltip("Only events whose other side is this unit: the attacker in take_damage, the victim in deal_damage " +
                 "and kill. Empty = any unit.")]
        [SerializeField] private AbilityUnitBehaviour _otherUnit;

        [Header("Events")]
        [SerializeField] private UnityEvent _onEvent = new UnityEvent();

        [Tooltip("The event amount: damage dealt, health restored, zone radius.")]
        [SerializeField] private UnityEvent<float> _onAmount = new UnityEvent<float>();

        [Tooltip("The GameObject of the unit the event is about, when it has a scene presence.")]
        [SerializeField] private UnityEvent<GameObject> _onUnitObject = new UnityEvent<GameObject>();

        [Tooltip("The GameObject of the unit on the other side of the event, when there is one.")]
        [SerializeField] private UnityEvent<GameObject> _onOtherObject = new UnityEvent<GameObject>();

        [Tooltip("The ability id of the event, when there is one.")]
        [SerializeField] private UnityEvent<string> _onAbilityId = new UnityEvent<string>();

        private AbilitySystemBehaviour _hub;
        private Action<AbilityEventArgs> _handler;
        private string _subscribedEventId;
        private bool _started;

        /// <summary>Raised for every event that passes the filters, with the full event data.</summary>
        public event Action<AbilityEventArgs> Raised;

        /// <summary>Event id this listener is subscribed to. Setting it re-subscribes.</summary>
        public string EventId
        {
            get => _eventId;
            set
            {
                _eventId = value ?? string.Empty;
                if (_hub != null)
                {
                    Unsubscribe();
                    Subscribe();
                }
            }
        }

        /// <summary>Ability filter. Empty = any ability.</summary>
        public string AbilityId
        {
            get => _abilityId;
            set => _abilityId = value ?? string.Empty;
        }

        /// <summary>Unit the event must be about. Null = any unit.</summary>
        public AbilityUnitBehaviour Unit
        {
            get => _unit;
            set => _unit = value;
        }

        /// <summary>Unit on the other side of the event. Null = any unit.</summary>
        public AbilityUnitBehaviour OtherUnit
        {
            get => _otherUnit;
            set => _otherUnit = value;
        }

        public UnityEvent OnEvent => _onEvent;
        public UnityEvent<float> OnAmount => _onAmount;
        public UnityEvent<GameObject> OnUnitObject => _onUnitObject;
        public UnityEvent<GameObject> OnOtherObject => _onOtherObject;
        public UnityEvent<string> OnAbilityId => _onAbilityId;

        private void Start()
        {
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            // WHY: the hub creates its system in Awake; the first subscription waits for Start so the order of
            // Awake calls between this component and the hub does not matter.
            if (_started)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Connects to the scene ability hub. Called on Start and on re-enable; safe to call again.</summary>
        public void Subscribe()
        {
            AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
            if (hub == null || hub == _hub || string.IsNullOrEmpty(_eventId))
            {
                return;
            }

            Unsubscribe();
            _hub = hub;
            _handler ??= Handle;
            _subscribedEventId = _eventId;
            hub.System.Events.Subscribe(_subscribedEventId, _handler);
        }

        private void Unsubscribe()
        {
            if (_hub != null && _hub == AbilitySystemBehaviour.InstanceOrNull && _subscribedEventId != null)
            {
                _hub.System.Events.Unsubscribe(_subscribedEventId, _handler);
            }

            _hub = null;
            _subscribedEventId = null;
        }

        private void Handle(AbilityEventArgs args)
        {
            if (!string.IsNullOrEmpty(_abilityId) && !AbilityIdComparer.Same(_abilityId, args.AbilityId))
            {
                return;
            }

            if (_unit != null && _unit.UnitId != args.Target)
            {
                return;
            }

            if (_otherUnit != null && _otherUnit.UnitId != args.Source)
            {
                return;
            }

            Raised?.Invoke(args);
            _onEvent.Invoke();
            _onAmount.Invoke(args.Amount);
            if (!string.IsNullOrEmpty(args.AbilityId))
            {
                _onAbilityId.Invoke(args.AbilityId);
            }

            if (_hub == null)
            {
                return;
            }

            AbilityUnitBehaviour unit = args.Target.IsValid ? _hub.GetBehaviour(args.Target) : null;
            if (unit != null)
            {
                _onUnitObject.Invoke(unit.gameObject);
            }

            AbilityUnitBehaviour other = args.Source.IsValid ? _hub.GetBehaviour(args.Source) : null;
            if (other != null)
            {
                _onOtherObject.Invoke(other.gameObject);
            }
        }
    }
}
