using System;
using System.Collections.Generic;
using Neo.Tools;
using UnityEngine;
using UnityEngine.Events;

namespace Neo.Abilities
{
    /// <summary>
    ///     Plays the presentation cues named in ability data: <see cref="AbilityBlueprint.CastCue" /> at the caster
    ///     when an ability is cast and <see cref="AbilityBlueprint.ImpactCue" /> at every unit the ability damages
    ///     (chain hops, zone pulses and projectile payloads included). A cue is a pooled prefab plus a UnityEvent, so
    ///     effects and sounds stay out of gameplay data and out of code.
    /// </summary>
    [NeoDoc("Abilities/AbilityCuePlayer.md")]
    [CreateFromMenu("Neoxider/Abilities/Ability Cue Player")]
    [AddComponentMenu("Neoxider/Abilities/Ability Cue Player")]
    public sealed class AbilityCuePlayer : MonoBehaviour
    {
        /// <summary>One presentation cue: an id from ability data, an effect to spawn and an event to raise.</summary>
        [Serializable]
        public sealed class Cue
        {
            [Tooltip("Cue id, as written in Cast Cue or Impact Cue of an ability.")]
            public string Id = string.Empty;

            [Tooltip("Prefab or inactive scene template taken from the pool at the cue position. It returns itself, " +
                     "for example with a Despawner and Despawn After Seconds. Empty = event only.")]
            public GameObject Prefab;

            [Tooltip("Raised every time the cue plays: sounds, camera shake.")]
            public UnityEvent OnPlayed = new UnityEvent();
        }

        [Tooltip("Cues by id. An ability names one in Cast Cue (played at the caster) and Impact Cue (played at " +
                 "each unit it damages).")]
        [SerializeField] private List<Cue> _cues = new List<Cue>();

        [Tooltip("Most effect prefabs spawned in one frame, so a blast that hits fifty units does not spawn fifty " +
                 "effects. The event of a cue is still raised for every play. 0 = no limit.")]
        [SerializeField] [Min(0)] private int _maxSpawnsPerFrame = 10;

        private readonly Dictionary<string, Cue> _byId = new Dictionary<string, Cue>(StringComparer.Ordinal);
        private AbilitySystemBehaviour _hub;
        private Action<AbilityEventArgs> _onCast;
        private Action<AbilityEventArgs> _onDamage;
        private bool _started;
        private int _frame = -1;
        private int _spawnsThisFrame;

        /// <summary>Configured cues.</summary>
        public IReadOnlyList<Cue> Cues => _cues;

        /// <summary>Most effect prefabs spawned in one frame. 0 = no limit.</summary>
        public int MaxSpawnsPerFrame
        {
            get => _maxSpawnsPerFrame;
            set => _maxSpawnsPerFrame = Mathf.Max(0, value);
        }

        private void Start()
        {
            _started = true;
            Subscribe();
        }

        private void OnEnable()
        {
            // WHY: the hub creates its system in Awake; the first subscription waits for Start so the order
            // of Awake calls between this component and the hub does not matter.
            if (_started)
            {
                Subscribe();
            }
        }

        private void OnDisable()
        {
            Unsubscribe();
        }

        /// <summary>Adds a cue at runtime, replacing one with the same id.</summary>
        public void SetCue(string id, GameObject prefab)
        {
            if (string.IsNullOrEmpty(id))
            {
                return;
            }

            for (int i = 0; i < _cues.Count; i++)
            {
                if (_cues[i] != null && _cues[i].Id == id)
                {
                    _cues[i].Prefab = prefab;
                    _byId[id] = _cues[i];
                    return;
                }
            }

            Cue cue = new Cue { Id = id, Prefab = prefab };
            _cues.Add(cue);
            _byId[id] = cue;
        }

        /// <summary>Connects to the scene ability hub. Called on Start and on re-enable; safe to call again.</summary>
        public void Subscribe()
        {
            AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
            if (hub == null || hub == _hub)
            {
                return;
            }

            Unsubscribe();
            _hub = hub;
            RebuildIndex();
            _onCast ??= HandleCast;
            _onDamage ??= HandleDamage;
            hub.System.Events.Subscribe(AbilityEvents.AbilityCast, _onCast);
            hub.System.Events.Subscribe(AbilityEvents.TakeDamage, _onDamage);
        }

        /// <summary>Plays a cue by id at a world position, for code and UnityEvents outside ability casts.</summary>
        public bool PlayAt(string cueId, Vector3 position)
        {
            if (string.IsNullOrEmpty(cueId))
            {
                return false;
            }

            if (_byId.Count == 0)
            {
                RebuildIndex();
            }

            if (!_byId.TryGetValue(cueId, out Cue cue))
            {
                return false;
            }

            PlayCue(cue, position);
            return true;
        }

        private void Unsubscribe()
        {
            if (_hub != null && _hub == AbilitySystemBehaviour.InstanceOrNull)
            {
                _hub.System.Events.Unsubscribe(AbilityEvents.AbilityCast, _onCast);
                _hub.System.Events.Unsubscribe(AbilityEvents.TakeDamage, _onDamage);
            }

            _hub = null;
        }

        private void RebuildIndex()
        {
            _byId.Clear();
            for (int i = 0; i < _cues.Count; i++)
            {
                Cue cue = _cues[i];
                if (cue != null && !string.IsNullOrEmpty(cue.Id))
                {
                    _byId[cue.Id] = cue;
                }
            }
        }

        private void HandleCast(AbilityEventArgs args)
        {
            PlayFor(args.AbilityId, true, args.Target);
        }

        private void HandleDamage(AbilityEventArgs args)
        {
            PlayFor(args.AbilityId, false, args.Target);
        }

        private void PlayFor(string abilityId, bool cast, UnitId unit)
        {
            if (_hub == null || string.IsNullOrEmpty(abilityId) ||
                !_hub.System.TryGetAbility(abilityId, out AbilityBlueprint blueprint))
            {
                return;
            }

            string cueId = cast ? blueprint.CastCue : blueprint.ImpactCue;
            if (string.IsNullOrEmpty(cueId) || !_byId.TryGetValue(cueId, out Cue cue))
            {
                return;
            }

            if (_hub.TryGetPosition(unit, out Vector3 position))
            {
                PlayCue(cue, position);
            }
        }

        private void PlayCue(Cue cue, Vector3 position)
        {
            if (cue.Prefab != null)
            {
                int frame = Time.frameCount;
                if (frame != _frame)
                {
                    _frame = frame;
                    _spawnsThisFrame = 0;
                }

                if (_maxSpawnsPerFrame <= 0 || _spawnsThisFrame < _maxSpawnsPerFrame)
                {
                    _spawnsThisFrame++;
                    PoolManager.Get(cue.Prefab, position, Quaternion.identity);
                }
            }

            cue.OnPlayed.Invoke();
        }
    }
}
