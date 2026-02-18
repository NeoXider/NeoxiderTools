using System.Collections.Generic;
using System.Linq;
using Neo;
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
    [NeoDoc("Tools/Managers/Bootstrap.md")]
    [CreateFromMenu("Neoxider/Tools/Bootstrap")]
    [AddComponentMenu("Neoxider/" + "Tools/" + nameof(Bootstrap))]
    public class Bootstrap : Singleton<Bootstrap>
    {
        [Header("References")] [SerializeField] [Tooltip("List of components to initialize manually")]
        private List<MonoBehaviour> _manualInitializables = new();

        [Header("Settings")]
        [SerializeField]
        [Tooltip("If true, automatically finds and initializes all IInit components in the scene")]
        private bool _autoFindComponents;

        private readonly List<IInit> _initializables = new();

        protected override bool DontDestroyOnLoadEnabled => true;

        /// <summary>
        ///     Initializes all components in priority order
        /// </summary>
        /// <remarks>
        ///     First initializes manual components, then finds and initializes automatic components if enabled.
        ///     Components are sorted by priority before initialization.
        /// </remarks>
        protected override void Init()
        {
            base.Init();

            // First initialize manual components
            foreach (MonoBehaviour component in _manualInitializables)
            {
                if (component is IInit initializable)
                {
                    _initializables.Add(initializable);
                }
            }

            // Then find other components if auto-find is enabled
            if (_autoFindComponents)
            {
                MonoBehaviour[] components =
                    FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                foreach (MonoBehaviour component in components)
                {
                    if (component is IInit initializable && !_initializables.Contains(initializable))
                    {
                        _initializables.Add(initializable);
                    }
                }
            }

            // Sort by priority and initialize
            List<IInit> sortedInitializables = _initializables.OrderByDescending(x => x.InitPriority).ToList();
            foreach (IInit initializable in sortedInitializables)
            {
                initializable.Init();
            }
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