using System.Collections.Generic;
using UnityEngine;
using Neo.Tools;

namespace Neo.Rpg.Components.Weapons
{
    /// <summary>
    /// An aura weapon that damages nearby enemies on a timer.
    /// Can be linked to a TimerObject's UnityEvent or driven interally.
    /// </summary>
    [NeoDoc("Rpg/Components/Weapons/AuraWeapon.md")]
    [RequireComponent(typeof(SphereCollider))]
    public class AuraWeapon : MeleeWeapon
    {
        private SphereCollider _auraCollider;
        private TimerObject _timer;
        private readonly HashSet<RpgCharacter> _affectedCharacters = new();
        private readonly RpgPhysicsQueryBuffers _queryBuffers = new();
        private bool _applying;

        private void Awake()
        {
            _auraCollider = GetComponent<SphereCollider>();
            _auraCollider.isTrigger = true;

            _timer = GetComponent<TimerObject>();
            if (_timer != null)
            {
                _timer.OnTimerCompleted.AddListener(ApplyAuraDamage);
            }
        }

        private void OnDestroy()
        {
            if (_timer != null)
            {
                _timer.OnTimerCompleted.RemoveListener(ApplyAuraDamage);
            }
        }

        /// <summary>
        /// Applies damage to all valid targets currently within the aura's trigger bounds.
        /// Call this from a TimerObject's OnTimerCompleted event (set Timer to loop).
        /// </summary>
        public void ApplyAuraDamage()
        {
            // WHY: a damage callback (death, pooling) may pulse the aura again; the nested pulse is skipped so the
            // shared query buffer and per-pulse character set stay valid for the running one.
            if (_applying)
            {
                return;
            }

            if (_auraCollider == null)
            {
                _auraCollider = GetComponent<SphereCollider>();
                if (_auraCollider == null)
                {
                    return;
                }
            }

            Vector3 scale = transform.lossyScale;
            // WHY: three-argument Mathf.Max is the params overload and allocated an array on every pulse.
            float radius = _auraCollider.radius *
                           Mathf.Max(Mathf.Abs(scale.x), Mathf.Max(Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            _applying = true;
            try
            {
                // WHY: hitLayers filters in the query itself; IsValidTarget keeps the identical check for safety.
                int count = _queryBuffers.OverlapSphere(transform.TransformPoint(_auraCollider.center), radius,
                    hitLayers);
                Collider[] hits = _queryBuffers.Colliders3D;
                _affectedCharacters.Clear();

                for (int i = 0; i < count; i++)
                {
                    Collider hit = hits[i];
                    if (hit == null || hit.gameObject == gameObject)
                    {
                        continue;
                    }

                    if (IsValidTarget(hit))
                    {
                        RpgCharacter character = hit.GetComponentInParent<RpgCharacter>();
                        if (character != null && _affectedCharacters.Add(character))
                        {
                            DealDamage(hit);
                        }
                    }
                }
            }
            finally
            {
                _affectedCharacters.Clear();
                _queryBuffers.Release();
                _applying = false;
            }
        }
    }
}
