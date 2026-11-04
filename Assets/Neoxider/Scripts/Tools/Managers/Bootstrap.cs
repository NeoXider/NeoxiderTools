using System;
using System.Collections.Generic;
using Neo.Extensions;
using UnityEngine;
using Object = UnityEngine.Object;

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

        private static readonly Comparison<PendingInit> PendingInitComparison = ComparePendingInit;

        private readonly List<IInit> _initializables = new();
        private readonly HashSet<IInit> _initializedInitializables = new();
        private readonly HashSet<IInit> _failedInitializables = new();
        private readonly List<PendingInit> _pendingBatch = new();
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

            try
            {
                CollectInitializables();
                InitializePendingRegistrations();
            }
            finally
            {
                // WHY: a throwing IInit.Init must not leave the startup pass "in progress" forever; the failing
                // target is quarantined, so later Register calls initialize new targets without retrying it.
                _bootstrapCompleted = true;
            }
        }

        /// <summary>
        ///     <see langword="true" /> once the startup pass has run (even if one of its targets threw).
        ///     From then on <see cref="Register" /> initializes new targets immediately.
        /// </summary>
        public bool IsBootstrapCompleted => _bootstrapCompleted;

        /// <summary>
        ///     Initializes every registered target that has not been initialized yet, in priority order, and retries
        ///     the targets whose <see cref="IInit.Init" /> threw.
        /// </summary>
        /// <remarks>
        ///     A target whose <see cref="IInit.Init" /> throws is quarantined: automatic drains (the startup pass and
        ///     <see cref="Register" />) skip it, so an unrelated registration never re-runs it or rethrows its
        ///     exception. Only this explicit call retries it (or <see cref="Unregister" /> followed by a new
        ///     <see cref="Register" />). The exception of a retried target propagates again. Calls made from inside an
        ///     <see cref="IInit.Init" /> are folded into the running pass.
        /// </remarks>
        public void InitializePending()
        {
            _failedInitializables.Clear();
            InitializePendingRegistrations();
        }

        /// <summary>
        ///     <see langword="true" /> when the target's last <see cref="IInit.Init" /> threw and it waits for
        ///     <see cref="InitializePending" /> (or a new registration) to be retried.
        /// </summary>
        /// <param name="initializable">Registered target.</param>
        public bool HasFailed(IInit initializable)
        {
            return initializable != null && _failedInitializables.Contains(initializable);
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
            _failedInitializables.Remove(initializable);
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
                // WHY: registrations made by Init are drained in a later batch, without re-entering Init.
                while (true)
                {
                    _initializables.RemoveAll(initializable => !IsAlive(initializable));
                    _initializedInitializables.RemoveWhere(initializable => !IsAlive(initializable));
                    _failedInitializables.RemoveWhere(initializable => !IsAlive(initializable));
                    _pendingBatch.Clear();
                    for (int i = 0; i < _initializables.Count; i++)
                    {
                        IInit candidate = _initializables[i];
                        if (!_initializedInitializables.Contains(candidate) &&
                            !_failedInitializables.Contains(candidate))
                        {
                            _pendingBatch.Add(new PendingInit(candidate, candidate.InitPriority, i));
                        }
                    }

                    if (_pendingBatch.Count == 0)
                    {
                        return;
                    }

                    // WHY: priority descending, registration order for ties (the stable order callers rely on).
                    _pendingBatch.Sort(PendingInitComparison);
                    for (int i = 0; i < _pendingBatch.Count; i++)
                    {
                        IInit initializable = _pendingBatch[i].Target;
                        if (!IsAlive(initializable) || !_initializables.Contains(initializable) ||
                            _initializedInitializables.Contains(initializable) ||
                            _failedInitializables.Contains(initializable))
                        {
                            continue;
                        }

                        try
                        {
                            initializable.Init();
                        }
                        catch
                        {
                            // WHY: fail-fast stays (the exception reaches this pass's caller), but the target is
                            // quarantined so the next automatic drain neither re-runs its side effects nor rethrows.
                            if (_initializables.Contains(initializable))
                            {
                                _failedInitializables.Add(initializable);
                            }

                            throw;
                        }

                        if (_initializables.Contains(initializable))
                        {
                            _initializedInitializables.Add(initializable);
                        }
                    }
                }
            }
            finally
            {
                _pendingBatch.Clear();
                _isInitializing = false;
            }
        }

        private static bool IsAlive(IInit initializable)
        {
            return initializable != null &&
                   (!(initializable is Object unityObject) || unityObject.IsValid());
        }

        private static int ComparePendingInit(PendingInit left, PendingInit right)
        {
            int byPriority = right.Priority.CompareTo(left.Priority);
            return byPriority != 0 ? byPriority : left.Order.CompareTo(right.Order);
        }

        private readonly struct PendingInit
        {
            public readonly IInit Target;
            public readonly int Priority;
            public readonly int Order;

            public PendingInit(IInit target, int priority, int order)
            {
                Target = target;
                Priority = priority;
                Order = order;
            }
        }
    }
}
