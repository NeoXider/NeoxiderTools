using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Neo.Tools
{
    /// <summary>
    ///     Interface for components that require initialization in a specific order
    /// </summary>
    public interface IInit
    {
        /// <summary>
        ///     Priority of initialization. Components with higher priority are initialized first
        /// </summary>
        /// <value>Integer value representing initialization priority</value>
        int InitPriority { get; }

        /// <summary>
        ///     Called when the component should initialize itself
        /// </summary>
        void Init();
    }

    /// <summary>
    ///     Manages initialization of game components in a specific order
    /// </summary>
    /// <remarks>
    ///     This class handles both manual and automatic component initialization.
    ///     Components can be added manually through the inspector or found automatically in the scene.
    /// </remarks>
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] [Tooltip("List of components to initialize manually")]
        private List<MonoBehaviour> _manualInitializables = new();

        [SerializeField] [Tooltip("If true, automatically finds and initializes all IInit components in the scene")]
        private bool _autoFindComponents;

        private readonly List<IInit> _initializables = new();

        private void Awake()
        {
            DontDestroyOnLoad(gameObject);
            InitializeComponents();
        }

        /// <summary>
        ///     Initializes all components in priority order
        /// </summary>
        /// <remarks>
        ///     First initializes manual components, then finds and initializes automatic components if enabled.
        ///     Components are sorted by priority before initialization.
        /// </remarks>
        private void InitializeComponents()
        {
            // First initialize manual components
            foreach (var component in _manualInitializables)
                if (component is IInit initializable)
                    _initializables.Add(initializable);

            // Then find other components if auto-find is enabled
            if (_autoFindComponents)
            {
                var components = FindObjectsOfType<MonoBehaviour>();
                foreach (var component in components)
                    if (component is IInit initializable && !_initializables.Contains(initializable))
                        _initializables.Add(initializable);
            }

            // Sort by priority and initialize
            var sortedInitializables = _initializables.OrderByDescending(x => x.InitPriority).ToList();
            foreach (var initializable in sortedInitializables) initializable.Init();
        }

        /// <summary>
        ///     Registers a component for initialization
        /// </summary>
        /// <param name="initializable">Component implementing IInit interface</param>
        /// <remarks>
        ///     If the component is not already registered, adds it to the initialization list and initializes it immediately
        /// </remarks>
        public void Register(IInit initializable)
        {
            if (!_initializables.Contains(initializable))
            {
                _initializables.Add(initializable);
                initializable.Init();
            }
        }
    }
}