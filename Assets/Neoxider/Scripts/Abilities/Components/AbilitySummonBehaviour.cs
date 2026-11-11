using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Makes a unit prefab a summon. Spawned by an ability (the <c>spawn</c> effect and an archetype of the hub),
    ///     it learns who summoned it: takes the owner's team and level, hands the owner to its movement or AI through
    ///     an event, lives for a limited time, respects a cap per owner and leaves when the owner dies. Put it next
    ///     to the unit's <see cref="AbilityUnitBehaviour" />; the unit's own abilities and auto-caster do the fighting.
    /// </summary>
    [NeoDoc("Abilities/AbilitySummonBehaviour.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Summon")]
    [AddComponentMenu("Neoxider/Abilities/Ability Summon")]
    public sealed class AbilitySummonBehaviour : MonoBehaviour, ISpawnedAbilityEntity
    {
        private static readonly Dictionary<long, List<AbilitySummonBehaviour>> ByOwner =
            new Dictionary<long, List<AbilitySummonBehaviour>>();

        [Tooltip("Take the owner's team, so the summon fights on the owner's side.")]
        [SerializeField] private bool _inheritTeam = true;

        [Tooltip("Take the owner's unit level, so leveled values of the summon's abilities follow the owner.")]
        [SerializeField] private bool _inheritLevel = true;

        [Tooltip("Seconds the summon lives. 0 = until it dies or is dismissed.")]
        [SerializeField] [Min(0f)] private float _lifetime;

        [Tooltip("Most summons of this archetype one owner keeps; a new one dismisses the oldest. 0 = no limit.")]
        [SerializeField] [Min(0)] private int _maxPerOwner;

        [Tooltip("Dismiss the summon when its owner dies or disappears.")]
        [SerializeField] private bool _dismissWithOwner = true;

        [Header("Events")]
        [Tooltip("Raised when the summon appears, with the owner's transform: wire it to Follow.SetTarget or to " +
                 "your own AI.")]
        [SerializeField] private UnityEvent<Transform> _onOwnerAssigned = new UnityEvent<Transform>();

        [Tooltip("Raised when the summon is dismissed (lifetime, cap or the owner's death), before it returns to the pool.")]
        [SerializeField] private UnityEvent _onDismissed = new UnityEvent();

        private AbilitySystemBehaviour _hub;
        private UnitId _ownerId;
        private long _key;
        private float _age;
        private bool _active;

        /// <summary>The unit that summoned this one; null before a spawn and after the owner is gone.</summary>
        public AbilityUnitBehaviour Owner { get; private set; }

        /// <summary>Domain id of the owner.</summary>
        public UnitId OwnerId => _ownerId;

        /// <summary>Ability whose effect spawned this summon.</summary>
        public string SourceAbilityId { get; private set; }

        /// <summary>True between the spawn and the dismissal.</summary>
        public bool IsSummoned => _active;

        /// <summary>Seconds the summon lives. 0 = no limit.</summary>
        public float Lifetime
        {
            get => _lifetime;
            set => _lifetime = Mathf.Max(0f, value);
        }

        /// <summary>Most summons of this archetype one owner keeps. 0 = no limit.</summary>
        public int MaxPerOwner
        {
            get => _maxPerOwner;
            set => _maxPerOwner = Mathf.Max(0, value);
        }

        public UnityEvent<Transform> OnOwnerAssigned => _onOwnerAssigned;
        public UnityEvent OnDismissed => _onDismissed;

        public void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)
        {
            Unregister();
            _hub = hub;
            _ownerId = request.Owner;
            SourceAbilityId = request.AbilityId;
            Owner = hub != null ? hub.GetBehaviour(request.Owner) : null;
            _age = 0f;
            _active = true;

            if (TryGetComponent(out AbilityUnitBehaviour unit) && unit.Unit != null && Owner != null && Owner.Unit != null)
            {
                if (_inheritTeam)
                {
                    unit.Unit.Team = Owner.Unit.Team;
                }

                if (_inheritLevel)
                {
                    unit.SetLevel(Owner.Unit.Level);
                }
            }

            Register(request.ArchetypeId);
            _onOwnerAssigned.Invoke(Owner != null ? Owner.transform : null);
        }

        /// <summary>Sends the summon away now: raises <see cref="OnDismissed" /> and returns it to the pool.</summary>
        public void Dismiss()
        {
            if (!_active)
            {
                return;
            }

            _active = false;
            Unregister();
            _onDismissed.Invoke();
            if (this != null && gameObject != null)
            {
                PoolManager.Release(gameObject);
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_dismissWithOwner && (Owner == null || Owner.Unit == null || !Owner.Unit.IsAlive))
            {
                Dismiss();
                return;
            }

            if (_lifetime > 0f)
            {
                _age += Time.deltaTime;
                if (_age >= _lifetime)
                {
                    Dismiss();
                }
            }
        }

        private void OnDisable()
        {
            // WHY: a summon that dies returns to the pool through its own death wiring, not through Dismiss.
            _active = false;
            Unregister();
        }

        private void Register(string archetypeId)
        {
            int archetypeHash = string.IsNullOrEmpty(archetypeId) ? 0 : archetypeId.ToLowerInvariant().GetHashCode();
            _key = ((long)_ownerId.Value << 32) ^ (uint)archetypeHash;
            if (!ByOwner.TryGetValue(_key, out List<AbilitySummonBehaviour> list))
            {
                list = new List<AbilitySummonBehaviour>();
                ByOwner[_key] = list;
            }

            list.Add(this);
            while (_maxPerOwner > 0 && list.Count > _maxPerOwner)
            {
                AbilitySummonBehaviour oldest = list[0];
                list.RemoveAt(0);
                if (oldest != null && oldest != this)
                {
                    oldest.DismissFromCap();
                }
            }
        }

        private void DismissFromCap()
        {
            // WHY: the cap loop already removed this summon from the list.
            _key = 0;
            Dismiss();
        }

        private void Unregister()
        {
            if (_key != 0 && ByOwner.TryGetValue(_key, out List<AbilitySummonBehaviour> list))
            {
                list.Remove(this);
                if (list.Count == 0)
                {
                    ByOwner.Remove(_key);
                }
            }

            _key = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticState()
        {
            ByOwner.Clear();
        }
    }
}
