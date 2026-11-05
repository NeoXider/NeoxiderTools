using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Neo.Abilities;
using TMPro;
using UnityEngine;

namespace Neo.Samples
{
    /// <summary>
    ///     C# side of the "Abilities: triggers and modifiers" tutorial (AbilitiesTriggersDemo.unity): button
    ///     handlers that cast through <see cref="AbilityCasterBehaviour" />, a live status of each unit (health,
    ///     shield, poison stacks, mark and empower charges), world indicators and the "why did this fire" panel read
    ///     from <see cref="AbilitySystem.Trace" />. Everything it touches is authored in the scene by
    ///     AbilitiesTriggersDemoBuilder; this behaviour only reacts.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class AbilitiesTriggersDemoGuide : MonoBehaviour
    {
        private const float RefreshSeconds = 0.1f;

        [Header("Units")]
        [SerializeField] private AbilityUnitBehaviour _hero;
        [SerializeField] private AbilityUnitBehaviour _golem;
        [SerializeField] private AbilityUnitBehaviour _imp;
        [SerializeField] private AbilityCasterBehaviour _heroCaster;
        [SerializeField] private AbilityCasterBehaviour _golemCaster;

        [Header("Ability and modifier ids")]
        [SerializeField] private string _strikeId = "strike";
        [SerializeField] private string _empowerCastId = "empower_cast";
        [SerializeField] private string _empowerId = "empower";
        [SerializeField] private string _markId = "hunter_mark";
        [SerializeField] private string _markModifierId = "hunter_mark_mark";
        [SerializeField] private string _dartId = "venom_dart";
        [SerializeField] private string _poisonId = "venom";
        [SerializeField] private string _formationId = "thunder_formation";
        [SerializeField] private string _smashId = "golem_smash";
        [SerializeField] private string _shieldId = "guardian_barrier";

        [Header("Readouts")]
        [SerializeField] private TMP_Text _heroLabel;
        [SerializeField] private TMP_Text _golemLabel;
        [SerializeField] private TMP_Text _impLabel;
        [SerializeField] private TMP_Text _status;
        [SerializeField] private TMP_Text _why;
        [SerializeField] private DemoLogFeed _log;

        [Header("World indicators")]
        [SerializeField] private GameObject _heroShield;
        [SerializeField] private GameObject _golemMark;
        [SerializeField] private GameObject _golemPoison;
        [SerializeField] private GameObject _heroEmpower;

        private readonly StringBuilder _text = new StringBuilder(256);
        private System.Action<AbilityEventArgs> _onEvent;
        private System.Func<UnitId, string> _unitName;

        /// <summary>True when the builder assigned the units, casters and readouts (checked by the smoke tests).</summary>
        public bool IsSceneBuilt => _hero != null && _golem != null && _imp != null && _heroCaster != null &&
                                    _golemCaster != null && _status != null && _why != null;
        private AbilitySystem _subscribed;
        private float _nextRefresh;
        private long _explainedSequence;

        /// <summary>Hero basic attack on the golem (20 physical).</summary>
        public void Strike()
        {
            Cast(_heroCaster, _strikeId, _golem, "Strike");
        }

        /// <summary>Attack enhancer: the next three hits deal x1.5.</summary>
        public void Empower()
        {
            Cast(_heroCaster, _empowerCastId, null, "Empower");
        }

        /// <summary>Mark: the next three hits on the golem deal +15.</summary>
        public void Mark()
        {
            Cast(_heroCaster, _markId, _golem, "Hunter's Mark");
        }

        /// <summary>Poison dart: adds a stack of venom (4 per second per stack, up to 5 stacks).</summary>
        public void PoisonDart()
        {
            Cast(_heroCaster, _dartId, _golem, "Venom Dart");
        }

        /// <summary>Formation: a 6 s zone under the golem pulsing damage and a slow on enemies inside.</summary>
        public void Formation()
        {
            if (_heroCaster == null || _golem == null)
            {
                return;
            }

            bool cast = _heroCaster.TryCastAtPoint(_formationId, _golem.transform.position);
            Report("Thunder Formation", cast, _heroCaster);
        }

        /// <summary>The golem smashes the hero (70 physical): below 40% health the hero's auto-defense shield triggers.</summary>
        public void GolemSmash()
        {
            Cast(_golemCaster, _smashId, _hero, "Golem Smash");
        }

        /// <summary>Restores every unit, clears temporary modifiers, zones and cooldowns.</summary>
        public void ResetDemo()
        {
            AbilitySystem system = SystemOrNull();
            if (system == null)
            {
                return;
            }

            ResetUnit(system, _hero);
            ResetUnit(system, _golem);
            ResetUnit(system, _imp);
            if (_hero != null && _hero.Unit != null)
            {
                system.RemoveZonesOf(_hero.UnitId);
            }

            RestartCooldowns(system, _hero, _heroCaster);
            RestartCooldowns(system, _golem, _golemCaster);
            system.Trace.Clear();
            _explainedSequence = 0;
            if (_why != null)
            {
                _why.text = "Use a lesson button: the latest reaction and its causes appear here.";
            }

            Log("Reset: full health, no temporary modifiers, no zones, cooldowns ready.");
            Refresh();
        }

        private void Update()
        {
            AbilitySystem system = SystemOrNull();
            if (system != null && !ReferenceEquals(system, _subscribed))
            {
                Subscribe(system);
            }

            if (Time.unscaledTime >= _nextRefresh)
            {
                _nextRefresh = Time.unscaledTime + RefreshSeconds;
                Refresh();
            }
        }

        private void OnDisable()
        {
            if (_subscribed != null && _onEvent != null)
            {
                _subscribed.Events.UnsubscribeAny(_onEvent);
            }

            _subscribed = null;
        }

        private void Subscribe(AbilitySystem system)
        {
            if (_subscribed != null && _onEvent != null)
            {
                _subscribed.Events.UnsubscribeAny(_onEvent);
            }

            _onEvent ??= OnEvent;
            system.Events.SubscribeAny(_onEvent);
            _subscribed = system;
        }

        private void OnEvent(AbilityEventArgs e)
        {
            switch (e.EventId)
            {
                case AbilityEvents.TakeDamage when e.Amount > 0f:
                    Log(NameOf(e.Target) + " takes " + F(e.Amount) + " " + e.DamageType +
                        (string.IsNullOrEmpty(e.AbilityId) ? string.Empty : " (" + e.AbilityId + ")"));
                    break;
                case AbilityEvents.DamageReflected:
                    Log("Thorns: " + NameOf(e.Target) + " reflects " + F(e.Amount) + " to " + NameOf(e.Source));
                    break;
                case AbilityEvents.ShieldAbsorbed:
                    Log("Shield absorbs " + F(e.Amount) + " for " + NameOf(e.Target));
                    break;
                case AbilityEvents.ModifierApplied:
                    if (e.ModifierId == _shieldId)
                    {
                        Log("Auto-defense: " + NameOf(e.Target) + " fell below 40% and gains a shield");
                    }

                    break;
                case AbilityEvents.ZoneSpawned:
                    Log("Formation placed: pulses every second for 6 s");
                    break;
                case AbilityEvents.ZoneExpired:
                    Log("Formation faded");
                    break;
            }
        }

        private void Refresh()
        {
            AbilitySystem system = SystemOrNull();
            if (system == null)
            {
                return;
            }

            ModifierInstance shield = Find(system, _hero, _shieldId);
            ModifierInstance empower = Find(system, _hero, _empowerId);
            ModifierInstance mark = Find(system, _golem, _markModifierId);
            ModifierInstance poison = Find(system, _golem, _poisonId);
            SetActive(_heroShield, shield != null);
            SetActive(_heroEmpower, empower != null);
            SetActive(_golemMark, mark != null);
            SetActive(_golemPoison, poison != null);

            SetLabel(_heroLabel, _hero, shield != null
                ? "shield " + F(DamageService.RemainingShield(shield))
                : empower != null
                    ? "empowered x" + empower.GetRemainingCharges(0)
                    : null);
            SetLabel(_golemLabel, _golem, Describe(mark, poison));
            SetLabel(_impLabel, _imp, Find(system, _imp, "thunder_formation_slow") != null ? "slowed" : null);
            RefreshStatus(system, empower, mark, poison, shield);
            RefreshWhy(system);
        }

        private void RefreshStatus(AbilitySystem system, ModifierInstance empower, ModifierInstance mark,
            ModifierInstance poison, ModifierInstance shield)
        {
            if (_status == null)
            {
                return;
            }

            _text.Clear();
            _text.Append("Empower ").Append(empower != null ? empower.GetRemainingCharges(0) + " hits" : "off");
            _text.Append("   ·   Mark ").Append(mark != null ? mark.GetRemainingCharges(0) + " hits" : "off");
            _text.Append("   ·   Venom ").Append(poison != null ? poison.Stacks + " stacks" : "off");
            _text.Append('\n');
            _text.Append("Shield ").Append(shield != null ? "on" : "off");
            _text.Append("   ·   Zones ").Append(system.ZoneCount);
            _text.Append("   ·   Smash ").Append(Ready(_golemCaster, _smashId));
            _text.Append("   ·   Formation ").Append(Ready(_heroCaster, _formationId));
            _status.text = _text.ToString();
        }

        private void RefreshWhy(AbilitySystem system)
        {
            if (_why == null || !system.Trace.Enabled ||
                !system.Trace.TryFindLatest(AbilityTraceKind.Reaction, null, out AbilityTraceEntry entry) ||
                entry.Sequence == _explainedSequence)
            {
                return;
            }

            _explainedSequence = entry.Sequence;
            _unitName ??= NameOf;
            _why.text = system.Trace.Explain(entry.Sequence, 5, _unitName);
        }

        private void Cast(AbilityCasterBehaviour caster, string abilityId, AbilityUnitBehaviour target, string label)
        {
            if (caster == null)
            {
                return;
            }

            bool cast = target != null ? caster.TryCastAtUnit(abilityId, target) : caster.TryCast(abilityId);
            Report(label, cast, caster);
        }

        private void Report(string label, bool cast, AbilityCasterBehaviour caster)
        {
            if (!cast)
            {
                Log(label + " rejected: " + caster.LastCastResult.Failure);
            }

            Refresh();
        }

        private void ResetUnit(AbilitySystem system, AbilityUnitBehaviour unit)
        {
            if (unit == null || unit.Unit == null)
            {
                return;
            }

            AbilityUnit domain = unit.Unit;
            List<ModifierInstance> modifiers = new List<ModifierInstance>();
            system.Modifiers.GetModifiers(domain.Id, modifiers);
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (!modifiers[i].IsPermanent)
                {
                    system.Modifiers.Remove(modifiers[i]);
                }
            }

            if (!domain.IsAlive)
            {
                system.Revive(domain);
            }

            domain.Resources.SetCurrent(AbilityResourceIds.Health, domain.MaxHealth);
        }

        private static void RestartCooldowns(AbilitySystem system, AbilityUnitBehaviour unit,
            AbilityCasterBehaviour caster)
        {
            if (unit == null || unit.Unit == null || caster == null)
            {
                return;
            }

            List<AbilitySlot> slots = new List<AbilitySlot>();
            system.GetSlots(unit.UnitId, slots);
            for (int i = 0; i < slots.Count; i++)
            {
                string id = slots[i].Blueprint.Id;
                system.RevokeAbility(unit.UnitId, id);
                system.GrantAbility(unit.UnitId, id);
            }
        }

        private static ModifierInstance Find(AbilitySystem system, AbilityUnitBehaviour unit, string modifierId)
        {
            return unit != null && unit.Unit != null &&
                   system.Modifiers.TryGetActive(unit.UnitId, modifierId, out ModifierInstance instance)
                ? instance
                : null;
        }

        private string Describe(ModifierInstance mark, ModifierInstance poison)
        {
            if (mark == null && poison == null)
            {
                return "thorns 30%";
            }

            string text = mark != null ? "marked x" + mark.GetRemainingCharges(0) : string.Empty;
            if (poison != null)
            {
                text += (text.Length > 0 ? " · " : string.Empty) + "venom " + poison.Stacks;
            }

            return text;
        }

        private static string Ready(AbilityCasterBehaviour caster, string abilityId)
        {
            if (caster == null)
            {
                return "-";
            }

            float cooldown = caster.GetCooldownNormalized(abilityId);
            return cooldown <= 0f ? "ready" : Mathf.RoundToInt(cooldown * 100f) + "%";
        }

        private static void SetLabel(TMP_Text label, AbilityUnitBehaviour unit, string extra)
        {
            if (label == null || unit == null)
            {
                return;
            }

            string name = unit.Unit != null ? unit.Unit.DisplayName : unit.name;
            string health = unit.IsAlive
                ? Mathf.CeilToInt(unit.CurrentHealth) + " / " + Mathf.CeilToInt(unit.MaxHealth)
                : "down";
            label.text = string.IsNullOrEmpty(extra)
                ? name + "\n" + health
                : name + "\n" + health + "\n<size=75%>" + extra + "</size>";
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private string NameOf(UnitId id)
        {
            if (_hero != null && _hero.UnitId == id)
            {
                return "Hero";
            }

            if (_golem != null && _golem.UnitId == id)
            {
                return "Golem";
            }

            return _imp != null && _imp.UnitId == id ? "Imp" : "unit";
        }

        private void Log(string message)
        {
            if (_log != null)
            {
                _log.Log(message);
            }
        }

        private AbilitySystem SystemOrNull()
        {
            AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
            return hub != null ? hub.System : null;
        }

        private static string F(float value)
        {
            return value.ToString("0.#", CultureInfo.InvariantCulture);
        }
    }
}
