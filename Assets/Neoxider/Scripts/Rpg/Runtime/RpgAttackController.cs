using System;
using System.Collections;
using System.Collections.Generic;
using Neo;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    /// Universal attack caster that supports direct, area, and projectile attacks.
    /// </summary>
    [NeoDoc("Rpg/RpgAttackController.md")]
    [CreateFromMenu("Neoxider/RPG/RpgAttackController")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgAttackController))]
    public sealed class RpgAttackController : MonoBehaviour
    {
        [Header("Setup")]
        [SerializeField] private RpgAttackDefinition[] _attacks = Array.Empty<RpgAttackDefinition>();
        [SerializeField] private RpgAttackPreset[] _presets = Array.Empty<RpgAttackPreset>();
        [SerializeField] private Transform _origin;
        [SerializeField] private Transform _projectileSpawnPoint;
        [SerializeField] private RpgCombatant _combatantSource;
        [SerializeField] private RpgStatsManager _profileSource;
        [SerializeField] private RpgTargetSelector _targetSelector;

        [Header("Built-in Input")]
        [SerializeField] private bool _enableBuiltInInput = true;
        [SerializeField] private RpgButtonBinding _primaryAttackBinding = RpgButtonBinding.CreatePrimaryAttackDefault();

        [Header("Events")]
        [SerializeField] private RpgAttackEvent _onAttackStarted = new();
        [SerializeField] private RpgAttackEvent _onAttackResolved = new();
        [SerializeField] private RpgAttackEvent _onPresetUsed = new();
        [SerializeField] private RpgGameObjectEvent _onTargetResolved = new();
        [SerializeField] private RpgStringEvent _onAttackFailed = new();

        private readonly Dictionary<string, float> _cooldowns = new(StringComparer.Ordinal);
        private Coroutine _castCoroutine;

        /// <summary>
        /// Gets whether the controller is currently casting.
        /// </summary>
        public bool IsCasting => _castCoroutine != null;

        /// <summary>
        /// Gets or sets whether the built-in input listener is enabled.
        /// </summary>
        public bool EnableBuiltInInput
        {
            get => _enableBuiltInInput;
            set => _enableBuiltInInput = value;
        }

        private void Update()
        {
            if (_enableBuiltInInput && _primaryAttackBinding != null && _primaryAttackBinding.IsPressedThisFrame())
            {
                UsePrimaryAttack();
            }
        }

        /// <summary>
        /// Uses the first configured attack.
        /// </summary>
        [Button]
        public bool UsePrimaryAttack()
        {
            return TryUseAttack(0, out _);
        }

        /// <summary>
        /// Uses the first configured preset.
        /// </summary>
        [Button]
        public bool UsePrimaryPreset()
        {
            return TryUsePreset(0, out _);
        }

        /// <summary>
        /// Tries to use an attack by index.
        /// </summary>
        public bool TryUseAttack(int attackIndex, out string failReason)
        {
            failReason = null;
            if (attackIndex < 0 || attackIndex >= _attacks.Length || _attacks[attackIndex] == null)
            {
                failReason = $"Attack index out of range: {attackIndex}";
                EmitFailure(failReason);
                return false;
            }

            return TryUseAttack(_attacks[attackIndex].Id, out failReason);
        }

        /// <summary>
        /// Tries to use an attack by identifier.
        /// </summary>
        public bool TryUseAttack(string attackId, out string failReason)
        {
            RpgAttackDefinition definition = ResolveAttack(attackId);
            if (!CanUseAttack(definition, out failReason))
            {
                EmitFailure(failReason);
                return false;
            }

            return BeginAttack(definition, null, attackId, true);
        }

        /// <summary>
        /// Tries to use a preset by index.
        /// </summary>
        public bool TryUsePreset(int presetIndex, out string failReason)
        {
            failReason = null;
            if (presetIndex < 0 || presetIndex >= _presets.Length || _presets[presetIndex] == null)
            {
                failReason = $"Preset index out of range: {presetIndex}";
                EmitFailure(failReason);
                return false;
            }

            return TryUsePreset(_presets[presetIndex].Id, out failReason);
        }

        /// <summary>
        /// Tries to use a preset by id.
        /// </summary>
        public bool TryUsePreset(string presetId, out string failReason)
        {
            RpgAttackPreset preset = ResolvePreset(presetId);
            return TryUseResolvedPreset(preset, null, out failReason);
        }

        /// <summary>
        /// Tries to use a preset asset directly.
        /// </summary>
        public bool TryUsePreset(RpgAttackPreset preset, out string failReason)
        {
            return TryUseResolvedPreset(preset, null, out failReason);
        }

        /// <summary>
        /// Tries to use a preset asset against a specific target.
        /// </summary>
        public bool TryUsePreset(RpgAttackPreset preset, GameObject forcedTarget, out string failReason)
        {
            return TryUseResolvedPreset(preset, forcedTarget, out failReason);
        }

        private bool TryUseResolvedPreset(RpgAttackPreset preset, GameObject forcedTarget, out string failReason)
        {
            if (preset == null)
            {
                failReason = "Attack preset not found.";
                EmitFailure(failReason);
                return false;
            }

            RpgAttackDefinition definition = preset.AttackDefinition;
            if (!CanUseAttack(definition, out failReason))
            {
                EmitFailure(failReason);
                return false;
            }

            GameObject target = forcedTarget;
            if (!TryResolveTargetForPreset(preset, ref target, out failReason))
            {
                EmitFailure(failReason);
                return false;
            }

            if (target != null)
            {
                _onTargetResolved?.Invoke(target);
            }

            bool started = BeginAttack(definition, target, preset.Id, preset.AimAtTarget);
            if (started)
            {
                _onPresetUsed?.Invoke(preset.Id);
            }

            return started;
        }

        /// <summary>
        /// Returns whether an attack is currently available.
        /// </summary>
        public bool CanUseAttack(string attackId, out string failReason)
        {
            return CanUseAttack(ResolveAttack(attackId), out failReason);
        }

        /// <summary>
        /// Returns whether a preset's underlying attack is currently available.
        /// </summary>
        public bool CanUsePreset(RpgAttackPreset preset, out string failReason)
        {
            if (preset == null)
            {
                failReason = "Attack preset not found.";
                return false;
            }

            return CanUseAttack(preset.AttackDefinition, out failReason);
        }

        /// <summary>
        /// Returns remaining cooldown for an attack.
        /// </summary>
        public float GetRemainingCooldown(string attackId)
        {
            if (!_cooldowns.TryGetValue(attackId, out float readyAt))
            {
                return 0f;
            }

            return Mathf.Max(0f, readyAt - Time.time);
        }

        private IEnumerator CastAttackAfterDelay(RpgAttackDefinition definition, GameObject forcedTarget, string eventId, bool aimAtTarget)
        {
            yield return new WaitForSeconds(definition.CastDelay);
            ExecuteAttack(definition, forcedTarget, aimAtTarget, eventId);
            _castCoroutine = null;
        }

        private void ExecuteAttack(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget, string eventId)
        {
            switch (definition.DeliveryType)
            {
                case RpgAttackDeliveryType.Direct:
                    ExecuteDirectAttack(definition, forcedTarget, aimAtTarget);
                    break;
                case RpgAttackDeliveryType.Area:
                    ExecuteAreaAttack(definition, forcedTarget, aimAtTarget);
                    break;
                case RpgAttackDeliveryType.Projectile:
                    SpawnProjectile(definition, forcedTarget, aimAtTarget);
                    break;
            }

            _onAttackResolved?.Invoke(eventId);
        }

        private void ExecuteDirectAttack(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget)
        {
            Vector3 origin = GetOriginPosition();
            Vector3 direction = GetAttackDirection(origin, forcedTarget, aimAtTarget);
            int hits = 0;

            if (definition.Use3D)
            {
                if (definition.Radius > 0f)
                {
                    RaycastHit[] hitBuffer = Physics.SphereCastAll(origin, definition.Radius, direction, definition.Range, definition.TargetLayers);
                    hits += ApplyHits3D(hitBuffer, definition, definition.MaxTargets);
                }
                else if (Physics.Raycast(origin, direction, out RaycastHit hit, definition.Range, definition.TargetLayers))
                {
                    hits += ApplyHitToGameObject(hit.collider.gameObject, definition) ? 1 : 0;
                }
            }

            if (hits >= definition.MaxTargets)
            {
                return;
            }

            if (definition.Use2D)
            {
                RaycastHit2D[] hitBuffer = definition.Radius > 0f
                    ? Physics2D.CircleCastAll(origin, definition.Radius, direction, definition.Range, definition.TargetLayers)
                    : Physics2D.RaycastAll(origin, direction, definition.Range, definition.TargetLayers);
                ApplyHits2D(hitBuffer, definition, definition.MaxTargets - hits);
            }
        }

        private void ExecuteAreaAttack(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget)
        {
            Vector3 center = forcedTarget != null && aimAtTarget
                ? forcedTarget.transform.position
                : GetOriginPosition() + GetAttackDirection(GetOriginPosition(), forcedTarget, aimAtTarget) * definition.Range;
            int hits = 0;

            if (definition.Use3D)
            {
                Collider[] colliders = Physics.OverlapSphere(center, Mathf.Max(0.01f, definition.Radius), definition.TargetLayers);
                hits += ApplyColliders3D(colliders, definition, definition.MaxTargets);
            }

            if (hits >= definition.MaxTargets)
            {
                return;
            }

            if (definition.Use2D)
            {
                Collider2D[] colliders2D = Physics2D.OverlapCircleAll(center, Mathf.Max(0.01f, definition.Radius), definition.TargetLayers);
                ApplyColliders2D(colliders2D, definition, definition.MaxTargets - hits);
            }
        }

        private void SpawnProjectile(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget)
        {
            if (definition.ProjectilePrefab == null)
            {
                EmitFailure($"Projectile prefab is missing for attack '{definition.Id}'.");
                return;
            }

            Transform spawn = _projectileSpawnPoint != null ? _projectileSpawnPoint : (_origin != null ? _origin : transform);
            Vector3 direction = GetAttackDirection(spawn.position, forcedTarget, aimAtTarget);
            RpgProjectile projectile = Instantiate(definition.ProjectilePrefab, spawn.position, Quaternion.LookRotation(direction));
            projectile.Initialize(this, definition, ResolveSourceReceiver(), direction);
        }

        internal bool ApplyHitToGameObject(GameObject target, RpgAttackDefinition definition)
        {
            if (!TryResolveReceiver(target, out IRpgCombatReceiver receiver))
            {
                return false;
            }

            IRpgCombatReceiver sourceReceiver = ResolveSourceReceiver();
            if (sourceReceiver is Component sourceComponent && sourceComponent.gameObject == target)
            {
                return false;
            }

            float amount = definition.Power;
            if (sourceReceiver != null && definition.HitMode == RpgHitMode.Damage)
            {
                amount *= sourceReceiver.GetOutgoingDamageMultiplier();
            }

            bool affected = definition.HitMode == RpgHitMode.Damage
                ? receiver.TakeDamage(amount) > 0f
                : receiver.Heal(amount) > 0f;

            ApplyEffects(definition, sourceReceiver, receiver);

            if (definition.ImpactEffectPrefab != null)
            {
                Instantiate(definition.ImpactEffectPrefab, target.transform.position, Quaternion.identity);
            }

            return affected;
        }

        private void ApplyEffects(RpgAttackDefinition definition, IRpgCombatReceiver source, IRpgCombatReceiver target)
        {
            RpgAttackEffectRefs effects = definition.Effects;
            if (effects == null)
            {
                return;
            }

            IReadOnlyList<string> targetBuffs = effects.TargetBuffIds;
            for (int i = 0; i < targetBuffs.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(targetBuffs[i]))
                {
                    target.TryApplyBuff(targetBuffs[i], out _);
                }
            }

            IReadOnlyList<string> targetStatuses = effects.TargetStatusIds;
            for (int i = 0; i < targetStatuses.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(targetStatuses[i]))
                {
                    target.TryApplyStatus(targetStatuses[i], out _);
                }
            }

            if (source == null)
            {
                return;
            }

            IReadOnlyList<string> selfBuffs = effects.SelfBuffIds;
            for (int i = 0; i < selfBuffs.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(selfBuffs[i]))
                {
                    source.TryApplyBuff(selfBuffs[i], out _);
                }
            }
        }

        public bool CanUseAttack(RpgAttackDefinition definition, out string failReason)
        {
            failReason = null;
            if (definition == null)
            {
                failReason = "Attack definition not found.";
                return false;
            }

            if (IsCasting)
            {
                failReason = "Attack controller is already casting.";
                return false;
            }

            IRpgCombatReceiver sourceReceiver = ResolveSourceReceiver();
            if (sourceReceiver != null && !sourceReceiver.CanPerformActions)
            {
                failReason = "Source cannot perform actions right now.";
                return false;
            }

            if (GetRemainingCooldown(definition.Id) > 0f)
            {
                failReason = $"Attack '{definition.Id}' is on cooldown.";
                return false;
            }

            return true;
        }

        private RpgAttackDefinition ResolveAttack(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId))
            {
                return null;
            }

            for (int i = 0; i < _attacks.Length; i++)
            {
                if (_attacks[i] != null && string.Equals(_attacks[i].Id, attackId, StringComparison.Ordinal))
                {
                    return _attacks[i];
                }
            }

            return null;
        }

        private RpgAttackPreset ResolvePreset(string presetId)
        {
            if (string.IsNullOrWhiteSpace(presetId))
            {
                return null;
            }

            for (int i = 0; i < _presets.Length; i++)
            {
                if (_presets[i] != null && string.Equals(_presets[i].Id, presetId, StringComparison.Ordinal))
                {
                    return _presets[i];
                }
            }

            return null;
        }

        private IRpgCombatReceiver ResolveSourceReceiver()
        {
            if (_combatantSource != null)
            {
                return _combatantSource;
            }

            if (_profileSource != null)
            {
                return _profileSource;
            }

            return TryResolveReceiver(gameObject, out IRpgCombatReceiver receiver) ? receiver : null;
        }

        private Vector3 GetOriginPosition()
        {
            return _origin != null ? _origin.position : transform.position;
        }

        private Vector3 GetForwardDirection()
        {
            Transform source = _origin != null ? _origin : transform;
            return source.forward.sqrMagnitude > 0f ? source.forward.normalized : Vector3.forward;
        }

        private Vector3 GetAttackDirection(Vector3 origin, GameObject forcedTarget, bool aimAtTarget)
        {
            if (aimAtTarget && forcedTarget != null)
            {
                Vector3 directionToTarget = forcedTarget.transform.position - origin;
                if (directionToTarget.sqrMagnitude > 0.0001f)
                {
                    return directionToTarget.normalized;
                }
            }

            return GetForwardDirection();
        }

        private static bool TryResolveReceiver(GameObject target, out IRpgCombatReceiver receiver)
        {
            receiver = null;
            if (target == null)
            {
                return false;
            }

            receiver = target.GetComponent<RpgCombatant>();
            if (receiver != null)
            {
                return true;
            }

            receiver = target.GetComponent<RpgStatsManager>();
            return receiver != null;
        }

        private int ApplyHits3D(RaycastHit[] hits, RpgAttackDefinition definition, int remainingTargets)
        {
            int applied = 0;
            for (int i = 0; i < hits.Length && applied < remainingTargets; i++)
            {
                applied += ApplyHitToGameObject(hits[i].collider.gameObject, definition) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyHits2D(RaycastHit2D[] hits, RpgAttackDefinition definition, int remainingTargets)
        {
            int applied = 0;
            for (int i = 0; i < hits.Length && applied < remainingTargets; i++)
            {
                if (hits[i].collider == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(hits[i].collider.gameObject, definition) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyColliders3D(Collider[] colliders, RpgAttackDefinition definition, int remainingTargets)
        {
            int applied = 0;
            for (int i = 0; i < colliders.Length && applied < remainingTargets; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(colliders[i].gameObject, definition) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyColliders2D(Collider2D[] colliders, RpgAttackDefinition definition, int remainingTargets)
        {
            int applied = 0;
            for (int i = 0; i < colliders.Length && applied < remainingTargets; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(colliders[i].gameObject, definition) ? 1 : 0;
            }

            return applied;
        }

        private void EmitFailure(string message)
        {
            _onAttackFailed?.Invoke(string.IsNullOrWhiteSpace(message) ? "Attack failed." : message);
        }

        private bool BeginAttack(RpgAttackDefinition definition, GameObject forcedTarget, string eventId, bool aimAtTarget)
        {
            _cooldowns[definition.Id] = Time.time + definition.Cooldown;
            _onAttackStarted?.Invoke(eventId);

            if (definition.CastDelay > 0f)
            {
                _castCoroutine = StartCoroutine(CastAttackAfterDelay(definition, forcedTarget, eventId, aimAtTarget));
            }
            else
            {
                ExecuteAttack(definition, forcedTarget, aimAtTarget, eventId);
            }

            return true;
        }

        private bool TryResolveTargetForPreset(RpgAttackPreset preset, ref GameObject target, out string failReason)
        {
            failReason = null;

            if (target != null)
            {
                return true;
            }

            if (preset.UseSelectorComponentWhenAvailable && _targetSelector != null && _targetSelector.TrySelectTarget(out target))
            {
                return true;
            }

            target = RpgTargetingUtility.SelectTarget(_origin != null ? _origin : transform, preset.TargetQuery, ResolveReceiverFromGameObject);
            if (target != null)
            {
                return true;
            }

            if (preset.RequireTarget)
            {
                failReason = $"Preset '{preset.Id}' requires a target, but none was found.";
                return false;
            }

            return true;
        }

        private static IRpgCombatReceiver ResolveReceiverFromGameObject(GameObject target)
        {
            return TryResolveReceiver(target, out IRpgCombatReceiver receiver) ? receiver : null;
        }
    }
}
