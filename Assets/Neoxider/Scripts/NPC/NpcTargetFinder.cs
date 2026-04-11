using UnityEngine;

namespace Neo.NPC
{
    /// <summary>
    /// Automatically finds a target by tag or name and assigns it to NpcNavigation.
    /// </summary>
    [RequireComponent(typeof(NpcNavigation))]
    [AddComponentMenu("Neoxider/NPC/NpcTargetFinder")]
    public class NpcTargetFinder : MonoBehaviour
    {
        [Header("Search Settings")]
        [SerializeField] private bool _findByTag = true;
        [SerializeField] private string _targetTag = "Player";
        
        [SerializeField] private bool _findByName = false;
        [SerializeField] private string _targetName = "Player";

        [Header("Behavior")]
        [SerializeField] private bool _setModeToFollowOnFind = true;
        [SerializeField] private bool _findOnAwake = true;

        private void Awake()
        {
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
            GameObject targetObj = null;

            if (_findByTag && !string.IsNullOrEmpty(_targetTag))
            {
                targetObj = GameObject.FindGameObjectWithTag(_targetTag);
            }
            
            if (targetObj == null && _findByName && !string.IsNullOrEmpty(_targetName))
            {
                targetObj = GameObject.Find(_targetName);
            }

            if (targetObj != null)
            {
                var nav = GetComponent<NpcNavigation>();
                nav.SetFollowTarget(targetObj.transform);
                
                if (_setModeToFollowOnFind)
                {
                    nav.SetMode(NpcNavigation.NavigationMode.FollowTarget);
                }
            }
        }
    }
}
