using UnityEngine;

namespace Neo.NPC
{
    /// <summary>
    /// Automatically finds a target by tag or name and assigns it to NpcNavigation.
    /// </summary>
    [RequireComponent(typeof(NpcNavigation))]
    [AddComponentMenu("Neoxider/NPC/NpcTargetFinder")]
    [NeoDoc("NPC/NpcTargetFinder.md")]
    public class NpcTargetFinder : MonoBehaviour
    {
        private const float DefaultRetryInterval = 1f;

        [Header("Override")]
        [Tooltip("Explicit target. When assigned, scene search by tag/name is skipped.")]
        [SerializeField]
        private Transform _targetOverride;

        [Header("Search Settings")] [SerializeField]
        private bool _findByTag = true;

        [SerializeField] private string _targetTag = "Player";

        [SerializeField] private bool _findByName = false;
        [SerializeField] private string _targetName = "Player";

        [Header("Behavior")] [SerializeField] private bool _setModeToFollowOnFind = true;
        [SerializeField] private bool _findOnAwake = true;
        [SerializeField] [Min(0f)] private float _retryInterval = DefaultRetryInterval;

        [Header("Diagnostics")] [SerializeField]
        private bool _debugLogMissingTarget;

        private NpcNavigation _navigation;
        private float _nextSearchTime;
        private bool _hasWarnedMissingTarget;

        public Transform TargetOverride
        {
            get => _targetOverride;
            set => _targetOverride = value;
        }

        private void Awake()
        {
            _navigation = GetComponent<NpcNavigation>();
            if (_findOnAwake)
            {
                FindAndSetTarget();
            }
        }

        /// <summary>
        /// Forces to find target using configured rules and applies it to NpcNavigation.
        /// </summary>
        public void FindAndSetTarget()
        {
            if (_targetOverride != null)
            {
                ApplyTarget(_targetOverride);
                return;
            }

            if (_retryInterval > 0f && Time.realtimeSinceStartup < _nextSearchTime)
            {
                return;
            }

            _nextSearchTime = Time.realtimeSinceStartup + Mathf.Max(0f, _retryInterval);
            Transform target = null;

            if (_findByTag && !string.IsNullOrEmpty(_targetTag))
            {
                var targetObj = GameObject.FindGameObjectWithTag(_targetTag);
                if (targetObj != null)
                {
                    target = targetObj.transform;
                }
            }

            if (target == null && _findByName && !string.IsNullOrEmpty(_targetName))
            {
                var targetObj = GameObject.Find(_targetName);
                if (targetObj != null)
                {
                    target = targetObj.transform;
                }
            }

            if (target != null)
            {
                ApplyTarget(target);
                return;
            }

            if (_debugLogMissingTarget && !_hasWarnedMissingTarget)
            {
                _hasWarnedMissingTarget = true;
                NeoDiagnostics.LogWarning($"[NpcTargetFinder] Target not found on '{name}'.", this);
            }
        }

        private void ApplyTarget(Transform target)
        {
            if (target == null)
            {
                return;
            }

            _navigation ??= GetComponent<NpcNavigation>();
            if (_navigation == null)
            {
                return;
            }

            _hasWarnedMissingTarget = false;
            _navigation.SetFollowTarget(target);

            if (_setModeToFollowOnFind)
            {
                _navigation.SetMode(NpcNavigation.NavigationMode.FollowTarget);
            }
        }
    }
}
