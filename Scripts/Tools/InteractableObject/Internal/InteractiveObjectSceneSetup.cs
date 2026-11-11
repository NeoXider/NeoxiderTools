using System;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using Object = UnityEngine.Object;

namespace Neo.Tools
{
    internal static class InteractiveObjectSceneSetup
    {
        private static bool _eventSystemChecked;
        private static EventSystem _lastFoundEventSystem;
        private static int _sceneSearchCount;

        private static readonly Type InputSystemUiInputModuleType =
            ResolveType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");

        /// <summary>Scene searches for an EventSystem since the last reset; pins the lookup cost in tests.</summary>
        internal static int SceneSearchCount => _sceneSearchCount;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        internal static void ResetStaticState()
        {
            _eventSystemChecked = false;
            _lastFoundEventSystem = null;
            _sceneSearchCount = 0;
        }

        public static void EnsureEventSystem(MonoBehaviour owner, bool autoCheckEventSystem,
            bool autoCreateEventSystemIfMissing)
        {
            if (!autoCheckEventSystem)
            {
                return;
            }

            EventSystem eventSystem = FindEventSystem();
            if (_eventSystemChecked && eventSystem != null)
            {
                EnsureInputModule(eventSystem.gameObject);
                return;
            }

            if (eventSystem == null)
            {
                if (autoCreateEventSystemIfMissing)
                {
                    eventSystem = CreateEventSystem();
                }
                else if (owner != null)
                {
                    NeoDiagnostics.LogWarning("InteractiveObject: EventSystem not found in scene", owner);
                }
            }

            if (eventSystem != null)
            {
                EnsureInputModule(eventSystem.gameObject);
            }

            _eventSystemChecked = true;
        }

        public static bool TryEnsureRaycasters(MonoBehaviour owner, bool hasCollider3D, bool hasCollider2D)
        {
            if (owner == null)
            {
                return false;
            }

            Camera cam = InteractionCameraResolver.Resolve();
            if (cam == null)
            {
                // WHY: In multiplayer or dynamic scenes, the camera might spawn later.
                // We return true here so the InteractiveObject isn't disabled prematurely.
                return true;
            }

            bool isUi = owner.GetComponentInParent<Canvas>() != null && owner.TryGetComponent<RectTransform>(out _);
            if (isUi)
            {
                return true;
            }

            if (hasCollider2D)
            {
                if (cam.GetComponent<Physics2DRaycaster>() == null)
                {
                    cam.gameObject.AddComponent<Physics2DRaycaster>();
                }
            }

            if (hasCollider3D)
            {
                // WHY: Physics2DRaycaster derives from PhysicsRaycaster; it cannot serve 3D hits.
                bool has3DRaycaster = false;
                foreach (PhysicsRaycaster raycaster in cam.GetComponents<PhysicsRaycaster>())
                {
                    if (raycaster is not Physics2DRaycaster)
                    {
                        has3DRaycaster = true;
                        break;
                    }
                }

                if (!has3DRaycaster)
                {
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
                }
            }

            return true;
        }

        /// <summary>
        ///     The current EventSystem, else the first one a scene search finds. The found instance is remembered and
        ///     returned without a new search while its GameObject stays active; once it is deactivated or destroyed
        ///     (for example by a scene change) the next call searches again. With several active EventSystems the
        ///     remembered one is simply the first found, not necessarily the one a new search would return.
        /// </summary>
        internal static EventSystem FindEventSystem()
        {
            // WHY: during a scene load every InteractiveObject Awake can run before the EventSystem's OnEnable
            // registers it as current, and a scene search per object made loading N objects cost N searches.
            EventSystem current = EventSystem.current;
            if (current != null)
            {
                return current;
            }

            if (_lastFoundEventSystem != null && _lastFoundEventSystem.gameObject.activeInHierarchy)
            {
                return _lastFoundEventSystem;
            }

            _sceneSearchCount++;
            _lastFoundEventSystem = Object.FindFirstObjectByType<EventSystem>();
            return _lastFoundEventSystem;
        }

        private static EventSystem CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            EnsureInputModule(eventSystemObject);
            return eventSystem;
        }

        private static void EnsureInputModule(GameObject eventSystemObject)
        {
            if (eventSystemObject == null)
            {
                return;
            }

            bool legacyInputAvailable = IsLegacyInputAvailable();
            if (!legacyInputAvailable)
            {
                RemoveStandaloneInputModule(eventSystemObject);

                if (InputSystemUiInputModuleType != null)
                {
                    if (eventSystemObject.GetComponent(InputSystemUiInputModuleType) == null)
                    {
                        eventSystemObject.AddComponent(InputSystemUiInputModuleType);
                    }

                    return;
                }

                NeoDiagnostics.LogWarning(
                    "InteractiveObject: Input System is active, but InputSystemUIInputModule type is unavailable. StandaloneInputModule was removed to avoid InvalidOperationException.",
                    eventSystemObject);
                return;
            }

            if (eventSystemObject.GetComponent<BaseInputModule>() != null)
            {
                return;
            }

            eventSystemObject.AddComponent<StandaloneInputModule>();
        }

        // WHY: in an Input System-only configuration the probe always throws, so skipping it there stops spawning
        // many InteractiveObjects from throwing and catching one exception per Awake. The probe itself stays the
        // answer elsewhere: a forced optional backend in a Both project must keep the legacy UI module.
        private static bool IsLegacyInputAvailable()
        {
            if (!InputBackendCompat.IsLegacyInputConfigured)
            {
                return false;
            }

            try
            {
                Vector3 _ = Input.mousePosition;
                return true;
            }
            catch (InvalidOperationException)
            {
                return false;
            }
        }

        private static void RemoveStandaloneInputModule(GameObject eventSystemObject)
        {
            // WHY: this runs in every InteractiveObject Awake; the common case has no legacy module, and only the
            // removal path needs the allocating component array.
            if (!eventSystemObject.TryGetComponent(out StandaloneInputModule _))
            {
                return;
            }

            StandaloneInputModule[] modules = eventSystemObject.GetComponents<StandaloneInputModule>();
            foreach (StandaloneInputModule module in modules)
            {
                if (module == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
                    module.enabled = false;
                    Object.Destroy(module);
                }
                else
                {
                    Object.DestroyImmediate(module);
                }
            }
        }

        private static Type ResolveType(string fullTypeName)
        {
            TryLoadAssembly("Unity.InputSystem");
            TryLoadAssembly("Unity.InputSystem.ForUI");

            Type directType = Type.GetType($"{fullTypeName}, Unity.InputSystem", false);
            if (directType != null)
            {
                return directType;
            }

            Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
            return assemblies.Select(assembly => assembly.GetType(fullTypeName, false))
                .FirstOrDefault(type => type != null);
        }

        private static void TryLoadAssembly(string assemblyName)
        {
            try
            {
                Assembly.Load(assemblyName);
            }
            catch
            {
            }
        }
    }
}
