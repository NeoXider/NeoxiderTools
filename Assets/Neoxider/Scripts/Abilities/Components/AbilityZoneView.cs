using System;
using Neo.Tools;
using UnityEngine;

namespace Neo.Abilities
{
    /// <summary>
    ///     Visual of a domain zone (aura or formation) spawned by the hub for a zone archetype: scales to the zone
    ///     radius, follows the zone's anchor unit and returns to the pool when the zone ends (zone_expired) or its
    ///     lifetime runs out. Gameplay lives in the domain zone; this is presentation only.
    /// </summary>
    [NeoDoc("Abilities/AbilitySystemBehaviour.md")]
    [AddComponentMenu("Neoxider/Abilities/Ability Zone View")]
    public sealed class AbilityZoneView : MonoBehaviour, ISpawnedAbilityEntity
    {
        [Tooltip("Scale the object so the mesh matches the zone radius.")]
        [SerializeField] private bool _scaleToRadius = true;

        [Tooltip("Ground XZ: a 3D floor disc (scales X and Z). Screen XY: 2D sprites facing the camera (scales X and Y).")]
        [SerializeField] private AbilityZonePlane _plane = AbilityZonePlane.GroundXZ;

        [Tooltip("Radius of the mesh at scale 1 (0.5 for Unity's cylinder and quad primitives).")]
        [SerializeField] [Min(0.001f)] private float _meshRadius = 0.5f;

        [Tooltip("Scale of the axis that is not scaled to the radius (Y on the ground, Z on screen).")]
        [SerializeField] private float _heightScale = 1f;

        [Tooltip("Offset from the zone centre (e.g. a little above the ground).")]
        [SerializeField] private Vector3 _offset = new Vector3(0f, 0.02f, 0f);

        private AbilitySystemBehaviour _hub;
        private Action<AbilityEventArgs> _onExpired;
        private int _zoneId;
        private UnitId _anchor;
        private float _remaining;
        private bool _active;

        /// <summary>Zone this view shows (0 when idle).</summary>
        public int ZoneId => _active ? _zoneId : 0;

        public void OnSpawned(SpawnRequest request, AbilitySystemBehaviour hub)
        {
            Unsubscribe();
            _hub = hub;
            _zoneId = request.ZoneId;
            _anchor = request.TargetUnit;
            _remaining = request.Duration;
            _active = true;
            transform.position = request.Position + _offset;
            if (_scaleToRadius && request.Magnitude > 0f)
            {
                float scale = request.Magnitude / Mathf.Max(0.001f, _meshRadius);
                transform.localScale = _plane == AbilityZonePlane.ScreenXY
                    ? new Vector3(scale, scale, _heightScale)
                    : new Vector3(scale, _heightScale, scale);
            }

            if (_zoneId != 0 && hub != null)
            {
                _onExpired ??= HandleEvent;
                hub.System.Events.Subscribe(AbilityEvents.ZoneExpired, _onExpired);
            }
        }

        private void Update()
        {
            if (!_active)
            {
                return;
            }

            if (_anchor.IsValid && _hub != null && _hub.TryGetPosition(_anchor, out Vector3 position))
            {
                transform.position = position + _offset;
            }

            if (_remaining > 0f)
            {
                _remaining -= Time.deltaTime;
                if (_remaining <= 0f)
                {
                    Despawn();
                }
            }
        }

        private void HandleEvent(AbilityEventArgs args)
        {
            if (_active && args.CastId == (uint)_zoneId)
            {
                Despawn();
            }
        }

        private void Despawn()
        {
            _active = false;
            Unsubscribe();
            PoolManager.Release(gameObject);
        }

        private void OnDisable()
        {
            _active = false;
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            AbilitySystemBehaviour hub = AbilitySystemBehaviour.InstanceOrNull;
            if (_onExpired != null && hub != null && hub == _hub)
            {
                hub.System.Events.Unsubscribe(AbilityEvents.ZoneExpired, _onExpired);
            }

            _hub = null;
        }
    }

    /// <summary>Rotation that faces a direction in a <see cref="AbilityZonePlane" />.</summary>
    public static class AbilityZonePlaneFacing
    {
        /// <summary>
        ///     GroundXZ: forward (Z) along <paramref name="direction" />. ScreenXY: a turn around Z so right (+X) points
        ///     along the direction's XY part. Identity for a zero direction.
        /// </summary>
        public static Quaternion Facing(Vector3 direction, AbilityZonePlane plane)
        {
            if (plane == AbilityZonePlane.ScreenXY)
            {
                if (direction.x * direction.x + direction.y * direction.y < 0.000001f)
                {
                    return Quaternion.identity;
                }

                return Quaternion.Euler(0f, 0f, Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg);
            }

            return direction.sqrMagnitude > 0.000001f ? Quaternion.LookRotation(direction.normalized) : Quaternion.identity;
        }
    }

    /// <summary>Plane a zone visual lies in.</summary>
    public enum AbilityZonePlane
    {
        /// <summary>3D ground: the visual scales on X and Z.</summary>
        GroundXZ = 0,

        /// <summary>2D screen plane: the visual scales on X and Y.</summary>
        ScreenXY = 1
    }
}
