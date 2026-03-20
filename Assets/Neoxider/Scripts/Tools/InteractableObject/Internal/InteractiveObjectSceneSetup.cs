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
        private static bool _physicsRaycasterEnsured3D;
        private static bool _physicsRaycasterEnsured2D;
        private static bool _eventSystemChecked;

        private static readonly Type InputSystemUiInputModuleType =
            ResolveType("UnityEngine.InputSystem.UI.InputSystemUIInputModule");

        public static void EnsureEventSystem(MonoBehaviour owner, bool autoCheckEventSystem,
            bool autoCreateEventSystemIfMissing)
        {
            if (!autoCheckEventSystem || _eventSystemChecked)
            {
                return;
            }

            EventSystem eventSystem = EventSystem.current ?? Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                if (autoCreateEventSystemIfMissing)
                {
                    eventSystem = CreateEventSystem();
                }
                else if (owner != null)
                {
                    Debug.LogWarning("InteractiveObject: EventSystem not found in scene", owner);
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

            Camera cam = Camera.main ?? Object.FindFirstObjectByType<Camera>();
            if (cam == null)
            {
                Debug.LogError(
                    $"[InteractiveObject] No camera found on {owner.gameObject.name}. Component will be disabled.",
                    owner);
                return false;
            }

            bool isUi = owner.GetComponentInParent<Canvas>() != null && owner.TryGetComponent<RectTransform>(out _);
            if (isUi)
            {
                return true;
            }

            if (hasCollider2D && !_physicsRaycasterEnsured2D)
            {
                if (cam.GetComponent<Physics2DRaycaster>() == null)
                {
                    cam.gameObject.AddComponent<Physics2DRaycaster>();
                }

                _physicsRaycasterEnsured2D = true;
            }

            if (hasCollider3D && !_physicsRaycasterEnsured3D)
            {
                if (cam.GetComponent<PhysicsRaycaster>() == null)
                {
                    cam.gameObject.AddComponent<PhysicsRaycaster>();
                }

                _physicsRaycasterEnsured3D = true;
            }

            return true;
        }

        private static EventSystem CreateEventSystem()
        {
            GameObject eventSystemObject = new("EventSystem", typeof(EventSystem));
            EventSystem eventSystem = eventSystemObject.GetComponent<EventSystem>();
            EnsureInputModule(eventSystemObject);
            Debug.LogWarning(
                "InteractiveObject: EventSystem not found in scene. Created EventSystem automatically.",
                eventSystemObject);
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

                Debug.LogWarning(
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

        private static bool IsLegacyInputAvailable()
        {
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
            StandaloneInputModule[] modules = eventSystemObject.GetComponents<StandaloneInputModule>();
            foreach (StandaloneInputModule module in modules)
            {
                if (module == null)
                {
                    continue;
                }

                if (Application.isPlaying)
                {
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

            var directType = Type.GetType($"{fullTypeName}, Unity.InputSystem", false);
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
