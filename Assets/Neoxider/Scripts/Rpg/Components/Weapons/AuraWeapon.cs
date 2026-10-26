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
            Vector3 scale = transform.lossyScale;
            Collider[] hits = Physics.OverlapSphere(transform.TransformPoint(_auraCollider.center),
                _auraCollider.radius *
                Mathf.Max(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z)));
            _affectedCharacters.Clear();

            foreach (Collider hit in hits)
            {
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
    }
}
