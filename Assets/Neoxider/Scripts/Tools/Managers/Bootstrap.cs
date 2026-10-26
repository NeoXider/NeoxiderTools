using System.Collections.Generic;
using System.Linq;
using Neo.Extensions;
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
    [CreateFromMenu("Neoxider/Tools/Managers/Bootstrap")]
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
        private readonly HashSet<IInit> _initializedInitializables = new();
        private bool _bootstrapCompleted;
        private bool _isInitializing;

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

            CollectInitializables();
            InitializePendingRegistrations();
            _bootstrapCompleted = true;
        }

        /// <summary>
        ///     Registers a component for bootstrap initialization.
        /// </summary>
        /// <param name="initializable">Component implementing <see cref="IInit" />.</param>
        /// <remarks>
        ///     If bootstrap has already completed its first pass, the component is initialized
        ///     through the same priority-based pipeline as startup registrations.
        /// </remarks>
        public void Register(IInit initializable)
        {
            if (!IsAlive(initializable) || _initializables.Contains(initializable))
            {
                return;
            }

            _initializables.Add(initializable);
            if (_bootstrapCompleted)
            {
                InitializePendingRegistrations();
            }
        }

        /// <summary>
        ///     Removes a component from bootstrap tracking.
        /// </summary>
        /// <param name="initializable">Component implementing <see cref="IInit" />.</param>
        public void Unregister(IInit initializable)
        {
            if (initializable == null)
            {
                return;
            }

            _initializables.Remove(initializable);
            _initializedInitializables.Remove(initializable);
        }

        private void CollectInitializables()
        {
            foreach (MonoBehaviour component in _manualInitializables)
            {
                if (component != null && component is IInit initializable && !_initializables.Contains(initializable))
                {
                    _initializables.Add(initializable);
                }
            }

            if (!_autoFindComponents)
            {
                return;
            }

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

        private void InitializePendingRegistrations()
        {
            if (_isInitializing)
            {
                return;
            }

            _isInitializing = true;
            try
            {
                // Registrations made by Init are drained in a later batch, without re-entering Init.
                while (true)
                {
                    _initializables.RemoveAll(initializable => !IsAlive(initializable));
                    _initializedInitializables.RemoveWhere(initializable => !IsAlive(initializable));
                    List<IInit> sortedInitializables = _initializables
                        .Where(initializable => !_initializedInitializables.Contains(initializable))
                        .OrderByDescending(initializable => initializable.InitPriority)
                        .ToList();

                    if (sortedInitializables.Count == 0)
                    {
                        return;
                    }

                    for (int i = 0; i < sortedInitializables.Count; i++)
                    {
                        IInit initializable = sortedInitializables[i];
                        if (!IsAlive(initializable) || !_initializables.Contains(initializable) ||
                            _initializedInitializables.Contains(initializable))
                        {
                            continue;
                        }

                        initializable.Init();
                        if (_initializables.Contains(initializable))
                        {
                            _initializedInitializables.Add(initializable);
                        }
                    }
                }
            }
            finally
            {
                _isInitializing = false;
            }
        }

        private static bool IsAlive(IInit initializable)
        {
            return initializable != null &&
                   (!(initializable is Object unityObject) || unityObject.IsValid());
        }
    }
}
