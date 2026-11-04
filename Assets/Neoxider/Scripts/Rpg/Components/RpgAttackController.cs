using System;
using System.Collections;
using System.Collections.Generic;
using Neo.Rpg.Components;
using UnityEngine;

namespace Neo.Rpg
{
    /// <summary>
    ///     Universal attack caster that supports direct, area, and projectile attacks.
    /// </summary>
    [NeoDoc("Rpg/RpgAttackController.md")]
    [CreateFromMenu("Neoxider/RPG/RpgAttackController")]
    [AddComponentMenu("Neoxider/RPG/" + nameof(RpgAttackController))]
    public sealed class RpgAttackController : MonoBehaviour
    {
        [Header("Setup")] [SerializeField] private RpgAttackDefinition[] _attacks = Array.Empty<RpgAttackDefinition>();

        [SerializeField] private RpgAttackPreset[] _presets = Array.Empty<RpgAttackPreset>();
        [SerializeField] private Transform _origin;
        [SerializeField] private Transform _projectileSpawnPoint;

        [Tooltip("Optional RpgCharacter providing damage multipliers, level, and outgoing buff bonuses. " +
                 "When empty the controller searches the parent hierarchy.")]
        [SerializeField]
        private RpgCharacter _characterSource;

        [SerializeField] private RpgTargetSelector _targetSelector;

        [Header("Built-in Input")] [SerializeField]
        private bool _enableBuiltInInput = true;

        [SerializeField] private RpgButtonBinding _primaryAttackBinding = RpgButtonBinding.CreatePrimaryAttackDefault();

        [Header("Attack Speed")]
        [Tooltip("When enabled, the next cooldown is divided by the source RpgCharacter's attack speed multiplier " +
                 "(AttackSpeedPercent buffs). Already running cooldowns are not changed.")]
        [SerializeField]
        private bool _scaleCooldownByAttackSpeed;

        [Header("Events")] [SerializeField] private RpgAttackEvent _onAttackStarted = new();

        [SerializeField] private RpgAttackEvent _onAttackResolved = new();
        [SerializeField] private RpgAttackEvent _onPresetUsed = new();
        [SerializeField] private RpgGameObjectEvent _onTargetResolved = new();
        [SerializeField] private RpgStringEvent _onAttackFailed = new();

        private readonly Dictionary<string, float> _cooldowns = new(StringComparer.Ordinal);
        private readonly HashSet<IRpgCombatReceiver> _affectedReceivers = new();
        private RpgPhysicsQueryBuffers _queryBuffers;
        private bool _queryInUse;
        private Coroutine _castCoroutine;
        private bool _isStartingAttack;
        private int _castVersion;
        private IRpgAttackNetworkAdapter _networkAdapter;

        /// <summary>
        ///     Gets whether the controller is currently casting.
        /// </summary>
        public bool IsCasting => _isStartingAttack || _castCoroutine != null;

        /// <summary>
        ///     Gets or sets whether the built-in input listener is enabled.
        /// </summary>
        public bool EnableBuiltInInput
        {
            get => _enableBuiltInInput;
            set => _enableBuiltInInput = value;
        }

        /// <summary>
        ///     Gets or sets whether new cooldowns are divided by the source character's attack speed multiplier.
        /// </summary>
        public bool ScaleCooldownByAttackSpeed
        {
            get => _scaleCooldownByAttackSpeed;
            set => _scaleCooldownByAttackSpeed = value;
        }

        private void Update()
        {
            // WHY: the cheap press check runs first; the authority check resolves components and is only needed on a press.
            if (_enableBuiltInInput && _primaryAttackBinding != null && _primaryAttackBinding.IsPressedThisFrame() &&
                CanProcessBuiltInInput())
            {
                UsePrimaryAttack();
            }
        }

        private void OnDisable()
        {
            _castVersion++;
            _isStartingAttack = false;
            if (_castCoroutine != null)
            {
                StopCoroutine(_castCoroutine);
                _castCoroutine = null;
            }
        }

        /// <summary>
        ///     Uses the first configured attack.
        /// </summary>
        [Button]
        public bool UsePrimaryAttack()
        {
            return TryUseAttack(0, out _);
        }

        /// <summary>
        ///     Uses the first configured preset.
        /// </summary>
        [Button]
        public bool UsePrimaryPreset()
        {
            return TryUsePreset(0, out _);
        }

        /// <summary>
        ///     Tries to use an attack by index.
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
        ///     Tries to use an attack by identifier.
        /// </summary>
        public bool TryUseAttack(string attackId, out string failReason)
        {
            failReason = null;
            RpgAttackDefinition definition = ResolveAttack(attackId);
            if (!CanUseAttack(definition, out failReason))
            {
                EmitFailure(failReason);
                return false;
            }

            if (definition.CostAmount > 0f)
            {
                IRpgCombatReceiver source = ResolveSourceReceiver();
                if (source == null ||
                    !source.TrySpendResource(definition.CostResourceId, definition.CostAmount, out failReason))
                {
                    EmitFailure(failReason ?? "Not enough resource.");
                    return false;
                }
            }

            return BeginAttack(definition, null, attackId, true);
        }

        /// <summary>
        ///     Tries to use a preset by index.
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
        ///     Tries to use a preset by id.
        /// </summary>
        public bool TryUsePreset(string presetId, out string failReason)
        {
            RpgAttackPreset preset = ResolvePreset(presetId);
            return TryUseResolvedPreset(preset, null, out failReason);
        }

        /// <summary>
        ///     Tries to use a preset asset directly.
        /// </summary>
        public bool TryUsePreset(RpgAttackPreset preset, out string failReason)
        {
            return TryUseResolvedPreset(preset, null, out failReason);
        }

        /// <summary>
        ///     Tries to use a preset asset against a specific target.
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

            if (definition.CostAmount > 0f)
            {
                IRpgCombatReceiver source = ResolveSourceReceiver();
                if (source == null ||
                    !source.TrySpendResource(definition.CostResourceId, definition.CostAmount, out failReason))
                {
                    EmitFailure(failReason ?? "Not enough resource.");
                    return false;
                }
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
        ///     Returns whether an attack is currently available.
        /// </summary>
        public bool CanUseAttack(string attackId, out string failReason)
        {
            return CanUseAttack(ResolveAttack(attackId), out failReason);
        }

        /// <summary>
        ///     Returns whether a preset's underlying attack is currently available.
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
        ///     Cooldown the next use of <paramref name="definition" /> would start: the authored cooldown, divided by the
        ///     source character's attack speed multiplier when <see cref="ScaleCooldownByAttackSpeed" /> is enabled.
        /// </summary>
        public float GetEffectiveCooldown(RpgAttackDefinition definition)
        {
            if (definition == null)
            {
                return 0f;
            }

            float cooldown = definition.Cooldown;
            if (!_scaleCooldownByAttackSpeed || cooldown <= 0f ||
                !(ResolveSourceReceiver() is RpgCharacter character) || character == null)
            {
                return cooldown;
            }

            return cooldown / Mathf.Max(0.01f, character.GetAttackSpeedMultiplier());
        }

        /// <summary>
        ///     Returns remaining cooldown for an attack.
        /// </summary>
        public float GetRemainingCooldown(string attackId)
        {
            if (string.IsNullOrWhiteSpace(attackId) || !_cooldowns.TryGetValue(attackId, out float readyAt))
            {
                return 0f;
            }

            return Mathf.Max(0f, readyAt - Time.time);
        }

        private IEnumerator CastAttackAfterDelay(RpgAttackDefinition definition, GameObject forcedTarget,
            string eventId, bool aimAtTarget, int version)
        {
            yield return new WaitForSeconds(definition.CastDelay);
            if (version != _castVersion)
            {
                yield break;
            }

            try
            {
                ExecuteAttack(definition, forcedTarget, aimAtTarget, eventId);
            }
            finally
            {
                if (version == _castVersion)
                {
                    _castCoroutine = null;
                }
            }
        }

        private void ExecuteAttack(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget,
            string eventId)
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
            RpgPhysicsQueryBuffers buffers = RentQueryBuffers(out bool pooled);
            HashSet<IRpgCombatReceiver> affectedReceivers = pooled ? _affectedReceivers : new HashSet<IRpgCombatReceiver>();
            affectedReceivers.Clear();
            try
            {
                int hits = 0;
                if (definition.Use3D)
                {
                    if (definition.Radius > 0f)
                    {
                        int count = buffers.SphereCast(origin, definition.Radius, direction, definition.Range,
                            definition.TargetLayers);
                        hits += ApplyHits3D(buffers.Hits3D, count, definition, definition.MaxTargets,
                            affectedReceivers);
                    }
                    else if (Physics.Raycast(origin, direction, out RaycastHit hit, definition.Range,
                                 definition.TargetLayers))
                    {
                        hits += ApplyHitToGameObject(hit.collider.gameObject, definition, affectedReceivers) ? 1 : 0;
                    }
                }

                if (hits >= definition.MaxTargets || !definition.Use2D)
                {
                    return;
                }

                int count2D = definition.Radius > 0f
                    ? buffers.CircleCast2D(origin, definition.Radius, direction, definition.Range,
                        definition.TargetLayers)
                    : buffers.Raycast2D(origin, direction, definition.Range, definition.TargetLayers);
                RpgHitSort.Sort(buffers.Hits2D, buffers.Hits2D.Count, RpgHit2DDistanceComparer.Instance);
                ApplyHits2D(buffers.Hits2D, count2D, definition, definition.MaxTargets - hits, affectedReceivers);
            }
            finally
            {
                ReturnQueryBuffers(buffers, pooled, affectedReceivers);
            }
        }

        private void ExecuteAreaAttack(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget)
        {
            Vector3 center = forcedTarget != null && aimAtTarget
                ? forcedTarget.transform.position
                : GetOriginPosition() + GetAttackDirection(GetOriginPosition(), forcedTarget, aimAtTarget) *
                definition.Range;
            float radius = Mathf.Max(0.01f, definition.Radius);
            RpgPhysicsQueryBuffers buffers = RentQueryBuffers(out bool pooled);
            HashSet<IRpgCombatReceiver> affectedReceivers = pooled ? _affectedReceivers : new HashSet<IRpgCombatReceiver>();
            affectedReceivers.Clear();
            try
            {
                int hits = 0;
                if (definition.Use3D)
                {
                    int count = buffers.OverlapSphere(center, radius, definition.TargetLayers);
                    hits += ApplyColliders3D(buffers.Colliders3D, count, definition, definition.MaxTargets,
                        affectedReceivers);
                }

                if (hits >= definition.MaxTargets || !definition.Use2D)
                {
                    return;
                }

                int count2D = buffers.OverlapCircle2D(center, radius, definition.TargetLayers);
                ApplyColliders2D(buffers.Colliders2D, count2D, definition, definition.MaxTargets - hits,
                    affectedReceivers);
            }
            finally
            {
                ReturnQueryBuffers(buffers, pooled, affectedReceivers);
            }
        }

        private RpgPhysicsQueryBuffers RentQueryBuffers(out bool pooled)
        {
            // WHY: a hit callback may start another attack on this controller; nested queries get private buffers.
            pooled = !_queryInUse;
            if (!pooled)
            {
                return new RpgPhysicsQueryBuffers();
            }

            _queryInUse = true;
            return _queryBuffers ??= new RpgPhysicsQueryBuffers();
        }

        private void ReturnQueryBuffers(RpgPhysicsQueryBuffers buffers, bool pooled,
            HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            affectedReceivers.Clear();
            if (!pooled)
            {
                return;
            }

            buffers.Release();
            _queryInUse = false;
        }

        private void SpawnProjectile(RpgAttackDefinition definition, GameObject forcedTarget, bool aimAtTarget)
        {
            if (definition.ProjectilePrefab == null)
            {
                EmitFailure($"Projectile prefab is missing for attack '{definition.Id}'.");
                return;
            }

            IRpgAttackNetworkAdapter networkAdapter = ResolveNetworkAdapter();
            GameObject source = ResolveSourceGameObject();
            if (networkAdapter != null && networkAdapter.ShouldBlockProjectileSpawn(source))
            {
                EmitFailure("Networked projectile attacks must be executed by the server.");
                return;
            }

            Transform spawn = _projectileSpawnPoint != null ? _projectileSpawnPoint :
                _origin != null ? _origin : transform;
            Vector3 direction = GetAttackDirection(spawn.position, forcedTarget, aimAtTarget);
            RpgProjectile projectile = Instantiate(definition.ProjectilePrefab, spawn.position,
                Quaternion.LookRotation(direction));
            projectile.Initialize(this, definition, ResolveSourceReceiver(), direction);

            if (networkAdapter != null)
            {
                networkAdapter.SpawnProjectile(projectile.gameObject);
            }
        }

        internal bool ApplyHitToGameObject(GameObject target, RpgAttackDefinition definition)
        {
            return ApplyHitToGameObject(target, definition, null);
        }

        internal bool ApplyHitToGameObject(GameObject target, RpgAttackDefinition definition,
            HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            if (!TryResolveReceiver(target, out IRpgCombatReceiver receiver))
            {
                return false;
            }

            IRpgCombatReceiver sourceReceiver = ResolveSourceReceiver();
            if (ReferenceEquals(sourceReceiver, receiver))
            {
                return false;
            }

            if (sourceReceiver is Component sourceComponent && sourceComponent.gameObject == target)
            {
                return false;
            }

            if (affectedReceivers != null && !affectedReceivers.Add(receiver))
            {
                return false;
            }

            float amount = definition.Power;
            if (sourceReceiver != null && definition.HitMode == RpgHitMode.Damage)
            {
                amount *= sourceReceiver.GetOutgoingDamageMultiplier();
            }

            bool affected = definition.HitMode == RpgHitMode.Damage
                ? receiver.TakeDamage(new RpgDamageInfo(amount, definition.DamageType, sourceReceiver)) > 0f
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

            if (!isActiveAndEnabled)
            {
                failReason = "Attack controller is inactive.";
                return false;
            }

            if (definition.DeliveryType == RpgAttackDeliveryType.Projectile)
            {
                if (definition.ProjectilePrefab == null)
                {
                    failReason = $"Projectile prefab is missing for attack '{definition.Id}'.";
                    return false;
                }

                IRpgAttackNetworkAdapter networkAdapter = ResolveNetworkAdapter();
                if (networkAdapter != null && networkAdapter.ShouldBlockProjectileSpawn(ResolveSourceGameObject()))
                {
                    failReason = "Networked projectile attacks must be executed by the server.";
                    return false;
                }
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
            if (_characterSource != null)
            {
                return _characterSource;
            }

            if (TryResolveReceiver(gameObject, out IRpgCombatReceiver receiver))
            {
                return receiver;
            }

            return GetComponentInParent<IRpgCombatReceiver>();
        }

        private GameObject ResolveSourceGameObject()
        {
            if (_characterSource != null)
            {
                return _characterSource.gameObject;
            }

            if (TryResolveReceiver(gameObject, out IRpgCombatReceiver receiver) && receiver is Component component)
            {
                return component.gameObject;
            }

            IRpgCombatReceiver parentReceiver = GetComponentInParent<IRpgCombatReceiver>();
            return parentReceiver is Component parentComponent ? parentComponent.gameObject : gameObject;
        }

        private bool CanProcessBuiltInInput()
        {
            IRpgAttackNetworkAdapter networkAdapter = ResolveNetworkAdapter();
            return networkAdapter == null || networkAdapter.CanProcessBuiltInInput(ResolveSourceGameObject());
        }

        private IRpgAttackNetworkAdapter ResolveNetworkAdapter()
        {
            if (_networkAdapter is UnityEngine.Object adapterObject && adapterObject == null)
            {
                _networkAdapter = null;
            }

            if (_networkAdapter != null)
            {
                return _networkAdapter;
            }

            // WHY: TryGetComponent with the interface type allocates neither an array nor an editor fake-null.
            _networkAdapter = TryGetComponent(out IRpgAttackNetworkAdapter adapter) ? adapter : null;
            return _networkAdapter;
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

        internal static bool TryResolveReceiver(GameObject target, out IRpgCombatReceiver receiver)
        {
            receiver = null;
            if (target == null)
            {
                return false;
            }

            // WHY: TryGetComponent never yields the editor-only fake-null that would defeat the fallbacks below.
            if (target.TryGetComponent(out RpgCharacter character))
            {
                receiver = character;
                return true;
            }

            character = target.GetComponentInParent<RpgCharacter>();
            if (character != null)
            {
                receiver = character;
                return true;
            }

            if (target.TryGetComponent(out IRpgCombatReceiver direct))
            {
                receiver = direct;
                return true;
            }

            receiver = target.GetComponentInChildren<IRpgCombatReceiver>() ??
                       target.GetComponentInParent<IRpgCombatReceiver>();
            return receiver != null;
        }

        private int ApplyHits3D(RaycastHit[] hits, int count, RpgAttackDefinition definition, int remainingTargets,
            HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            int applied = 0;
            for (int i = 0; i < count && applied < remainingTargets; i++)
            {
                Collider hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(hitCollider.gameObject, definition, affectedReceivers) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyHits2D(List<RaycastHit2D> hits, int count, RpgAttackDefinition definition,
            int remainingTargets, HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            int applied = 0;
            for (int i = 0; i < count && i < hits.Count && applied < remainingTargets; i++)
            {
                Collider2D hitCollider = hits[i].collider;
                if (hitCollider == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(hitCollider.gameObject, definition, affectedReceivers) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyColliders3D(Collider[] colliders, int count, RpgAttackDefinition definition,
            int remainingTargets, HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            int applied = 0;
            for (int i = 0; i < count && applied < remainingTargets; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(colliders[i].gameObject, definition, affectedReceivers) ? 1 : 0;
            }

            return applied;
        }

        private int ApplyColliders2D(List<Collider2D> colliders, int count, RpgAttackDefinition definition,
            int remainingTargets, HashSet<IRpgCombatReceiver> affectedReceivers)
        {
            int applied = 0;
            for (int i = 0; i < count && i < colliders.Count && applied < remainingTargets; i++)
            {
                if (colliders[i] == null)
                {
                    continue;
                }

                applied += ApplyHitToGameObject(colliders[i].gameObject, definition, affectedReceivers) ? 1 : 0;
            }

            return applied;
        }

        private void EmitFailure(string message)
        {
            _onAttackFailed?.Invoke(string.IsNullOrWhiteSpace(message) ? "Attack failed." : message);
        }

        private bool BeginAttack(RpgAttackDefinition definition, GameObject forcedTarget, string eventId,
            bool aimAtTarget)
        {
            int version = ++_castVersion;
            _isStartingAttack = true;
            _cooldowns[definition.Id] = Time.time + GetEffectiveCooldown(definition);
            try
            {
                _onAttackStarted?.Invoke(eventId);
                if (version != _castVersion || !isActiveAndEnabled)
                {
                    return true;
                }

                if (definition.CastDelay > 0f)
                {
                    _castCoroutine = StartCoroutine(CastAttackAfterDelay(definition, forcedTarget, eventId,
                        aimAtTarget, version));
                }
                else
                {
                    ExecuteAttack(definition, forcedTarget, aimAtTarget, eventId);
                }
            }
            finally
            {
                if (version == _castVersion)
                {
                    _isStartingAttack = false;
                }
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

            if (preset.UseSelectorComponentWhenAvailable && _targetSelector != null &&
                _targetSelector.TrySelectTarget(out target))
            {
                return true;
            }

            target = RpgTargetingUtility.SelectTarget(_origin != null ? _origin : transform, preset.TargetQuery,
                ResolveReceiver, ResolveSourceReceiver());
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

        // WHY: cached; a method-group argument would allocate a new delegate on every preset target search.
        private static readonly Func<GameObject, IRpgCombatReceiver> ResolveReceiver = ResolveReceiverFromGameObject;

        private static IRpgCombatReceiver ResolveReceiverFromGameObject(GameObject target)
        {
            return TryResolveReceiver(target, out IRpgCombatReceiver receiver) ? receiver : null;
        }
    }
}
