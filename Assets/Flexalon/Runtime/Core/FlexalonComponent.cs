using UnityEngine;
using UnityEngine.Serialization;

namespace Flexalon
{
    /// <summary>
    /// Base type for many Flexalon components. Deals with FlexalonNode lifecycle,
    /// and provides the ForceUpdate and MarkDirty methods to trigger a Flexalon update.
    /// </summary>
    [ExecuteAlways, RequireComponent(typeof(FlexalonResult))]
    public abstract class FlexalonComponent : MonoBehaviour
    {
        private FlexalonNode _node;

        /// <summary> The FlexalonNode associated with this gameObject. </summary>
        public FlexalonNode Node => _node ??= Flexalon.GetOrCreateNode(gameObject);

        [SerializeField, HideInInspector, FormerlySerializedAs("_initialized")]
        private int _version;

        private static readonly int _currentVersion = 4;

        void Update()
        {
            DoUpdate();
        }

        void OnEnable()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);

            DoOnEnable();

            if (_version == 0)
            {
                Initialize();
            }
            else if (_version < _currentVersion)
            {
                Upgrade(_version);
            }

            if (!_node.HasResult || _version == 0)
            {
                MarkDirty();
            }
            else
            {
                UpdateProperties();
            }

            _version = _currentVersion;
        }

        void OnDisable()
        {
            DoOnDisable();
        }

        void OnDestroy()
        {
            if (_node != null)
            {
                ResetProperties();
                Flexalon.RecordFrameChanges = true;
                _node.MarkDirty();
                _node = null;
            }
        }

        /// <summary> Marks this component needing an update. The Flexalon singleton
        /// will visit it in dependency order on LateUpdate. </summary>
        public void MarkDirty()
        {
            if (_node != null)
            {
                UpdateProperties();
                _node.MarkDirty();
            }
        }

        /// <summary> Forces this component, its parent nodes, and its children nodes to update immediately. </summary>
        public void ForceUpdate()
        {
            _node = Flexalon.GetOrCreateNode(gameObject);
            MarkDirty();
            _node.ForceUpdate();
        }

        void OnDidApplyAnimationProperties()
        {
            MarkDirty();
        }

        /// <summary> Called when the component is enabled to apply properties to the FlexalonNode. </summary>
        protected virtual void UpdateProperties() {}

        /// <summary> Called when the component is destroyed to reset properties on the FlexalonNode. </summary>
        protected virtual void ResetProperties() {}

        /// <summary> Called when the component is enabled. </summary>
        protected virtual void DoOnEnable() {}

        /// <summary> Called when the component is disabled. </summary>
        protected virtual void DoOnDisable() {}

        /// <summary> Called when the component is updated. </summary>
        public virtual void DoUpdate() {}

        /// <summary> Called when the component is first created. </summary>
        protected virtual void Initialize() {}

        /// <summary> Called when the component is upgraded to a new version of Flexalon. </summary>
        protected virtual void Upgrade(int fromVersion) {}
    }
}